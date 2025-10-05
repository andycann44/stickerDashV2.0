#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro {
  // Normalize per line: keep newlines, fix dashes/units/spacing.
  public static class NLPre {
    // literal dash chars: ‐-‒–—−
    static readonly Regex Dash      = new Regex(@"[‐-‒–—−]", RegexOptions.Compiled);
    static readonly Regex RangeRows = new Regex(@"\brows?\s+(\d+)\s*(?:to|-)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Units     = new Regex(@"\b(?:metres?|meters?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Space     = new Regex(@"[ \t]+", RegexOptions.Compiled);

    public static string Normalize(string raw) {
      if (string.IsNullOrEmpty(raw)) return raw;

      string text = raw.Replace("\r\n", "\n"); // unify EOL
      var lines = text.Split(n);            // preserve line boundaries
      for (int i = 0; i < lines.Length; i++) {
        string s = lines[i];

        s = Dash.Replace(s, "-");                                         // fancy dashes -> hyphen
        s = RangeRows.Replace(s, m => $"rows {m.Groups[1].Value}-{m.Groups[2].Value}");
        s = Units.Replace(s, "m");                                        // metres/meters -> m
        s = Space.Replace(s, " ").Trim();                                 // tidy spaces per line

        lines[i] = s;
      }
      return string.Join("\n", lines);
    }
  }
}
#endif
