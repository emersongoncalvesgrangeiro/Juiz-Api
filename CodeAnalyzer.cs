using Google.GenAI;
using System.Text.RegularExpressions;

namespace CodeAnalysis {
  public class CodeAnalyzer {
    public int errors;
    public int warnings;
    private string apikey;
    private int score = 1000;
    private Client client;
    public CodeAnalyzer(string KEY) {
      apikey = KEY;
      client = new Client(apiKey: apikey);
    }
    public async Task AnalyzeCode(string code) {
      for (int i = 0; i < 3; i++) {
        try {
          string instructions =
          $"Analise o código abaixo de forma estritamente objetiva. " +
          $"Se houver erros de compilação, retorne EXATAMENTE no formato: erros: <n>, warnings: <n>. " +
          $"Se não houver erros, retorne: erros: 0, warnings: 0. " +
          $"Não invente problemas, não interprete estilo ou formatação como erro. " +
          $"Código:\n\n{code}";

          var response = await client.Models.GenerateContentAsync(model: "gemini-3-pro-preview", contents: instructions);
          string analiyzed = response.Candidates[0].Content.Parts[0].Text;
          var err = Regex.Match(analiyzed, @"erros:\s\s*(\d\d+)");
          errors = err.Success ? int.Parse(err.Groups[1].Value) : 0;
          var warn = Regex.Match(analiyzed, @"warnings:\s\s*(\d\d+)");
          warnings = warn.Success ? int.Parse(warn.Groups[1].Value) : 0;
        } catch (TaskCanceledException) {
          errors = 0;
          warnings = 0;
        } catch (Exception) {
          errors = 0;
          warnings = 0;
        }
      }
    }
    public async Task<string> Responsedata(string data) {
      string instructions = $"Responda de forma clara e objetiva ao que se pede no augorítimo se atendo apenas ao que é pedido: {data}";
      string finalresponse = "";
      bool err = false;
      for (int i = 0; i < 3; i++) {
        try {
          var response = await client.Models.GenerateContentAsync(model: "gemini-3-pro-preview", contents: instructions);
          finalresponse = response.Candidates[0].Content.Parts[0].Text;
        } catch (TaskCanceledException) {

        } catch (Exception) {
          err = true;
        }
      }
      if (err) {
        return "Erro ao processar a solicitação.";
      }
      return finalresponse;
    }
  }
}