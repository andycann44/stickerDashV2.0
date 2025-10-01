#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
using System; using System.IO; using System.Linq; using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.Track {
  public static class CanonicalRunner {
    static readonly Regex rxBuild     = new Regex(@"^\s*buildAbs\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxDelRows   = new Regex(@"^\s*deleteRows\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxDelTiles1 = new Regex(@"^\s*deleteTiles\s*\(\s*([0-9,\s]+)\s*,\s*row\s*=\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxHoles     = new Regex(@"^\s*randomHoles\s*\(\s*(\d+(?:\.\d+)?)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxJumps     = new Regex(@"^\s*insertJumpGaps\s*\(\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxSeed      = new Regex(@"^\s*seed\s*\(\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);

    [MenuItem("Window/Aim2Pro/Track Creator/Run Last Canonical")]
    public static void RunLast(){
      string path = Path.Combine("StickerDash_Status","LastCanonical.plan");
      if(!File.Exists(path)){ Debug.LogWarning("[A2P] No LastCanonical.plan found. Use Track Gen V2 → Parse → Canonical."); return; }
      var lines = File.ReadAllLines(path).Select(l=>l.Trim()).Where(l=>l.Length>0 && !l.StartsWith("#") && !l.StartsWith("//")).ToArray();
      if(lines.Length==0){ Debug.LogWarning("[A2P] LastCanonical.plan is empty."); return; }

      int builtRows=0, builtCols=0;
      foreach(var line in lines){
        Match m;
        if((m=rxSeed.Match(line)).Success){ TrackSceneBuilder.Seed(int.Parse(m.Groups[1].Value)); continue; }
        if((m=rxBuild.Match(line)).Success){
          int L=int.Parse(m.Groups[1].Value), W=int.Parse(m.Groups[2].Value);
          var res = TrackSceneBuilder.BuildAbs(L,W); builtRows=res.rows; builtCols=res.cols; continue;
        }
        if((m=rxDelRows.Match(line)).Success){
          int s=int.Parse(m.Groups[1].Value), e=int.Parse(m.Groups[2].Value);
          int n=TrackSceneBuilder.DeleteRows(s,e); Debug.Log($"[A2P] deleteRows {s}-{e}: removed {n}"); continue;
        }
        if((m=rxDelTiles1.Match(line)).Success){
          var csv=m.Groups[1].Value; int row=int.Parse(m.Groups[2].Value);
          var cols = csv.Split(new[]{","," "}, StringSplitOptions.RemoveEmptyEntries).Select(x=>int.Parse(x.Trim())).ToArray();
          int n=TrackSceneBuilder.DeleteTilesInRow(row, cols); Debug.Log($"[A2P] deleteTiles row {row}: removed {n}"); continue;
        }
        if((m=rxHoles.Match(line)).Success){
          float pct=float.Parse(m.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
          int n=TrackSceneBuilder.RandomHoles(pct); Debug.Log($"[A2P] randomHoles {pct}%: removed {n}"); continue;
        }
        if((m=rxJumps.Match(line)).Success){
          int cnt=int.Parse(m.Groups[1].Value);
          int n=TrackSceneBuilder.InsertJumpGaps(cnt); Debug.Log($"[A2P] insertJumpGaps {cnt}: removed {n} rows"); continue;
        }
        Debug.LogWarning("[A2P] Unsupported canonical call (ignored): "+line);
      }
      Debug.Log($"[A2P] Done. Approx size: rows={builtRows} cols={builtCols}");
    }
  }
}
#endif
