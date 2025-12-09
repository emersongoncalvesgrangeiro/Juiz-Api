using System;
using System.IO;

namespace Archives {
  public class ArchiveManager {
    public int ID_Code;
    public List<string> list = new List<string>();
    public async Task MakeArchives(string HashOutput, List<IFormFile> files) {
      try {
        if (!Directory.Exists(HashOutput)) {
          ID_Code++;
          Directory.CreateDirectory(HashOutput);
          await DirecoryMaker(files, HashOutput);
        } else {

          if (ID_Code <= 2) {
            ID_Code++;
            await DirecoryMaker(files, HashOutput);
          }
        }
      } catch (Exception e) {
        Console.Error.WriteLine(e.Message);
      }
    }
    private async Task DirecoryMaker(List<IFormFile> files, string HashOutput) {
      foreach (var file in files) {
        string content;
        using (var reader = new StreamReader(file.OpenReadStream())) {
          content = await reader.ReadToEndAsync();
        }
        Directory.CreateDirectory(HashOutput + "/" + ID_Code);
        await File.AppendAllTextAsync(HashOutput + "/" + ID_Code + "/" + file.FileName, content);
        list.Add(content);
      }
    }
    public void DeleteDirectory(string directoryname) {
      try {
        if (ID_Code == 3) {
          Directory.Delete(directoryname, true);
        }
      } catch (IOException e) {
        Console.WriteLine(e.Message);
      }
      finally {
        ID_Code = 0;
      }
    }
  }
}