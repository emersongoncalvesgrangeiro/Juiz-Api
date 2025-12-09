using System.IO;
using HashSystem;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/", async (HttpRequest request, string Name, string Team) => {
  var from = await request.ReadFormAsync();
  var data = new { Files = from.Files.Select(f => f.FileName).ToList() };
  var files = from.Files.ToList();
  HashMaker hash = new HashMaker(Name, Team);
  string HashOutput = hash.Output;
  try {
    if (!Directory.Exists(HashOutput)) {
      Directory.CreateDirectory(HashOutput);
      foreach (var file in files) {
        string content;
        using (var reader = new StreamReader(file.OpenReadStream())) {
          content = await reader.ReadToEndAsync();
        }
        await File.AppendAllTextAsync(HashOutput + "/" + file.FileName, content);
      }
    } else {

    }
  } catch (Exception e) {
    Console.Error.WriteLine(e.Message);
  }
  return Results.Ok(data);
}).DisableAntiforgery().WithName("/");


app.Run();
