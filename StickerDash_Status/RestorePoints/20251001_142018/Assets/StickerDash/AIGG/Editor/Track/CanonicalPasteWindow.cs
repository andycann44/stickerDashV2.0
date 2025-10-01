#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Aim2Pro.AIGG.Track {

  // Extension point other code can hook into to actually APPLY a canonical plan.
  // Wire your real applier by calling: CanonicalDispatcher.OnRun = (lines, dry) => { ...return report; };
  public static class CanonicalDispatcher {
    // Return a human-readable report string. Throw on fatal errors if you want the UI to stop.
    public static Func<string[], bool, string> OnRun;
    public static bool TryDispatch(string[] lines, bool dryRun, out string report) {
      report = "";
      try {
        if (OnRun == null) { report = "(No dispatcher wired)"; return false; }
        report = OnRun(lines, dryRun) ?? "";
        return true;
      } catch (Exception e) {
        report = e.Message + "\n" + e.StackTrace;
        return false;
      }
    }
  }

  public class CanonicalPasteWindow : EditorWindow {
    const string MENU_PATH = "Window/Aim2Pro/Track Creator/Canonical Paste…";
    const string LAST_PLAN_PATH = "StickerDash_Status/LastCanonical.plan";
    string canonical = "";
    string log = "";
    Vector2 scroll;
    bool showHelp;

    [MenuItem(MENU_PATH)]
    public static void Open(){
      var w = GetWindow<CanonicalPasteWindow>();
      w.titleContent = new GUIContent("Canonical Paste");
      w.minSize = new Vector2(760, 520);
      w.LoadLastPlanIfAny();
    }

    void OnGUI(){
      GUILayout.Label("Canonical Plan (one call per line)", EditorStyles.boldLabel);

      if (GUILayout.Button("Paste from Clipboard")) {
        canonical = EditorGUIUtility.systemCopyBuffer ?? "";
      }

      // Editor
      scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
      canonical = EditorGUILayout.TextArea(canonical, GUILayout.ExpandHeight(true));
      GUILayout.EndScrollView();

      GUILayout.Space(6);
      GUILayout.BeginHorizontal();
      if (GUILayout.Button("Validate")) ValidateOnly();
      if (GUILayout.Button("Dry Run")) DryRun();
      GUI.enabled = true;
      if (GUILayout.Button("Apply")) Apply();
      GUI.enabled = true;
      if (GUILayout.Button("Create Restore Point")) CreateRestorePoint();
      GUILayout.EndHorizontal();

      GUILayout.Space(6);
      GUILayout.BeginHorizontal();
      if (GUILayout.Button("Load .sdplan…")) LoadPlan();
      if (GUILayout.Button("Save .sdplan…")) SavePlan();
      if (GUILayout.Button("Copy Example")) CopyExample();
      if (GUILayout.Button("Open Last Plan")) LoadLastPlanIfAny(true);
      GUILayout.EndHorizontal();

      GUILayout.Space(6);
      showHelp = EditorGUILayout.Foldout(showHelp, "Accepted Calls / Tips");
      if (showHelp) {
        EditorGUILayout.HelpBox(
@"• buildAbs(LENGTH_M, WIDTH_M)
• curveRows(START, END, left|right, DEG)
• sBend(START, END, left-right|right-left|lr|rl, DEG)
• deleteRows(START, END)
• deleteTiles(2,4,6, row=55)     // or deleteTiles(2,4,6,55,55)
• straightenRows(START, END)
• randomHoles(PERCENT)
• insertJumpGaps(COUNT)
• randomStraightSlopes(MIN_DEG, MAX_DEG)
• splineSmooth(WINDOW)
• resample(ROWS_PER_M)
• seed(NUMBER)

Notes:
- Lines may have comments starting with '#' or '//' — these are ignored.
- Trailing semicolons are fine.
- This window saves the last plan to StickerDash_Status/LastCanonical.plan.", MessageType.Info);
      }

      GUILayout.Space(8);
      GUILayout.Label("Log", EditorStyles.boldLabel);
      var style = new GUIStyle(EditorStyles.helpBox){ wordWrap = true };
      EditorGUILayout.TextArea(log, style, GUILayout.MinHeight(140));
    }

    void Log(string s){ log = DateTime.Now.ToString("HH:mm:ss") + " — " + s + "\n" + log; Repaint(); }

    void ValidateOnly(){
      var r = ValidateLines(out var lines, out var errors);
      if (!r) {
        Log("❌ Validation failed:\n" + string.Join("\n", errors));
      } else {
        Log("✅ Validation OK ("+lines.Length+" lines)");
      }
      SaveLast(linesFallback: null); // just persist editor text
    }

    void DryRun(){
      if (!ValidateLines(out var lines, out var errors)) {
        Log("❌ Dry Run aborted. Fix errors:\n" + string.Join("\n", errors));
        return;
      }
      SaveLast(lines);
      // Try dispatcher
      if (CanonicalDispatcher.OnRun != null) {
        if (CanonicalDispatcher.TryDispatch(lines, true, out var report)) {
          Log("🔎 Dry Run report:\n" + (string.IsNullOrEmpty(report) ? "(no details)" : report));
        } else {
          Log("❌ Dry Run failed (dispatcher threw):\n" + report);
        }
      } else {
        Log("ℹ️ No dispatcher wired. Dry-run would execute "+lines.Length+" calls:\n" + string.Join("\n", lines));
      }
    }

    void Apply(){
      if (!ValidateLines(out var lines, out var errors)) {
        Log("❌ Apply aborted. Fix errors:\n" + string.Join("\n", errors));
        return;
      }
      if (!EditorUtility.DisplayDialog("Apply Canonical Plan", "Create a Restore Point first?", "Proceed", "Cancel")) {
        return;
      }
      SaveLast(lines);
      if (CanonicalDispatcher.OnRun != null) {
        if (CanonicalDispatcher.TryDispatch(lines, false, out var report)) {
          Log("✅ Applied.\n" + (string.IsNullOrEmpty(report) ? "(no details)" : report));
        } else {
          Log("❌ Apply failed:\n" + report);
        }
      } else {
        Log("ℹ️ No dispatcher wired. Simulating apply of "+lines.Length+" calls:\n" + string.Join("\n", lines));
      }
    }

    void CreateRestorePoint(){
      // Try your existing menu(s)
      string[] menus = {
        "Window/Aim2Pro/Tools/Create Restore Point",
        "Window/Aim2Pro/Tools/Restore Points/Create Restore Point"
      };
      foreach (var m in menus) {
        if (EditorApplication.ExecuteMenuItem(m)) { Log("Restore Point created via menu: "+m); return; }
      }
      Log("⚠️ Restore Point menu not found. Tell me the exact menu path text and I’ll update.");
    }

    void LoadPlan(){
      string path = EditorUtility.OpenFilePanel("Load .sdplan", "StickerDash_Status/Plans", "sdplan");
      if (string.IsNullOrEmpty(path)) return;
      try {
        canonical = File.ReadAllText(path);
        Log("Loaded plan: " + path);
      } catch (Exception e) {
        Log("Failed to load: " + e.Message);
      }
    }

    void SavePlan(){
      Directory.CreateDirectory("StickerDash_Status/Plans");
      string path = EditorUtility.SaveFilePanel("Save .sdplan", "StickerDash_Status/Plans", "plan", "sdplan");
      if (string.IsNullOrEmpty(path)) return;
      try {
        File.WriteAllText(path, canonical);
        Log("Saved plan: " + path);
      } catch (Exception e) {
        Log("Failed to save: " + e.Message);
      }
    }

    void LoadLastPlanIfAny(bool logIt=false){
      if (File.Exists(LAST_PLAN_PATH)) {
        canonical = File.ReadAllText(LAST_PLAN_PATH);
        if (logIt) Log("Opened last plan: "+LAST_PLAN_PATH);
      }
    }

    void SaveLast(string[] linesFallback){
      try {
        Directory.CreateDirectory(Path.GetDirectoryName(LAST_PLAN_PATH));
        var text = string.IsNullOrEmpty(canonical) && linesFallback!=null
          ? string.Join("\n", linesFallback)
          : canonical;
        File.WriteAllText(LAST_PLAN_PATH, text ?? "");
      } catch {}
    }

    // ---------------- Parsing / Validation ----------------
    static readonly Regex rxComment = new Regex(@"^\s*(#|//)", RegexOptions.Compiled);
    static readonly Regex rxBuild    = new Regex(@"^\s*buildAbs\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxCurve    = new Regex(@"^\s*curveRows\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(left|right)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxSBend    = new Regex(@"^\s*sBend\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(left-right|right-left|lr|rl)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxDelRows  = new Regex(@"^\s*deleteRows\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxDelTiles1= new Regex(@"^\s*deleteTiles\s*\(\s*([0-9,\s]+)\s*,\s*row\s*=\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxDelTiles2= new Regex(@"^\s*deleteTiles\s*\(\s*([0-9,\s]+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxStraight = new Regex(@"^\s*straightenRows\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxHoles    = new Regex(@"^\s*randomHoles\s*\(\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxJumps    = new Regex(@"^\s*insertJumpGaps\s*\(\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxSlopes   = new Regex(@"^\s*randomStraightSlopes\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxSmooth   = new Regex(@"^\s*splineSmooth\s*\(\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxResample = new Regex(@"^\s*resample\s*\(\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);
    static readonly Regex rxSeed     = new Regex(@"^\s*seed\s*\(\s*(\d+)\s*\)\s*;?\s*$", RegexOptions.IgnoreCase|RegexOptions.Compiled);

    bool ValidateLines(out string[] outLines, out List<string> errors){
      var lines = new List<string>();
      errors = new List<string>();
      var raw = (canonical ?? "").Replace("\r\n","\n").Replace("\r","\n").Split('\n');
      foreach (var rawLine in raw){
        var line = rawLine.Trim();
        if (string.IsNullOrEmpty(line)) continue;
        if (rxComment.IsMatch(line)) continue;
        if (line.EndsWith(";")) line = line.Substring(0, line.Length-1).Trim();

        string norm = null;
        Match m;
        if ((m=rxBuild.Match(line)).Success)
          norm = $"buildAbs({m.Groups[1].Value},{m.Groups[2].Value})";
        else if ((m=rxCurve.Match(line)).Success)
          norm = $"curveRows({m.Groups[1].Value},{m.Groups[2].Value},{m.Groups[3].Value.ToLower()},{m.Groups[4].Value})";
        else if ((m=rxSBend.Match(line)).Success)
          norm = $"sBend({m.Groups[1].Value},{m.Groups[2].Value},{m.Groups[3].Value.ToLower()},{m.Groups[4].Value})";
        else if ((m=rxDelRows.Match(line)).Success)
          norm = $"deleteRows({m.Groups[1].Value},{m.Groups[2].Value})";
        else if ((m=rxDelTiles1.Match(line)).Success)
          norm = $"deleteTiles({CollapseCsv(m.Groups[1].Value)}, row={m.Groups[2].Value})";
        else if ((m=rxDelTiles2.Match(line)).Success)
          norm = $"deleteTiles({CollapseCsv(m.Groups[1].Value)},{m.Groups[2].Value},{m.Groups[3].Value})";
        else if ((m=rxStraight.Match(line)).Success)
          norm = $"straightenRows({m.Groups[1].Value},{m.Groups[2].Value})";
        else if ((m=rxHoles.Match(line)).Success)
          norm = $"randomHoles({m.Groups[1].Value})";
        else if ((m=rxJumps.Match(line)).Success)
          norm = $"insertJumpGaps({m.Groups[1].Value})";
        else if ((m=rxSlopes.Match(line)).Success)
          norm = $"randomStraightSlopes({m.Groups[1].Value},{m.Groups[2].Value})";
        else if ((m=rxSmooth.Match(line)).Success)
          norm = $"splineSmooth({m.Groups[1].Value})";
        else if ((m=rxResample.Match(line)).Success)
          norm = $"resample({m.Groups[1].Value})";
        else if ((m=rxSeed.Match(line)).Success)
          norm = $"seed({m.Groups[1].Value})";
        else
          errors.Add("Unrecognized: " + rawLine);

        if (!string.IsNullOrEmpty(norm))
          lines.Add(norm);
      }

      outLines = lines.ToArray();

      // Basic logical checks (in addition to syntax)
      // (We can't compute actual bounds without a track, so we keep it light here.)
      // Ensure a buildAbs exists before row ops:
      bool hasBuild = outLines.Any(l => l.StartsWith("buildAbs("));
      if (!hasBuild) {
        // Not fatal: you may be applying to an existing track.
      }

      return errors.Count == 0;
    }

    static string CollapseCsv(string s){
      var nums = s.Split(new[]{',',' '}, StringSplitOptions.RemoveEmptyEntries)
                  .Select(x=>x.Trim()).ToArray();
      return string.Join(",", nums);
    }

    void CopyExample(){
      var ex = "# StickerDash Canonical Plan v2\n"
             + "buildAbs(250,6)\n"
             + "sBend(40,80,left-right,25)\n"
             + "randomHoles(10)\n"
             + "insertJumpGaps(3)\n"
             + "randomStraightSlopes(2,6)\n"
             + "splineSmooth(4)\n"
             + "resample(2)\n";
      canonical = ex;
      EditorGUIUtility.systemCopyBuffer = ex;
      Log("Copied example plan to clipboard.");
    }
  }
}
#endif
