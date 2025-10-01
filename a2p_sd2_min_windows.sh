#!/usr/bin/env bash
set -euo pipefail
ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "ERR: run at Unity project root"; exit 1; }

E="$ROOT/Assets/StickerDash/AIGG/Editor"
SPEC="$ROOT/Assets/StickerDash/AIGG/Resources/Spec"
mkdir -p "$E/Workbench" "$E/Aigg" "$E/Settings" "$E/Terminal" "$E/Track" "$E/Tools" "$SPEC"

write(){ local p="$1"; shift; printf "%s" "$*" > "$p"; echo "Wrote: ${p#$ROOT/}"; }

# -------- Workbench (shell) --------
write "$E/Workbench/WorkbenchWindow.cs" '
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
'

# -------- AIGG Main (shell launcher) --------
write "$E/Aigg/AIGGMainWindow.cs" '
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
'

# -------- Paste & Merge (stub that just shows a big text box) --------
write "$E/Aigg/SpecPasteMergeWindow.cs" '
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
'

# -------- API Settings (stores to EditorPrefs safely) --------
write "$E/Settings/ApiSettingsWindow.cs" '
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
'

# -------- Track Generator (NL shell) --------
write "$E/Track/TrackGeneratorWindow.cs" '
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
'

# -------- Force Recompile --------
write "$E/Terminal/ForceRecompileMenu.cs" '
#if UNITY_EDITOR
using UnityEditor; using UnityEditor.Compilation; using UnityEngine;
namespace Aim2Pro.AIGG {
  public static class ForceRecompileMenu {
    [MenuItem("Window/Aim2Pro/Terminal/Force Recompile")]
    public static void ForceRecompile(){ AssetDatabase.SaveAssets(); CompilationPipeline.RequestScriptCompilation(); Debug.Log("[A2P] Requested script recompilation."); }
  }
}
#endif
'

# -------- Restore Point: Create --------
write "$E/Tools/CreateRestorePoint.cs" '
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;
namespace Aim2Pro.AIGG.Tools {
  public static class CreateRestorePoint {
    [MenuItem("Window/Aim2Pro/Tools/Create Restore Point")]
    public static void Create(){
      string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string dest = Path.Combine("StickerDash_Status","RestorePoints", ts);
      Directory.CreateDirectory(dest);
      CopyDir("Assets", Path.Combine(dest,"Assets"));
      CopyDir("ProjectSettings", Path.Combine(dest,"ProjectSettings"));
      if(Directory.Exists("Packages")) CopyDir("Packages", Path.Combine(dest,"Packages"));
      AssetDatabase.Refresh();
      Debug.Log("[A2P] Restore Point created at: "+dest);
    }
    static void CopyDir(string src, string dst){
      foreach(var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        Directory.CreateDirectory(dir.Replace(src, dst));
      foreach(var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        File.Copy(file, file.Replace(src, dst), true);
    }
  }
}
#endif
'

# -------- Restore Point: Load (safety prompt + soft restore) --------
write "$E/Tools/LoadRestorePoint.cs" '
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;
namespace Aim2Pro.AIGG.Tools {
  public class LoadRestorePoint : EditorWindow {
    string chosen=""; Vector2 scroll;
    [MenuItem("Window/Aim2Pro/Tools/Load Restore Point")]
    public static void Open(){ var w=GetWindow<LoadRestorePoint>(); w.titleContent=new GUIContent("Load Restore Point"); w.minSize=new Vector2(640,360); }
    void OnGUI(){
      GUILayout.Label("Choose a Restore Point to load", EditorStyles.boldLabel);
      if(GUILayout.Button("Pick Folder…")){ var p = EditorUtility.OpenFolderPanel("Pick Restore Point","StickerDash_Status/RestorePoints",""); if(!string.IsNullOrEmpty(p)) chosen=p; }
      EditorGUILayout.TextField("Selected", chosen);
      GUILayout.Space(6);
      if(GUILayout.Button("Load (copy files back)")){
        if(string.IsNullOrEmpty(chosen) || !Directory.Exists(chosen)){ Debug.LogWarning("[A2P] No folder selected."); return; }
        if(!EditorUtility.DisplayDialog("Load Restore Point","This will overwrite Assets/ & ProjectSettings/. Continue?","Load","Cancel")) return;
        CopyDir(Path.Combine(chosen,"Assets"), "Assets");
        CopyDir(Path.Combine(chosen,"ProjectSettings"), "ProjectSettings");
        if(Directory.Exists(Path.Combine(chosen,"Packages"))) CopyDir(Path.Combine(chosen,"Packages"), "Packages");
        AssetDatabase.Refresh();
        Debug.Log("[A2P] Restore Point loaded from: "+chosen);
      }
    }
    static void CopyDir(string src, string dst){
      if(!Directory.Exists(src)) return;
      foreach(var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        Directory.CreateDirectory(dir.Replace(src, dst));
      foreach(var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        File.Copy(file, file.Replace(src, dst), true);
    }
  }
}
#endif
'

# -------- Restore Point: Clear --------
write "$E/Tools/ClearRestorePoints.cs" '
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;
namespace Aim2Pro.AIGG.Tools {
  public static class ClearRestorePoints {
    [MenuItem("Window/Aim2Pro/Tools/Clear All Restore Points…")]
    public static void Clear(){
      string root = Path.Combine("StickerDash_Status","RestorePoints");
      if(!Directory.Exists(root)){ Debug.Log("[A2P] No restore points."); return; }
      if(!EditorUtility.DisplayDialog("Clear Restore Points","Delete ALL restore points under "+root+" ?","Delete","Cancel")) return;
      Directory.Delete(root, true); AssetDatabase.Refresh();
      Debug.Log("[A2P] Cleared restore points.");
    }
  }
}
#endif
'

# -------- Minimal intents.json (meters-by-default) --------
if [ ! -f "$SPEC/intents.json" ]; then
  write "$SPEC/intents.json" '{
  "intents":[
    {"name":"build by meters","regex":"\\\\b(?:rebuild|build)\\\\s+(\\\\d+)\\\\s*(?:m|meters)?\\\\s*by\\\\s*(\\\\d+)\\\\s*(?:m|meters)?\\\\b","ops":[{"op":"custom","value":"buildAbs($1:int,$2:int)"}]},
    {"name":"curve rows","regex":"\\\\bcurve\\\\s+rows?\\\\s+(\\\\d+)\\\\s*[-to]+\\\\s*(\\\\d+)\\\\s+(left|right)\\\\s+(\\\\d+)\\\\s*(?:deg|degrees|°)?\\\\b","ops":[{"op":"custom","value":"curveRows($1:int,$2:int,$3,$4:int)"}]}
  ]}'
fi

# Remove any earlier broken stubs to avoid dup menus
rm -f "$E/Workbench/SD2_WorkbenchWindow.cs" "$E/Terminal/A2P_OpenTerminal.cs" "$E/Track/TrackLabWindow.cs" 2>/dev/null || true

# Note
NOTES="$ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Installed minimal, compile-safe Editor windows (menus under Window → Aim2Pro)."
  echo "- meters-by-default intents.json ensured."
} >> "$NOTES"

echo "Done. Open Unity; if menus don’t appear immediately, do Assets → Reimport All or close/reopen."
