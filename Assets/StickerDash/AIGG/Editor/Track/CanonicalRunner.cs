#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
using System; using System.IO; using System.Text.RegularExpressions; using System.Linq;
namespace Aim2Pro.AIGG.Track {
  public static class CanonicalRunner {
    static readonly Regex rxBuild = new Regex(@"^\\s*buildAbs\\s*\\(\\s*(\\d+)\\s*,\\s*(\\d+)\\s*\\)\\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxDelRows = new Regex(@"^\\s*deleteRows\\s*\\(\\s*(\\d+)\\s*,\\s*(\\d+)\\s*\\)\\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxDelTiles1 = new Regex(@"^\\s*deleteTiles\\s*\\(\\s*([0-9,\\s]+)\\s*,\\s*row\\s*=\\s*(\\d+)\\s*\\)\\s*$", RegexOptions.IgnoreCase);
    [MenuItem("Window/Aim2Pro/Track Creator/Run Last Canonical")]
    public static void RunLast(){
      string path = Path.Combine("StickerDash_Status","LastCanonical.plan");
      if (!File.Exists(path)){ Debug.LogWarning("[A2P] No LastCanonical.plan found. In Track Generator, click Parse → Canonical first."); return; }
      var lines = File.ReadAllLines(path).Select(l=>l.Trim()).Where(l=>l.Length>0 && !l.StartsWith("#") && !l.StartsWith("//")).ToArray();
      if (lines.Length==0){ Debug.LogWarning("[A2P] LastCanonical.plan is empty."); return; }
      int builtRows=0, builtCols=0;
      foreach (var line in lines){
        Match m;
        if ((m=rxBuild.Match(line)).Success){
          var L=int.Parse(m.Groups[1].Value); var W=int.Parse(m.Groups[2].Value);
          var res = TrackSceneBuilder.BuildAbs(L,W); builtRows=res.rows; builtCols=res.cols; continue;
        }
        if ((m=rxDelRows.Match(line)).Success){
          var s=int.Parse(m.Groups[1].Value); var e=int.Parse(m.Groups[2].Value);
          var n = TrackSceneBuilder.DeleteRows(s,e); Debug.Log($"[A2P] deleteRows: removed {n} tiles"); continue;
        }
        if ((m=rxDelTiles1.Match(line)).Success){
          var csv=m.Groups[1].Value; var row=int.Parse(m.Groups[2].Value);
          var cols = csv.Split(new[]{\',\',\' \'}, StringSplitOptions.RemoveEmptyEntries).Select(x=>int.Parse(x.Trim())).ToArray();
          var n=TrackSceneBuilder.DeleteTilesInRow(row, cols); Debug.Log($"[A2P] deleteTiles row {row}: removed {n} tiles"); continue;
        }
        Debug.LogWarning("[A2P] Unsupported canonical call (ignored for now): "+line);
      }
      Debug.Log($"[A2P] Done. Current track approx size: rows={builtRows} cols={builtCols}");
    }
  }
}
#endif
