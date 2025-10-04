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

        // ---------- track root ----------
        private static Transform FindOrCreateTrack()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go) { go = new GameObject("A2P_Track"); Undo.RegisterCreatedObjectUndo(go, "Create Track Root"); }
            return go.transform;
        }
        private static Transform FindTrack() =>
            GameObject.Find("A2P_Track")?.transform ?? GameObject.Find("Track")?.transform;

        // ---------- util: measure/grid ----------
        private static GameObject GetAnyTileUnder(Transform root)
        {
            if (!root) return null;
            foreach (Transform c in root)
                if (TileRx.IsMatch(c.name)) return c.gameObject;
            return null;
        }
        private static Vector2 MeasureTileXZ(GameObject g)
        {
            if (!g) return new Vector2(1f, 1f);
            var mf = g.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh != null)
            {
                var b = mf.sharedMesh.bounds;
                var ls = g.transform.localScale;
                var x = Mathf.Abs(b.size.x * ls.x);
                var z = Mathf.Abs(b.size.z * ls.z);
                if (x > 1e-4f && z > 1e-4f) return new Vector2(x, z);
            }
            var r = g.GetComponentInChildren<Renderer>();
            if (r)
            {
                var s = r.bounds.size;
                if (s.x > 1e-4f && s.z > 1e-4f) return new Vector2(s.x, s.z);
            }
            return new Vector2(1f, 1f);
        }
        private static Dictionary<int,List<Transform>> GetRows(Transform root)
        {
            var rows = new Dictionary<int, List<Transform>>();
            if (!root) return rows;
            foreach (Transform c in root)
            {
                var m = TileRx.Match(c.name); if (!m.Success) continue;
                int r = int.Parse(m.Groups["r"].Value);
                (rows.TryGetValue(r, out var list) ? list : (rows[r] = new List<Transform>())).Add(c);
            }
            return rows;
        }
        private static Vector3 RowCentroid(List<Transform> ts)
        {
            if (ts == null || ts.Count == 0) return Vector3.zero;
            Vector3 s = Vector3.zero; foreach (var t in ts) s += t.position; return s / ts.Count;
        }

        // ---------- generator (contiguous tiles & fork) ----------
        private class Scenario
        {
            public float lengthM, widthM;
            public float missingPct = 0.10f;
            public float bendMaxDeg = 10f;
            public bool  randomBends = true;
            public bool  split;
            public float verticalAmp;
            public bool  lowSpeedStart;
            public bool  simple;
            public int   seed = 12345;
        }

        public static void GenerateScenarioFromPrompt(float lengthM, float widthM, string extras = "")
        {
            var root = FindOrCreateTrack();

            // template to copy (clone outside the root so we can clear safely)
            var any = GetAnyTileUnder(root);
            var template = any ? Object.Instantiate(any) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "tile_template_runtime";
            if (!any) template.transform.localScale = new Vector3(1f, 0.2f, 1f);

            var opt  = ParseExtras(lengthM, widthM, extras ?? "");
            var size = MeasureTileXZ(template);
            if (size.x < 1e-4f || size.y < 1e-4f) size = new Vector2(1f,1f);

            // derive counts so tiles TOUCH sideways and forward
            int cols = Mathf.Max(2, Mathf.RoundToInt(opt.widthM  / Mathf.Max(0.0001f, size.x)) + 1);
            int rows = Mathf.Max(2, Mathf.RoundToInt(opt.lengthM / Mathf.Max(0.0001f, size.y)) + 1);
            float colPitch = size.x;
            float rowPitch = size.y;
            float actualW  = (cols - 1) * colPitch;
            float actualL  = (rows - 1) * rowPitch;

            // fork staging
            int divergeStart   = opt.split ? Mathf.RoundToInt(rows * 0.30f) : int.MaxValue;
            int divergeLen     = opt.split ? Mathf.RoundToInt(rows * 0.10f) : 0;
            int plateauLen     = opt.split ? Mathf.RoundToInt(rows * 0.10f) : 0;
            int rejoinLen      = opt.split ? divergeLen : 0;
            int divergeEnd     = divergeStart + divergeLen;
            int plateauEnd     = divergeEnd   + plateauLen;
            int rejoinEnd      = plateauEnd   + rejoinLen;
            float branchSep    = actualW + colPitch;

            var rng     = new System.Random(opt.seed);
            float yaw   = 0f;
            int bendN   = opt.simple ? 20 : 12;
            float bendMax = opt.simple ? Mathf.Min(opt.bendMaxDeg, 12f) : opt.bendMaxDeg;

            // clear tiles
            var toDel = new List<GameObject>();
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) toDel.Add(c.gameObject);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Generate Scenario");
            foreach (var g in toDel) Object.DestroyImmediate(g);

            Vector3 center = Vector3.zero;
            for (int r = 1; r <= rows; r++)
            {
                if (opt.randomBends && r % bendN == 0)
                {
                    float limit = bendMax;
                    if (opt.lowSpeedStart && r < rows * 0.2f) limit *= 0.4f;
                    yaw += (float)((rng.NextDouble() * 2.0 - 1.0) * limit);
                }

                Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
                Vector3 fwd = rot * Vector3.forward;
                Vector3 right = rot * Vector3.right;
                if (r == 1) center = Vector3.zero; else center += fwd * rowPitch;
                center.y = opt.verticalAmp > 0f ? Mathf.Sin(r * Mathf.PI / 50f) * opt.verticalAmp : 0f;

                float half = (cols - 1) * 0.5f;
                for (int c = 0; c < cols; c++)
                {
                    if (opt.missingPct > 0f && rng.NextDouble() < opt.missingPct) continue;
                    var g = Object.Instantiate(template, root);
                    g.name = $"tile_r{r}_c{c}";
                    g.transform.position = center + right * ((c - half) * colPitch);
                    g.transform.rotation = rot;
                    g.transform.localScale = template.transform.localScale;
                    EditorUtility.SetDirty(g);
                }

                if (opt.split)
                {
                    float off = 0f;
                    if      (r >= divergeStart && r < divergeEnd)   off = Mathf.SmoothStep(0f, branchSep, Mathf.InverseLerp(divergeStart, divergeEnd, r));
                    else if (r >= divergeEnd   && r < plateauEnd)   off = branchSep;
                    else if (r >= plateauEnd   && r < rejoinEnd)    off = Mathf.SmoothStep(branchSep, 0f, Mathf.InverseLerp(plateauEnd, rejoinEnd, r));

                    if (off > 0f)
                    {
                        double branchMiss = Mathf.Clamp01(opt.missingPct + 0.05f); // branch worse to slow you down
                        for (int c = 0; c < cols; c++)
                        {
                            if (branchMiss > 0 && rng.NextDouble() < branchMiss) continue;
                            var g = Object.Instantiate(template, root);
                            g.name = $"tile_r{r}_c{c}";
                            g.transform.position = center + right * ((c - half) * colPitch + off);
                            g.transform.rotation = rot;
                            g.transform.localScale = template.transform.localScale;
                            EditorUtility.SetDirty(g);
                        }
                    }
                }
            }

            Object.DestroyImmediate(template);
            EditorUtility.SetDirty(root);
            Debug.Log($"[Kernel] Generated contiguous tiles ~{actualL:F1}m x {actualW:F1}m, rows={rows}, cols={cols}, split={(opt.split ? "yes" : "no")}.");
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

        // ---------- NO-GAPS MESH BAKER ----------
        /// <summary>
        /// Builds a continuous ribbon mesh from the current tile rows (mainline),
        /// adds MeshCollider, and (optionally) replaces the tiles.
        /// widthOverride<=0 means: infer width from columns.
        /// </summary>
        public static void BuildSplineFromTrack(float widthOverride = 0f, float top = 0f, float thickness = 0.2f, bool replace = true)
        {
            var root = FindTrack(); if (!root) { Debug.LogWarning("[Kernel] No track root found."); return; }
            var rowsDict = GetRows(root); if (rowsDict.Count < 2) { Debug.LogWarning("[Kernel] Need at least 2 rows to build mesh."); return; }
            var rowIdx = rowsDict.Keys.OrderBy(k => k).ToList();

            // pick mainline centers through any splits (follow nearest cluster)
            List<Vector3> centers = new();
            Vector3? prev = null;
            foreach (var r in rowIdx)
            {
                var tiles = rowsDict[r];
                // cluster by X gaps inside this row
                var sorted = tiles.OrderBy(t => t.position.x).ToList();
                if (sorted.Count == 0) { centers.Add(prev ?? Vector3.zero); continue; }

                List<List<Transform>> clusters = new() { new List<Transform> { sorted[0] } };
                for (int i = 1; i < sorted.Count; i++)
                {
                    float gap = Mathf.Abs(sorted[i].position.x - sorted[i-1].position.x);
                    // heuristic: big gap if > 2.5× median of local deltas (estimate via first few)
                    bool big = false;
                    if (i >= 2)
                    {
                        float g1 = Mathf.Abs(sorted[i-1].position.x - sorted[i-2].position.x);
                        float med = (g1 + gap) * 0.5f;
                        big = gap > med * 2.5f;
                    }
                    if (big) clusters.Add(new List<Transform>());
                    clusters[^1].Add(sorted[i]);
                }
                Vector3 pick = RowCentroid(clusters.OrderByDescending(c => c.Count).First());
                if (prev.HasValue)
                {
                    // choose cluster centroid nearest previous
                    var cands = clusters.Select(c => RowCentroid(c)).OrderBy(v => (v - prev.Value).sqrMagnitude).ToList();
                    pick = cands.First();
                }
                centers.Add(pick);
                prev = pick;
            }

            // compute forward/right per row
            List<Vector3> rights = new();
            for (int i = 0; i < centers.Count; i++)
            {
                Vector3 fwd =
                    (i == 0) ? (centers[1] - centers[0]) :
                    (i == centers.Count - 1) ? (centers[i] - centers[i-1]) :
                    ((centers[i+1] - centers[i-1]) * 0.5f);
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                rights.Add(Quaternion.Euler(0f, 90f, 0f) * fwd.normalized);
            }

            // infer width from columns in the first populated row
            float width;
            {
                var firstRow = rowsDict[rowIdx.First()];
                var cols = firstRow
                    .Select(t => new { t, m = TileRx.Match(t.name) })
                    .Where(x => x.m.Success)
                    .Select(x => new { t = x.t, c = int.Parse(x.m.Groups["c"].Value) })
                    .OrderBy(x => x.c).ToList();
                if (cols.Count >= 2)
                {
                    float avgPitch = 0f; int cnt = 0;
                    for (int i = 1; i < cols.Count; i++) { avgPitch += Vector3.Distance(cols[i-1].t.position, cols[i].t.position); cnt++; }
                    avgPitch = cnt > 0 ? (avgPitch / cnt) : 1f;
                    int spanCols = cols.Last().c - cols.First().c;
                    width = (spanCols > 0 ? spanCols * avgPitch : Mathf.Abs(cols.Last().t.position.x - cols.First().t.position.x));
                }
                else width = 6f;
            }
            if (widthOverride > 0f) width = widthOverride;
            float halfW = width * 0.5f;

            // build top edge strips
            List<Vector3> lefts = new(); List<Vector3> rightsPts = new();
            for (int i = 0; i < centers.Count; i++)
            {
                var c = centers[i] + new Vector3(0f, top, 0f);
                var rgt = rights[i];
                lefts.Add(c - rgt * halfW);
                rightsPts.Add(c + rgt * halfW);
            }

            // vertices/uvs for top surface
            int n = centers.Count;
            var verts = new List<Vector3>(n * 2);
            var uvs   = new List<Vector2>(n * 2);
            var tris  = new List<int>((n - 1) * 6);

            // v coordinate (length) normalized
            float total = 0f;
            var seg = new float[n]; seg[0] = 0;
            for (int i = 1; i < n; i++) { seg[i] = Vector3.Distance(centers[i-1], centers[i]); total += seg[i]; }
            float acc = 0f;

            for (int i = 0; i < n; i++)
            {
                if (i > 0) acc += seg[i] / Mathf.Max(0.0001f, total);
                verts.Add(lefts[i]);   uvs.Add(new Vector2(0f, acc));
                verts.Add(rightsPts[i]); uvs.Add(new Vector2(1f, acc));
                if (i < n - 1)
                {
                    int a = i * 2;
                    tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                    tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
                }
            }

            // sides & bottom (optional thickness)
            if (thickness > 0.0001f)
            {
                int baseTopVerts = verts.Count;
                // duplicate bottom
                for (int i = 0; i < n; i++)
                {
                    verts.Add(lefts[i]  + Vector3.down * thickness); uvs.Add(new Vector2(0f, uvs[i*2].y));
                    verts.Add(rightsPts[i] + Vector3.down * thickness); uvs.Add(new Vector2(1f, uvs[i*2+1].y));
                }
                // bottom triangles (flip winding)
                for (int i = 0; i < n - 1; i++)
                {
                    int a = baseTopVerts + i * 2;
                    tris.Add(a + 2); tris.Add(a + 1); tris.Add(a);
                    tris.Add(a + 2); tris.Add(a + 3); tris.Add(a + 1);
                }
                // left side
                int leftStart = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    verts.Add(lefts[i]);                   uvs.Add(new Vector2(0f, uvs[i*2].y));
                    verts.Add(lefts[i] + Vector3.down*thickness); uvs.Add(new Vector2(1f, uvs[i*2].y));
                    if (i < n - 1)
                    {
                        int a = leftStart + i * 2;
                        tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
                        tris.Add(a + 2); tris.Add(a + 3); tris.Add(a + 1);
                    }
                }
                // right side
                int rightStart = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    verts.Add(rightsPts[i]);                   uvs.Add(new Vector2(0f, uvs[i*2+1].y));
                    verts.Add(rightsPts[i] + Vector3.down*thickness); uvs.Add(new Vector2(1f, uvs[i*2+1].y));
                    if (i < n - 1)
                    {
                        int a = rightStart + i * 2;
                        tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                        tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
                    }
                }
            }

            // create/replace mesh object
            var meshGO = GameObject.Find("A2P_TrackMesh") ?? new GameObject("A2P_TrackMesh");
            meshGO.transform.SetParent(root, worldPositionStays: true);

            var mf = meshGO.GetComponent<MeshFilter>() ?? meshGO.AddComponent<MeshFilter>();
            var mr = meshGO.GetComponent<MeshRenderer>() ?? meshGO.AddComponent<MeshRenderer>();
            var mc = meshGO.GetComponent<MeshCollider>() ?? meshGO.AddComponent<MeshCollider>();

            var m = new Mesh { name = "TrackRibbonMesh" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0, true);
            m.RecalculateNormals();
            m.RecalculateTangents();
            m.RecalculateBounds();

            mf.sharedMesh = m;
            mc.sharedMesh = m;

            if (mr.sharedMaterial == null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.name = "A2P_Track_DefaultMat";
                mr.sharedMaterial = mat;
            }

            if (replace)
            {
                var toDel = new List<GameObject>();
                foreach (Transform c in root) if (TileRx.IsMatch(c.name)) toDel.Add(c.gameObject);
                foreach (var g in toDel) Object.DestroyImmediate(g);
            }

            EditorUtility.SetDirty(meshGO);
            EditorUtility.SetDirty(root.gameObject);
            Debug.Log($"[Kernel] Built clean mesh (no gaps). verts={verts.Count}, tris={tris.Count/3}, replaceTiles={replace}");
        }

        // ---------- other ops (unchanged) ----------
        public static void DeleteRowsRange(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "DeleteRowsRange");
            for (int r = start; r <= end; r++)
                if (rows.TryGetValue(r, out var tiles))
                    foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
            EditorUtility.SetDirty(root);
        }
        public static void OffsetRowsX(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsX");
            for (int r = start; r <= end; r++)
                if (rows.TryGetValue(r, out var tiles))
                    foreach (var t in tiles) { t.position += new Vector3(meters,0,0); EditorUtility.SetDirty(t.gameObject); }
        }
        public static void OffsetRowsY(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsY");
            for (int r = start; r <= end; r++)
                if (rows.TryGetValue(r, out var tiles))
                    foreach (var t in tiles) { t.position += new Vector3(0,meters,0); EditorUtility.SetDirty(t.gameObject); }
        }
        public static void StraightenRows(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "StraightenRows");
            for (int r = start; r <= end; r++)
                if (rows.TryGetValue(r, out var tiles))
                    foreach (var t in tiles) { t.rotation = Quaternion.identity; EditorUtility.SetDirty(t.gameObject); }
        }
        public static void AppendStraight(float distance, float step = 1f)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root);
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
                    var clone = Object.Instantiate(src.gameObject, src.parent);
                    int newRow = lastRow + i;
                    var m = Regex.Match(src.name, @"_c(\d+)$");
                    int col = m.Success ? int.Parse(m.Groups[1].Value) : 0;
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
            var rows = GetRows(root);
            if (rows.Count == 0) { Debug.LogWarning("[Kernel] No tiles to arc."); return; }
            int lastRow = rows.Keys.Max();
            if (!rows.TryGetValue(lastRow, out var lastTiles) || lastTiles.Count == 0) { Debug.LogWarning("[Kernel] Last row empty."); return; }

            float theta = Mathf.Abs(deg) * Mathf.Deg2Rad;
            if (theta < 1e-5f) { Debug.LogWarning("[Kernel] AppendArc: deg too small."); return; }
            // estimate step by row spacing
            float stepGuess = 1f;
            if (rows.TryGetValue(lastRow-1, out var prevTiles))
                stepGuess = Vector3.Distance(RowCentroid(prevTiles), RowCentroid(lastTiles));
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
                    var clone = Object.Instantiate(src.gameObject, src.parent);
                    clone.name = Regex.Replace(src.name, @"_r\d+_", m => $"_r{newRow}_");
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
    }
}
#endif
