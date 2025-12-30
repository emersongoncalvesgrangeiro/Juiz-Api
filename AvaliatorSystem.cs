using System;

namespace AvaliatorSystem {
  public class Avaliator {
    public KeyValuePair<bool, int> Calculating(int errorsIA, int warningsIA, int errorscompilation, int warningscompilation, int errorrunning, int warningsrunning) {
      int score = 1000;
      int errors = 0;
      int warnings = 0;
      int finalsum = 0;
      bool approved = false;
      if (errorscompilation > 0) {
        approved = false;
        return new KeyValuePair<bool, int>(approved, 0);
      }
      if (warningscompilation == warningsIA) {
        int sum = warningscompilation + warningsIA + warningsrunning;
        for (int i = 0; i < sum; i++) {
          warnings += 150;
        }
      } else {
        int sum = warningscompilation + warningsrunning;
        for (int i = 0; i < sum; i++) {
          warnings += 150;
        }
      }
      if (errorrunning == errorsIA) {
        int sum = errorsIA + errorrunning;
        for (int i = 0; i < sum; i++) {
          errors += 250;
        }
      } else {
        for (int i = 0; i < errorrunning; i++) {
          errors += 250;
        }
      }
      finalsum = errors + warnings;

      if (finalsum >= score) {
        approved = false;
        return new KeyValuePair<bool, int>(approved, 0);
      } else {
        approved = true;
        int finalscore = score - finalsum;
        return new KeyValuePair<bool, int>(approved, finalscore);
      }
    }
  }
}