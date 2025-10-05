#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro {
  // Tiny normalizer: fixes dashes, units, spacing.
  public static class NLPre {
    static readonly Regex Ws   = new Regex(@"\s+", RegexOptions.Compiled);
    static readonly Regex Dsh  = new Regex(@"[\u2010\u2011\u2012\u2013\u2014\u2212]", RegexOptions.Compiled); // hyphen/fig/en/em/minus
    public static string Normalize(string raw) {
      if (string.IsNullOrEmpty(raw)) return raw;

      // 1) unify dashes to ASCII hyphen
      string s = Dsh.Replace(raw, "-");

      // 2) common range phrasing
      s = Regex.Replace(s, @"\brows?\s+(\d+)\s*to\s*(\d+)", "rows $1-$2", RegexOptions.IgnoreCase);

      // 3) unit synonyms → "m"
      s = Regex.Replace(s, @"\bmetres?\b", "m", RegexOptions.IgnoreCase);
      s = Regex.Replace(s, @"\bmeters?\b", "m", RegexOptions.IgnoreCase);

      // 4) tidy whitespace
      s = Ws.Replace(s, " ").Trim();

      return s;
    }
  }
}
#endif
