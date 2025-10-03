
#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
namespace Aim2Pro.AIGG {
  public class AIGGMainWindow : EditorWindow {
    [MenuItem("Window/Aim2Pro/Aigg/AIGG")]
    public static void Open(){ var w=GetWindow<AIGGMainWindow>(); w.titleContent=new GUIContent("AIGG"); w.minSize=new Vector2(520,260); }
    void OnGUI(){
      GUILayout.Label("Aim2Pro — AIGG", EditorStyles.boldLabel);
      if(GUILayout.Button("Open Workbench")) Aim2Pro.AIGG.Workbench.WorkbenchWindow.Open();
      if(GUILayout.Button("Open Track Generator")) Aim2Pro.AIGG.Track.TrackGeneratorWindow.Open();
      if(GUILayout.Button("Open Paste & Merge")) Aim2Pro.AIGG.SpecPasteMergeWindow.Open();
      if(GUILayout.Button("Open API Settings")) Aim2Pro.AIGG.Settings.ApiSettingsWindow.Open();
    }
  }
}
#endif
