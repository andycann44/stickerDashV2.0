#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.Track
{
    public static class QuickBuildFromClipboard
    {
        [MenuItem("Window/Aim2Pro/Test/Quick Build From Clipboard")]
        public static void BuildFromClipboard()
        {
            string json = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Clipboard is empty. Copy canonical JSON first.");
                return;
            }

            try
            {
                var spec = JsonUtility.FromJson<TrackSpec>(json);
                if (spec == null) throw new Exception("Could not parse JSON.");

                float tileStep = 1f; // 1 m forward per row
                float trackWidth = (spec.track != null && spec.track.width > 0) ? spec.track.width : 3f;

                var parent = new GameObject("Track_Built_" + DateTime.Now.ToString("HHmmss"));
                Undo.RegisterCreatedObjectUndo(parent, "Build Track");

                var rand = new System.Random(spec.track != null ? spec.track.seed : 0);
                float gapRowPercent = 0f;   // removes whole rows
                float gapTilePercent = 0f;  // removes individual tiles within a row
                bool safeZones = (spec.rules != null && spec.rules.safeZones);

                // Read gaps (strip priority first)
                if (spec.commands != null)
                {
                    foreach (var raw in spec.commands)
                    {
                        var cmd = StripPriority(raw);
                        if (cmd.StartsWith("AddGapsTile:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = cmd.Split(':');
                            if (parts.Length >= 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var p))
                                gapTilePercent = Mathf.Clamp(p, 0f, 95f);
                        }
                        else if (cmd.StartsWith("AddGaps:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = cmd.Split(':');
                            if (parts.Length >= 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var p))
                                gapRowPercent = Mathf.Clamp(p, 0f, 95f);
                        }
                    }
                }

                Vector3 pos = Vector3.zero;     // center of current row
                Vector3 fwd = Vector3.forward;  // forward direction
                float builtMeters = 0f;

                // Place one full WIDTH ROW made of 1 m tiles
                void PlaceRow(Vector3 center, Vector3 forward)
                {
                    bool inSafe = safeZones && (builtMeters < 5f);
                    // Row-level gap
                    if (!inSafe && gapRowPercent > 0f && rand.NextDouble() < (gapRowPercent / 100.0))
                    { builtMeters += tileStep; return; }

                    float tileUnit = 1f; // 1 m squares
                    int cols = Mathf.Max(1, Mathf.RoundToInt(trackWidth / tileUnit));
                    var right = Vector3.Cross(Vector3.up, forward).normalized;
                    float half = (cols - 1) * 0.5f;

                    for (int j = 0; j < cols; j++)
                    {
                        // Tile-level gap
                        if (!inSafe && gapTilePercent > 0f && rand.NextDouble() < (gapTilePercent / 100.0))
                            continue;

                        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = $"Tile_{Mathf.RoundToInt(builtMeters)}m_c{j}";
                        go.transform.SetParent(parent.transform, true);
                        go.transform.position = center + right * ((j - half) * tileUnit);
                        go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                        go.transform.localScale = new Vector3(tileUnit * 0.98f, 0.1f, tileStep); // tiny seam
                    }

                    builtMeters += tileStep;
                }

                // Default straight if no commands
                if (spec.commands == null || spec.commands.Length == 0)
                    spec.commands = new[] { "AppendStraight:" + ((spec.track != null && spec.track.length > 0) ? spec.track.length : 60) };

                // Priority sort (P##| prefix), then merge consecutive straights
                var prioritized = new List<CmdItem>();
                for (int i = 0; i < spec.commands.Length; i++)
                {
                    int pri; string clean;
                    GetPriorityAndClean(spec.commands[i], out pri, out clean);
                    prioritized.Add(new CmdItem { pri = pri, idx = i, cmd = clean });
                }
                prioritized.Sort((a, b) => a.pri != b.pri ? a.pri.CompareTo(b.pri) : a.idx.CompareTo(b.idx));

                var merged = new List<CmdItem>();
                for (int i = 0; i < prioritized.Count; i++)
                {
                    var cur = prioritized[i];
                    if (cur.cmd.StartsWith("AppendStraight:", StringComparison.OrdinalIgnoreCase) && merged.Count > 0)
                    {
                        var last = merged[merged.Count - 1];
                        if (last.cmd.StartsWith("AppendStraight:", StringComparison.OrdinalIgnoreCase))
                        {
                            float d1 = ParseDistance(last.cmd);
                            float d2 = ParseDistance(cur.cmd);
                            last.cmd = "AppendStraight:" + (d1 + d2).ToString(CultureInfo.InvariantCulture);
                            merged[merged.Count - 1] = last;
                            continue;
                        }
                    }
                    merged.Add(cur);
                }
                prioritized = merged;

                // Execute
                foreach (var item in prioritized)
                {
                    var cmd = item.cmd;
                    if (string.IsNullOrWhiteSpace(cmd)) continue;

                    if (cmd.StartsWith("SetWidth:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = cmd.Split(':');
                        if (parts.Length >= 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                            trackWidth = Mathf.Max(0.5f, w);
                    }
                    else if (cmd.StartsWith("AppendStraight", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = cmd.Split(':');
                        float dist = (parts.Length >= 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) ? d : 10f;
                        float step = (parts.Length >= 3 && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var s)) ? s : tileStep;

                        int steps = Mathf.Max(1, Mathf.RoundToInt(dist / step));
                        for (int i = 0; i < steps; i++) { pos += fwd * step; PlaceRow(pos, fwd); }
                    }
                    else if (cmd.StartsWith("AppendArc", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = cmd.Split(':');
                        if (parts.Length >= 3 && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var deg))
                        {
                            bool left = parts[1].Equals("left", StringComparison.OrdinalIgnoreCase);
                            int steps = (parts.Length >= 4 && int.TryParse(parts[3], out var st)) ? Mathf.Max(st, 1) : Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(deg) / 5f));
                            float stepDeg = deg / steps;
                            float stepLen = tileStep;

                            for (int i = 0; i < steps; i++)
                            {
                                float yaw = left ? stepDeg : -stepDeg;
                                fwd = Quaternion.Euler(0f, yaw, 0f) * fwd;
                                pos += fwd * stepLen;
                                PlaceRow(pos, fwd);
                            }
                        }
                    }
                    else if (cmd.StartsWith("SlopePercent:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = cmd.Split(':');
                        if (parts.Length >= 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var p))
                        {
                            int steps = 20;
                            float risePerStep = (p / 100f) * tileStep;
                            for (int i = 0; i < steps; i++)
                            { pos += fwd * tileStep + Vector3.up * risePerStep; PlaceRow(pos, fwd); }
                        }
                    }
                }

                Selection.activeGameObject = parent;
                Debug.Log("[QuickBuild] Built ~" + Mathf.RoundToInt(builtMeters) + " m, width " + trackWidth + " m, tiles " + parent.transform.childCount + ".");
            }
            catch (Exception ex)
            {
                Debug.LogError("QuickBuild failed: " + ex.Message);
                Debug.LogException(ex);
            }
        }

        // Helpers
        private static void GetPriorityAndClean(string raw, out int pri, out string clean)
        {
            pri = 100; clean = raw ?? "";
            if (string.IsNullOrWhiteSpace(raw)) return;
            raw = raw.Trim();
            if (raw.Length > 3 && (raw[0] == 'P' || raw[0] == 'p'))
            {
                int bar = raw.IndexOf('|');
                if (bar > 1 && bar <= 5)
                {
                    string num = raw.Substring(1, bar - 1);
                    if (int.TryParse(num, out var p)) { pri = Mathf.Clamp(p, 0, 999); clean = raw.Substring(bar + 1); return; }
                }
            }
            clean = raw;
        }
        private static string StripPriority(string raw) { GetPriorityAndClean(raw, out var _, out var clean); return clean; }
        private static float ParseDistance(string s)
        {
            var parts = s.Split(':');
            if (parts.Length >= 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return d;
            return 0f;
        }

        [Serializable] public class TrackSpec { public Track track; public Rules rules; public string[] commands; }
        [Serializable] public class Track { public int length; public int width; public int seed; public bool startCountdown; public bool addDartboards; }
        [Serializable] public class Rules { public bool safeZones; }

        private struct CmdItem { public int pri; public int idx; public string cmd; }
    }
}
#endif
