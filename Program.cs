using HashSystem;
using Archives;
using CodeAnalysis;
using CompilationSystem;
using dotenv.net;

DotEnv.Load();
string key = Environment.GetEnvironmentVariable("Gemini");

ArchiveManager manager = new ArchiveManager();
CodeAnalyzer codeAnalyzer;
CompilationSystem_ compilation;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/", async (HttpRequest request, string Name, string Team) => {
  compilation = new CompilationSystem_();
  codeAnalyzer = new CodeAnalyzer(key);
  var from = await request.ReadFormAsync();
  var data = new { Files = from.Files.Select(f => f.FileName).ToList() };
  var files = from.Files.ToList();
  HashMaker hash = new HashMaker(Name, Team);
  string HashOutput = hash.Output;
  await manager.MakeArchives(HashOutput, files);
  string codeforanalyze = "";
  foreach (string code in manager.list) {
    codeforanalyze += code + "\n";
  }
  await codeAnalyzer.AnalyzeCode(codeforanalyze);
  int sum = (codeAnalyzer.errors * 250) + (codeAnalyzer.warnings * 150);
  if (codeAnalyzer.warnings > 6 || codeAnalyzer.errors > 3 || sum >= 1000) {
    manager.DeleteDirectory(HashOutput);
  } else {
    List<string> list = new List<string>();
    foreach (string d in data.Files) {
      list.Add(d);
    }
    await compilation.Checker(HashOutput + "/" + manager.ID_Code + "/");
    Console.WriteLine("Erros: " + compilation.error);
    Console.WriteLine("Output: " + compilation.output);
  }
  return Results.Ok(data);
}).DisableAntiforgery().WithName("/");


app.Run();
