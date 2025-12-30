using MySql.Data.MySqlClient;

namespace DataBaseSystem {
  public class DBConnector {
    private string connectionstring;
    public DBConnector(string stringconnection) {
      connectionstring = stringconnection;
    }
    public async Task Connect(string leadername, string teamname, string hash, int codenumber, int score, bool approved) {
      using MySqlConnection conn = new MySqlConnection(connectionstring);
      string query = $"INSERT INTO Assessment (leaderName, teamname, teamhash, codenumber, score, approved) " + "VALUES (@leadername, @teamname, @teamhash, @codenumber, @score, @approved);";
      try {
        await conn.OpenAsync();
        MySqlCommand cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@leadername", leadername);
        cmd.Parameters.AddWithValue("@teamname", teamname);
        cmd.Parameters.AddWithValue("@teamhash", hash);
        cmd.Parameters.AddWithValue("@codenumber", codenumber);
        cmd.Parameters.AddWithValue("@score", score);
        cmd.Parameters.AddWithValue("@approved", approved);
        await cmd.ExecuteNonQueryAsync();
      } catch (Exception ex) {
        Console.WriteLine("An error occurred: " + ex.Message);
      }
      finally {
        conn.Close();
      }
    }
  }
}