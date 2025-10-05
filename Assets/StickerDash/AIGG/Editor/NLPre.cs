#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro {
  // Normalizes per line: keeps newlines, fixes dashes, units, spacing.
  public static class NLPre {
    static readonly Regex Dash = new Regex(@"[\u2010\u2011\u2012\u2013\u2014\u2212]", RegexOptions.Compiled); // hyphen/non-breaking/en/em/minus
    static readonly Regex Space= new Regex(@"[ \t]+", RegexOptions.Compiled);
    public static string Normalize(string raw) {
      if (string.IsNullOrEmpty(raw)) return raw;

      string text = raw.Replace("\r\n","\n");           // unify EOL
      var parts = text.Split(n);                     // keep line boundaries
      for (int i=0;i<parts.Length;i++){
        var s = parts[i];

        // dashes → ASCII hyphen
        s = Dash.Replace(s, "-");

        // ranges: "rows 1 to 2" → "rows 1-2"
        s = Regex.Replace(s, @"\brows?\s+(\d+)\s*to\s*(\d+)", "rows $1-$2", RegexOptions.IgnoreCase);

        // unit synonyms → m
        s = Regex.Replace(s, @"\bmetres?\b|\bmeters?\b", "m", RegexOptions.IgnoreCase);

        // tidy spaces (but NOT newlines)
        s = Space.Replace(s, " ").Trim();

        parts[i] = s;
      }
      return string.Join("\n", parts);
    }
  }
}
#endif
