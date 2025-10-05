#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
using System; using System.IO; using System.Linq; using System.Text.RegularExpressions;
using System.Globalization;

namespace Aim2Pro.AIGG.Track {
  public static class CanonicalRunner {
    static readonly Regex rxSeed       = new Regex(@"^\s*seed\s*\(\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxBuild      = new Regex(@"^\s*buildAbs\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxDelRows    = new Regex(@"^\s*deleteRows\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxDelTiles1  = new Regex(@"^\s*deleteTiles\s*\(\s*([0-9,\s]+)\s*,\s*row\s*=\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxHoles      = new Regex(@"^\s*randomHoles\s*\(\s*(\d+(?:\.\d+)?)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxJumps      = new Regex(@"^\s*insertJumpGaps\s*\(\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxCurve      = new Regex(@"^\s*curveRows\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(left|right)\s*,\s*(\d+(?:\.\d+)?)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxSBend      = new Regex(@"^\s*sBend\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+(?:\.\d+)?)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxSBendAuto  = new Regex(@"^\s*sBendAuto\s*\(\s*(\d+)\s*,\s*(\d+(?:\.\d+)?)\s*\)\s*$", RegexOptions.IgnoreCase);
    static readonly Regex rxSlopesAuto = new Regex(@"^\s*slopesRandomAuto\s*\(\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*,\s*(\d+)\s*\)\s*$", RegexOptions.IgnoreCase);

    [MenuItem("Window/Aim2Pro/Track Creator/Run Last Canonical")]
    public static void RunLast(){
      string path = Path.Combine("StickerDash_Status","LastCanonical.plan");
      if(!File.Exists(path)){ Debug.LogWarning("[A2P] No LastCanonical.plan found. Use Track Gen V2 → Parse → Canonical."); return; }
      var lines = File.ReadAllLines(path).Select(l=>l.Trim()).Where(l=>l.Length>0 && !l.StartsWith("#") && !l.StartsWith("//")).ToArray();
      if(lines.Length==0){ Debug.LogWarning("[A2P] LastCanonical.plan is empty."); return; }

      foreach(var line in lines){
        Match m;
        if((m=rxSeed.Match(line)).Success){ TrackSceneBuilder.Seed(int.Parse(m.Groups[1].Value)); continue; }
        if((m=rxBuild.Match(line)).Success){ TrackSceneBuilder.BuildAbs(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)); continue; }
        if((m=rxDelRows.Match(line)).Success){ TrackSceneBuilder.DeleteRows(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)); continue; }
        if((m=rxDelTiles1.Match(line)).Success){
          var csv=m.Groups[1].Value; int row=int.Parse(m.Groups[2].Value);
          var cols = csv.Split(new[]{","," "}, StringSplitOptions.RemoveEmptyEntries).Select(x=>int.Parse(x.Trim())).ToArray();
          TrackSceneBuilder.DeleteTilesInRow(row, cols); continue;
        }
        if((m=rxHoles.Match(line)).Success){ float pct=float.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture); TrackSceneBuilder.RandomHoles(pct); continue; }
        if((m=rxJumps.Match(line)).Success){ TrackSceneBuilder.InsertJumpGaps(int.Parse(m.Groups[1].Value)); continue; }
        if((m=rxCurve.Match(line)).Success){ TrackSceneBuilder.CurveRows(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), m.Groups[3].Value, float.Parse(m.Groups[4].Value,CultureInfo.InvariantCulture)); continue; }
        if((m=rxSBend.Match(line)).Success){ TrackSceneBuilder.SBend(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), float.Parse(m.Groups[3].Value,CultureInfo.InvariantCulture)); continue; }
        if((m=rxSBendAuto.Match(line)).Success){ TrackSceneBuilder.SBendAuto(int.Parse(m.Groups[1].Value), float.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture)); continue; }
        if((m=rxSlopesAuto.Match(line)).Success){ TrackSceneBuilder.SlopesRandomAuto(float.Parse(m.Groups[1].Value,CultureInfo.InvariantCulture), float.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture), int.Parse(m.Groups[3].Value)); continue; }
        Debug.LogWarning("[A2P] Unsupported canonical: "+line);
      }
      Debug.Log("[A2P] Canonical complete.");
    }
  }
}
#endif
