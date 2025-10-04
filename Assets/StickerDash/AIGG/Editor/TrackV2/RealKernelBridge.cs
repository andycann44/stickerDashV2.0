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

        // ---------- utils ----------
        private static Transform FindOrCreateTrack()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go)
            {
                go = new GameObject("A2P_Track");
                Undo.RegisterCreatedObjectUndo(go, "Create Track Root");
            }
            return go.transform;
        }
        private static Transform FindTrack() =>
            GameObject.Find("A2P_Track")?.transform ?? GameObject.Find("Track")?.transform;

        private static GameObject AnyTile(Transform root)
        {
            if (!root) return null;
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) return c.gameObject;
            return null;
        }

        private static Vector2 MeasureXZ(GameObject g)
        {
            if (!g) return new Vector2(1, 1);
            var mf = g.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var b = mf.sharedMesh.bounds; var ls = g.transform.localScale;
                float x = Mathf.Abs(b.size.x * ls.x), z = Mathf.Abs(b.size.z * ls.z);
                if (x > 1e-4f && z > 1e-4f) return new Vector2(x, z);
            }
            var r = g.GetComponentInChildren<Renderer>();
            if (r) { var s = r.bounds.size; if (s.x > 1e-4f && s.z > 1e-4f) return new Vector2(s.x, s.z); }
            return new Vector2(1, 1);
        }

        private static Dictionary<int, List<Transform>> GetRows(Transform root)
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
        private static Vector3 Centroid(List<Transform> ts)
        {
            if (ts == null || ts.Count == 0) return Vector3.zero;
            Vector3 s = Vector3.zero; foreach (var t in ts) s += t.position; return s / ts.Count;
        }
        private static int ExtractCol(string name)
        {
            var m = Regex.Match(name, @"_c(\d+)$");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        // Always ensure our mesh carrier has the right components
        private static void EnsureMeshCarrier(Transform root,
            out GameObject meshGO, out MeshFilter mf, out MeshRenderer mr, out MeshCollider mc)
        {
            meshGO = GameObject.Find("A2P_TrackMesh");
            if (!meshGO) meshGO = new GameObject("A2P_TrackMesh");
            meshGO.transform.SetParent(root, true);

            mf = meshGO.GetComponent<MeshFilter>();   if (!mf) mf = meshGO.AddComponent<MeshFilter>();
            mr = meshGO.GetComponent<MeshRenderer>(); if (!mr) mr = meshGO.AddComponent<MeshRenderer>();
            mc = meshGO.GetComponent<MeshCollider>(); if (!mc) mc = meshGO.AddComponent<MeshCollider>();
        }

        // ------- scenario/options -------
        private class Scenario
        {
            public float lengthM, widthM;
            public float missingPct = 0.10f;
            public float bendMaxDeg = 10f;
            public bool  randomBends = true;
            public bool  split = true;
            public float verticalAmp = 0.25f;
            public bool  lowSpeedStart = true;
            public bool  simple = true;
            public int   seed = 12345;

            // split minimum
            public float splitMinMeters = 0f;   // overrides fraction if > 0
            public float splitMinFrac   = 0.5f; // default: 50% of width
        }

        
        /// <summary>
        /// Builds the "first level" you described:
        /// start → early holes → slight hill (you can see the track behind) →
        /// more holes → split (L/R, min 0.5*width) → merge → S-bends (±25°) →
        /// sharp right with an outside guard rail → straight with holes → finish line.
        /// </summary>
        public static void GenerateFirstLevelScenario(float widthM = 6f, float lengthApproxM = 320f, int seed = 12345)
        {
            var root = FindOrCreateTrack();

            // Pick a template (existing tile) or a unit cube fallback
            var any = AnyTile(root);
            var template = any ? Object.Instantiate(any) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "tile_template_runtime";
            if (!any) template.transform.localScale = new Vector3(1f, 0.2f, 1f);

            var sz  = MeasureXZ(template); if (sz.x < 1e-4f || sz.y < 1e-4f) sz = new Vector2(1,1);
            int cols = Mathf.Max(3, Mathf.RoundToInt(widthM / sz.x) + 1);
            float colPitch = sz.x, rowPitch = sz.y;

            // Helper: place one contiguous row (optionally with branch offset and hole pct)
            System.Action<int, Vector3, Quaternion, float, float> placeRow = (r, center, rot, holePct, branchOffset) =>
            {
                float half = (cols - 1) * 0.5f;
                // main line
                for (int c = 0; c < cols; c++)
                {
                    if (holePct > 0f && UnityEngine.Random.value < holePct) continue;
                    var g = Object.Instantiate(template, root);
                    g.name = $"tile_r{r}_c{c}";
                    g.transform.position = center + (rot * Vector3.right) * ((c - half) * colPitch);
                    g.transform.rotation = rot;
                    g.transform.localScale = template.transform.localScale;
                }
                // optional branch (offset to the right)
                if (branchOffset > 0.0001f)
                {
                    double hole2 = Mathf.Clamp01((float)holePct + 0.05f); // slightly more holes on branch
                    for (int c = 0; c < cols; c++)
                    {
                        if (hole2 > 0f && UnityEngine.Random.value < hole2) continue;
                        var g = Object.Instantiate(template, root);
                        g.name = $"tile_r{r}_c{c}";
                        g.transform.position = center + (rot * Vector3.right) * ((c - half) * colPitch + branchOffset);
                        g.transform.rotation = rot;
                        g.transform.localScale = template.transform.localScale;
                    }
                }
            };

            // Start clean
            var toDel = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) toDel.Add(c.gameObject);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Build First Level");
            foreach (var g in toDel) Object.DestroyImmediate(g);

            // Randoms
            var rng = new System.Random(seed);
            float Rand01() => (float)rng.NextDouble();

            // Running state
            int row = 1;
            float yawDeg = 0f;
            float y = 0f;
            Vector3 centerPos = Vector3.zero;

            // Derived values
            float widthWorld = (cols - 1) * colPitch;
            float minSplit = Mathf.Max(0.5f * widthWorld, 0.5f * widthM); // >= 0.5 * track width
            float splitSep  = Mathf.Max(widthWorld + colPitch, minSplit);

            // ---- 1) Start line + early gaps (gentle) ----
            int rowsStart = Mathf.RoundToInt(30f / rowPitch);
            for (int i=0;i<rowsStart;i++,row++)
            {
                var rot = Quaternion.Euler(0, yawDeg, 0);
                placeRow(row, centerPos, rot,  0.05f,  0f);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }

            // ---- 2) Slight hill and a gentle bend so you can see behind ----
            int rowsHill = Mathf.RoundToInt(40f / rowPitch);
            for (int i=0;i<rowsHill;i++,row++)
            {
                float t = (float)i / Mathf.Max(1, rowsHill-1);
                yawDeg = Mathf.Lerp(0f, 15f, t);           // slow gentle left
                y     = Mathf.Sin(t * Mathf.PI) * 2.0f;    // up then slightly down
                var rot = Quaternion.Euler(0, yawDeg, 0);
                var pos = new Vector3(centerPos.x, y, centerPos.z);
                placeRow(row, pos, rot,  0.10f,  0f);
                centerPos = pos + (rot * Vector3.forward) * rowPitch;
            }

            // ---- 3) Holes & dodging ----
            int rowsDodge = Mathf.RoundToInt(40f / rowPitch);
            for (int i=0;i<rowsDodge;i++,row++)
            {
                // small random yaw jiggle within ±10
                yawDeg = Mathf.Clamp(yawDeg + (Rand01()*2f-1f)*4f, -10f, 20f);
                var rot = Quaternion.Euler(0, yawDeg, 0);
                placeRow(row, centerPos, rot,  0.15f,  0f);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }

            // ---- 4) Split (L/R) then merge ----
            int diverge = Mathf.RoundToInt(20f / rowPitch);
            int plateau = Mathf.RoundToInt(30f / rowPitch);
            int converge= diverge;
            for (int i=0;i<diverge;i++,row++)
            {
                var rot = Quaternion.Euler(0, yawDeg, 0);
                float t = (float)i / Mathf.Max(1, diverge-1);
                float off = Mathf.SmoothStep(0f, splitSep, t);
                placeRow(row, centerPos, rot,  0.12f,  off);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }
            for (int i=0;i<plateau;i++,row++)
            {
                var rot = Quaternion.Euler(0, yawDeg, 0);
                placeRow(row, centerPos, rot,  0.12f,  splitSep);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }
            for (int i=0;i<converge;i++,row++)
            {
                var rot = Quaternion.Euler(0, yawDeg, 0);
                float t = (float)i / Mathf.Max(1, converge-1);
                float off = Mathf.SmoothStep(splitSep, 0f, t);
                placeRow(row, centerPos, rot,  0.12f,  off);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }

            // ---- 5) S-bends (±25°) ----
            int sRows = Mathf.RoundToInt(60f / rowPitch);
            for (int i=0;i<sRows;i++,row++)
            {
                float t = (float)i / Mathf.Max(1, sRows-1);
                // go left to +25 then back to -25 along a smooth curve
                yawDeg = Mathf.Sin(t * Mathf.PI) * 25f;
                var rot = Quaternion.Euler(0, yawDeg, 0);
                placeRow(row, centerPos, rot,  0.08f,  0f);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }

            // ---- 6) Sharp right with guard rail ----
            int turnRows = Mathf.RoundToInt(35f / rowPitch); // ~70° over these rows
            float yawStart = yawDeg;
            var railRoot = GameObject.Find("A2P_GuardRails") ?? new GameObject("A2P_GuardRails");
            railRoot.transform.SetParent(root, true);
            float guardWidth = Mathf.Min(0.4f * widthWorld, 0.6f); // narrow bumper
            for (int i=0;i<turnRows;i++,row++)
            {
                float t = (float)i / Mathf.Max(1, turnRows-1);
                yawDeg = Mathf.Lerp(yawStart, yawStart - 70f, t); // sharp right
                var rot = Quaternion.Euler(0, yawDeg, 0);
                placeRow(row, centerPos, rot,  0.08f,  0f);

                // Guard rail on outside (right side of the curve)
                // Put a slim cube along the outer edge of the row
                float half = (cols - 1) * 0.5f;
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"guard_r{row}";
                rail.transform.SetParent(railRoot.transform, true);
                // position at outside-right edge
                rail.transform.position = centerPos + (rot * Vector3.right) * ((half * colPitch) + guardWidth * 0.5f);
                rail.transform.rotation = rot;
                rail.transform.localScale = new Vector3(guardWidth, 1.2f, rowPitch * 1.05f);

                centerPos += (rot * Vector3.forward) * rowPitch;
            }

            // ---- 7) Straight with more missing tiles ----
            int straightRows = Mathf.RoundToInt(50f / rowPitch);
            for (int i=0;i<straightRows;i++,row++)
            {
                var rot = Quaternion.Euler(0, yawDeg, 0);
                placeRow(row, centerPos, rot,  0.18f,  0f);
                centerPos += (rot * Vector3.forward) * rowPitch;
            }

            // ---- 8) Finish line ----
            {
                var rot = Quaternion.Euler(0, yawDeg, 0);
                float half = (cols - 1) * 0.5f;
                var finish = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finish.name = "A2P_Finish";
                finish.transform.SetParent(root, true);
                finish.transform.position = centerPos; // across the width
                finish.transform.rotation = rot;
                finish.transform.localScale = new Vector3(widthWorld + colPitch, 0.1f, rowPitch * 0.8f);
            }

            Object.DestroyImmediate(template);
            Debug.Log("[Kernel] Built First Level preset: start→hill→holes→split→S-bends→sharp-right(rail)→straight→finish");
        }
    

        public static void GenerateScenarioFromPrompt(float lengthM, float widthM, string extras = "")
        {
            var root = FindOrCreateTrack();
            var any = AnyTile(root);
            var template = any ? Object.Instantiate(any) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "tile_template_runtime";
            if (!any) template.transform.localScale = new Vector3(1f, 0.2f, 1f);

            var opt = ParseExtras(lengthM, widthM, extras ?? "");
            var sz  = MeasureXZ(template);
            if (sz.x < 1e-4f || sz.y < 1e-4f) sz = new Vector2(1, 1);

            // contiguous grid so tiles touch
            int cols = Mathf.Max(2, Mathf.RoundToInt(opt.widthM  / sz.x) + 1);
            int rows = Mathf.Max(2, Mathf.RoundToInt(opt.lengthM / sz.y) + 1);
            float colPitch = sz.x, rowPitch = sz.y;
            float actualW = (cols - 1) * colPitch, actualL = (rows - 1) * rowPitch;

            // split timing (diverge -> plateau -> rejoin)
            int dStart = opt.split ? Mathf.RoundToInt(rows * 0.30f) : int.MaxValue;
            int dLen   = opt.split ? Mathf.RoundToInt(rows * 0.10f) : 0;
            int pLen   = opt.split ? Mathf.RoundToInt(rows * 0.10f) : 0;
            int rLen   = opt.split ? dLen : 0;
            int dEnd = dStart + dLen, pEnd = dEnd + pLen, rEnd = pEnd + rLen;

            // min split requirement
            float minSplitMeters = (opt.splitMinMeters > 0f) ? opt.splitMinMeters : opt.splitMinFrac * actualW;
            float baseSep        = actualW + colPitch; // at least a full extra-track gap
            float branchSep      = Mathf.Max(baseSep, minSplitMeters);

            // clear current tiles
            var toDel = new List<GameObject>();
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) toDel.Add(c.gameObject);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Generate Scenario");
            foreach (var g in toDel) Object.DestroyImmediate(g);

            var rng = new System.Random(opt.seed);
            float yaw = 0f;
            int   bendN = opt.simple ? 20 : 12;
            float bendMax = opt.simple ? Mathf.Min(opt.bendMaxDeg, 12f) : opt.bendMaxDeg;

            Vector3 center = Vector3.zero;
            for (int r = 1; r <= rows; r++)
            {
                if (opt.randomBends && r % bendN == 0)
                {
                    float lim = bendMax; if (opt.lowSpeedStart && r < rows * 0.2f) lim *= 0.4f;
                    yaw += (float)((rng.NextDouble() * 2 - 1) * lim);
                }
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var fwd = rot * Vector3.forward;
                var right = rot * Vector3.right;
                if (r == 1) center = Vector3.zero; else center += fwd * rowPitch;
                center.y = opt.verticalAmp > 0f ? Mathf.Sin(r * Mathf.PI / 50f) * opt.verticalAmp : 0f;

                float half = (cols - 1) * 0.5f;

                // mainline contiguous row
                for (int c = 0; c < cols; c++)
                {
                    if (opt.missingPct > 0 && rng.NextDouble() < opt.missingPct) continue;
                    var g = Object.Instantiate(template, root);
                    g.name = $"tile_r{r}_c{c}";
                    g.transform.position = center + right * ((c - half) * colPitch);
                    g.transform.rotation = rot;
                    g.transform.localScale = template.transform.localScale;
                }

                // branch offset
                if (opt.split)
                {
                    float off = 0f;
                    if      (r >= dStart && r < dEnd) off = Mathf.SmoothStep(0, branchSep, Mathf.InverseLerp(dStart, dEnd, r));
                    else if (r >= dEnd   && r < pEnd) off = branchSep;
                    else if (r >= pEnd   && r < rEnd) off = Mathf.SmoothStep(branchSep, 0, Mathf.InverseLerp(pEnd, rEnd, r));

                    if (off > 0f)
                    {
                        double miss = Mathf.Clamp01(opt.missingPct + 0.05f); // branch is slightly worse
                        for (int c = 0; c < cols; c++)
                        {
                            if (miss > 0 && rng.NextDouble() < miss) continue;
                            var g = Object.Instantiate(template, root);
                            g.name = $"tile_r{r}_c{c}";
                            g.transform.position = center + right * ((c - half) * colPitch + off);
                            g.transform.rotation = rot;
                            g.transform.localScale = template.transform.localScale;
                        }
                    }
                }
            }
            Object.DestroyImmediate(template);
            Debug.Log($"[Kernel] Built contiguous ~{actualL:F1}m x {actualW:F1}m; split min >= {minSplitMeters:F2}m, plateau={branchSep:F2}m.");
        }

        private static Scenario ParseExtras(float L, float W, string e)
        {
            var s = new Scenario { lengthM = L, widthM = W };
            e = (e ?? "").ToLowerInvariant();

            var mMiss = Regex.Match(e, @"(\d+)\s*%.*?(tiles?|holes?|gaps?).*?missing?");
            if (mMiss.Success) s.missingPct = Mathf.Clamp01(int.Parse(mMiss.Groups[1].Value) / 100f);

            var mBend = Regex.Match(e, @"random\s+bends?.*?(\d+(?:\.\d+)?)\s*(deg|degree|degrees)");
            if (mBend.Success) { s.randomBends = true; s.bendMaxDeg = float.Parse(mBend.Groups[1].Value); }

            if (Regex.IsMatch(e, @"\b(split|fork|branch)\b")) s.split = true;

            // split min — meters / % / fraction of width (“w”)
            var mSplitM = Regex.Match(e, @"(?:min\s*split|split\s*min|split\s*>=|at\s*least\s*split)\s*(\d+(?:\.\d+)?)\s*(m|meter|metre|meters|metres)\b");
            if (mSplitM.Success) s.splitMinMeters = float.Parse(mSplitM.Groups[1].Value);

            var mSplitPct = Regex.Match(e, @"(?:min\s*split|split\s*min|split\s*>=)\s*(\d+(?:\.\d+)?)\s*%");
            if (mSplitPct.Success) s.splitMinFrac = Mathf.Clamp01(float.Parse(mSplitPct.Groups[1].Value) / 100f);

            var mSplitW = Regex.Match(e, @"(?:min\s*split|split\s*min|split\s*>=)\s*(\d+(?:\.\d+)?)\s*w\b");
            if (mSplitW.Success) s.splitMinFrac = Mathf.Clamp01(float.Parse(mSplitW.Groups[1].Value));

            if (Regex.IsMatch(e, @"ups?\s*and\s*downs?|hills|slight up")) s.verticalAmp = Mathf.Max(s.verticalAmp, 0.25f);
            if (e.Contains("low speed") || e.Contains("first level") || e.Contains("tutorial")) s.lowSpeedStart = true;
            if (e.Contains("simple") || e.Contains("first level")) s.simple = true;

            var mSeed = Regex.Match(e, @"seed\s+(\d+)");
            if (mSeed.Success) s.seed = int.Parse(mSeed.Groups[1].Value);

            return s;
        }

        // ------- clean ribbon baker (no gaps) -------
        public static void BuildSplineFromTrack(float widthOverride = 0f, float top = 0f, float thickness = 0.2f, bool replace = true)
        {
            var root = FindTrack(); if (!root) { Debug.LogWarning("[Kernel] No track root."); return; }
            var rows = GetRows(root); if (rows.Count < 2) { Debug.LogWarning("[Kernel] Need rows."); return; }
            var keys = rows.Keys.OrderBy(k => k).ToList();

            // mainline centers
            var centers = new List<Vector3>();
            Vector3? prev = null;
            foreach (var r in keys)
            {
                var ts = rows[r].OrderBy(t => t.position.x).ToList();
                if (ts.Count == 0) { centers.Add(prev ?? Vector3.zero); continue; }
                centers.Add(Centroid(ts));
                prev = centers[^1];
            }

            // width from first row (fallback 6m)
            float width;
            {
                var first = rows[keys.First()].OrderBy(t => t.position.x).ToList();
                width = (first.Count >= 2) ? Mathf.Abs(first.Last().position.x - first.First().position.x) : 6f;
            }
            if (widthOverride > 0f) width = widthOverride;
            float hw = width * 0.5f;

            // tangents/right
            var rights = new List<Vector3>(centers.Count);
            for (int i = 0; i < centers.Count; i++)
            {
                Vector3 f = (i == 0)
                    ? (centers[1] - centers[0])
                    : (i == centers.Count - 1 ? centers[i] - centers[i - 1] : (centers[i + 1] - centers[i - 1]) * 0.5f);
                f.y = 0; if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
                rights.Add(Quaternion.Euler(0, 90, 0) * f.normalized);
            }

            var lefts = new List<Vector3>(centers.Count);
            var rightsPts = new List<Vector3>(centers.Count);
            for (int i = 0; i < centers.Count; i++)
            {
                var c = centers[i] + new Vector3(0, top, 0);
                var rgt = rights[i];
                lefts.Add(c - rgt * hw);
                rightsPts.Add(c + rgt * hw);
            }

            // mesh (top strip)
            int n = centers.Count;
            var verts = new List<Vector3>(n * 4);
            var uvs   = new List<Vector2>(n * 4);
            var tris  = new List<int>((n - 1) * 6);

            float total = 0; var seg = new float[n]; seg[0] = 0;
            for (int i = 1; i < n; i++) { seg[i] = Vector3.Distance(centers[i - 1], centers[i]); total += seg[i]; }
            float acc = 0;

            for (int i = 0; i < n; i++)
            {
                if (i > 0) acc += seg[i] / Mathf.Max(0.0001f, total);
                verts.Add(lefts[i]);      uvs.Add(new Vector2(0, acc));
                verts.Add(rightsPts[i]);  uvs.Add(new Vector2(1, acc));
                if (i < n - 1)
                {
                    int a = i * 2;
                    tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                    tris.Add(a + 1); tris.Add(a + 3); tris.Add(a + 2);
                }
            }

            // simple bottom (thickness)
            if (thickness > 0.0001f)
            {
                int baseTop = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    verts.Add(lefts[i] + Vector3.down * thickness);     uvs.Add(new Vector2(0, uvs[i * 2].y));
                    verts.Add(rightsPts[i] + Vector3.down * thickness); uvs.Add(new Vector2(1, uvs[i * 2 + 1].y));
                }
                for (int i = 0; i < n - 1; i++)
                {
                    int a = baseTop + i * 2;
                    tris.Add(a + 2); tris.Add(a + 1); tris.Add(a);
                    tris.Add(a + 2); tris.Add(a + 3); tris.Add(a + 1);
                }
            }

            EnsureMeshCarrier(root, out var meshGO, out var mf, out var mr, out var mc);

            var m = mf.sharedMesh; if (m == null) m = new Mesh();
            else m.Clear();
            m.name = "TrackRibbonMesh";
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0, true);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            mf.sharedMesh = m; mc.sharedMesh = m;
            if (!mr.sharedMaterial) mr.sharedMaterial = new Material(Shader.Find("Standard")) { name = "A2P_Track_DefaultMat" };

            if (replace)
            {
                var del = new List<GameObject>();
                foreach (Transform c in root) if (TileRx.IsMatch(c.name)) del.Add(c.gameObject);
                foreach (var g in del) Object.DestroyImmediate(g);
            }
            EditorUtility.SetDirty(meshGO); EditorUtility.SetDirty(root.gameObject);
            Debug.Log($"[Kernel] Built clean mesh (no gaps). verts={verts.Count}, tris={tris.Count / 3}, replaceTiles={replace}");
        }

        // ------ v1-style ops (used by rules) ------
        
        /// <summary>
        /// Bake a mesh that covers ONLY the tiles that exist, so missing tiles remain as holes.
        /// Uses per-row trapezoids to keep clean edges on curves. Adds MeshCollider.
        /// </summary>
        
        /// <summary>
        /// Bake a mesh that covers ONLY existing tiles, so missing tiles remain as holes.
        /// Uses per-row trapezoids to keep clean edges on curves. Adds MeshCollider.
        /// </summary>
        public static void BuildMeshFromTilesPreserveHoles(float thickness = 0.2f, bool replace = true)
        {
            var root = FindTrack(); if (!root) { Debug.LogWarning("[Kernel] No track root."); return; }
            var rows = GetRows(root); if (rows.Count < 1) { Debug.LogWarning("[Kernel] No tiles found."); return; }

            // Collect present (row,col) indices
            var rowKeys = new List<int>(rows.Keys); rowKeys.Sort();
            var present = new HashSet<string>(); // key = "row:col"
            int minC = int.MaxValue, maxC = int.MinValue;

            foreach (var rk in rowKeys)
            {
                foreach (var t in rows[rk])
                {
                    var match  = System.Text.RegularExpressions.Regex.Match(t.name, @"_r(\d+)_c(\d+)$|^tile_r(\d+)_c(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (!match.Success)
                    {
                        var match2 = System.Text.RegularExpressions.Regex.Match(t.name, @"r(\d+)_c(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match2.Success) match = match2;
                    }

                    int cIdx = 0;
                    if (match.Success)
                    {
                        var g2 = match.Groups[2]; var g4 = match.Groups[4];
                        cIdx = int.Parse(g2.Success ? g2.Value : (g4.Success ? g4.Value : "0"));
                    }
                    present.Add(rk + ":" + cIdx);
                    if (cIdx < minC) minC = cIdx;
                    if (cIdx > maxC) maxC = cIdx;
                }
            }
            if (minC == int.MaxValue) { Debug.LogWarning("[Kernel] No tile indices parsed."); return; }
            int colCount = maxC - minC + 1;

            // Estimate column pitch from consecutive columns in any row
            float colPitch = 1f;
            bool gotPitch = false;
            foreach (var rk in rowKeys)
            {
                var list = rows[rk];
                var byCol = new List<(int c, Transform t)>();
                foreach (var t in list)
                {
                    var mcMatch = System.Text.RegularExpressions.Regex.Match(t.name, @"_c(\d+)$");
                    if (mcMatch.Success) byCol.Add((int.Parse(mcMatch.Groups[1].Value), t));
                }
                byCol.Sort((a,b)=>a.c.CompareTo(b.c));
                for (int i = 1; i < byCol.Count; i++)
                {
                    if (byCol[i].c == byCol[i-1].c + 1)
                    {
                        colPitch = Vector3.Distance(byCol[i].t.position, byCol[i-1].t.position);
                        gotPitch = true; break;
                    }
                }
                if (gotPitch) break;
            }
            if (!gotPitch)
            {
                var any = AnyTile(root);
                var sz  = MeasureXZ(any);
                colPitch = (sz.x > 1e-4f) ? sz.x : 1f;
            }
            float halfCol   = colPitch * 0.5f;
            float halfIndex = (colCount - 1) * 0.5f;

            // Row centers and lateral vectors
            var centers = new List<Vector3>();
            foreach (var rk in rowKeys) centers.Add(Centroid(rows[rk]));
            var rights = new List<Vector3>();
            for (int i=0;i<centers.Count;i++)
            {
                Vector3 fwd = (i==0) ? (centers[1]-centers[0])
                                     : (i==centers.Count-1 ? (centers[i]-centers[i-1]) : (centers[i+1]-centers[i-1])*0.5f);
                fwd.y = 0; if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                rights.Add(Quaternion.Euler(0,90,0) * fwd.normalized);
            }

            // Boundaries between rows (E) and their laterals (L)
            int n = centers.Count;
            var E = new List<Vector3>(n+1);
            var L = new List<Vector3>(n+1);
            for (int i=0;i<=n;i++)
            {
                if (i==0)
                {
                    Vector3 f = centers[1]-centers[0]; f.y=0;
                    E.Add(centers[0] - f.normalized * (f.magnitude*0.5f));
                    L.Add(rights[0]);
                }
                else if (i==n)
                {
                    Vector3 f = centers[n-1]-centers[n-2]; f.y=0;
                    E.Add(centers[n-1] + f.normalized * (f.magnitude*0.5f));
                    L.Add(rights[n-1]);
                }
                else
                {
                    E.Add( (centers[i-1] + centers[i]) * 0.5f );
                    Vector3 rL = rights[i-1] + rights[i];
                    if (rL.sqrMagnitude < 1e-6f) rL = rights[i-1];
                    L.Add(rL.normalized);
                }
            }

            var verts = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var tris  = new List<int>();

            // Build trapezoids per present tile
            for (int ri=0; ri<n; ri++)
            {
                for (int c=minC; c<=maxC; c++)
                {
                    if (!present.Contains(rowKeys[ri]+":"+c)) continue;

                    float offset = ((c - minC) - halfIndex) * colPitch;

                    Vector3 topC = E[ri];      Vector3 topR = L[ri];
                    Vector3 botC = E[ri+1];    Vector3 botR = L[ri+1];

                    Vector3 TL = topC + topR * (offset - halfCol);
                    Vector3 TR = topC + topR * (offset + halfCol);
                    Vector3 BL = botC + botR * (offset - halfCol);
                    Vector3 BR = botC + botR * (offset + halfCol);

                    int vi = verts.Count;
                    float v0 = (float)ri / Mathf.Max(1, n-1);
                    float v1 = (float)(ri+1) / Mathf.Max(1, n-1);
                    verts.Add(TL); uvs.Add(new Vector2(0, v0));
                    verts.Add(TR); uvs.Add(new Vector2(1, v0));
                    verts.Add(BL); uvs.Add(new Vector2(0, v1));
                    verts.Add(BR); uvs.Add(new Vector2(1, v1));

                    tris.Add(vi+0); tris.Add(vi+1); tris.Add(vi+2);
                    tris.Add(vi+1); tris.Add(vi+3); tris.Add(vi+2);

                    if (thickness > 0.0001f)
                    {
                        int vj = verts.Count;
                        verts.Add(TL + Vector3.down*thickness); uvs.Add(new Vector2(0,0));
                        verts.Add(TR + Vector3.down*thickness); uvs.Add(new Vector2(1,0));
                        verts.Add(BL + Vector3.down*thickness); uvs.Add(new Vector2(0,1));
                        verts.Add(BR + Vector3.down*thickness); uvs.Add(new Vector2(1,1));

                        // bottom
                        tris.Add(vj+2); tris.Add(vj+1); tris.Add(vj+0);
                        tris.Add(vj+2); tris.Add(vj+3); tris.Add(vj+1);

                        // sides
                        tris.Add(vi+0); tris.Add(vj+0); tris.Add(vi+2);
                        tris.Add(vj+0); tris.Add(vj+2); tris.Add(vi+2);

                        tris.Add(vi+1); tris.Add(vi+3); tris.Add(vj+1);
                        tris.Add(vi+3); tris.Add(vj+3); tris.Add(vj+1);

                        tris.Add(vi+0); tris.Add(vi+1); tris.Add(vj+0);
                        tris.Add(vi+1); tris.Add(vj+1); tris.Add(vj+0);

                        tris.Add(vi+2); tris.Add(vj+2); tris.Add(vi+3);
                        tris.Add(vj+2); tris.Add(vj+3); tris.Add(vi+3);
                    }
                }
            }

            EnsureMeshCarrier(root, out var meshGO, out var mf, out var mr, out var mc);
            var mesh = mf.sharedMesh; if (mesh == null) mesh = new Mesh(); else mesh.Clear();
            mesh.name = "TrackMesh_PreserveHoles";
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            mf.sharedMesh = mesh; mc.sharedMesh = mesh;
            if (!mr.sharedMaterial) mr.sharedMaterial = new Material(Shader.Find("Standard")) { name = "A2P_Track_DefaultMat" };

            if (replace)
            {
                var del = new List<GameObject>();
                foreach (Transform c in root) if (TileRx.IsMatch(c.name)) del.Add(c.gameObject);
                foreach (var g in del) Object.DestroyImmediate(g);
            }
            EditorUtility.SetDirty(meshGO); EditorUtility.SetDirty(root.gameObject);
            Debug.Log($"[Kernel] Baked mesh preserving holes. verts={verts.Count}, tris={tris.Count/3}, replaceTiles={replace}");
        }


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
                    foreach (var t in tiles) { t.position += new Vector3(meters, 0, 0); EditorUtility.SetDirty(t.gameObject); }
        }

        public static void OffsetRowsY(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsY");
            for (int r = start; r <= end; r++)
                if (rows.TryGetValue(r, out var tiles))
                    foreach (var t in tiles) { t.position += new Vector3(0, meters, 0); EditorUtility.SetDirty(t.gameObject); }
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
                    int col = ExtractCol(src.name);
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

            // estimate spacing by last two rows
            float stepGuess = 1f;
            if (rows.TryGetValue(lastRow - 1, out var prevTiles))
                stepGuess = Vector3.Distance(Centroid(prevTiles), Centroid(lastTiles));
            if (steps <= 0)
            {
                if (arcLen > 0f) steps = Mathf.Max(1, Mathf.RoundToInt(arcLen / Mathf.Max(0.0001f, stepGuess)));
                else steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(deg)));
            }
            if (arcLen <= 0f) arcLen = steps * stepGuess;

            float radius = arcLen / theta;
            float sign = side.ToLowerInvariant() == "left" ? +1f : -1f;
            Vector3 centroid = Centroid(lastTiles);
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
                    int col = ExtractCol(src.name);
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
    }
}
#endif
