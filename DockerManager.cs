using ICSharpCode.SharpZipLib.Tar;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text;
using CodeAnalysis;

namespace DockerSystem {
  public class DockerManager {
    private DockerClient client;
    private Stream? resp;
    private MultiplexedStream? attachStream;
    private string CID_;
    public string output;
    public StringBuilder sb;
    private CodeAnalyzer codeAnalyzer;
    private DateTime lastoutput;
    private string response = "";
    private bool sendresponse = false;
    public int warnings = 0;
    public int errors = 0;
    public bool timeout__ = false;
    public bool sucess = true;
    public DockerManager(string key) {
      client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
      CID_ = "";
      output = "";
      sb = new StringBuilder();
      codeAnalyzer = new CodeAnalyzer(key);
    }
    private static readonly string[] ErrorKeywords =
    {
        "error",
        "exception",
        "segmentation fault",
        "abort",
        "fatal"
    };

    private static readonly string[] WarningKeywords =
    {
      "warning",
      "deprecated",
      "unchecked"
    };

    public async Task DockerFileMaker(string path, string type) {
      string DockerFilec =
      "FROM ubuntu:latest AS build\n" +
      "WORKDIR /app\n" +
      "COPY out .\n" +
      "RUN chmod +x out\n" +
      "ENTRYPOINT [\"./out\"]";
      string DockerFileJava =
      "FROM openjdk:26-ea-trixie\n" +
      "WORKDIR /app\n" +
      "COPY out.jar .\n" +
      "ENTRYPOINT [\"java\", \"-jar\", \"out.jar\"]";
      try {
        if (type == "java") {
          await File.WriteAllTextAsync(path + "Dockerfile", DockerFileJava);
        } else if (type == "c") {
          await File.AppendAllTextAsync(path + "Dockerfile", DockerFilec);
        }
      } catch (Exception e) {
        await Console.Error.WriteLineAsync(e.Message);
      }
    }
    public async Task DockerBuildImage(string path) {
      try {
        using var ts = await TarMaker(path);
        var CID = Guid.NewGuid().ToString("N");
        string Tags = $"run-{CID}:latest";
        var dockerparams = new ImageBuildParameters {
          Dockerfile = "Dockerfile",
          Tags = new List<string> { Tags },
          Remove = true
        };
        resp?.Dispose();
        resp = await client.Images.BuildImageFromDockerfileAsync(ts, dockerparams);
        using var reader = new StreamReader(resp);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null) {
          sb.AppendLine(line);
          if (line.Contains("\"error\"", StringComparison.OrdinalIgnoreCase)) {
            sucess = false;
          }
        }
        if (!sucess) {
          Console.WriteLine("Erro ao construir a imagem Docker");
          return;
        }
        CID_ = Tags;
      } catch (Exception e) {
        await Console.Error.WriteLineAsync(e.Message);
      }
    }
    public async Task<string> Reader(CancellationToken cancellationToken = default) {
      if (attachStream == null) {
        throw new InvalidOperationException("Container não anexado");
      }
      var buffer = new byte[8192];
      while (!cancellationToken.IsCancellationRequested) {
        var result = await attachStream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
        if (result.EOF || result.Count == 0) {
          break;
        }
        var outputChunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
        sb.Append(outputChunk);
      }
      return sb.ToString();
    }
    public async Task Writer(string message) {
      if (attachStream == null) {
        throw new InvalidOperationException("Container não anexado");
      }
      var bytes = Encoding.UTF8.GetBytes(message + "\n");
      await attachStream.WriteAsync(bytes, 0, bytes.Length, CancellationToken.None);
    }
    private async Task<Stream> TarMaker(string directory) {
      var memorystream = new MemoryStream();
      var tarout = new TarOutputStream(memorystream, Encoding.UTF8);
      tarout.IsStreamOwner = false;
      foreach (var filepath in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)) {
        var relativepath = Path.GetRelativePath(directory, filepath).Replace("\\", "/");
        var bytes = await File.ReadAllBytesAsync(filepath);
        var entry = TarEntry.CreateTarEntry(relativepath);
        entry.Size = bytes.Length;
        tarout.PutNextEntry(entry);
        await tarout.WriteAsync(bytes, 0, bytes.Length);
        tarout.CloseEntry();
      }
      tarout.Close();
      memorystream.Seek(0, SeekOrigin.Begin);
      return memorystream;
    }
    private async Task Readerl(CancellationToken token) {
      if (attachStream == null) {
        throw new InvalidOperationException("Container não anexado");
      }
      try {
        var buffer = new byte[8192];
        var errbuffer = new StringBuilder();
        while (!token.IsCancellationRequested) {
          var result = await attachStream.ReadOutputAsync(buffer, 0, buffer.Length, token);
          if (result.EOF) {
            break;
          }
          if (result.Count == 0) {
            continue;
          }
          lastoutput = DateTime.UtcNow;
          if (result.Target != MultiplexedStream.TargetStream.StandardOut) {
            continue;
          }
          var txt = Encoding.UTF8.GetString(buffer, 0, result.Count);
          sb.Append(txt);
          errbuffer.Append(txt);
          response = await codeAnalyzer.Responsedata(txt);
          if (!string.IsNullOrEmpty(response)) {
            await Writer(response);
          }
          while (true) {
            var text = errbuffer.ToString();
            var index = text.IndexOf('\n');
            if (index < 0) {
              break;
            }
            var line = text[..index];
            errbuffer.Remove(0, index + 1);
            if (ContainsAny(line, ErrorKeywords)) {
              errors++;
            } else if (ContainsAny(line, WarningKeywords)) {
              warnings++;
            }
          }
        }
      } catch (OperationCanceledException) {
      } catch (ObjectDisposedException) {
      }
    }
    private async Task InputWatch(TimeSpan idletime, CancellationToken token) {
      try {
        while (!token.IsCancellationRequested) {
          if (!sendresponse && DateTime.UtcNow - lastoutput > idletime) {
            var response_ = await codeAnalyzer.Responsedata(response);
            if (!string.IsNullOrEmpty(response_)) {
              await Writer(response_);
              sendresponse = true;
            }
          }
          await Task.Delay(50, token);
        }
      } catch (OperationCanceledException) {
      }
    }
    private bool ContainsAny(string line, string[] keywords) {
      foreach (var keyword in keywords) {
        if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase)) {
          return true;
        }
      }
      return false;
    }
    public async Task<string> RunContainer(CancellationToken token = default) {
      var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters {
        Image = CID_,
        AttachStdout = true,
        AttachStderr = true,
        AttachStdin = true,
        OpenStdin = true,
        StdinOnce = false,
        Tty = false
      });
      var containerid = container.ID;
      lastoutput = DateTime.UtcNow;
      await client.Containers.StartContainerAsync(containerid, null);
      attachStream = await client.Containers.AttachContainerAsync(containerid, false, new ContainerAttachParameters {
        Stream = true,
        Stderr = true,
        Stdout = true,
        Stdin = true
      });
      var ct = CancellationTokenSource.CreateLinkedTokenSource(token);
      var Taskreader = Task.Run(() => Readerl(ct.Token));
      var Taskwatcher = Task.Run(() => InputWatch(TimeSpan.FromMilliseconds(500), ct.Token));
      var waittask = client.Containers.WaitContainerAsync(containerid);
      var globaltimeout = Task.Delay(TimeSpan.FromSeconds(30), token);
      var complete = await Task.WhenAny(waittask, globaltimeout);
      if (complete == globaltimeout) {
        await ContainerKill(containerid);
        timeout__ = true;
      }
      ct.Cancel();
      try {
        var info = await client.Containers.InspectContainerAsync(containerid);
        if (info.State.ExitCode != 0) {
          errors++;
        }
      } catch (DockerContainerNotFoundException) {
        timeout__ = true;
        errors++;
      }
      await Task.WhenAll(Taskreader, Taskwatcher);
      return containerid;
    }
    public async Task ContainerKill(string ID) {
      await client.Containers.StopContainerAsync(ID, new ContainerStopParameters { WaitBeforeKillSeconds = 0 });
      await client.Containers.RemoveContainerAsync(ID, new ContainerRemoveParameters { Force = true });
      await client.Images.DeleteImageAsync(CID_, new ImageDeleteParameters { Force = true });
    }
  }
}