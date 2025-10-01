#!/usr/bin/env bash
set -euo pipefail
ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "ERR: run at Unity project root"; exit 1; }

TOOLS="$ROOT/Assets/StickerDash/AIGG/Editor/Tools"
mkdir -p "$TOOLS"

# -- CreateRestorePoint.cs (hardened) --
cat > "$TOOLS/CreateRestorePoint.cs" <<'CS'
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;

namespace Aim2Pro.AIGG.Tools {
  public static class CreateRestorePoint {
    [MenuItem("Window/Aim2Pro/Tools/Create Restore Point")]
    public static void Create(){
      string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string destRoot = Path.Combine("StickerDash_Status","RestorePoints", ts);
      try {
        Directory.CreateDirectory(destRoot);
        CopyDir("Assets", Path.Combine(destRoot,"Assets"));
        CopyDir("ProjectSettings", Path.Combine(destRoot,"ProjectSettings"));
        if (Directory.Exists("Packages")) CopyDir("Packages", Path.Combine(destRoot,"Packages"));
        AssetDatabase.Refresh();
        Debug.Log("[A2P] Restore Point created at: " + Path.GetFullPath(destRoot));
      } catch (System.Exception e) {
        Debug.LogError("[A2P] Restore Point failed: " + e.Message);
      }
    }

    static void CopyDir(string src, string dst){
      if (!Directory.Exists(src)) return;
      Directory.CreateDirectory(dst); // ensure root exists
      foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories)) {
        var d = dir.Replace(src, dst);
        Directory.CreateDirectory(d);
      }
      foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories)) {
        var target = file.Replace(src, dst);
        var parent = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        File.Copy(file, target, true);
      }
      // Note: GetFiles(... AllDirectories) already includes files at the src root.
    }
  }
}
#endif
CS

# -- LoadRestorePoint.cs (hardened) --
cat > "$TOOLS/LoadRestorePoint.cs" <<'CS'
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;

namespace Aim2Pro.AIGG.Tools {
  public class LoadRestorePoint : EditorWindow {
    string chosen=""; Vector2 scroll;
    [MenuItem("Window/Aim2Pro/Tools/Load Restore Point")]
    public static void Open(){ var w=GetWindow<LoadRestorePoint>(); w.titleContent=new GUIContent("Load Restore Point"); w.minSize=new Vector2(640,360); }

    void OnGUI(){
      GUILayout.Label("Choose a Restore Point to load", EditorStyles.boldLabel);
      if (GUILayout.Button("Pick Folder…")) {
        var p = EditorUtility.OpenFolderPanel("Pick Restore Point","StickerDash_Status/RestorePoints","");
        if (!string.IsNullOrEmpty(p)) chosen = p;
      }
      EditorGUILayout.TextField("Selected", chosen);

      GUILayout.Space(6);
      if (GUILayout.Button("Load (copy files back)")){
        if (string.IsNullOrEmpty(chosen) || !Directory.Exists(chosen)) { Debug.LogWarning("[A2P] No folder selected."); return; }
        if (!EditorUtility.DisplayDialog("Load Restore Point","This will overwrite Assets/ & ProjectSettings/. Continue?","Load","Cancel")) return;

        CopyDir(Path.Combine(chosen,"Assets"), "Assets");
        CopyDir(Path.Combine(chosen,"ProjectSettings"), "ProjectSettings");
        if (Directory.Exists(Path.Combine(chosen,"Packages")))
          CopyDir(Path.Combine(chosen,"Packages"), "Packages");

        AssetDatabase.Refresh();
        Debug.Log("[A2P] Restore Point loaded from: " + chosen);
      }
    }

    static void CopyDir(string src, string dst){
      if (!Directory.Exists(src)) return;
      Directory.CreateDirectory(dst); // ensure root exists
      foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories)) {
        var d = dir.Replace(src, dst);
        Directory.CreateDirectory(d);
      }
      foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories)) {
        var target = file.Replace(src, dst);
        var parent = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        File.Copy(file, target, true);
      }
    }
  }
}
#endif
CS

# Note in Working Notes
NOTES="$ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## $(date +%Y-%m-%d' '%H:%M:%S) — Fix Restore Point copy"
  echo "- Ensure destination directories are created before copying (Create/Load)."
  echo "- Fixes DirectoryNotFoundException for ProjectSettings files."
} >> "$NOTES"

echo "Files updated:"
echo " - Assets/StickerDash/AIGG/Editor/Tools/CreateRestorePoint.cs"
echo " - Assets/StickerDash/AIGG/Editor/Tools/LoadRestorePoint.cs"
