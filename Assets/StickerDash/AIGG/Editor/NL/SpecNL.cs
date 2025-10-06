#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.NL {
  // Minimal implementation used by TrackLab to delete tile(s) via NL.
  public static class SpecNL {
    static readonly Regex One = new Regex(@"^delete\s+tile\s+(\d+)\s+in\s+row\s+(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    static readonly Regex Many= new Regex(@"^delete\s+tiles?\s+([\d,\s]+)\s+in\s+row\s+(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryApplyDeleteTiles(string line) {
      if (string.IsNullOrWhiteSpace(line)) return false;

      var m1 = One.Match(line);
      if (m1.Success) {
        int col = int.Parse(m1.Groups[1].Value);
        int row = int.Parse(m1.Groups[2].Value);
        Aim2Pro.TrackCreator.TrackOps.DeleteTilesInRow(row, new int[]{ col });
        return true;
      }

      var m2 = Many.Match(line);
      if (m2.Success) {
        int row = int.Parse(m2.Groups[2].Value);
        var parts = m2.Groups[1].Value.Split(new char[]{',',' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
        var cols = new List<int>();
        foreach (var p in parts) { if (int.TryParse(p, out var n) && n > 0) cols.Add(n); }
        if (cols.Count > 0) {
          Aim2Pro.TrackCreator.TrackOps.DeleteTilesInRow(row, cols.ToArray());
          return true;
        }
      }

      return false;
    }
  }
}
#endif
