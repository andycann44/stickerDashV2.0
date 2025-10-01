
#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
using System.IO;
namespace Aim2Pro.AIGG {
  public class SpecPasteMergeWindow : EditorWindow {
    string pasted=""; Vector2 scroll; string target="intents.json";
    [MenuItem("Window/Aim2Pro/Aigg/Paste & Merge")]
    public static void Open(){ var w=GetWindow<SpecPasteMergeWindow>(); w.titleContent=new GUIContent("Paste & Merge"); w.minSize=new Vector2(720,480); }
    void OnGUI(){
      GUILayout.Label("Paste JSON for Spec/Resources", EditorStyles.boldLabel);
      pasted = EditorGUILayout.TextArea(pasted, GUILayout.MinHeight(200));
      GUILayout.Space(6);
      target = EditorGUILayout.TextField("Target file (Resources/Spec/):", target);
      GUILayout.BeginHorizontal();
      if(GUILayout.Button("Apply (Replace)")){
        var path = Path.Combine("Assets/StickerDash/AIGG/Resources/Spec", target);
        File.WriteAllText(path, pasted);
        AssetDatabase.Refresh();
        Debug.Log("[AIGG] Wrote: "+path);
      }
      if(GUILayout.Button("Open Spec Folder")) EditorUtility.RevealInFinder("Assets/StickerDash/AIGG/Resources/Spec");
      GUILayout.EndHorizontal();
    }
  }
}
#endif
