#!/usr/bin/env bash
set -euo pipefail
NEW_ROOT="$(pwd)"
if [ ! -d "$NEW_ROOT/Assets" ] || [ ! -d "$NEW_ROOT/ProjectSettings" ]; then echo "ERR: run at Unity project root"; exit 1; fi

echo "Installing your exact original scripts (verbatim) into Assets/StickerDash/AIGG/Editor …"

E_ROOT="$NEW_ROOT/Assets/StickerDash/AIGG/Editor"
mkdir -p "$E_ROOT/Workbench" "$E_ROOT/Aigg" "$E_ROOT/Settings" "$E_ROOT/Terminal" "$E_ROOT/Track" "$E_ROOT/Tools"
RES_SPEC="$NEW_ROOT/Assets/StickerDash/AIGG/Resources/Spec"
mkdir -p "$RES_SPEC"
NOTES="$NEW_ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"

write_b64 () { # python3 preferred; fallback to macOS base64 -D
  local dest="$1"; shift
  if command -v python3 >/dev/null 2>&1; then
    python3 - "$dest" <<'PY'
import sys, os, base64
path = sys.argv[1]
os.makedirs(os.path.dirname(path), exist_ok=True)
data = base64.b64decode(sys.stdin.read().encode('ascii'))
open(path, "wb").write(data)
print("Wrote:", path)
PY
  else
    base64 -D > "$dest"
    echo "Wrote: $dest"
  fi
}

# ---------- Your exact files (verbatim) ----------

# WorkbenchWindow.cs
write_b64 "$E_ROOT/Workbench/WorkbenchWindow.cs" <<'B64'
<?base64:WorkbenchWindow.cs?>
B64

# ForceRecompileMenu.cs
write_b64 "$E_ROOT/Terminal/ForceRecompileMenu.cs" <<'B64'
<?base64:ForceRecompileMenu.cs?>
B64

# SpecPasteMergeWindow.cs
write_b64 "$E_ROOT/Aigg/SpecPasteMergeWindow.cs" <<'B64'
<?base64:SpecPasteMergeWindow.cs?>
B64

# ApiSettingsWindow.cs
write_b64 "$E_ROOT/Settings/ApiSettingsWindow.cs" <<'B64'
<?base64:ApiSettingsWindow.cs?>
B64

# AIGGMainWindow.cs
write_b64 "$E_ROOT/Aigg/AIGGMainWindow.cs" <<'B64'
<?base64:AIGGMainWindow.cs?>
B64

# ClearRestorePoints.cs
write_b64 "$E_ROOT/Tools/ClearRestorePoints.cs" <<'B64'
<?base64:ClearRestorePoints.cs?>
B64

# CreateRestorePoint.cs
write_b64 "$E_ROOT/Tools/CreateRestorePoint.cs" <<'B64'
<?base64:CreateRestorePoint.cs?>
B64

# LoadRestorePoint.cs
write_b64 "$E_ROOT/Tools/LoadRestorePoint.cs" <<'B64'
<?base64:LoadRestorePoint.cs?>
B64

# TrackGeneratorWindow.cs
write_b64 "$E_ROOT/Track/TrackGeneratorWindow.cs" <<'B64'
<?base64:TrackGeneratorWindow.cs?>
B64

# Remove any earlier stubs to avoid duplicate menus
rm -f "$E_ROOT/Workbench/SD2_WorkbenchWindow.cs" "$E_ROOT/Terminal/A2P_OpenTerminal.cs" "$E_ROOT/Track/TrackLabWindow.cs" 2>/dev/null || true

# Minimal intents.json if missing (meters-by-default)
if [ ! -f "$RES_SPEC/intents.json" ]; then
  echo "Writing minimal Resources/Spec/intents.json (meters-by-default)"
  write_b64 "$RES_SPEC/intents.json" <<'B64'
<?base64:intents.json?>
B64
else
  echo "Keeping existing Resources/Spec/intents.json"
fi

{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Installed your original files (Option B EXACT) and removed stubs."
  echo "- Ensured Resources/Spec/intents.json exists (meters-by-default)."
} >> "$NOTES"

echo "Done. Open Unity and let it recompile."
echo "If menus don’t show immediately: try 'Assets → Reimport All' or restart the Editor."
