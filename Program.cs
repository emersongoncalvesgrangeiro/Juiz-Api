using HashSystem;
using Archives;
using CodeAnalysis;
using CompilationSystem;
using DockerSystem;
using DataBaseSystem;
using AvaliatorSystem;
using dotenv.net;
using System.Collections.Concurrent;

DotEnv.Load();
string? key = Environment.GetEnvironmentVariable("Gemini");
string? cs = Environment.GetEnvironmentVariable("DBConnection");

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => {
  options.Limits.MinRequestBodyDataRate = null;
  options.Limits.MaxRequestBodySize = null;
});
/*builder.WebHost.ConfigureKestrel(options => {
  options.Limits.MaxRequestBodySize = 150994944;
  options.Limits.MinRequestBodyDataRate = null;
  options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
});*/
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => {
  options.MultipartBodyLengthLimit = 150994944;
  options.ValueLengthLimit = int.MaxValue;
  options.MemoryBufferThreshold = 150994944;
  options.ValueCountLimit = 200;
  options.MultipartHeadersCountLimit = 200;
});
var app = builder.Build();

HashSet<string> OldHashs = new HashSet<string>();
var managerrepo = new ConcurrentDictionary<string, ArchiveManager>();

app.MapPost("/", async (HttpRequest request, string Name, string Team) => {
  var from_ = await request.ReadFormAsync();
  var files = from_.Files.ToList();
  var data = new { Files = files.Select(f => f.FileName).ToList() };
  HashMaker hash = new HashMaker(Name, Team);
  string HashOutput = hash.Output;
  if (OldHashs.Contains(HashOutput)) {
    return Results.Ok(data);
  }
  CodeAnalyzer codeAnalyzer = new CodeAnalyzer(key);
  CompilationSystem_ compilation = new CompilationSystem_();
  DockerManager dockerManager = new DockerManager(key);
  DBConnector dbConnector = new DBConnector(cs);
  Avaliator avaliator = new Avaliator();
  var manager = managerrepo.GetOrAdd(HashOutput, _ => new ArchiveManager());
  await manager.MakeArchives(HashOutput, files);
  string codeforanalyze = "";
  foreach (string code in manager.list) {
    codeforanalyze += code + "\n";
  }
  await codeAnalyzer.AnalyzeCode(codeforanalyze);
  await compilation.Checker(HashOutput + "/" + manager.ID_Code + "/");
  int sum = (codeAnalyzer.errors * 250) + (codeAnalyzer.warnings * 150);
  if (codeAnalyzer.warnings > 6 || codeAnalyzer.errors > 3 || sum >= 1000) {
    manager.DeleteDirectory(HashOutput);
  }
  await dockerManager.DockerFileMaker(HashOutput + "/" + manager.ID_Code + "/", compilation.TYPE);
  await dockerManager.DockerBuildImage(HashOutput + "/" + manager.ID_Code + "/");
  string ID = "";
  if (dockerManager.sucess) {
    ID = await dockerManager.RunContainer();
  }
  //Console.WriteLine("\n\t\tIA:" + "\n\t\tError:" + codeAnalyzer.errors + "\n\t\tWarnings:" + codeAnalyzer.warnings + "\n\t\tCompilarion:" + "\n\t\tError:" + compilation.error + "\n\t\toutput:" + compilation.warnings + "\n\t\tDocker:" + "\n\t\tError:" + dockerManager.errors + "\n\t\tWarnings:" + dockerManager.warnings + "\n");
  var (approved, finalscore) = avaliator.Calculating(codeAnalyzer.errors, codeAnalyzer.warnings, compilation.error, compilation.warnings, dockerManager.errors, dockerManager.warnings);
  await dbConnector.Connect(Name, Team, HashOutput, manager.ID_Code, finalscore, approved);
  if (dockerManager.sucess && !dockerManager.timeout__) {
    await dockerManager.ContainerKill(ID);
  }
  if (manager.ID_Code == 3) {
    manager.DeleteDirectory(HashOutput);
    OldHashs.Add(HashOutput);
    managerrepo.TryRemove(HashOutput, out _);
  }
  return Results.Ok(data);
}).DisableAntiforgery().WithName("/");


app.Run();
