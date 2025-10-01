#!/usr/bin/env bash
set -euo pipefail
ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "ERR: run at Unity project root"; exit 1; }

E="$ROOT/Assets/StickerDash/AIGG/Editor/Git"
mkdir -p "$E"

# --- Git Quick Actions editor window ---
cat > "$E/GitQuickActions.cs" <<'CS'
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.Git {
  public class GitQuickActions : EditorWindow {
    string commitMsg = "chore: update";
    string tagName = "v0.1.0";
    string branch = "main";
    string remote = "origin";
    string lastLog = "";

    [MenuItem("Window/Aim2Pro/Git/Quick Actions")]
    public static void Open(){
      var w = GetWindow<GitQuickActions>();
      w.titleContent = new GUIContent("Git Quick Actions");
      w.minSize = new Vector2(560, 380);
      w.Show();
    }

    void OnGUI(){
      GUILayout.Label("Repository", EditorStyles.boldLabel);
      EditorGUILayout.LabelField("Project Root", Directory.GetCurrentDirectory());
      EditorGUILayout.Space(6);

      GUILayout.Label("Pull / Fetch", EditorStyles.boldLabel);
      GUILayout.BeginHorizontal();
      if (GUILayout.Button("Fetch"))     RunGit("fetch --all");
      if (GUILayout.Button("Pull (FF)")) RunGit("pull --ff-only");
      GUILayout.EndHorizontal();

      EditorGUILayout.Space(6);
      GUILayout.Label("Commit + Push", EditorStyles.boldLabel);
      commitMsg = EditorGUILayout.TextField("Commit message", commitMsg);
      GUILayout.BeginHorizontal();
      if (GUILayout.Button("Commit All")) RunGit($"add -A && git commit -m \"{commitMsg.Replace("\"","\\\"")}\" || echo \"(nothing to commit)\"");
      if (GUILayout.Button("Commit + Push")) RunGit($"add -A && git commit -m \"{commitMsg.Replace("\"","\\\"")}\" || true; git push");
      GUILayout.EndHorizontal();

      EditorGUILayout.Space(6);
      GUILayout.Label("Branch / Tag", EditorStyles.boldLabel);
      branch = EditorGUILayout.TextField("Branch", branch);
      tagName = EditorGUILayout.TextField("Tag", tagName);
      GUILayout.BeginHorizontal();
      if (GUILayout.Button("Checkout/Create Branch")) RunGit($"checkout -B {branch}");
      if (GUILayout.Button("Create Tag + Push"))      RunGit($"tag -a {tagName} -m \"{commitMsg.Replace("\"","\\\"")}\" && git push origin {tagName}");
      GUILayout.EndHorizontal();

      EditorGUILayout.Space(6);
      GUILayout.Label("Remote", EditorStyles.boldLabel);
      remote = EditorGUILayout.TextField("Remote name", remote);
      GUILayout.BeginHorizontal();
      if (GUILayout.Button("Show Remotes")) RunGit("remote -v");
      if (GUILayout.Button("Open on GitHub")) OpenOnGitHub();
      GUILayout.EndHorizontal();

      EditorGUILayout.Space(8);
      GUILayout.Label("Log", EditorStyles.boldLabel);
      var style = new GUIStyle(EditorStyles.helpBox){ wordWrap = true };
      EditorGUILayout.TextArea(lastLog, style, GUILayout.MinHeight(120));
    }

    void OpenOnGitHub(){
      string url = RunGitCapture("remote get-url " + remote).Trim();
      if (string.IsNullOrEmpty(url)) { lastLog += "\n(no remote)\n"; return; }
      // Normalize SSH to https for browser
      string http = url;
      if (url.StartsWith("git@github.com:"))
        http = "https://github.com/" + url.Substring("git@github.com:".Length);
      if (http.EndsWith(".git")) http = http.Substring(0, http.Length - 4);
      Application.OpenURL(http);
      lastLog += "\nOpened: " + http + "\n";
    }

    void RunGit(string args){
      string cmd = (Application.platform == RuntimePlatform.WindowsEditor) ? "cmd.exe" : "/bin/bash";
      string argLine = (Application.platform == RuntimePlatform.WindowsEditor)
        ? "/c git " + args
        : "-lc \"git " + args + "\"";
      var psi = new ProcessStartInfo(cmd, argLine);
      psi.WorkingDirectory = Directory.GetCurrentDirectory();
      psi.UseShellExecute = false; psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
      var p = Process.Start(psi);
      p.WaitForExit();
      lastLog += "\n$ git " + args + "\n" + p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
      Repaint();
    }

    string RunGitCapture(string args){
      string cmd = (Application.platform == RuntimePlatform.WindowsEditor) ? "cmd.exe" : "/bin/bash";
      string argLine = (Application.platform == RuntimePlatform.WindowsEditor)
        ? "/c git " + args
        : "-lc \"git " + args + "\"";
      var psi = new ProcessStartInfo(cmd, argLine);
      psi.WorkingDirectory = Directory.GetCurrentDirectory();
      psi.UseShellExecute = false; psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
      var p = Process.Start(psi);
      var outp = p.StandardOutput.ReadToEnd(); p.WaitForExit();
      lastLog += "\n$ git " + args + "\n" + outp + p.StandardError.ReadToEnd();
      Repaint();
      return outp;
    }
  }
}
#endif
CS

# --- Pre-commit hook: block stray Unity menus outside Window/Aim2Pro ---
mkdir -p .git/hooks
cat > .git/hooks/pre-commit <<'HOOK'
#!/usr/bin/env bash
# Block commits that introduce Editor menu items outside Window/Aim2Pro
set -euo pipefail
changed=$(git diff --cached --name-only --diff-filter=ACMR | grep -E '\.cs$' || true)
[ -z "$changed" ] && exit 0
bad=""
while IFS= read -r f; do
  # only check files under Assets/
  [[ "$f" != Assets/* ]] && continue
  # look for [MenuItem("Window/  but not Window/Aim2Pro/
  if grep -n '\[MenuItem("Window/' "$f" | grep -v 'Window/Aim2Pro/' >/dev/null; then
    bad="$bad\n - $f"
  fi
done <<< "$changed"
if [ -n "$bad" ]; then
  echo "❌ Commit blocked: menu items must live under Window/Aim2Pro/"
  echo -e "$bad"
  exit 1
fi
exit 0
HOOK
chmod +x .git/hooks/pre-commit

# Note
NOTES="$ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## $(date +%Y-%m-%d' '%H:%M:%S) — Git tools installed"
  echo "- Added Window → Aim2Pro → Git → Quick Actions (pull/commit/push/open/tag)."
  echo "- Added pre-commit hook to block stray MenuItem paths."
} >> "$NOTES"

echo "Installed:"
echo " - Assets/StickerDash/AIGG/Editor/Git/GitQuickActions.cs"
echo " - .git/hooks/pre-commit  (menu path policy)"
echo "Open Unity → Window → Aim2Pro → Git → Quick Actions"
