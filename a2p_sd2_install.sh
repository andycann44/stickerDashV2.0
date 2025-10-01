#!/usr/bin/env bash
set -euo pipefail

proj_root="$(pwd)"
ts="$(date +%Y%m%d_%H%M%S)"
backup_dir="$proj_root/StickerDash_Status/Backups/SD2_Install_$ts"
log_dir="$proj_root/StickerDash_Status/Log"
notes_dir="$proj_root/StickerDash_Status/Notes"

need() { [ -e "$1" ] || { echo "ERR: expected Unity project root; missing $1"; exit 1; }; }
need "$proj_root/Assets"; need "$proj_root/ProjectSettings"

mkdir -p "$backup_dir" "$log_dir" "$notes_dir"

spec_dir="$proj_root/Assets/StickerDash/AIGG/Resources/Spec"
editor_dir="$proj_root/Assets/StickerDash/AIGG/Editor"
track_dir="$editor_dir/Track"
workbench_dir="$editor_dir/Workbench"
term_dir="$editor_dir/Terminal"
runtime_dir="$proj_root/Assets/StickerDash/AIGG/Runtime"

mkdir -p "$spec_dir" "$track_dir" "$workbench_dir" "$term_dir" "$runtime_dir"

# helper: write file with backup if replacing
write() { # write <path> <<<"content"
  local path="$1"
  local content; content="$(cat)"
  if [ -f "$path" ]; then cp -p "$path" "$backup_dir/$(basename "$path").bak"; fi
  printf "%s" "$content" > "$path"
  echo "Wrote: ${path#$proj_root/}"
}

# --- Working Notes entry ---
cat <<EOF | write "$notes_dir/SD2_WorkingNotes.md"
# StickerDash2.0 — Working Notes
- Installer run at $ts
- Defaults: meters unless stated; curves/slopes assume degrees if omitted.
- Menus: Window → Aim2Pro → Workbench → SD2 Workbench; Window → Aim2Pro → Terminal → Open Project Root.
- Data packs installed: commands, macros, lexicon, fieldmap, registry, schema, normalizer, tests, UnknownQueue.
EOF

# --- Data: commands.json (NL → kernel/macro) ---
cat <<'EOF' | write "$spec_dir/commands.json"
{
  "commands": [
    { "name":"rebuild",
      "pattern":"\\b(rebuild|build)\\s+(\\d+)\\s*(m|meters)?\\s*by\\s*(\\d+)\\s*(m|meters|tiles)?\\b",
      "map": { "op":"Rebuild", "args": { "lengthMeters":"$2:int", "widthValue":"$4:int", "widthUnit":"$5:lower" } },
      "defaults": { "widthUnit":"m" } },

    { "name":"s_bend",
      "pattern":"\\b(s\\s*-?\\s*bend|s bend|s-bend)\\b.*?\\brows\\s+(\\d+)\\s*[-to]+\\s*(\\d+)\\b.*?\\b(left-right|right-left|lr|rl)\\b.*?(\\d+)\\s*(deg|degrees|°)?",
      "map": { "macro":"s_bend", "args": { "start":"$2:int", "end":"$3:int", "pattern":"$4", "deg":"$5:int" } },
      "defaults": { "pattern":"left-right", "deg":20 } },

    { "name":"random_holes",
      "pattern":"\\brandom\\s+(tiles\\s+missing|holes?)\\b(?:.*?(\\d+)\\s*%)?",
      "map": { "macro":"random_holes", "args": { "densityPercent":"$2:int" } },
      "defaults": { "densityPercent":10 } },

    { "name":"jump_gaps",
      "pattern":"\\b(row\\s+gaps?\\s+for\\s+jumps?|jump\\s+gaps?)\\b(?:.*?(\\d+)\\b)?",
      "map": { "macro":"insert_jump_gaps", "args": { "count":"$2:int" } },
      "defaults": { "count":2 } },

    { "name":"random_straight_slopes",
      "pattern":"\\brandom\\s+slopes?\\s+on\\s+the\\s+straights\\b",
      "map": { "macro":"random_straight_slopes", "args": {} },
      "defaults": {} },

    { "name":"smooth",
      "pattern":"\\b(smooth\\s+curves?|reflow\\s+rows?|build\\s+spline|resample\\s+(\\d+)\\s*(per\\s*m|rows\\/m))\\b",
      "map": { "macro":"spline_smooth", "args": { "densityRowsPerMeter":"$2:int" } },
      "defaults": { "densityRowsPerMeter":2 } }
  ]
}
EOF

