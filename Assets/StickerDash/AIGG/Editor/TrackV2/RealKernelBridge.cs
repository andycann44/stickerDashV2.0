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
        // Ensures we have a mesh GO with MeshFilter/Renderer/Collider; if not possible, creates a fresh one.
        private static void EnsureMeshCarrier(Transform root,
            out GameObject meshGO, out MeshFilter mf, out MeshRenderer mr, out MeshCollider mc)
        {
            meshGO = GameObject.Find("A2P_TrackMesh");
            if (!meshGO) meshGO = new GameObject("A2P_TrackMesh");
            meshGO.transform.SetParent(root, true);

            mf = meshGO.GetComponent<MeshFilter>()    ?? meshGO.AddComponent<MeshFilter>();
            mr = meshGO.GetComponent<MeshRenderer>()  ?? meshGO.AddComponent<MeshRenderer>();
            mc = meshGO.GetComponent<MeshCollider>()  ?? meshGO.AddComponent<MeshCollider>();

            // If something odd prevents adding components (e.g., locked prefab), fall back to a fresh GO.
            if (mf == null || mr == null || mc == null)
            {
                var fresh = new GameObject("A2P_TrackMesh_Auto");
                fresh.transform.SetParent(root, true);
                mf = fresh.AddComponent<MeshFilter>();
                mr = fresh.AddComponent<MeshRenderer>();
                mc = fresh.AddComponent<MeshCollider>();
                meshGO = fresh;
            }
        }
    {
        private static readonly Regex TileRx = new Regex(@"^tile_r(?<r>\d+)_c(?<c>\d+)$", RegexOptions.IgnoreCase);

        // ---------- utils ----------
        private static Transform FindOrCreateTrack()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go) { go = new GameObject("A2P_Track"); Undo.RegisterCreatedObjectUndo(go, "Create Track Root"); }
            return go.transform;
        }
        private static Transform FindTrack() => GameObject.Find("A2P_Track")?.transform ?? GameObject.Find("Track")?.transform;

        private static GameObject AnyTile(Transform root)
        {
            if (!root) return null;
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) return c.gameObject;
            return null;
        }
        private static Vector2 MeasureXZ(GameObject g)
        {
            if (!g) return new Vector2(1,1);
            var mf = g.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var b = mf.sharedMesh.bounds; var ls = g.transform.localScale;
                float x = Mathf.Abs(b.size.x * ls.x), z = Mathf.Abs(b.size.z * ls.z);
                if (x>1e-4f && z>1e-4f) return new Vector2(x,z);
            }
            var r = g.GetComponentInChildren<Renderer>();
            if (r) { var s = r.bounds.size; if (s.x>1e-4f && s.z>1e-4f) return new Vector2(s.x,s.z); }
            return new Vector2(1,1);
        }
        private static Dictionary<int,List<Transform>> GetRows(Transform root)
        {
            var rows = new Dictionary<int,List<Transform>>();
            if (!root) return rows;
            foreach (Transform c in root)
            {
                var m = TileRx.Match(c.name); if (!m.Success) continue;
                int r = int.Parse(m.Groups["r"].Value);
                (rows.TryGetValue(r, out var list) ? list : (rows[r]=new List<Transform>())).Add(c);
            }
            return rows;
        }
        private static Vector3 Centroid(List<Transform> ts){ if (ts==null||ts.Count==0) return Vector3.zero; var s=Vector3.zero; foreach(var t in ts)s+=t.position; return s/ts.Count; }

        // ------- generator -------
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

            // NEW: split minimum
            public float splitMinMeters = 0f;   // overrides fraction if > 0
            public float splitMinFrac   = 0.5f; // default: 50% of width
        }

        public static void GenerateScenarioFromPrompt(float lengthM, float widthM, string extras = "")
        {
            var root = FindOrCreateTrack();
            var any = AnyTile(root);
            var template = any ? Object.Instantiate(any) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            template.name = "tile_template_runtime";
            if (!any) template.transform.localScale = new Vector3(1f,0.2f,1f);

            var opt = ParseExtras(lengthM, widthM, extras ?? "");
            var sz  = MeasureXZ(template);
            if (sz.x<1e-4f || sz.y<1e-4f) sz = new Vector2(1,1);

            // contiguous grid so tiles touch
            int cols = Mathf.Max(2, Mathf.RoundToInt(opt.widthM  / sz.x) + 1);
            int rows = Mathf.Max(2, Mathf.RoundToInt(opt.lengthM / sz.y) + 1);
            float colPitch = sz.x, rowPitch = sz.y;
            float actualW = (cols-1)*colPitch, actualL = (rows-1)*rowPitch;

            // split timing (diverge -> plateau -> rejoin)
            int dStart = opt.split ? Mathf.RoundToInt(rows*0.30f) : int.MaxValue;
            int dLen   = opt.split ? Mathf.RoundToInt(rows*0.10f) : 0;
            int pLen   = opt.split ? Mathf.RoundToInt(rows*0.10f) : 0;
            int rLen   = opt.split ? dLen : 0;
            int dEnd = dStart+dLen, pEnd = dEnd+pLen, rEnd = pEnd+rLen;

            // NEW: min split requirement
            float minSplitMeters = (opt.splitMinMeters > 0f) ? opt.splitMinMeters : opt.splitMinFrac * actualW;
            float baseSep        = actualW + colPitch; // safe default (completely side-by-side with a 1-col gap)
            float branchSep      = Mathf.Max(baseSep, minSplitMeters); // enforce requested minimum

            // clear current tiles
            var toDel = new List<GameObject>();
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) toDel.Add(c.gameObject);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Generate Scenario");
            foreach (var g in toDel) Object.DestroyImmediate(g);

            var rng = new System.Random(opt.seed);
            float yaw = 0f;
            int   bendN = opt.simple? 20:12;
            float bendMax = opt.simple? Mathf.Min(opt.bendMaxDeg,12f):opt.bendMaxDeg;

            Vector3 center = Vector3.zero;
            for (int r = 1; r <= rows; r++)
            {
                if (opt.randomBends && r % bendN == 0)
                {
                    float lim = bendMax; if (opt.lowSpeedStart && r<rows*0.2f) lim *= 0.4f;
                    yaw += (float)((rng.NextDouble()*2-1) * lim);
                }
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var fwd = rot * Vector3.forward;
                var right = rot * Vector3.right;
                if (r==1) center = Vector3.zero; else center += fwd * rowPitch;
                center.y = opt.verticalAmp>0f ? Mathf.Sin(r*Mathf.PI/50f)*opt.verticalAmp : 0f;

                float half = (cols-1)*0.5f;
                // mainline row (contiguous)
                for (int c = 0; c < cols; c++)
                {
                    if (opt.missingPct>0 && rng.NextDouble()<opt.missingPct) continue;
                    var g = Object.Instantiate(template, root);
                    g.name = $"tile_r{r}_c{c}";
                    g.transform.position = center + right * ((c-half)*colPitch);
                    g.transform.rotation = rot;
                    g.transform.localScale = template.transform.localScale;
                }

                // branch offset
                if (opt.split)
                {
                    float off = 0f;
                    if      (r>=dStart && r<dEnd) off = Mathf.SmoothStep(0, branchSep, Mathf.InverseLerp(dStart,dEnd,r));
                    else if (r>=dEnd   && r<pEnd) off = branchSep;                            // plateau = at least min split
                    else if (r>=pEnd   && r<rEnd) off = Mathf.SmoothStep(branchSep, 0, Mathf.InverseLerp(pEnd,rEnd,r));

                    if (off>0f)
                    {
                        double miss = Mathf.Clamp01(opt.missingPct+0.05f); // branch worse to slow you down
                        for (int c = 0; c < cols; c++)
                        {
                            if (miss>0 && rng.NextDouble()<miss) continue;
                            var g = Object.Instantiate(template, root);
                            g.name = $"tile_r{r}_c{c}";
                            g.transform.position = center + right * ((c-half)*colPitch + off);
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
            var s = new Scenario { lengthM=L, widthM=W };
            e = (e ?? "").ToLowerInvariant();

            // holes
            var mMiss = Regex.Match(e, @"(\d+)\s*%.*?(tiles?|holes?|gaps?).*?missing?");
            if (mMiss.Success) s.missingPct = Mathf.Clamp01(int.Parse(mMiss.Groups[1].Value)/100f);

            // bends
            var mBend = Regex.Match(e, @"random\s+bends?.*?(\d+(?:\.\d+)?)\s*(deg|degree|degrees)");
            if (mBend.Success) { s.randomBends=true; s.bendMaxDeg=float.Parse(mBend.Groups[1].Value); }

            // split request
            if (Regex.IsMatch(e, @"\b(split|fork|branch)\b")) s.split=true;

            // NEW: split min — meters / % / “w” (fraction of width)
            var mSplitM = Regex.Match(e, @"(?:min\s*split|split\s*min|split\s*>=|at\s*least\s*split)\s*(\d+(?:\.\d+)?)\s*(m|meter|metre|meters|metres)\b");
            if (mSplitM.Success) s.splitMinMeters = float.Parse(mSplitM.Groups[1].Value);

            var mSplitPct = Regex.Match(e, @"(?:min\s*split|split\s*min|split\s*>=)\s*(\d+(?:\.\d+)?)\s*%");
            if (mSplitPct.Success) s.splitMinFrac = Mathf.Clamp01(float.Parse(mSplitPct.Groups[1].Value)/100f);

            var mSplitW = Regex.Match(e, @"(?:min\s*split|split\s*min|split\s*>=)\s*(\d+(?:\.\d+)?)\s*w\b");
            if (mSplitW.Success) s.splitMinFrac = Mathf.Clamp01(float.Parse(mSplitW.Groups[1].Value));

            // terrain/feel
            if (Regex.IsMatch(e, @"ups?\s*and\s*downs?|hills|slight up")) s.verticalAmp=Mathf.Max(s.verticalAmp,0.25f);
            if (e.Contains("low speed") || e.Contains("first level") || e.Contains("tutorial")) s.lowSpeedStart=true;
            if (e.Contains("simple") || e.Contains("first level")) s.simple=true;

            // seed
            var mSeed = Regex.Match(e, @"seed\s+(\d+)");
            if (mSeed.Success) s.seed=int.Parse(mSeed.Groups[1].Value);

            return s;
        }

        // ------- baker (no-gaps ribbon mesh) -------
        public static void BuildSplineFromTrack(float widthOverride=0f, float top=0f, float thickness=0.2f, bool replace=true)
        {
            var root = FindTrack(); if (!root) { Debug.LogWarning("[Kernel] No track root."); return; }
            var rows = GetRows(root); if (rows.Count<2) { Debug.LogWarning("[Kernel] Need rows."); return; }
            var keys = rows.Keys.OrderBy(k=>k).ToList();

            var centers = new List<Vector3>();
            Vector3? prev = null;
            foreach (var r in keys)
            {
                var ts = rows[r].OrderBy(t=>t.position.x).ToList();
                if (ts.Count==0) { centers.Add(prev??Vector3.zero); continue; }
                var pick = Centroid(ts);
                centers.Add(pick); prev = pick;
            }

            float width;
            {
                var first = rows[keys.First()].OrderBy(t=>t.position.x).ToList();
                width = (first.Count>=2) ? Mathf.Abs(first.Last().position.x - first.First().position.x) : 6f;
            }
            if (widthOverride>0f) width = widthOverride;
            float hw = width*0.5f;

            var rights = new List<Vector3>(centers.Count);
            for (int i=0;i<centers.Count;i++){
                Vector3 f = (i==0)? (centers[1]-centers[0]) : (i==centers.Count-1? centers[i]-centers[i-1] : (centers[i+1]-centers[i-1])*0.5f);
                f.y=0; if (f.sqrMagnitude<1e-6f) f=Vector3.forward;
                rights.Add(Quaternion.Euler(0,90,0)*f.normalized);
            }

            var lefts = new List<Vector3>(centers.Count);
            var rightsPts = new List<Vector3>(centers.Count);
            for (int i=0;i<centers.Count;i++){
                var c = centers[i] + new Vector3(0,top,0);
                var r = rights[i];
                lefts.Add(c - r*hw); rightsPts.Add(c + r*hw);
            }

            int n = centers.Count;
            var verts = new List<Vector3>(n*4);
            var uvs   = new List<Vector2>(n*4);
            var tris  = new List<int>((n-1)*6);

            float total=0; var seg=new float[n]; seg[0]=0;
            for (int i=1;i<n;i++){ seg[i]=Vector3.Distance(centers[i-1],centers[i]); total+=seg[i]; }
            float acc=0;

            for (int i=0;i<n;i++){
                if (i>0) acc += seg[i]/Mathf.Max(0.0001f,total);
                verts.Add(lefts[i]);      uvs.Add(new Vector2(0,acc));
                verts.Add(rightsPts[i]);  uvs.Add(new Vector2(1,acc));
                if (i<n-1){
                    int a=i*2;
                    tris.Add(a); tris.Add(a+1); tris.Add(a+2);
                    tris.Add(a+1); tris.Add(a+3); tris.Add(a+2);
                }
            }

            if (thickness>0.0001f){
                int baseTop=verts.Count;
                for (int i=0;i<n;i++){
                    verts.Add(lefts[i]+Vector3.down*thickness);      uvs.Add(new Vector2(0,uvs[i*2].y));
                    verts.Add(rightsPts[i]+Vector3.down*thickness);  uvs.Add(new Vector2(1,uvs[i*2+1].y));
                }
                for (int i=0;i<n-1;i++){
                    int a=baseTop+i*2;
                    tris.Add(a+2); tris.Add(a+1); tris.Add(a);
                    tris.Add(a+2); tris.Add(a+3); tris.Add(a+1);
                }
            }

            var meshGO = GameObject.Find("A2P_TrackMesh") ?? new GameObject("A2P_TrackMesh");
            meshGO.transform.SetParent(root, true);
            var mf = meshGO.GetComponent<MeshFilter>() ?? meshGO.AddComponent<MeshFilter>();
            var mr = meshGO.GetComponent<MeshRenderer>() ?? meshGO.AddComponent<MeshRenderer>();
            var mc = meshGO.GetComponent<MeshCollider>() ?? meshGO.AddComponent<MeshCollider>();

            var m = new Mesh { name="TrackRibbonMesh" };
            m.SetVertices(verts); m.SetUVs(0,uvs); m.SetTriangles(tris,0,true);
            m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
            mf.sharedMesh = m; mc.sharedMesh = m;
            if (!mr.sharedMaterial) mr.sharedMaterial = new Material(Shader.Find("Standard")) { name="A2P_Track_DefaultMat" };

            if (replace){
                var del = new List<GameObject>();
                foreach (Transform c in root) if (TileRx.IsMatch(c.name)) del.Add(c.gameObject);
                foreach (var g in del) Object.DestroyImmediate(g);
            }
            EditorUtility.SetDirty(meshGO); EditorUtility.SetDirty(root.gameObject);
            Debug.Log($"[Kernel] Built clean mesh (no gaps). verts={verts.Count}, tris={tris.Count/3}, replaceTiles={replace}");
        }

        // ------ v1-style ops (signatures used by rules) ------
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

            // estimate spacing by last two rows
            float stepGuess = 1f;
            if (rows.TryGetValue(lastRow-1, out var prevTiles))
                stepGuess = Vector3.Distance(Centroid(prevTiles), Centroid(lastTiles));
            if (steps <= 0) { if (arcLen > 0f) steps = Mathf.Max(1, Mathf.RoundToInt(arcLen / Mathf.Max(0.0001f, stepGuess))); else steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(deg))); }
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
