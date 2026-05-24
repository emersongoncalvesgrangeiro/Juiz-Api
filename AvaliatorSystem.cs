namespace AvaliatorSystem {
  public class Avaliator {
    public KeyValuePair<bool, int> Calculating(int errorsIA, int warningsIA, int errorscompilation, int warningscompilation, int errorrunning, int warningsrunning) {
      int score = 1000;
      int errors = 250;
      int warnings = 150;
      if (errorscompilation > 0) {
        return new KeyValuePair<bool, int>(false, 0);
      }
      int totalwarings = warningscompilation + warningsrunning;
      int totalerros = errorscompilation + errorrunning;
      int penality = (totalwarings * warnings) + (totalerros * errors);
      if (penality >= score) {
        return new KeyValuePair<bool, int>(false, 0);
      }
      int finalscore = score - penality;
      return new KeyValuePair<bool, int>(true, finalscore);
    }
  }
}