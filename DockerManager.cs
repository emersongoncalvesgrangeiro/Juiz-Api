using ICSharpCode.SharpZipLib.Tar;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace DockerSystem {
  public class DockerManager {
    private DockerClient client;
    private Stream resp;
    private string CID_;
    public DockerManager() {
      client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
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
      string Tags = "";
      using var ts = await TarMaker(path);
      var CID = Guid.NewGuid().ToString("N");
      Tags = $"run-{CID}:latest";
      var dockerparams = new ImageBuildParameters {
        Dockerfile = "Dockerfile",
        Tags = new List<string> { Tags },
        Remove = true
      };
      resp?.Dispose();
      resp = await client.Images.BuildImageFromDockerfileAsync(ts, dockerparams);
      using var reader = new StreamReader(resp);
      string? line;
      bool success = true;
      while ((line = await reader.ReadLineAsync()) != null) {
        Console.WriteLine(line);
        if (line.Contains("error", StringComparison.OrdinalIgnoreCase)) {
          success = false;
        }
      }
      if (success) {
        CID_ = Tags;
      } else {
        throw new Exception("Error");
      }
    }
    /* public async Task Reader() {
       using var reader = new StreamReader(resp);
       while (!reader.EndOfStream) {
         Console.WriteLine(await reader.ReadLineAsync());
       }
     }*/
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
    public async Task<string> RunContainer() {
      var container = await client.Containers.CreateContainerAsync(new CreateContainerParameters {
        Image = CID_,
        Tty = true,
        AttachStderr = true,
        AttachStdin = true,
        AttachStdout = true,
        StdinOnce = false
      });
      var containerid = container.ID;
      bool containerrunning = await client.Containers.StartContainerAsync(containerid, null);
      var stream = await client.Containers.AttachContainerAsync(containerid, false, new ContainerAttachParameters { Stream = true, Stdin = true, Stdout = true, Stderr = true });
      if (!containerrunning) {
        throw new Exception("Erro ao iniciar container");
      }
      await client.Containers.WaitContainerAsync(containerid);
      await ContainerKill(containerid);
      return containerid;
    }
    public async Task ContainerKill(string ID) {
      await client.Containers.StopContainerAsync(ID, new ContainerStopParameters { WaitBeforeKillSeconds = 0 });
      await client.Containers.RemoveContainerAsync(ID, new ContainerRemoveParameters { Force = true });
      await client.Images.DeleteImageAsync(CID_, new ImageDeleteParameters { Force = true });
    }
    public async Task<string> LogReader() {
      return "";
    }
  }
}