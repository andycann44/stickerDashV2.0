#!/usr/bin/env bash
set -euo pipefail

NEW_ROOT="$(pwd)"
read -rp "Path to your OLD Unity project (where your originals live): " OLD_ROOT
if [ ! -d "$OLD_ROOT" ]; then echo "ERR: not a folder: $OLD_ROOT"; exit 1; fi

need(){ [ -e "$1" ] || { echo "ERR: missing $1 (run at NEW Unity project root)"; exit 1; }; }
need "$NEW_ROOT/Assets"; need "$NEW_ROOT/ProjectSettings"

say(){ printf "• %s\n" "$*"; }

# Dest folders (tidy)
E_ROOT="$NEW_ROOT/Assets/StickerDash/AIGG/Editor"
mkdir -p "$E_ROOT/Workbench" "$E_ROOT/Aigg" "$E_ROOT/Settings" "$E_ROOT/Terminal" "$E_ROOT/Track"
RES_SPEC="$NEW_ROOT/Assets/StickerDash/AIGG/Resources/Spec"
mkdir -p "$RES_SPEC"

# Helper to find one file by name in OLD project
ff(){ find "$OLD_ROOT" -type f -name "$1" -print -quit; }

copy_if_found(){
  local name="$1" dst="$2"
  local src; src="$(ff "$name" || true)"
  if [ -z "${src:-}" ]; then
    say "WARN: not found in OLD project → $name (skipped)"
  else
    mkdir -p "$(dirname "$dst")"
    cp -p "$src" "$dst"
    say "Copied $name → ${dst#$NEW_ROOT/}"
  fi
}

say "Copying your originals from: $OLD_ROOT"
copy_if_found "WorkbenchWindow.cs"      "$E_ROOT/Workbench/WorkbenchWindow.cs"
copy_if_found "SpecPasteMergeWindow.cs" "$E_ROOT/Aigg/SpecPasteMergeWindow.cs"
copy_if_found "ApiSettingsWindow.cs"    "$E_ROOT/Settings/ApiSettingsWindow.cs"
copy_if_found "AIGGMainWindow.cs"       "$E_ROOT/Aigg/AIGGMainWindow.cs"
copy_if_found "ForceRecompileMenu.cs"   "$E_ROOT/Terminal/ForceRecompileMenu.cs"

# Restore Point tools (your originals)
copy_if_found "CreateRestorePoint.cs"   "$E_ROOT/Tools/CreateRestorePoint.cs"
copy_if_found "LoadRestorePoint.cs"     "$E_ROOT/Tools/LoadRestorePoint.cs"
copy_if_found "ClearRestorePoints.cs"   "$E_ROOT/Tools/ClearRestorePoints.cs"

# Track Generator (your NL window)
copy_if_found "TrackGeneratorWindow.cs" "$E_ROOT/Track/TrackGeneratorWindow.cs"

# Remove any prior stubs to avoid duplicate menus
rm -f "$E_ROOT/Workbench/SD2_WorkbenchWindow.cs" 2>/dev/null || true
rm -f "$E_ROOT/Terminal/A2P_OpenTerminal.cs"     2>/dev/null || true
rm -f "$E_ROOT/Track/TrackLabWindow.cs"          2>/dev/null || true

# Minimal intents.json if missing (TrackGenerator expects Resources/Spec/intents.json)
INTENTS="$RES_SPEC/intents.json"
if [ ! -f "$INTENTS" ]; then
  say "No intents.json found → writing a minimal one (meters by default)."
  cat > "$INTENTS" <<'JSON'
{
  "intents": [
    {
      "name": "build by meters",
      "regex": "\\b(?:rebuild|build)\\s+(\\d+)\\s*(?:m|meters)?\\s*by\\s*(\\d+)\\s*(?:m|meters)?\\b",
      "ops": [
        { "op":"custom", "value":"buildAbs($1:int,$2:int)" }
      ]
    },
    {
      "name": "curve rows",
      "regex": "\\bcurve\\s+rows?\\s+(\\d+)\\s*[-to]+\\s*(\\d+)\\s+(left|right)\\s+(\\d+)\\s*(?:deg|degrees|°)?\\b",
      "ops": [ { "op":"custom", "value":"curveRows($1:int,$2:int,$3,$4:int)"} ]
    },
    {
      "name": "remove rows",
      "regex": "\\bremove\\s+rows?\\s+(\\d+)\\s*[-to]+\\s*(\\d+)\\b",
      "ops": [ { "op":"custom", "value":"deleteRows($1:int,$2:int)"} ]
    },
    {
      "name": "remove tiles in row",
      "regex": "\\bremove\\s+tiles?\\s+([\\d,\\s]+)\\s+row\\s+(\\d+)\\b",
      "ops": [ { "op":"custom", "value":"deleteTiles($1:csv,$2:int,$2:int)"} ]
    },
    {
      "name": "straighten rows",
      "regex": "\\bstraighten\\s+rows?\\s+(\\d+)\\s*[-to]+\\s*(\\d+)\\b",
      "ops": [ { "op":"custom", "value":"straightenRows($1:int,$2:int)"} ]
    }
  ]
}
JSON
else
  say "Keeping existing intents.json"
fi

# Working Notes entry
NOTES="$NEW_ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"
mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Installed your original Workbench, AIGG windows, API Settings, Force Recompile, Restore Point tools, Track Generator."
  echo "- Removed any placeholder stubs to keep menus tidy."
  echo "- Ensured Resources/Spec/intents.json exists (meters-by-default)."
} >> "$NOTES"

echo "------------------------------------------------------------"
echo "Done. Now open Unity and let it recompile."
echo "Menus you should see under Window → Aim2Pro:"
echo "  • Tools → Create/Load/Clear Restore Point"
echo "  • Aigg  → AIGG, Paste & Merge"
echo "  • Settings → API Settings"
echo "  • Track Creator → Track Generator"
echo "  • Terminal → Force Recompile (if present)"
