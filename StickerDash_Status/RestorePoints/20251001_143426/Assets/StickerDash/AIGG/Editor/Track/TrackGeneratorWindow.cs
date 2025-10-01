
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.Text.RegularExpressions;
namespace Aim2Pro.AIGG.Track {
  public class TrackGeneratorWindow : EditorWindow {
    string nl=""; string log=""; Vector2 scroll;
    [MenuItem("Window/Aim2Pro/Track Creator/Track Generator")]
    public static void Open(){ var w=GetWindow<TrackGeneratorWindow>(); w.titleContent=new GUIContent("Track Generator"); w.minSize=new Vector2(720,480); }
    void OnGUI(){
      GUILayout.Label("Natural Language → Plan (shell)", EditorStyles.boldLabel);
      nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(80));
      if(GUILayout.Button("Parse")){
        var m = Regex.Match(nl.ToLower(), @"\\b(?:rebuild|build)\\s+(\\d+)\\s*(?:m|meters)?\\s*by\\s*(\\d+)\\s*(?:m|meters)?\\b");
        if(m.Success) Log($"buildAbs(lengthM={m.Groups[1].Value}, widthM={m.Groups[2].Value})");
        else Log("no match; add intents later");
      }
      GUILayout.Space(8);
      GUILayout.Label("Log", EditorStyles.boldLabel);
      scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
      EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
      GUILayout.EndScrollView();
    }
    void Log(string s){ log = System.DateTime.Now.ToString("HH:mm:ss")+" — "+s+"\n"+log; Repaint(); }
  }
}
#endif
