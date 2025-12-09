using System.Diagnostics;

namespace CompilationSystem {
  public class CompilationSystem_ {
    public string output = "", error = "";
    private List<string> list = new List<string>();
    public CompilationSystem_() {

    }
    public void Add_(List<string> a) {
      foreach (string b in a) {
        list.Add(b);
      }
    }
    public async Task Checker(string path) {
      string[] archives = Directory.GetFiles(path).Where(v => v.EndsWith(".c") || v.EndsWith(".h")).ToArray();
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
      int v = 0;
      foreach (string archive in list) {
        v++;
        if (v > 1) {
          archivelist += " ";
        }
        archivelist += archive;
      }
      process.FileName = "/bin/bash";
      process.RedirectStandardOutput = true;
      process.RedirectStandardError = true;
      process.UseShellExecute = false;
      process.CreateNoWindow = true;
      try {
        if (type == "java") {
          process.Arguments = $"-c \"cd '{path}'/ && javac *.java -d out";
        } else if (type == "c") {
          process.Arguments = $"-c \"cd '{path}'/ && gcc '{archivelist}' -o out";
        }
        using var processstart = Process.Start(process);
        output = processstart.StandardOutput.ReadToEnd();
        error = processstart.StandardError.ReadToEnd();
        await processstart.WaitForExitAsync();
      } catch (Exception e) {
        Console.WriteLine($"Error: ${e.Message}");
      }
    }
  }
}