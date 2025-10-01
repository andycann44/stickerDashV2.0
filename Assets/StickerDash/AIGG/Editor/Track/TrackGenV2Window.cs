#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
using System; using System.IO; using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.Track {
  public class TrackGenV2Window : EditorWindow {
    string nl = "build 250 by 6 with random tiles missing 12% and row gaps for jumps 2";
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
      var m = Regex.Match(s, @"\b(?:build|rebuild)\s+(\d+)\s*(?:m|meter|meters)?\s*by\s*(\d+)\s*(?:m|meter|meters)?");
      string can = "seed(12345)\n";
      if(m.Success) can += $"buildAbs({m.Groups[1].Value},{m.Groups[2].Value})\n";
      m = Regex.Match(s, @"random\s+tiles\s+missing\s+(\d+)%");
      if(m.Success) can += $"randomHoles({m.Groups[1].Value})\n";
      m = Regex.Match(s, @"row\s+gaps?\s+for\s+jumps?\s+(\d+)");
      if(m.Success) can += $"insertJumpGaps({m.Groups[1].Value})\n";
      m = Regex.Match(s, @"remove\s+rows?\s+(\d+)\s*-\s*(\d+)");
      if(m.Success) can += $"deleteRows({m.Groups[1].Value},{m.Groups[2].Value})\n";
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

  // Fallback for code that calls the old window:
  public class TrackGeneratorWindow : EditorWindow {
    [MenuItem("Window/Aim2Pro/Track Creator/Track Generator")]
    public static void Open(){ TrackGenV2Window.Open(); }
  }
}
#endif
