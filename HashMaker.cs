using System;
using System.Text;
using System.Security.Cryptography;

namespace HashSystem {
  public class HashMaker {
    public string Output;
    public HashMaker(string Name, string Team) {
      string tth = string.Concat(Team, Name);
      Output = HashGenerator(tth);
    }
    private string HashGenerator(string texttohash) {
      string output;
      using (SHA512 sha512 = SHA512.Create()) {
        var data = sha512.ComputeHash(Encoding.UTF8.GetBytes(texttohash));
        var builder = new StringBuilder();
        for (int i = 0; i < data.Length; i++) {
          builder.Append(data[i].ToString("x2"));
        }
        output = builder.ToString();
      }
      return output;
    }
  }
}