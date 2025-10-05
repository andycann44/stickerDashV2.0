// Editor-only tiny normalizer that keeps newlines and fixes dashes/units/spacing.
#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro {
  public static class NLPre {
    static readonly Regex RangeRows = new Regex(@"\brows?\s+(\d+)\s*(?:to|-)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Units     = new Regex(@"\b(?:metres?|meters?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Space     = new Regex(@"[ \t]+", RegexOptions.Compiled);

    static string FixDashes(string s) {
      if (string.IsNullOrEmpty(s)) return s;
      // Replace fancy dashes with ASCII hyphen: ‐ - ‒ – — −
      var specials = new char[]{u2010,u2011,u2012,u2013,u2014,u2212};
      foreach (var ch in specials) s = s.Replace(ch, -);
      return s;
    }

    public static string Normalize(string raw) {
      if (string.IsNullOrEmpty(raw)) return raw;

      // Unify EOL, but preserve line boundaries
      string text = raw.Replace("\r\n", "\n").Replace("\r", "\n");
      var lines = text.Split(n);
      for (int i = 0; i < lines.Length; i++) {
        string s = lines[i];
        s = FixDashes(s);                                                      // em/en/minus → "-"
        s = RangeRows.Replace(s, m => $"rows {m.Groups[1].Value}-{m.Groups[2].Value}");
        s = Units.Replace(s, "m");                                             // metres/meters → m
        s = Space.Replace(s, " ").Trim();                                      // tidy spaces (keep newlines)
        lines[i] = s;
      }
      return string.Join("\n", lines);
    }
  }
}
#endif
