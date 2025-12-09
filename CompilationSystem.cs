using System.Diagnostics;

namespace CompilationSystem {
  public class CompilationSystem_ {
    public string output = "", error = "";
    public CompilationSystem_() {

    }
    public async Task Checker(string path) {
      string[] archives = Directory.GetFiles(path);
      string compilationinfo = "";
      switch (Path.GetExtension(archives[0])) {
        case ".java":
          compilationinfo = "java";
          break;
        case ".c":
          compilationinfo = "c";
          break;
        case ".h":
          compilationinfo = "c";
          break;
        default:
          break;
      }
      await Compiler(archives, path, compilationinfo);
    }
    public async Task Compiler(string[] archives, string path, string type) {
      var process = new ProcessStartInfo();
      string archivelist = "";
      for (int i = 0; i < archives.Length; i++) {
        archivelist = archivelist + " " + archives[i];
      }
      process.FileName = "/bin/bash";
      process.RedirectStandardOutput = true;
      process.RedirectStandardError = true;
      process.UseShellExecute = false;
      process.CreateNoWindow = true;
      try {
        if (type == "java") {
          process.Arguments = $"-c \"cd '{path}'/ && javac *.java ";
        } else if (type == "c") {
          string arch__ = "";
          foreach (string archives_ in archives) {
            arch__ += archives_ + " ";
          }
          process.Arguments = $"-c \"mv main.c '{path}' && cd /'{path}'/ && gcc '{arch__}' -o out\"";
        }
        using var processstart = Process.Start(process);
        await processstart.WaitForExitAsync();
        output = processstart.StandardOutput.ReadToEnd();
        error = processstart.StandardError.ReadToEnd();
      } catch (Exception e) {
        Console.WriteLine($"Error: ${e.Message}");
      }
    }
  }
}