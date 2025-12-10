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
        Console.WriteLine(b);
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
      await Compiler(path, compilationinfo);
    }
    public async Task Compiler(string path, string type) {
      var process = new ProcessStartInfo();
      string archivelist = "";
      process.FileName = "/bin/bash";
      process.RedirectStandardOutput = true;
      process.RedirectStandardError = true;
      process.UseShellExecute = false;
      process.CreateNoWindow = true;
      try {
        if (type == "java") {
          process.Arguments = $"-c \"cd '{path}'/ && javac *.java -d out";
        } else if (type == "c") {
          archivelist = string.Join(" ", list.Select(x => $"'{x}'"));
          process.Arguments = $"-c \"cd '{path}'/ && gcc *.c *.h -o out";
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