#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
using System; using System.IO; using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.Track {
  public class TrackGenV2Window : EditorWindow {
    string nl = "build 250 by 6 with random tiles missing 12% and row gaps for jumps 2, a couple of s bend curves, random slopes on the straights";
    string canonical = ""; Vector2 scroll;

    [MenuItem("Window/Aim2Pro/Track Creator/Track Gen V2")]
    public static void Open(){ GetWindow<TrackGenV2Window>("Track Gen V2"); }

    void OnGUI(){
      GUILayout.Label("Natural Language", EditorStyles.boldLabel);
      nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(60));
      if(GUILayout.Button("Parse → Canonical (meters default)")){
        canonical = ParseToCanonical(nl);
        SaveCanonical(canonical);
        ShowNotification(new GUIContent("Saved → StickerDash_Status/LastCanonical.plan"));
      }
      GUILayout.Space(6);
      GUILayout.Label("Canonical Preview", EditorStyles.boldLabel);
      scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(120));
      EditorGUILayout.TextArea(canonical, GUILayout.ExpandHeight(true));
      EditorGUILayout.EndScrollView();
      GUILayout.Space(6);
      if(GUILayout.Button("Rebuild Track (Run Last Canonical)")){
        CanonicalRunner.RunLast();
      }
    }

    string ParseToCanonical(string text){
      var s = text.ToLowerInvariant();
      string can = "seed(12345)\n";

      // build L by W (meters default)
      var m = Regex.Match(s, @"\b(?:build|rebuild)\s+(\d+)\s*(?:m|meter|meters)?\s*by\s*(\d+)\s*(?:m|meter|meters)?");
      if(m.Success) can += $"buildAbs({m.Groups[1].Value},{m.Groups[2].Value})\n";

      // random tiles missing X%
      m = Regex.Match(s, @"random\s+tiles\s+missing\s+(\d+)%");
      if(m.Success) can += $"randomHoles({m.Groups[1].Value})\n";

      // row gaps for jumps N
      m = Regex.Match(s, @"row\s+gaps?\s+for\s+jumps?\s+(\d+)");
      if(m.Success) can += $"insertJumpGaps({m.Groups[1].Value})\n";

      // explicit curve rows a-b left/right deg
      m = Regex.Match(s, @"curve\s+rows?\s+(\d+)\s*(?:-|to)\s*(\d+)\s+(left|right)\s+(\d+)\s*(?:deg|degree|degrees|°)?");
      if(m.Success) can += $"curveRows({m.Groups[1].Value},{m.Groups[2].Value},{m.Groups[3].Value},{m.Groups[4].Value})\n";

      // s-bend auto: phrases like "s bend", "s-bends", "a couple of s bend curves"
      int sCount = 0;
      var mc = Regex.Match(s, @"(\d+)\s*(?:s\s*-?bends?|s\s*bend\s*curves?)");
      if(mc.Success) sCount = int.Parse(mc.Groups[1].Value);
      if(sCount==0){
        if(Regex.IsMatch(s, @"a\s+couple\s+of\s+s\s*-?bend")) sCount = 2;
        else if(Regex.IsMatch(s, @"several\s+s\s*-?bend")) sCount = 3;
        else if(Regex.IsMatch(s, @"s\s*-?bend")) sCount = 1;
      }
      if(sCount>0) can += $"sBendAuto({sCount},8)\n"; // default 8°

      // random slopes on the straights (defaults)
      if(Regex.IsMatch(s, @"random\s+slopes?\s+on\s+the\s+straights?")){
        can += "slopesRandomAuto(2,8,10)\n"; // min 2°, max 8°, segments ~10 rows
      }

      // delete rows a-b
      m = Regex.Match(s, @"remove\s+rows?\s+(\d+)\s*-\s*(\d+)");
      if(m.Success) can += $"deleteRows({m.Groups[1].Value},{m.Groups[2].Value})\n";

      // remove tiles "1,3,5 row 10"
      m = Regex.Match(s, @"remove\s+tiles?\s+([\d,\s]+)\s+row\s+(\d+)");
      if(m.Success) can += $"deleteTiles({m.Groups[1].Value}, row={m.Groups[2].Value})\n";

      return can.Trim();
    }

    void SaveCanonical(string content){
      var dir = "StickerDash_Status";
      if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
      File.WriteAllText(Path.Combine(dir,"LastCanonical.plan"), content);
    }
  }

  // Back-compat for old menu
  public class TrackGeneratorWindow : EditorWindow {
    [MenuItem("Window/Aim2Pro/Track Creator/Track Generator")]
    public static void Open(){ TrackGenV2Window.Open(); }
  }
}
#endif
