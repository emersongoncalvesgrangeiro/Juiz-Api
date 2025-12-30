using System.Diagnostics;

namespace CompilationSystem {
  public class CompilationSystem_ {
    public int warnings = 0, error = 0;
    public string TYPE = "";
    public async Task Checker(string path) {
      string[] archives = Directory.GetFiles(path).Where(v => v.EndsWith(".c") || v.EndsWith(".h") || v.EndsWith(".java")).ToArray();
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
      TYPE = compilationinfo;
      await Compiler(path, compilationinfo);
    }
    public async Task Compiler(string path, string type) {
      var process = new ProcessStartInfo();
      process.FileName = "/bin/bash";
      process.RedirectStandardOutput = true;
      process.RedirectStandardError = true;
      process.UseShellExecute = false;
      process.CreateNoWindow = true;
      try {
        if (type == "java") {
          process.Arguments = $"-c \"cd '{path}'/ && javac -Xlint:all -d . *.java && jar cfe out.jar Main *.class";
        } else if (type == "c") {
          process.Arguments = $"-c \"cd '{path}'/ && gcc -Wall -Wextra -Wpedantic *.c *.h -o out";
        }
        using var processstart = Process.Start(process)!;
        var out_ = processstart.StandardOutput.ReadToEndAsync();
        var err_ = processstart.StandardError.ReadToEndAsync();
        await processstart.WaitForExitAsync();
        string stdout = await out_;
        string stderr = await err_;
        var errorslines = stderr.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var waringslines = stdout.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        warnings = waringslines.Count(a => a.Contains("warning:") || a.Contains("warning"));
        error = errorslines.Count(l => l.Contains("error:") || l.Contains("error"));
      } catch (Exception e) {
        Console.WriteLine($"Error: ${e.Message}");
      }
    }
  }
}