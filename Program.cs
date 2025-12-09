using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using DTO;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/", async (HttpRequest request) => {
  var from = await request.ReadFormAsync();
  var data = new {
    Files = from.Files.Select(f => f.FileName).ToList(),
    Keys = from.Keys.ToList(),
    Values = from.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value.ToString())
  };
  string names = "";
  foreach (string dataelement in data.Files) {
    names += dataelement + " ";
    Console.WriteLine(dataelement);
  }
  return Results.Ok(data);
}).DisableAntiforgery().WithName("/");


app.Run();
