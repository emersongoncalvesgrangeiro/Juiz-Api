using HashSystem;
using Archives;
using CodeAnalysis;
using CompilationSystem;
using DockerSystem;
using DataBaseSystem;
using AvaliatorSystem;
using dotenv.net;

DotEnv.Load();
string? key = Environment.GetEnvironmentVariable("Gemini");
string? cs = Environment.GetEnvironmentVariable("DBConnection");

ArchiveManager manager = new ArchiveManager();
CodeAnalyzer codeAnalyzer;
CompilationSystem_ compilation;
DockerManager dockerManager;
DBConnector dbConnector = new DBConnector(cs);
Avaliator avaliator = new Avaliator();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/", async (HttpRequest request, string Name, string Team) => {
  compilation = new CompilationSystem_();
  codeAnalyzer = new CodeAnalyzer(key);
  dockerManager = new DockerManager(key);
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
    await compilation.Checker(HashOutput + "/" + manager.ID_Code + "/");
    //    Console.WriteLine("Erros: " + compilation.error);
    //    Console.WriteLine("Output: " + compilation.warnings);
  }
  await dockerManager.DockerFileMaker(HashOutput + "/" + manager.ID_Code + "/", compilation.TYPE);
  await dockerManager.DockerBuildImage(HashOutput + "/" + manager.ID_Code + "/");
  string ID = await dockerManager.RunContainer();
  Console.WriteLine("\n\t\tIA:" + "\n\t\tError:" + codeAnalyzer.errors + "\n\t\tWarnings:" + codeAnalyzer.warnings + "\n\t\tCompilarion:" + "\n\t\tError:" + compilation.error + "\n\t\toutput:" + compilation.warnings + "\n\t\tDocker:" + "\n\t\tError:" + dockerManager.output);
  // avaliator.Calculating(codeAnalyzer.errors, codeAnalyzer.warnings, compilation.error, compilation., dockerManager.errorrunning, dockerManager.warningsrunning);
  //dbConnector.Connect(Name, Team, HashOutput, dockerManager.codenumber, sum, compilation.approved);
  //await dockerManager.ContainerKill(ID);
  return Results.Ok(data);
}).DisableAntiforgery().WithName("/");


app.Run();
