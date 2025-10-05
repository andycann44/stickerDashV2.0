#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro {
  // Normalize per line: keep newlines, fix dashes/units/spacing.
  public static class NLPre {
    static readonly Regex AnyDash   = new Regex(@"[\u2010\u2011\u2012\u2013\u2014\u2015\u2212]", RegexOptions.Compiled);
    static readonly Regex RangeRows = new Regex(@"\brows?\s+(\d+)\s*(?:to|-)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Units     = new Regex(@"\b(?:metres?|meters?)\b",           RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Space     = new Regex(@"[ \t]+",                             RegexOptions.Compiled);

    public static string Normalize(string raw) {
      if (string.IsNullOrEmpty(raw)) return raw;

      // Unify EOL but preserve line boundaries
      string text = raw.Replace("\r\n","\n").Replace("\r","\n");
      var lines = Regex.Split(text, "\n"); // string-based split avoids char-escape issues
      for (int i=0; i<lines.Length; i++) {
        string s = lines[i];
        s = AnyDash.Replace(s, "-");                                                   // em/en/minus → "-"
        s = RangeRows.Replace(s, m => $"rows {m.Groups[1].Value}-{m.Groups[2].Value}");
        s = Units.Replace(s, "m");                                                     // metres/meters → m
        s = Space.Replace(s, " ").Trim();                                              // tidy spaces per line
        lines[i] = s;
      }
      return string.Join("\n", lines);
    }
  }
}
#endif
