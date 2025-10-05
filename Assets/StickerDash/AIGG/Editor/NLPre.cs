#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro {
  // Normalize per line: keep newlines, fix dashes/units/spacing.
  public static class NLPre {
    // Verbatim regex strings avoid escape issues like \b, \s.
    static readonly Regex RangeRows = new Regex(@"\brows?\s+(\d+)\s*(?:to|-)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Units     = new Regex(@"\b(?:metres?|meters?)\b",           RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Space     = new Regex(@"[ \t]+",                             RegexOptions.Compiled);

    static string FixDashes(string s) {
      if (string.IsNullOrEmpty(s)) return s;
      // Replace unicode dashes/bars/minus with ASCII hyphen-minus.
      s = s.Replace(u2010,-); // hyphen
      s = s.Replace(u2011,-); // non-breaking hyphen
      s = s.Replace(u2012,-); // figure dash
      s = s.Replace(u2013,-); // en dash
      s = s.Replace(u2014,-); // em dash
      s = s.Replace(u2015,-); // horizontal bar
      s = s.Replace(u2212,-); // minus sign
      return s;
    }

    public static string Normalize(string raw) {
      if (string.IsNullOrEmpty(raw)) return raw;
      // Unify EOL but preserve line boundaries
      string text = raw.Replace("\r\n","\n").Replace("\r","\n");
      var lines = text.Split(n);
      for (int i=0;i<lines.Length;i++){
        string s = lines[i];
        s = FixDashes(s); // em/en/minus → "-"
        s = RangeRows.Replace(s, m => $"rows {m.Groups[1].Value}-{m.Groups[2].Value}");
        s = Units.Replace(s, "m"); // metres/meters → m
        s = Space.Replace(s, " ").Trim(); // tidy spaces per line
        lines[i] = s;
      }
      return string.Join("\n", lines);
    }
  }
}
#endif