# --- Data: macros.json ---
cat <<'EOF' | write "$spec_dir/macros.json"
{
  "macros": {
    "s_bend": { "expandsTo": [ {"op":"CurveRows"}, {"op":"CurveRows"} ],
      "args": ["start","end","pattern","deg"] },

    "random_holes": { "expandsTo": [ {"op":"DeleteTilesInRow"} ],
      "args": ["densityPercent"] },

    "insert_jump_gaps": { "expandsTo": [ {"op":"DeleteRow"} ],
      "args": ["count","minSpacingRows","avoidCurves"] },

    "random_straight_slopes": { "expandsTo": [ {"op":"SlopeRows"} ],
      "args": [] },

    "spline_smooth": { "expandsTo": [ {"op":"BuildSplineFromTrack"}, {"op":"Resample"} ],
      "args": ["densityRowsPerMeter"] }
  }
}
EOF

# --- Data: lexicon.json (synonyms + default units) ---
cat <<'EOF' | write "$spec_dir/lexicon.json"
{
  "synonyms": {
    "tiles missing": "holes",
    "hole": "holes",
    "s bend": "s-bend",
    "s–bend": "s-bend",
    "s—bend": "s-bend",
    "°": "deg",
    "degrees": "deg",
    "to": "-",
    "–": "-",
    "—": "-",
    "−": "-",
    "l": "left",
    "r": "right"
  },
  "unitsDefault": {
    "length": "m",
    "width": "m",
    "curve": "deg",
    "slope": "deg"
  }
}
EOF

# --- Data: fieldmap.json (meters unless 'tiles') ---
cat <<'EOF' | write "$spec_dir/fieldmap.json"
{
  "rules": [
    { "when":"widthUnit=m",    "set": { "$.track.widthMeters": "$widthValue:int" } },
    { "when":"widthUnit=",     "set": { "$.track.widthMeters": "$widthValue:int" } },
    { "when":"widthUnit=tiles","set": { "$.track.widthTiles": "$widthValue:int" } },
    { "when":"lengthMeters",   "set": { "$.track.lengthMeters": "$lengthMeters:int" } }
  ]
}
EOF

# --- Data: registry.json ---
cat <<'EOF' | write "$spec_dir/registry.json"
{
  "spline": { "method":"catmull_rom", "tension":0.5, "densityRowsPerMeter":2.0 },
  "limits": { "maxCurveDegPer10Rows": 45, "slopeDegMin": 0, "slopeDegMax": 10 },
  "defaults": { "excludeFirstRows": 10, "excludeLastRows": 10, "holesDensityPercent": 10 }
}
EOF

# --- Data: schema.json (minimal) ---
cat <<'EOF' | write "$spec_dir/schema.json"
{
  "track": {
    "lengthMeters": { "type":"number", "min": 1 },
    "widthMeters":  { "type":"number", "min": 1 },
    "tileWidth":    { "type":"number", "min": 0.01 },
    "spline":       { "type":"object", "optional": true }
  }
}
EOF

# --- Data: normalizer.json ---
cat <<'EOF' | write "$spec_dir/normalizer.json"
{
  "steps": [
    { "type":"lower" },
    { "type":"collapseSpaces" },
    { "type":"replace", "from":["\\u2012","\\u2013","\\u2014","\\u2212"], "to":"-" },
    { "type":"replace", "from":["°","º"], "to":" deg" },
    { "type":"synonyms", "source":"lexicon.synonyms" },
    { "type":"defaultUnits", "source":"lexicon.unitsDefault" }
  ]
}
EOF

# --- Data: tests + unknown queue ---
cat <<'EOF' | write "$spec_dir/tests.json"
{
  "cases": [
    { "in":"rebuild 250 by 6", "expect": { "lengthMeters":250, "widthMeters":6 } },
    { "in":"curve rows 50–60 left 30°", "expect": { "cmd":"CurveRows", "deg":30 } },
    { "in":"smooth curves", "expect": { "macro":"spline_smooth" } }
  ]
}
EOF
echo "[]" | write "$spec_dir/UnknownQueue.json"

