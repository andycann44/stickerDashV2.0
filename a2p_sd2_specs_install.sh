#!/usr/bin/env bash
set -euo pipefail
ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "ERR: run at Unity project root"; exit 1; }

E="$ROOT/Assets/StickerDash/AIGG/Editor/Status"
mkdir -p "$E"
SPECS_DIR="$ROOT/StickerDash_Status/Specs"
NOTES_DIR="$ROOT/StickerDash_Status/Notes"
mkdir -p "$SPECS_DIR" "$NOTES_DIR"

# 1) Editor script: builds/updates Specs Registry JSON automatically
cat > "$E/SD2_SpecRegistry.cs" <<'CS'
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Aim2Pro.Status {
  [InitializeOnLoad]
  public static class SD2_SpecRegistry {
    static readonly string ProjectRoot = Directory.GetCurrentDirectory();
    static readonly string AssetsRoot  = Path.Combine(ProjectRoot, "Assets");
    static readonly string SpecsDir    = Path.Combine(ProjectRoot, "StickerDash_Status/Specs");
    static readonly string NotesDir    = Path.Combine(ProjectRoot, "StickerDash_Status/Notes");
    static readonly string JsonPath    = Path.Combine(SpecsDir, "SD2_Specs.json");
    static readonly string NotesPath   = Path.Combine(SpecsDir, "SD2_SpecsLog.md");

    static readonly string[] EditorFiles = new[]{
      "Assets/StickerDash/AIGG/Editor/Track/TrackGeneratorWindow.cs",
      "Assets/StickerDash/AIGG/Editor/Workbench/WorkbenchWindow.cs",
      "Assets/StickerDash/AIGG/Editor/Aigg/AIGGMainWindow.cs",
      "Assets/StickerDash/AIGG/Editor/Aigg/SpecPasteMergeWindow.cs",
      "Assets/StickerDash/AIGG/Editor/Settings/ApiSettingsWindow.cs",
      "Assets/StickerDash/AIGG/Editor/Terminal/ForceRecompileMenu.cs",
      "Assets/StickerDash/AIGG/Editor/Tools/CreateRestorePoint.cs",
      "Assets/StickerDash/AIGG/Editor/Tools/LoadRestorePoint.cs",
      "Assets/StickerDash/AIGG/Editor/Tools/ClearRestorePoints.cs"
    };

    static readonly string[] SpecFiles = new[]{
      "Assets/StickerDash/AIGG/Resources/Spec/intents.json",
      "Assets/StickerDash/AIGG/Resources/Spec/commands.json",
      "Assets/StickerDash/AIGG/Resources/Spec/macros.json",
      "Assets/StickerDash/AIGG/Resources/Spec/lexicon.json",
      "Assets/StickerDash/AIGG/Resources/Spec/fieldmap.json",
      "Assets/StickerDash/AIGG/Resources/Spec/registry.json",
      "Assets/StickerDash/AIGG/Resources/Spec/schema.json",
      "Assets/StickerDash/AIGG/Resources/Spec/normalizer.json",
      "Assets/StickerDash/AIGG/Resources/Spec/tests.json",
      "Assets/StickerDash/AIGG/Resources/Spec/UnknownQueue.json"
    };

    static SD2_SpecRegistry() {
      EditorApplication.delayCall += EnsureDirsAndRebuildOnce;
    }

    static void EnsureDirsAndRebuildOnce(){
      try{
        Directory.CreateDirectory(SpecsDir);
        Directory.CreateDirectory(NotesDir);
        RebuildRegistry("Domain Reload");
      } catch(Exception e){
        Debug.LogWarning("[A2P][Specs] init failed: "+e.Message);
      }
    }

    [MenuItem("Window/Aim2Pro/Status/Open Specs Register")]
    public static void OpenSpecsRegister(){
      if (!File.Exists(JsonPath)) RebuildRegistry("OpenSpecsRegister (no file)");
      EditorUtility.RevealInFinder(JsonPath);
    }

    [MenuItem("Window/Aim2Pro/Status/Force Rescan Specs")]
    public static void ForceRescan(){
      RebuildRegistry("Manual Rescan");
    }

    [MenuItem("Window/Aim2Pro/Status/Add Spec Note...")]
    public static void AddSpecNote(){
      string note = EditorUtility.DisplayDialogComplex(
        "Add Spec Note",
        "Write a short note about what changed (it appends to SD2_SpecsLog.md).",
        "OK","Cancel","") == 0
        ? EditorUtility.DisplayDialogComplex("","(Type in console)","OK","Cancel","")==-1?null:null // fallback
        : null;

      // Proper text input (fallback): use a simple prompt window
      string input = EditorUtility.DisplayDialog("Add Spec Note","Paste your note then click OK.\n(If you cancel, nothing is written.)","OK","Cancel")
        ? GUIUtility.systemCopyBuffer // allow quick paste from clipboard
        : null;

      if (string.IsNullOrEmpty(input)) return;
      AppendNote(input.Trim());
      RebuildRegistry("Add Note");
    }

    static void AppendNote(string text){
      try{
        Directory.CreateDirectory(SpecsDir);
        var sb = new StringBuilder();
        sb.AppendLine("### " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine(text);
        sb.AppendLine();
        File.AppendAllText(NotesPath, sb.ToString());
        Debug.Log("[A2P][Specs] Note appended.");
      } catch(Exception e){
        Debug.LogWarning("[A2P][Specs] note failed: "+e.Message);
      }
    }

    public static void RebuildRegistry(string reason){
      try{
        var obj = new Dictionary<string, object>();
        obj["updatedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        obj["reason"]    = reason;

        // Tag known feature versions
        var versions = new Dictionary<string, object>();
        versions["trackGenerator"] = "2.0"; // current working tag
        versions["unitsDefault"]   = "meters (length/width), degrees (curves/slopes)";
        obj["versions"] = versions;

        // Capture files
        var comp = new List<object>();
        foreach (var p in EditorFiles.Concat(SpecFiles)) {
          string full = ToFullPath(p);
          bool exists = File.Exists(full);
          var item = new Dictionary<string, object>();
          item["path"] = p;
          item["exists"] = exists;
          if (exists) {
            var fi = new FileInfo(full);
            item["bytes"] = fi.Length;
            item["mtime"] = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            item["sha256"] = Sha256OfFile(full);
          }
          comp.Add(item);
        }
        obj["components"] = comp;

        // Write JSON
        var json = MiniJson.ToJson(obj);
        File.WriteAllText(JsonPath, json);
        AssetDatabase.Refresh();
        Debug.Log("[A2P][Specs] Registry updated ("+reason+").");
      } catch(Exception e){
        Debug.LogWarning("[A2P][Specs] rebuild failed: "+e.Message);
      }
    }

    static string ToFullPath(string rel){
      if (string.IsNullOrEmpty(rel)) return ProjectRoot;
      if (rel.StartsWith("Assets")) return Path.Combine(ProjectRoot, rel.Replace("/", Path.DirectorySeparatorChar.ToString()));
      return Path.IsPathRooted(rel) ? rel : Path.Combine(ProjectRoot, rel);
    }

    static string Sha256OfFile(string path){
      try{
        using (var sha = SHA256.Create())
        using (var fs = File.OpenRead(path)){
          var hash = sha.ComputeHash(fs);
          var sb = new StringBuilder(hash.Length*2);
          for (int i=0;i<hash.Length;i++) sb.Append(hash[i].ToString("x2"));
          return sb.ToString();
        }
      } catch { return ""; }
    }
  }

  // Auto-rescan when tracked assets are imported/changed
  public class SD2_SpecAssetPostprocessor : AssetPostprocessor {
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom){
      // If any of the tracked files changed, rebuild
      var tracked = new HashSet<string>(
        SD2_SpecRegistryPaths().Select(p=>p.Replace("\\","/")),
        StringComparer.OrdinalIgnoreCase);
      bool hit = false;
      foreach (var arr in new[]{ imported, deleted, moved, movedFrom }){
        foreach (var a in arr) { if (tracked.Contains(a)) { hit = true; break; } }
        if (hit) break;
      }
      if (hit) SD2_SpecRegistry.RebuildRegistry("Asset Change");
    }

    static IEnumerable<string> SD2_SpecRegistryPaths(){
      // keep in sync with SD2_SpecRegistry arrays
      string[] a = {
        "Assets/StickerDash/AIGG/Editor/Track/TrackGeneratorWindow.cs",
        "Assets/StickerDash/AIGG/Editor/Workbench/WorkbenchWindow.cs",
        "Assets/StickerDash/AIGG/Editor/Aigg/AIGGMainWindow.cs",
        "Assets/StickerDash/AIGG/Editor/Aigg/SpecPasteMergeWindow.cs",
        "Assets/StickerDash/AIGG/Editor/Settings/ApiSettingsWindow.cs",
        "Assets/StickerDash/AIGG/Editor/Terminal/ForceRecompileMenu.cs",
        "Assets/StickerDash/AIGG/Editor/Tools/CreateRestorePoint.cs",
        "Assets/StickerDash/AIGG/Editor/Tools/LoadRestorePoint.cs",
        "Assets/StickerDash/AIGG/Editor/Tools/ClearRestorePoints.cs",
        "Assets/StickerDash/AIGG/Resources/Spec/intents.json",
        "Assets/StickerDash/AIGG/Resources/Spec/commands.json",
        "Assets/StickerDash/AIGG/Resources/Spec/macros.json",
        "Assets/StickerDash/AIGG/Resources/Spec/lexicon.json",
        "Assets/StickerDash/AIGG/Resources/Spec/fieldmap.json",
        "Assets/StickerDash/AIGG/Resources/Spec/registry.json",
        "Assets/StickerDash/AIGG/Resources/Spec/schema.json",
        "Assets/StickerDash/AIGG/Resources/Spec/normalizer.json",
        "Assets/StickerDash/AIGG/Resources/Spec/tests.json",
        "Assets/StickerDash/AIGG/Resources/Spec/UnknownQueue.json"
      };
      return a;
    }
  }

  // Tiny JSON utility (UnityEditor compatible)
  static class MiniJson {
    public static string ToJson(object obj){
      var sb = new StringBuilder(); WriteValue(sb, obj); return sb.ToString();
    }
    static void WriteValue(StringBuilder sb, object obj){
      if (obj == null){ sb.Append("null"); return; }
      if (obj is string s){ sb.Append('\"').Append(Escape(s)).Append('\"'); return; }
      if (obj is bool b){ sb.Append(b ? "true" : "false"); return; }
      if (obj is IDictionary<string, object> dict){
        sb.Append('{'); bool first=true;
        foreach (var kv in dict){ if(!first) sb.Append(','); first=false;
          sb.Append('\"').Append(Escape(kv.Key)).Append('\"').Append(':'); WriteValue(sb, kv.Value);
        } sb.Append('}'); return;
      }
      if (obj is IEnumerable<object> list){
        sb.Append('['); bool first=true;
        foreach (var v in list){ if(!first) sb.Append(','); first=false; WriteValue(sb, v); } sb.Append(']'); return;
      }
      if (obj is IEnumerable<KeyValuePair<string, object>> kvs){
        sb.Append('{'); bool first=true;
        foreach (var kv in kvs){ if(!first) sb.Append(','); first=false;
          sb.Append('\"').Append(Escape(kv.Key)).Append('\"').Append(':'); WriteValue(sb, kv.Value);
        } sb.Append('}'); return;
      }
      if (obj is int || obj is long || obj is float || obj is double || obj is decimal){
        sb.Append(Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture)); return;
      }
      sb.Append('\"').Append(Escape(obj.ToString())).Append('\"');
    }
    static string Escape(string s){ return s.Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n","\\n").Replace("\r","\\r").Replace("\t","\\t"); }
  }
}
#endif
CS

# 2) Seed empty registry + log (so it’s visible in Finder immediately)
cat > "$SPECS_DIR/SD2_Specs.json" <<'JSON'
{
  "updatedAt": "",
  "reason": "bootstrap",
  "versions": {
    "trackGenerator": "2.0",
    "unitsDefault": "meters (length/width), degrees (curves/slopes)"
  },
  "components": []
}
JSON

cat > "$SPECS_DIR/SD2_SpecsLog.md" <<'MD'
# StickerDash2.0 — Specs Log

- This file records human-readable notes when we change behavior or specifications.
- Use **Window → Aim2Pro → Status → Add Spec Note…** to append entries.

MD

# 3) Working Notes entry
{
  echo ""
  echo "## $(date +%Y-%m-%d' '%H:%M:%S) — Specs Registry Installed"
  echo "- Auto builds StickerDash_Status/Specs/SD2_Specs.json with hashes + timestamps of all key files."
  echo "- Menu: Window → Aim2Pro → Status → {Open Specs Register, Force Rescan Specs, Add Spec Note…}"
} >> "$NOTES_DIR/SD2_WorkingNotes.md"

echo "Installed: Assets/StickerDash/AIGG/Editor/Status/SD2_SpecRegistry.cs"
echo "Specs:     StickerDash_Status/Specs/SD2_Specs.json (auto-updates on changes)"
echo "Specs Log: StickerDash_Status/Specs/SD2_SpecsLog.md"
echo "Open Unity, let it compile. Then: Window → Aim2Pro → Status."
