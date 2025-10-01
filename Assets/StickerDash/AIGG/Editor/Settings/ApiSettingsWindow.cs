
#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
namespace Aim2Pro.AIGG.Settings {
  public class ApiSettingsWindow : EditorWindow {
    const string K="A2P_API_KEY";
    string apiKey;
    [MenuItem("Window/Aim2Pro/Settings/API Settings")]
    public static void Open(){ var w=GetWindow<ApiSettingsWindow>(); w.titleContent=new GUIContent("API Settings"); w.minSize=new Vector2(520,200); }
    void OnEnable(){ apiKey = EditorPrefs.GetString(K, ""); }
    void OnGUI(){
      GUILayout.Label("OpenAI API Key (stored in EditorPrefs)", EditorStyles.boldLabel);
      apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
      if(GUILayout.Button("Save")){ EditorPrefs.SetString(K, apiKey??""); Debug.Log("[A2P] API key saved."); }
    }
  }
}
#endif
