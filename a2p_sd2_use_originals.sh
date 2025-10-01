#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 1 ]; then
  echo "Usage: $0 /path/to/OLD_PROJECT"
  exit 1
fi
OLD_ROOT="$1"
NEW_ROOT="$(pwd)"

need(){ [ -e "$1" ] || { echo "ERR: missing $1"; exit 1; }; }
need "$NEW_ROOT/Assets"; need "$NEW_ROOT/ProjectSettings"
[ -d "$OLD_ROOT" ] || { echo "ERR: OLD_PROJECT not found: $OLD_ROOT"; exit 1; }

# Helper: find a file by name under OLD_ROOT
ff(){ find "$OLD_ROOT" -type f -name "$1" -print -quit; }

# Dest folders (tidy & namespaced)
E_ROOT="$NEW_ROOT/Assets/StickerDash/AIGG/Editor"
mkdir -p "$E_ROOT/Workbench" "$E_ROOT/Aigg" "$E_ROOT/Settings" "$E_ROOT/Terminal"

# Map: src file -> dest path
declare -A MAP
MAP["WorkbenchWindow.cs"]="$E_ROOT/Workbench/WorkbenchWindow.cs"
MAP["SpecPasteMergeWindow.cs"]="$E_ROOT/Aigg/SpecPasteMergeWindow.cs"
MAP["ApiSettingsWindow.cs"]="$E_ROOT/Settings/ApiSettingsWindow.cs"
MAP["AIGGMainWindow.cs"]="$E_ROOT/Aigg/AIGGMainWindow.cs"
MAP["ForceRecompileMenu.cs"]="$E_ROOT/Terminal/ForceRecompileMenu.cs"

echo "Searching originals under: $OLD_ROOT"
for fname in "${!MAP[@]}"; do
  src="$(ff "$fname" || true)"
  if [ -z "${src:-}" ]; then
    echo "WARN: Not found in OLD project: $fname (skipping)"
    continue
  fi
  dst="${MAP[$fname]}"
  mkdir -p "$(dirname "$dst")"
  cp -p "$src" "$dst"
  echo "Copied: $fname → ${dst#$NEW_ROOT/}"
done

# Remove my earlier stubs if present (to avoid duplicate menus)
rm -f "$E_ROOT/Workbench/SD2_WorkbenchWindow.cs" 2>/dev/null || true
rm -f "$E_ROOT/Terminal/A2P_OpenTerminal.cs"     2>/dev/null || true
# (Optional) remove any TrackLab stub I created earlier
rm -f "$E_ROOT/Track/TrackLabWindow.cs"          2>/dev/null || true

# Working Notes entry
notes="$NEW_ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"
mkdir -p "$(dirname "$notes")"
{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Replaced placeholder Workbench/Terminal with **your originals**:"
  echo "  - WorkbenchWindow.cs  → Window/Aim2Pro/Workbench/Workbench"
  echo "  - SpecPasteMergeWindow.cs → Window/Aim2Pro/Aigg/Paste & Merge"
  echo "  - ApiSettingsWindow.cs → Window/Aim2Pro/Settings/API Settings"
  echo "  - AIGGMainWindow.cs   → Window/Aim2Pro/Aigg/AIGG"
  echo "  - ForceRecompileMenu.cs → Window/Aim2Pro/Terminal/Force Recompile"
  echo "- Removed any placeholder files to keep menus tidy."
} >> "$notes"

echo "Done. Now open Unity and let it recompile."
echo "You should see ONLY your menu items under Window → Aim2Pro."
