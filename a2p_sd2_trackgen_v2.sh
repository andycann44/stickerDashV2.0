#!/usr/bin/env bash
set -euo pipefail
ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "Run this in your Unity project root"; exit 1; }

DIR="$ROOT/Assets/StickerDash/AIGG/Editor/Track"
mkdir -p "$DIR"

cat > "$DIR/TrackGeneratorV2.cs" <<'CS'
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Aim2Pro.AIGG.Track {
  public class TrackGeneratorV2 : EditorWindow {
    [MenuItem("Window/Aim2Pro/Track Creator/Track Generator v2")]
    public static void Open(){
      var w = GetWindow<TrackGeneratorV2>();
      w.titleContent = new GUIContent("Track Generator v2");
      w.minSize = new Vector2(820, 560);
      w.Show();
    }

    // UI state
    string _nl = "";
    string _planJson = "";
    string _steps = "";
    Vector2 _lScroll, _rScroll;
    string _log = "";

    void OnGUI(){
      GUILayout.Label("Natural Language → Execution Plan (meters/deg by default)", EditorStyles.boldLabel);
      _nl = EditorGUILayout.TextArea(_nl, GUILayout.MinHeight(90));

      GUILayout.Space(6);
      using (new EditorGUILayout.HorizontalScope()){
        if (GUILayout.Button("Compile Plan", GUILayout.Height(28), GUILayout.Width(140))) CompilePlan();
        if (GUILayout.Button("Preview Run", GUILayout.Height(28), GUILayout.Width(120))) PreviewRun();
        if (GUILayout.Button("Save Plan", GUILayout.Height(28), GUILayout.Width(110))) SavePlan();
        if (GUILayout.Button("Run (Create Restore Point → Execute)", GUILayout.Height(28))) RunPlan();
      }

      GUILayout.Space(8);
      using (new EditorGUILayout.HorizontalScope()){
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))){
          GUILayout.Label("Derived Steps", EditorStyles.miniBoldLabel);
          _lScroll = EditorGUILayout.BeginScrollView(_lScroll);
          EditorGUILayout.TextArea(_steps, GUILayout.ExpandHeight(true));
          EditorGUILayout.EndScrollView();
        }
        GUILayout.Space(8);
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))){
          GUILayout.Label("Plan JSON", EditorStyles.miniBoldLabel);
          _rScroll = EditorGUILayout.BeginScrollView(_rScroll);
          EditorGUILayout.TextArea(_planJson, GUILayout.ExpandHeight(true));
          EditorGUILayout.EndScrollView();
        }
      }

      GUILayout.Space(6);
      GUILayout.Label("Log", EditorStyles.miniBoldLabel);
      EditorGUILayout.HelpBox(_log, MessageType.None);
      if (GUILayout.Button("Clear Log", GUILayout.Width(100))) _log="";
    }

    // ---------- Compile NL → Plan ----------
    void CompilePlan(){
      string src = (_nl ?? "").Trim();
      if (src.Length == 0){ Log("Nothing to compile."); return; }

      string norm = Normalize(src);

      // Defaults
      int lengthM = ExtractFirstInt(norm, @"\b(?:rebuild|build)\s+(\d+)\s*(?:m|meters)?\s*by");
      int widthVal = ExtractFirstInt(norm,  @"\b(?:rebuild|build)\s+\d+\s*(?:m|meters)?\s*by\s+(\d+)\s*(?:m|meters|tiles)?");
      string widthUnit = ExtractFirstGroup(norm, @"\b(?:rebuild|build)\s+\d+\s*(?:m|meters)?\s*by\s+\d+\s*(m|meters|tiles)?", 1);
      if (string.IsNullOrEmpty(widthUnit)) widthUnit = "m";

      // Features inferred from phrases
      bool wantSBends = Regex.IsMatch(norm, @"\bs\-?bend\b");
      int sCount = Regex.IsMatch(norm, @"\ba\s+couple\b") ? 2 : ExtractFirstInt(norm, @"\bs\-?bend(?:s)?\s+(\d+)\b");
      if (wantSBends && sCount==0) sCount=2;

      bool wantHoles = Regex.IsMatch(norm, @"\brandom\s+(?:tiles\s+missing|holes?)\b");
      int holesPct = ExtractFirstInt(norm, @"\b(\d+)\s*%\b");
      if (wantHoles && holesPct==0) holesPct = 10;

      bool wantJumps = Regex.IsMatch(norm, @"\brow\s+gaps?\s+for\s+jumps?\b|\bjump\s+gaps?\b");
      int jumpsCount = ExtractFirstInt(norm, @"\bjump\s+gaps?\s+(\d+)\b"); if (wantJumps && jumpsCount==0) jumpsCount=2;

      bool slopesStraights = Regex.IsMatch(norm, @"\brandom\s+slopes?\s+on\s+the\s+straights\b");
      int slopeMin = ExtractFirstInt(norm, @"\bslopes?.*?(\d+)\s*-\s*(\d+)\s*deg"); // try range
      int slopeMax = ExtractSecondInt(norm, @"\bslopes?.*?(\d+)\s*-\s*(\d+)\s*deg");
      if (slopesStraights && (slopeMin==0 || slopeMax==0)){ slopeMin=3; slopeMax=6; }

      bool smoothCurves = Regex.IsMatch(norm, @"\bsmooth\s+curves?\b|\bresample\b|\bspline\b");
      int density = ExtractFirstInt(norm, @"\bresample\s+(\d+)\s*(?:per\s*m|rows\/m)\b"); if (density==0) density=2;

      // Seed (optional)
      int seed = ExtractFirstInt(norm, @"\bseed\s+(\d+)\b");

      // Build plan object (as a simple JSON string)
      var steps = new List<string>();
      var sbJson = new StringBuilder();
      sbJson.Append("{\n");
      sbJson.AppendFormat("  \"build\": {{ \"lengthMeters\": {0}, \"widthValue\": {1}, \"widthUnit\": \"{2}\" }},\n", lengthM, widthVal, widthUnit);
      sbJson.AppendFormat("  \"rand\": {{ \"seed\": \"{0}\" }},\n", seed==0 ? "auto" : seed.ToString());
      sbJson.Append("  \"ops\": [\n");

      // Steps text
      steps.Add($"buildAbs(lengthM={lengthM}, width{(widthUnit=="tiles"?"Tiles":"M")}={widthVal})");

      if (wantSBends){
        steps.Add($"s_bend(count={sCount}, pattern=alternate, deg=20)");
        sbJson.Append("    { \"macro\":\"s_bend\", \"args\": {\"count\": "+sCount+", \"pattern\":\"alternate\", \"deg\": 20 } },\n");
      }
      if (wantHoles){
        steps.Add($"random_holes(density={holesPct}%, keepEdges=true, keepPath=true)");
        sbJson.Append("    { \"macro\":\"random_holes\", \"args\": {\"densityPercent\": "+holesPct+", \"keepEdges\": true, \"keepPath\": true } },\n");
      }
      if (wantJumps){
        steps.Add($"insert_jump_gaps(count={jumpsCount}, minSpacingRows=30, avoidCurves=true)");
        sbJson.Append("    { \"macro\":\"insert_jump_gaps\", \"args\": {\"count\": "+jumpsCount+", \"minSpacingRows\": 30, \"avoidCurves\": true } },\n");
      }
      if (slopesStraights){
        steps.Add($"random_straight_slopes(degMin={slopeMin}, degMax={slopeMax}, segmentLenRows=12..25)");
        sbJson.Append("    { \"macro\":\"random_straight_slopes\", \"args\": {\"degMin\": "+slopeMin+", \"degMax\": "+slopeMax+", \"segmentLenRows\": [12,25] } },\n");
      }
      if (smoothCurves){
        steps.Add($"spline_smooth(densityRowsPerMeter={density})");
        sbJson.Append("    { \"macro\":\"spline_smooth\", \"args\": {\"densityRowsPerMeter\": "+density+" } },\n");
      }

      // Trim trailing comma
      string opsJson = sbJson.ToString().TrimEnd('\n',',');
      _planJson = opsJson.EndsWith(",") ? opsJson.Substring(0, opsJson.Length-1) : opsJson;
      if(!_planJson.EndsWith("\n")) _planJson += "\n";
      _planJson += "  ]\n}\n";

      _steps = string.Join("\n", steps);
      Log("Compiled plan from NL.");
    }

    // ---------- Preview ----------
    void PreviewRun(){
      if (string.IsNullOrEmpty(_planJson)){ Log("No plan yet. Click Compile Plan first."); return; }
      EditorUtility.DisplayDialog("Preview Plan", _steps.Length==0 ? "(no steps)" : _steps, "OK");
    }

    // ---------- Save Plan ----------
    void SavePlan(){
      if (string.IsNullOrEmpty(_planJson)){ Log("No plan yet. Click Compile Plan first."); return; }
      var dir = Path.Combine("StickerDash_Status","Plans");
      Directory.CreateDirectory(dir);
      string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string path = Path.Combine(dir, "Plan_"+ts+".json");
      File.WriteAllText(path, _planJson);
      AssetDatabase.Refresh();
      Log("Saved plan: "+path);
    }

    // ---------- Run Plan (safe stub) ----------
    void RunPlan(){
      if (string.IsNullOrEmpty(_planJson)){ Log("No plan yet. Click Compile Plan first."); return; }

      // Try to create a Restore Point via your menu (non-blocking)
      TryExecuteMenu("Window/Aim2Pro/Tools/Create Restore Point");

      // Here we just log the steps; you can wire these to your kernel.
      // If you have a command bus or runner, call it here.
      Debug.Log("[A2P TrackGen v2] EXECUTION START");
      foreach (var line in _steps.Split(new[]{'\n'}, StringSplitOptions.RemoveEmptyEntries))
        Debug.Log("[Plan] " + line);
      Debug.Log("[A2P TrackGen v2] EXECUTION END");

      Log("Ran plan (logged steps). We can wire these calls to your kernel next.");
    }

    // ---------- Helpers ----------
    string Normalize(string s){
      s = s.ToLowerInvariant();
      s = Regex.Replace(s, @"[\u2012\u2013\u2014\u2212]", "-"); // fancy dashes → -
      s = s.Replace("°"," deg").Replace("º"," deg");
      s = Regex.Replace(s, @"\s+", " ").Trim();

      // synonyms
      var map = new Dictionary<string,string>{
        {"tiles missing","holes"},
        {"s bend", "s-bend"},
        {"s–bend","s-bend"},
        {"s—bend","s-bend"},
        {"to","-"}
      };
      foreach (var kv in map)
        s = Regex.Replace(s, @"\b"+Regex.Escape(kv.Key)+@"\b", kv.Value);
      return s;
    }

    int ExtractFirstInt(string s, string pattern){
      var m = Regex.Match(s, pattern);
      if (!m.Success) return 0;
      int v; return int.TryParse(m.Groups[1].Value, out v) ? v : 0;
    }
    int ExtractSecondInt(string s, string pattern){
      var m = Regex.Match(s, pattern);
      if (!m.Success || m.Groups.Count < 3) return 0;
      int v; return int.TryParse(m.Groups[2].Value, out v) ? v : 0;
    }
    string ExtractFirstGroup(string s, string pattern, int groupIndex){
      var m = Regex.Match(s, pattern);
      return m.Success && m.Groups.Count>groupIndex ? m.Groups[groupIndex].Value : "";
    }

    void TryExecuteMenu(string menuPath){
      try {
        if (!EditorApplication.ExecuteMenuItem(menuPath))
          Debug.Log("[A2P] Menu not found: "+menuPath);
      } catch (Exception ex) {
        Debug.LogWarning("[A2P] Menu exec failed: "+ex.Message);
      }
    }

    void Log(string msg){
      if (!string.IsNullOrEmpty(_log)) _log += "\n";
      _log += DateTime.Now.ToString("HH:mm:ss") + " — " + msg;
      Repaint();
    }
  }
}
#endif
CS

# Notes update
NOTES="$ROOT/StickerDash_Status/Notes/SD2_WorkingNotes.md"; mkdir -p "$(dirname "$NOTES")"
{
  echo ""
  echo "## Update $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Added Track Generator v2: NL → plan → preview/run (meters/deg default)."
  echo "- Menu: Window → Aim2Pro → Track Creator → Track Generator v2"
} >> "$NOTES"

echo "Wrote: Assets/StickerDash/AIGG/Editor/Track/TrackGeneratorV2.cs"
echo "Open Unity, let it recompile. Find it under Window → Aim2Pro → Track Creator → Track Generator v2"
