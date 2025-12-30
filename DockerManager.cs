using ICSharpCode.SharpZipLib.Tar;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text;
using CodeAnalysis;

namespace DockerSystem {
  public class DockerManager {
    public delegate Task<string> outputdelegate(string output);
    private DockerClient client;
    private Stream? resp;
    private MultiplexedStream? attachStream;
    private string CID_;
    public string output;
    public StringBuilder sb;
    public DockerManager() {
      client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
      CID_ = "";
      output = "";
      sb = new StringBuilder();
    }
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
      bool sucess = true;
      while ((line = await reader.ReadLineAsync()) != null) {
        sb.AppendLine(line);
        if (line.Contains("\"error\"", StringComparison.OrdinalIgnoreCase)) {
          sucess = false;
        }
      }
      if (!sucess) {
        throw new Exception("Erro ao construir a imagem Docker");
      }
      CID_ = Tags;
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
    private async Task Readerl(outputdelegate output, CancellationToken token) {
      if (attachStream == null) {
        throw new InvalidOperationException("Container não anexado");
      }
      var buffer = new byte[8192];
      while (!token.IsCancellationRequested) {
        var result = await attachStream.ReadOutputAsync(buffer, 0, buffer.Length, token);
        if (result.EOF) {
          break;
        }
        if (result.Count == 0) {
          continue;
        }
        if (result.Target != MultiplexedStream.TargetStream.StandardOut) {
          continue;
        }
        var txt = Encoding.UTF8.GetString(buffer, 0, result.Count);
        sb.Append(txt);
        var response = await output(txt);
        if (!string.IsNullOrEmpty(response)) {
          await Writer(response);
        }
      }
    }
    public async Task<string> RunContainer(outputdelegate response, CancellationToken token = default) {
      var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters {
        Image = CID_,
        Tty = false,
        AttachStderr = true,
        AttachStdin = true,
        AttachStdout = true,
        OpenStdin = true,
      });
      var containerid = container.ID;
      bool containerrunning = await client.Containers.StartContainerAsync(containerid, null);
      attachStream = await client.Containers.AttachContainerAsync(containerid, false, new ContainerAttachParameters {
        Stream = true,
        Stderr = true,
        Stdout = true,
        Stdin = true
      });
      if (!containerrunning) {
        throw new Exception("Erro ao iniciar container");
      }
      await client.Containers.WaitContainerAsync(containerid);
      using var ct = CancellationTokenSource.CreateLinkedTokenSource(token);
      var Taskreader = Readerl(response, ct.Token);
      await client.Containers.WaitContainerAsync(containerid);
      await ct.CancelAsync();
      await Taskreader;
      return containerid;
    }
    public async Task ContainerKill(string ID) {
      await client.Containers.StopContainerAsync(ID, new ContainerStopParameters { WaitBeforeKillSeconds = 0 });
      await client.Containers.RemoveContainerAsync(ID, new ContainerRemoveParameters { Force = true });
      await client.Images.DeleteImageAsync(CID_, new ImageDeleteParameters { Force = true });
    }
  }
}