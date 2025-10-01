
#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
namespace Aim2Pro.AIGG.Workbench {
  public class WorkbenchWindow : EditorWindow {
    string nl=""; Vector2 scroll; string log="";
    [MenuItem("Window/Aim2Pro/Workbench/Workbench")]
    public static void Open(){ var w=GetWindow<WorkbenchWindow>(); w.titleContent=new GUIContent("Workbench"); w.minSize=new Vector2(720,480); }
    void OnGUI(){
      GUILayout.Label("Natural Language", EditorStyles.boldLabel);
      nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(80));
      GUILayout.BeginHorizontal();
      if(GUILayout.Button("Normalize")) Log("normalize (stub): "+nl);
      if(GUILayout.Button("Parse"))     Log("parse (stub): "+nl);
      if(GUILayout.Button("Run"))       Log("run (stub): "+nl);
      GUILayout.EndHorizontal();
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
