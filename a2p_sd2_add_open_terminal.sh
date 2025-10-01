#!/usr/bin/env bash
set -euo pipefail
ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "ERR: run at Unity project root"; exit 1; }

DIR="$ROOT/Assets/StickerDash/AIGG/Editor/Terminal"
mkdir -p "$DIR"

cat > "$DIR/A2P_OpenTerminalRoot.cs" <<'CS'
#if UNITY_EDITOR
using UnityEditor;
using System.Diagnostics;

namespace Aim2Pro.Terminal {
  public static class A2P_OpenTerminalRoot {
    // Hotkey: %t  → Ctrl (Win) / Cmd (macOS) + T
    [MenuItem("Window/Aim2Pro/Terminal/Open Project Root %t")]
    public static void OpenRoot() {
      string proj = System.IO.Directory.GetCurrentDirectory();

#if UNITY_EDITOR_OSX
      // macOS: open Terminal.app and cd to project root
      string osa = "tell application \\\"Terminal\\\" to activate\n"
                 + "tell application \\\"Terminal\\\" to do script \\\"cd " 
                 + proj.Replace("\\", "\\\\").Replace("\"","\\\"")
                 + " && clear && pwd\\\"";
      var psi = new ProcessStartInfo("osascript", "-e \"" + osa + "\"");
      psi.UseShellExecute = false; psi.CreateNoWindow = true; Process.Start(psi);

#elif UNITY_EDITOR_WIN
      // Windows: open new CMD at project root
      var psi = new ProcessStartInfo("cmd.exe", "/c start cmd /K \"cd /d " + proj + " && cd && title Aim2Pro Terminal\"");
      psi.UseShellExecute = false; psi.CreateNoWindow = true; Process.Start(psi);

#elif UNITY_EDITOR_LINUX
      // Linux: try common terminals
      string[] terms = { "gnome-terminal", "x-terminal-emulator", "konsole", "xfce4-terminal", "xterm" };
      foreach (var t in terms) {
        try {
          var psi = new ProcessStartInfo(t, "-- bash -lc 'cd \"" + proj + "\"; clear; pwd; exec bash'");
          psi.UseShellExecute = false; psi.CreateNoWindow = true; Process.Start(psi);
          UnityEngine.Debug.Log("[A2P] Opened terminal ("+t+") at: " + proj);
          return;
        } catch {}
      }
      UnityEngine.Debug.LogWarning("[A2P] Could not launch a terminal on Linux.");
#else
      UnityEngine.Debug.Log("[A2P] Unsupported editor OS for terminal launcher.");
#endif

      UnityEngine.Debug.Log("[A2P] Opened terminal at: " + proj);
    }
  }
}
#endif
CS

# Notes
NOTES="$ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Added 'Open Project Root' under Window → Aim2Pro → Terminal (hotkey: Cmd/Ctrl+T)."
} >> "$NOTES"

echo "Added: Assets/StickerDash/AIGG/Editor/Terminal/A2P_OpenTerminalRoot.cs"
echo "Open Unity and let it recompile. Use Window → Aim2Pro → Terminal → Open Project Root, or press Cmd/Ctrl+T."
