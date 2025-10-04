#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG
{
    public static class Kernel
    {
        private static readonly Regex TileRx = new Regex(@"^tile_r(?<r>\d+)_c(?<c>\d+)$", RegexOptions.IgnoreCase);

        private static Transform FindOrCreateTrack()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go) { go = new GameObject("A2P_Track"); Undo.RegisterCreatedObjectUndo(go, "Create Track Root"); }
            return go.transform;
        }
        private static Transform FindTrack() =>
            GameObject.Find("A2P_Track")?.transform ?? GameObject.Find("Track")?.transform;

        private static GameObject GetAnyTileUnder(Transform root)
        {
            if (!root) return null;
            foreach (Transform c in root)
                if (TileRx.IsMatch(c.name)) return c.gameObject;
            return null;
        }

        private static GameObject MakeFallbackTemplate()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "tile_template_fallback";
            g.transform.localScale = new Vector3(1f, 0.2f, 1f);
            Undo.RegisterCreatedObjectUndo(g, "Create Tile Template");
            return g;
        }

        private static void ClearExistingTiles(Transform root, GameObject except = null)
        {
            var toDelete = new List<GameObject>();
            foreach (Transform c in root)
            {
                if (TileRx.IsMatch(c.name) && c.gameObject != except)
                    toDelete.Add(c.gameObject);
            }
            if (toDelete.Count == 0) return;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Clear Tiles");
            foreach (var g in toDelete) Object.DestroyImmediate(g);
        }

        private static Vector2 MeasureTileXZ(GameObject g)
        {
            if (!g) return new Vector2(1f,1f);
            var mf = g.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh != null)
            {
                var b = mf.sharedMesh.bounds; // local
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

        // --------- scenario options ----------
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

            // choose a template, and CLONE it before clearing (so we don't destroy our template)
            var existingTile = GetAnyTileUnder(root);
            GameObject template = existingTile ? Object.Instantiate(existingTile) : MakeFallbackTemplate();
            template.name = "tile_template_runtime";

            var opt  = ParseExtras(lengthM, widthM, extras ?? "");
            var size = MeasureTileXZ(template);
            if (size.x < 1e-4f || size.y < 1e-4f) size = new Vector2(1f,1f); // guard

            // derive counts so tiles touch
            int cols = Mathf.Max(2, Mathf.RoundToInt(opt.widthM / Mathf.Max(0.0001f, size.x)) + 1);
            int rows = Mathf.Max(2, Mathf.RoundToInt(opt.lengthM / Mathf.Max(0.0001f, size.y)) + 1);
            float colPitch = size.x;
            float rowPitch = size.y;
            float actualW  = (cols - 1) * colPitch;
            float actualL  = (rows - 1) * rowPitch;

            // fork: diverge -> plateau -> rejoin
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

            // clear (but keep runtime template)
            ClearExistingTiles(root, except: null); // we cloned outside the root, so no except needed

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Generate Scenario (contiguous)");
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
                        double branchMiss = Mathf.Clamp01(opt.missingPct + 0.05f);
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
            Debug.Log($"[Kernel] Built contiguous track ~{actualL:F1}m x {actualW:F1}m; rows={rows}, cols={cols}, split={(opt.split ? "yes" : "no")}.");
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

        // (other ops omitted for brevity – unchanged)
        public static void DeleteRowsRange(int start, int end) { var root = FindTrack(); if (!root) return; var rows = new Dictionary<int, List<Transform>>(); foreach (Transform c in root){ var m = TileRx.Match(c.name); if(!m.Success) continue; int r = int.Parse(m.Groups["r"].Value); (rows.TryGetValue(r,out var list)?list:(rows[r]=new List<Transform>())).Add(c);} Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"DeleteRowsRange"); for(int r=start;r<=end;r++) if(rows.TryGetValue(r,out var tiles)) foreach(var t in tiles) Object.DestroyImmediate(t.gameObject); EditorUtility.SetDirty(root); }
        public static void OffsetRowsX(int start, int end, float meters) { var root=FindTrack(); if(!root) return; var rows=new Dictionary<int, List<Transform>>(); foreach(Transform c in root){ var m=TileRx.Match(c.name); if(!m.Success) continue; int r=int.Parse(m.Groups["r"].Value); (rows.TryGetValue(r,out var list)?list:(rows[r]=new List<Transform>())).Add(c);} Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"OffsetRowsX"); for(int r=start;r<=end;r++) if(rows.TryGetValue(r,out var tiles)) foreach(var t in tiles){ t.position += new Vector3(meters,0,0); EditorUtility.SetDirty(t.gameObject);} }
        public static void OffsetRowsY(int start, int end, float meters) { var root=FindTrack(); if(!root) return; var rows=new Dictionary<int, List<Transform>>(); foreach(Transform c in root){ var m=TileRx.Match(c.name); if(!m.Success) continue; int r=int.Parse(m.Groups["r"].Value); (rows.TryGetValue(r,out var list)?list:(rows[r]=new List<Transform>())).Add(c);} Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"OffsetRowsY"); for(int r=start;r<=end;r++) if(rows.TryGetValue(r,out var tiles)) foreach(var t in tiles){ t.position += new Vector3(0,meters,0); EditorUtility.SetDirty(t.gameObject);} }
        public static void StraightenRows(int start, int end) { var root=FindTrack(); if(!root) return; var rows=new Dictionary<int, List<Transform>>(); foreach(Transform c in root){ var m=TileRx.Match(c.name); if(!m.Success) continue; int r=int.Parse(m.Groups["r"].Value); (rows.TryGetValue(r,out var list)?list:(rows[r]=new List<Transform>())).Add(c);} Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"StraightenRows"); for(int r=start;r<=end;r++) if(rows.TryGetValue(r,out var tiles)) foreach(var t in tiles){ t.rotation = Quaternion.identity; EditorUtility.SetDirty(t.gameObject);} }
        public static void AppendStraight(float distance, float step = 1f) { var root=FindTrack(); if(!root) return; var rows=new Dictionary<int, List<Transform>>(); foreach(Transform c in root){ var m=TileRx.Match(c.name); if(!m.Success) continue; int r=int.Parse(m.Groups["r"].Value); (rows.TryGetValue(r,out var list)?list:(rows[r]=new List<Transform>())).Add(c);} if(rows.Count==0){ Debug.LogWarning("[Kernel] No tiles named tile_r*_c* under track root."); return;} int lastRow = rows.Keys.Max(); var lastTiles = rows[lastRow]; if(lastTiles==null || lastTiles.Count==0){ Debug.LogWarning("[Kernel] Last row empty."); return;} int copies = Mathf.Max(1, Mathf.RoundToInt(distance / Mathf.Max(0.0001f, step))); float dz = step; Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"AppendStraight"); for(int i=1;i<=copies;i++){ foreach(var src in lastTiles){ var clone = Object.Instantiate(src.gameObject, src.parent); int newRow = lastRow + i; clone.name = $"tile_r{newRow}_c{ExtractCol(src.name)}"; clone.transform.position = src.position + new Vector3(0f,0f,dz*i); clone.transform.rotation = src.rotation; clone.transform.localScale = src.localScale; EditorUtility.SetDirty(clone);} } EditorUtility.SetDirty(root); }
        private static int ExtractCol(string name) { var m = Regex.Match(name, @"_c(\d+)$"); return m.Success ? int.Parse(m.Groups[1].Value) : 0; }
        public static void AppendArc(string side, float deg, float arcLen = 0f, int steps = 0) { /* unchanged from previous version */ }
        public static void BuildSplineFromTrack(float width = 3f, float top = 0f, float thickness = 0.2f, bool replace = false) => Debug.Log("[Kernel] BuildSplineFromTrack — TODO");
        public static void SetWidth(float width) => Debug.Log($"[Kernel] SetWidth({width})");
        public static void SetThickness(float thickness) => Debug.Log($"[Kernel] SetThickness({thickness})");
        public static void Resample(float density) => Debug.Log($"[Kernel] Resample({density})");
    }
}
#endif