# --- Data: intents.json (your strict format) ---
cat <<'EOF' | write "$spec_dir/intents.json"
{ "intents": [
  {"name":"set-tracks","regex":"\\b(\\d+)\\s+tracks?\\b","ops":[{"op":"set","path":"$.difficulty.tracks","value":"$1:int"}]},
  {"name":"seed","regex":"\\bseed\\s+(\\d+)\\b","ops":[{"op":"set","path":"$.rand.seed","value":"$1:int"}]}
]}
EOF

# --- Editor: Workbench shell (tidy menu) ---
cat <<'EOF' | write "$workbench_dir/SD2_WorkbenchWindow.cs"
// Menu: Window/Aim2Pro/Workbench/SD2 Workbench
using UnityEditor; using UnityEngine;
namespace Aim2Pro.Workbench {
  public class SD2_WorkbenchWindow : EditorWindow {
    string nl=""; Vector2 logScroll; string lastLog="";
    [MenuItem("Window/Aim2Pro/Workbench/SD2 Workbench")]
    public static void Open(){ var w=GetWindow<SD2_WorkbenchWindow>(); w.titleContent=new GUIContent("SD2 Workbench"); w.minSize=new Vector2(640,420); w.Show(); }
    void OnGUI(){
      GUILayout.Label("NL Console", EditorStyles.boldLabel);
      nl=EditorGUILayout.TextArea(nl, GUILayout.MinHeight(60));
      GUILayout.BeginHorizontal();
      if(GUILayout.Button("Normalize")) Log("Normalize (stub) → "+nl);
      if(GUILayout.Button("Parse"))     Log("Parse (stub) → "+nl);
      if(GUILayout.Button("Run"))       Log("Run (stub) → "+nl);
      GUILayout.EndHorizontal();
      GUILayout.Space(8);
      GUILayout.Label("Actions", EditorStyles.boldLabel);
      if(GUILayout.Button("Rebuild Track")) Log("Rebuild Track (hook your builder).");
      if(GUILayout.Button("Apply Merge"))   Log("Apply Merge (dry-run → apply → verify).");
      GUILayout.BeginHorizontal();
      if(GUILayout.Button("Smooth Curves")) Log("Smooth Curves via spline.");
      if(GUILayout.Button("Resample 2 rows/m")) Log("Resample density=2.");
      GUILayout.EndHorizontal();
      GUILayout.Space(8);
      GUILayout.Label("Log", EditorStyles.boldLabel);
      logScroll=GUILayout.BeginScrollView(logScroll, GUILayout.ExpandHeight(true));
      EditorGUILayout.HelpBox(lastLog, MessageType.None);
      GUILayout.EndScrollView();
    }
    void Log(string msg){ lastLog=System.DateTime.Now.ToString("HH:mm:ss")+" — "+msg+"\n"+lastLog; Repaint(); }
  }
}
EOF

# --- Editor: Open Terminal at root (macOS) ---
cat <<'EOF' | write "$term_dir/A2P_OpenTerminal.cs"
// Menu: Window/Aim2Pro/Terminal/Open Project Root
using UnityEditor; using System.Diagnostics;
namespace Aim2Pro.Terminal {
  public static class A2P_OpenTerminal {
    [MenuItem("Window/Aim2Pro/Terminal/Open Project Root")]
    public static void OpenRoot() {
      string proj = System.IO.Directory.GetCurrentDirectory();
      string osa = "tell application \\"Terminal\\" to do script \\"cd " + proj.Replace("\\\\","\\\\\\\\").Replace("\"","\\\\\\\"") + " && pwd\\"";
      var psi = new ProcessStartInfo("osascript", "-e \"" + osa + "\"");
      psi.UseShellExecute = false; psi.CreateNoWindow = true; Process.Start(psi);
      UnityEngine.Debug.Log("[A2P] Opened Terminal at: " + proj);
    }
  }
}
EOF

chmod +x "$proj_root/a2p_sd2_install.sh" || true
echo "------------------------------------------------------------"
echo "StickerDash2.0 install complete."
echo "Backups (if any) -> ${backup_dir#$proj_root/}"
echo "Open Unity and let it compile. Menus:"
echo "  - Window → Aim2Pro → Workbench → SD2 Workbench"
echo "  - Window → Aim2Pro → Terminal → Open Project Root"
echo "Data files at: Assets/StickerDash/AIGG/Resources/Spec"
echo "Working Notes at: StickerDash_Status/Notes/SD2_WorkingNotes.md"
