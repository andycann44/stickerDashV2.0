#!/usr/bin/env bash
set -euo pipefail
NEW_ROOT="$(pwd)"
if [ ! -d "$NEW_ROOT/Assets" ] || [ ! -d "$NEW_ROOT/ProjectSettings" ]; then echo "ERR: run at Unity project root"; exit 1; fi

echo "Option B FIX: installing your exact original scripts (verbatim)..."
E_ROOT="$NEW_ROOT/Assets/StickerDash/AIGG/Editor"
mkdir -p "$E_ROOT/Workbench" "$E_ROOT/Aigg" "$E_ROOT/Settings" "$E_ROOT/Terminal" "$E_ROOT/Track" "$E_ROOT/Tools"
RES_SPEC="$NEW_ROOT/Assets/StickerDash/AIGG/Resources/Spec"
mkdir -p "$RES_SPEC"

write_b64 () {
  local dest="$1"; shift
  if command -v python3 >/dev/null 2>&1; then
  python3 - "$dest" <<'PY'
import sys, os, base64
path = sys.argv[1]
os.makedirs(os.path.dirname(path), exist_ok=True)
data = base64.b64decode(sys.stdin.read())
open(path, "wb").write(data)
print("Wrote:", path)
PY
else
  base64 -D > "$dest"
  echo "Wrote: $dest"
fi
}

write_b64 "$E_ROOT/Workbench/WorkbenchWindow.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7CnVzaW5nIFN5c3R...
B64

write_b64 "$E_ROOT/Terminal/ForceRecompileMenu.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7CgpwdWJsaWMgc3R...
B64

write_b64 "$E_ROOT/Aigg/SpecPasteMergeWindow.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgppbXBvcnQgc3lzdGVtOwp1c2luZyBTeXN0ZW0uSU87CnVzaW5nIFN5c3RlbS5UZXh0...
B64

write_b64 "$E_ROOT/Settings/ApiSettingsWindow.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7CnVzaW5nIFN5c3R...
B64

write_b64 "$E_ROOT/Aigg/AIGGMainWindow.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7CnVzaW5nIFN5c3R...
B64

write_b64 "$E_ROOT/Tools/ClearRestorePoints.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7Cgp1c2luZyBTeXN0...
B64

write_b64 "$E_ROOT/Tools/CreateRestorePoint.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7CmltcG9ydCBTeXN0...
B64

write_b64 "$E_ROOT/Tools/LoadRestorePoint.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgp1c2luZyBVbml0eUVkaXRvcjsKdXNpbmcgVW5pdHlFbmdpbmU7CnVzaW5nIFN5c3R...
B64

write_b64 "$E_ROOT/Track/TrackGeneratorWindow.cs" <<'B64'
I2lmIFVOSVRZX0VESVRPUgovLyBBbWJpZ3VpdGllczogVW5pdHkgQyMgd...
B64

rm -f "$E_ROOT/Workbench/SD2_WorkbenchWindow.cs" "$E_ROOT/Terminal/A2P_OpenTerminal.cs" "$E_ROOT/Track/TrackLabWindow.cs" 2>/dev/null || true

if [ ! -f "$RES_SPEC/intents.json" ]; then
  echo "Writing minimal Resources/Spec/intents.json (meters-by-default)"
  write_b64 "$RES_SPEC/intents.json" <<'B64'
ewogICJpbnRlbnRzIjogWwogICAgeyJuYW1lIjogImJ1aWxkIGJ5IG1ldGVycyIsICJyZWdleCI6ICJcYig/OnJlYnVpbGR8YnVpbGQpXFxzKyhcXGQpXFxzKyg/Om18bWV0ZXJzKT9cXHMqYnlcXHMqKFxcZClcXHMqKD86bXxtZXRlcnMpP1xcYiIsICJvcHMiOiBbIHsib3AiOiAiY3VzdG9tIiwgInZhbHVlIjoiYnVpbGRBYnMoJDE6aW50LCRyOmludCkifSBdIH0sCiAgICB7Im5hbWUiOiAiY3VydmUgcm93cyIsICJyZWdleCI6ICJcXGJjdXJ2ZVxccytyb3dzP1xccysoXFxkKSlfKlstdG8rXVx4dypcXHMoXFxkKSlfKlxccysoPzpsZWZ0fHJpZ2h0KVxccysoXFxkKSlfKig/OjRlZ3xkZWdyZWVzfMKuKT9cXGIiLCAib3BzIjogW3sib3AiOiAiY3VzdG9tIiwgInZhbHVlIjoiY3VydmVSb3dzKCRyOmludCwkMjppbnQsJDMsJDQ6aW50KSJ9XX0sCiAgICB7Im5hbWUiOiAicmVtb3ZlIHJvd3MiLCAicmVnZXgiOiAiXFxicmVtb3ZlXFxzK3Jvd3M/XFxzKyhcXGQpXFxzKystdG8rXFxzKygkMilcXGJcXGIiLCAib3BzIjogW3sib3AiOiAiY3VzdG9tIiwgInZhbHVlIjoiZGVsZXRlUm93cygkMTppbnQsJDI6aW50KSJ9XX0sCiAgICB7Im5hbWUiOiAicmVtb3ZlIHRpbGVzIGluIHJvdyIsICJyZWdleCI6ICJcXGJyZW1vdmVcXHMrdGlsZXM/XFxzKyhbXFxiLFxcc10rKVxccytyb3dcXHMrKFxcZCkpXFxiIiwgIm9wcyI6IFt7Im9wIjogImN1c3RvbSIsICJ2YWx1ZSI6ImRlbGV0ZVRpbGVzKCRxOmNzdiwkMjppbnQsJDI6aW50KSJ9XX0sCiAgICB7Im5hbWUiOiAic3RyYWlnaHRlbiByb3dzIiwgInJlZ2V4IjogIlxcYnN0cmFpZ2h0ZW5cXHNyb3dzP1xccysoXFxkKSlfKlstdG8rXVx4dypcXHMoKDIpKVxcYiIsICJvcHMiOiBbeyJvcCI6ICJjdXN0b20iLCAidmFsdWUiOiJzdHJhaWdodGVuUm93cygkMTppbnQsJDI6aW50KSJ9XX0KICBdCn0=
B64
else
  echo "Keeping existing Resources/Spec/intents.json"
fi

NOTES="$NEW_ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Installed your original files (Option B FIX) and removed stubs."
  echo "- Ensured Resources/Spec/intents.json exists (meters-by-default)."
} >> "$NOTES"

echo "Done. Open Unity and let it recompile."
