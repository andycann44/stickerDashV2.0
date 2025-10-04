#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG
{
    /// Works with parent "A2P_Track" (or "Track") and child tiles named: tile_r{row}_c{col}
    public static class Kernel
    {
        private static readonly Regex TileRx = new Regex(@"^tile_r(?<r>\d+)_c(?<c>\d+)$", RegexOptions.IgnoreCase);

        // ---------- TRACK ROOT / TILES ----------
        private static Transform FindOrCreateTrack()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go) { go = new GameObject("A2P_Track"); Undo.RegisterCreatedObjectUndo(go, "Create Track Root"); }
            return go.transform;
        }
        private static Transform FindTrack() =>
            GameObject.Find("A2P_Track")?.transform ?? GameObject.Find("Track")?.transform;

        private static GameObject GetAnyTileTemplate(Transform root)
        {
            if (root)
                foreach (Transform c in root)
                    if (TileRx.IsMatch(c.name)) return c.gameObject;

            // fallback cube (1 x 0.2 x 1)
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "tile_template";
            g.transform.localScale = new Vector3(1f, 0.2f, 1f);
            Undo.RegisterCreatedObjectUndo(g, "Create Tile Template");
            return g;
        }

        private static void ClearExistingTiles(Transform root)
        {
            var toDelete = new List<GameObject>();
            foreach (Transform c in root)
                if (TileRx.IsMatch(c.name)) toDelete.Add(c.gameObject);
            if (toDelete.Count == 0) return;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Clear Tiles");
            foreach (var g in toDelete) Object.DestroyImmediate(g);
        }

        // Measure tile footprint (X width, Z depth) from mesh/collider (world-space aware enough for default tiles)
        private static Vector2 MeasureTileXZ(GameObject g)
        {
            var mf = g.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh != null)
            {
                var b = mf.sharedMesh.bounds; // local
                var ls = g.transform.localScale;
                return new Vector2(Mathf.Abs(b.size.x * ls.x), Mathf.Abs(b.size.z * ls.z));
            }
            var r = g.GetComponentInChildren<Renderer>();
            if (r) { var s = r.bounds.size; return new Vector2(s.x, s.z); }
            return new Vector2(1f, 1f);
        }

        private static Dictionary<int, List<Transform>> GetRowGroups(Transform trackRoot)
        {
            var rows = new Dictionary<int, List<Transform>>();
            if (!trackRoot) return rows;
            foreach (Transform c in trackRoot)
            {
                var m = TileRx.Match(c.name);
                if (!m.Success) continue;
                int r = int.Parse(m.Groups["r"].Value);
                (rows.TryGetValue(r, out var list) ? list : (rows[r] = new List<Transform>())).Add(c);
            }
            return rows;
        }
        private static bool TryParseRC(string name, out int r, out int c)
        {
            var m = TileRx.Match(name);
            if (m.Success) { r = int.Parse(m.Groups["r"].Value); c = int.Parse(m.Groups["c"].Value); return true; }
            r = c = 0; return false;
        }
        private static Vector3 RowCentroid(List<Transform> tiles)
        {
            if (tiles == null || tiles.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero; foreach (var t in tiles) sum += t.position; return sum / tiles.Count;
        }
        private static float GuessRowStep(Dictionary<int,List<Transform>> rows, int lastRow)
        {
            if (rows.TryGetValue(lastRow, out var b) && rows.TryGetValue(lastRow-1, out var a))
            {
                var da = Vector3.Distance(RowCentroid(a), RowCentroid(b));
                if (da > 0.01f) return da;
            }
            return 1f;
        }

        // ---------- SCENARIO ----------
        private class Scenario
        {
            public float lengthM, widthM;
            public float missingPct = 0.10f;  // holes only when we remove tiles
            public float bendMaxDeg = 10f;
            public bool  randomBends = true;
            public bool  split;               // fork then rejoin
            public float verticalAmp;
            public bool  lowSpeedStart;
            public bool  simple;
            public int   seed = 12345;
        }

        public static void GenerateScenarioFromPrompt(float lengthM, float widthM, string extras = "")
        {
            var root = FindOrCreateTrack();
            var opt  = ParseExtras(lengthM, widthM, extras ?? "");
            var tmpl = GetAnyTileTemplate(root);
            var tileSize = MeasureTileXZ(tmpl);    // exact footprint

            // --- derive rows/cols so tiles TOUCH ---
            int cols = Mathf.Max(2, Mathf.RoundToInt(opt.widthM / Mathf.Max(0.0001f, tileSize.x)) + 1);
            int rows = Mathf.Max(2, Mathf.RoundToInt(opt.lengthM / Mathf.Max(0.0001f, tileSize.y)) + 1);
            float colPitch = tileSize.x;  // contiguous sideways
            float rowPitch = tileSize.y;  // contiguous forward
            float actualW  = (cols - 1) * colPitch;
            float actualL  = (rows - 1) * rowPitch;

            // split: diverge → parallel → rejoin
            int divergeStart   = opt.split ? Mathf.RoundToInt(rows * 0.30f) : int.MaxValue;
            int divergeLen     = opt.split ? Mathf.RoundToInt(rows * 0.10f) : 0;
            int plateauLen     = opt.split ? Mathf.RoundToInt(rows * 0.10f) : 0;
            int rejoinLen      = opt.split ? divergeLen : 0;
            int divergeEnd     = divergeStart + divergeLen;
            int plateauEnd     = divergeEnd   + plateauLen;
            int rejoinEnd      = plateauEnd   + rejoinLen;
            float branchSep    = actualW + colPitch; // keep a full width gap between branches

            var rng     = new System.Random(opt.seed);
            float yaw   = 0f;
            int bendN   = opt.simple ? 20 : 12;
            float bendMax = opt.simple ? Mathf.Min(opt.bendMaxDeg, 12f) : opt.bendMaxDeg;

            // build continuous path
            ClearExistingTiles(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Generate Scenario (contiguous)");

            Vector3 center = Vector3.zero;
            for (int r = 1; r <= rows; r++)
            {
                // bends
                if (opt.randomBends && r % bendN == 0)
                {
                    float limit = bendMax;
                    if (opt.lowSpeedStart && r < rows * 0.2f) limit *= 0.4f;
                    yaw += (float)((rng.NextDouble() * 2.0 - 1.0) * limit);
                }

                // frame + advance
                Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
                Vector3 fwd = rot * Vector3.forward;
                Vector3 right = rot * Vector3.right;
                if (r == 1) center = Vector3.zero; else center += fwd * rowPitch;

                // vertical
                center.y = opt.verticalAmp > 0f ? Mathf.Sin(r * Mathf.PI / 50f) * opt.verticalAmp : 0f;

                // contiguous main row (0..cols-1 inclusive)
                float half = (cols - 1) * 0.5f;
                for (int c = 0; c < cols; c++)
                {
                    if (opt.missingPct > 0f && rng.NextDouble() < opt.missingPct) continue; // holes only when chosen
                    var g = Object.Instantiate(tmpl, root);
                    g.name = $"tile_r{r}_c{c}";
                    g.transform.position = center + right * ((c - half) * colPitch);
                    g.transform.rotation = rot;
                    g.transform.localScale = tmpl.transform.localScale;
                    EditorUtility.SetDirty(g);
                }

                // branch offset (diverge/plateau/rejoin)
                if (opt.split)
                {
                    float off = 0f;
                    if (r >= divergeStart && r < divergeEnd)
                    {
                        float t = Mathf.InverseLerp(divergeStart, divergeEnd, r);
                        off = Mathf.SmoothStep(0f, branchSep, t);
                    }
                    else if (r >= divergeEnd && r < plateauEnd)
                    {
                        off = branchSep;
                    }
                    else if (r >= plateauEnd && r < rejoinEnd)
                    {
                        float t = Mathf.InverseLerp(plateauEnd, rejoinEnd, r);
                        off = Mathf.SmoothStep(branchSep, 0f, t);
                    }

                    if (off > 0f)
                    {
                        double branchMiss = Mathf.Clamp01(opt.missingPct + 0.05f); // branch a bit worse
                        for (int c = 0; c < cols; c++)
                        {
                            if (branchMiss > 0 && rng.NextDouble() < branchMiss) continue;
                            var g = Object.Instantiate(tmpl, root);
                            g.name = $"tile_r{r}_c{c}";
                            g.transform.position = center + right * ((c - half) * colPitch + off);
                            g.transform.rotation = rot;
                            g.transform.localScale = tmpl.transform.localScale;
                            EditorUtility.SetDirty(g);
                        }
                    }
                }
            }

            if (tmpl.name == "tile_template") Object.DestroyImmediate(tmpl);
            EditorUtility.SetDirty(root);
            Debug.Log($"[Kernel] Built contiguous track ~{actualL:F1}m x {actualW:F1}m from tile size {tileSize.x:F2}x{tileSize.y:F2}m, rows={rows}, cols={cols}, split={(opt.split ? "yes" : "no")}.");
        }

        private static Scenario ParseExtras(float lengthM, float widthM, string extras)
        {
            var s = new Scenario { lengthM = lengthM, widthM = widthM };
            var e = (extras ?? string.Empty).ToLowerInvariant();

            var mMiss = Regex.Match(e, @"(\d+)\s*%.*?(tiles?|holes?|gaps?).*?missing?");
            if (mMiss.Success) s.missingPct = Mathf.Clamp01(int.Parse(mMiss.Groups[1].Value) / 100f);

            var mBend = Regex.Match(e, @"random\s+bends?.*?(\d+(?:\.\d+)?)\s*(deg|degree|degrees)");
            if (mBend.Success) { s.randomBends = true; s.bendMaxDeg = float.Parse(mBend.Groups[1].Value); }
            if (Regex.IsMatch(e, @"\b(split|fork|branch)\b")) s.split = true;
            if (Regex.IsMatch(e, @"ups?\s*and\s*downs?") || e.Contains("hills") || e.Contains("slight up"))
                s.verticalAmp = Mathf.Max(s.verticalAmp, 0.25f);
            if (e.Contains("low speed") || e.Contains("first level") || e.Contains("tutorial"))
                s.lowSpeedStart = true;
            if (e.Contains("simple") || e.Contains("first level")) s.simple = true;

            var mSeed = Regex.Match(e, @"seed\s+(\d+)");
            if (mSeed.Success) s.seed = int.Parse(mSeed.Groups[1].Value);

            return s;
        }

        // ---- other ops unchanged (append/split helpers for future) ----
        public static void DeleteRowsRange(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "DeleteRowsRange");
            for (int r = start; r <= end; r++) if (rows.TryGetValue(r, out var tiles)) foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
            EditorUtility.SetDirty(root);
        }
        public static void OffsetRowsX(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsX");
            for (int r = start; r <= end; r++) if (rows.TryGetValue(r, out var tiles)) foreach (var t in tiles) { t.position += new Vector3(meters,0,0); EditorUtility.SetDirty(t.gameObject); }
        }
        public static void OffsetRowsY(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsY");
            for (int r = start; r <= end; r++) if (rows.TryGetValue(r, out var tiles)) foreach (var t in tiles) { t.position += new Vector3(0,meters,0); EditorUtility.SetDirty(t.gameObject); }
        }
        public static void StraightenRows(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "StraightenRows");
            for (int r = start; r <= end; r++) if (rows.TryGetValue(r, out var tiles)) foreach (var t in tiles) { t.rotation = Quaternion.identity; EditorUtility.SetDirty(t.gameObject); }
        }
        public static void AppendStraight(float distance, float step = 1f)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            if (rows.Count == 0) { Debug.LogWarning("[Kernel] No tiles named tile_r*_c* under track root."); return; }
            int lastRow = rows.Keys.Max();
            var lastTiles = rows[lastRow]; if (lastTiles == null || lastTiles.Count == 0) { Debug.LogWarning("[Kernel] Last row empty."); return; }
            int copies = Mathf.Max(1, Mathf.RoundToInt(distance / Mathf.Max(0.0001f, step)));
            float dz = step;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "AppendStraight");
            for (int i = 1; i <= copies; i++)
            {
                foreach (var src in lastTiles)
                {
                    if (!TryParseRC(src.name, out _, out var col)) continue;
                    var clone = Object.Instantiate(src.gameObject, src.parent);
                    int newRow = lastRow + i;
                    clone.name = $"tile_r{newRow}_c{col}";
                    clone.transform.position = src.position + new Vector3(0f, 0f, dz * i);
                    clone.transform.rotation = src.rotation;
                    clone.transform.localScale = src.localScale;
                    EditorUtility.SetDirty(clone);
                }
            }
            EditorUtility.SetDirty(root);
        }
        public static void AppendArc(string side, float deg, float arcLen = 0f, int steps = 0)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            if (rows.Count == 0) { Debug.LogWarning("[Kernel] No tiles to arc."); return; }
            int lastRow = rows.Keys.Max();
            if (!rows.TryGetValue(lastRow, out var lastTiles) || lastTiles.Count == 0) { Debug.LogWarning("[Kernel] Last row empty."); return; }

            float theta = Mathf.Abs(deg) * Mathf.Deg2Rad;
            if (theta < 1e-5f) { Debug.LogWarning("[Kernel] AppendArc: deg too small."); return; }
            float stepGuess = GuessRowStep(rows, lastRow);
            if (steps <= 0) { if (arcLen > 0f) steps = Mathf.Max(1, Mathf.RoundToInt(arcLen / stepGuess)); else steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(deg))); }
            if (arcLen <= 0f) arcLen = steps * stepGuess;

            float radius = arcLen / theta;
            float sign = side.ToLowerInvariant() == "left" ? +1f : -1f;
            Vector3 centroid = RowCentroid(lastTiles);
            Vector3 pivot = centroid + (sign > 0 ? Vector3.left : Vector3.right) * radius;

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "AppendArc");
            float angleStep = sign * (theta / steps); // radians
            for (int i = 1; i <= steps; i++)
            {
                float ang = angleStep * i;
                int newRow = lastRow + i;

                foreach (var src in lastTiles)
                {
                    if (!TryParseRC(src.name, out _, out var col)) continue;
                    var clone = Object.Instantiate(src.gameObject, src.parent);
                    clone.name = $"tile_r{newRow}_c{col}";
                    Vector3 offset = src.position - pivot;
                    Quaternion rot = Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f);
                    clone.transform.position = pivot + rot * offset;
                    clone.transform.rotation = rot * src.rotation;
                    clone.transform.localScale = src.localScale;
                    EditorUtility.SetDirty(clone);
                }
            }
            EditorUtility.SetDirty(root);
        }
        public static void BuildSplineFromTrack(float width = 3f, float top = 0f, float thickness = 0.2f, bool replace = false)
            => Debug.Log("[Kernel] BuildSplineFromTrack — TODO");
        public static void SetWidth(float width) => Debug.Log($"[Kernel] SetWidth({width})");
        public static void SetThickness(float thickness) => Debug.Log($"[Kernel] SetThickness({thickness})");
        public static void Resample(float density) => Debug.Log($"[Kernel] Resample({density})");
    }
}
#endif
