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
        private static readonly Regex TileRx = new Regex(@"^tile_r(?<r>\\d+)_c(?<c>\\d+)$", RegexOptions.IgnoreCase);

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

        // ------- simple contiguous generator (fork optional) -------
        private class Scenario
        {
            public float lengthM, widthM;
            public float missingPct = 0.10f;
            public float bendMaxDeg = 10f;
            public bool randomBends = true;
            public bool split = true;
            public float verticalAmp = 0.25f;
            public bool lowSpeedStart = true;
            public bool simple = true;
            public int seed = 12345;
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

            int cols = Mathf.Max(2, Mathf.RoundToInt(opt.widthM  / sz.x) + 1);
            int rows = Mathf.Max(2, Mathf.RoundToInt(opt.lengthM / sz.y) + 1);
            float colPitch = sz.x, rowPitch = sz.y;
            float actualW = (cols-1)*colPitch, actualL = (rows-1)*rowPitch;

            // Fork timing (diverge->plateau->rejoin)
            int dStart = opt.split ? Mathf.RoundToInt(rows*0.30f) : int.MaxValue;
            int dLen   = opt.split ? Mathf.RoundToInt(rows*0.10f) : 0;
            int pLen   = opt.split ? Mathf.RoundToInt(rows*0.10f) : 0;
            int rLen   = opt.split ? dLen : 0;
            int dEnd = dStart+dLen, pEnd = dEnd+pLen, rEnd = pEnd+rLen;
            float branchSep = actualW + colPitch;

            // Clear existing tiles
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
                for (int c = 0; c < cols; c++)
                {
                    if (opt.missingPct>0 && rng.NextDouble()<opt.missingPct) continue;
                    var g = Object.Instantiate(template, root);
                    g.name = $"tile_r{r}_c{c}";
                    g.transform.position = center + right * ((c-half)*colPitch);
                    g.transform.rotation = rot;
                    g.transform.localScale = template.transform.localScale;
                }

                if (opt.split)
                {
                    float off = 0f;
                    if      (r>=dStart && r<dEnd) off = Mathf.SmoothStep(0, branchSep, Mathf.InverseLerp(dStart,dEnd,r));
                    else if (r>=dEnd   && r<pEnd) off = branchSep;
                    else if (r>=pEnd   && r<rEnd) off = Mathf.SmoothStep(branchSep, 0, Mathf.InverseLerp(pEnd,rEnd,r));

                    if (off>0f)
                    {
                        double miss = Mathf.Clamp01(opt.missingPct+0.05f); // branch is worse
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
            Debug.Log($"[Kernel] Built contiguous ~{actualL:F1}m x {actualW:F1}m; rows={rows}, cols={cols}, split={(opt.split?"yes":"no")}.");
        }

        private static Scenario ParseExtras(float L, float W, string e)
        {
            var s = new Scenario { lengthM=L, widthM=W };
            e = (e ?? "").ToLowerInvariant();
            var mMiss = Regex.Match(e, @"(\\d+)\\s*%.*?(tiles?|holes?|gaps?).*?missing?");
            if (mMiss.Success) s.missingPct = Mathf.Clamp01(int.Parse(mMiss.Groups[1].Value)/100f);
            var mBend = Regex.Match(e, @"random\\s+bends?.*?(\\d+(?:\\.\\d+)?)\\s*(deg|degree|degrees)");
            if (mBend.Success) { s.randomBends=true; s.bendMaxDeg=float.Parse(mBend.Groups[1].Value); }
            if (Regex.IsMatch(e, @"\\b(split|fork|branch)\\b")) s.split=true;
            if (Regex.IsMatch(e, @"ups?\\s*and\\s*downs?|hills|slight up")) s.verticalAmp=Mathf.Max(s.verticalAmp,0.25f);
            if (e.Contains("low speed") || e.Contains("first level") || e.Contains("tutorial")) s.lowSpeedStart=true;
            if (e.Contains("simple") || e.Contains("first level")) s.simple=true;
            var mSeed = Regex.Match(e, @"seed\\s+(\\d+)");
            if (mSeed.Success) s.seed=int.Parse(mSeed.Groups[1].Value);
            return s;
        }

        // ------- clean ribbon baker (no gaps) -------
        public static void BuildSplineFromTrack(float widthOverride=0f, float top=0f, float thickness=0.2f, bool replace=true)
        {
            var root = FindTrack(); if (!root) { Debug.LogWarning("[Kernel] No track root."); return; }
            var rows = GetRows(root); if (rows.Count<2) { Debug.LogWarning("[Kernel] Need rows."); return; }
            var keys = rows.Keys.OrderBy(k=>k).ToList();

            // mainline centers
            var centers = new List<Vector3>();
            Vector3? prev = null;
            foreach (var r in keys)
            {
                var ts = rows[r];
                var sorted = ts.OrderBy(t=>t.position.x).ToList();
                if (sorted.Count==0) { centers.Add(prev??Vector3.zero); continue; }
                // naive: centroid of densest cluster (good enough for fork)
                Vector3 pick = Centroid(sorted);
                centers.Add(pick);
                prev = pick;
            }

            // width from first row
            float width;
            {
                var first = rows[keys.First()].OrderBy(t=>t.position.x).ToList();
                width = (first.Count>=2) ? Mathf.Abs(first.Last().position.x - first.First().position.x) : 6f;
            }
            if (widthOverride>0f) width = widthOverride; float hw = width*0.5f;

            // tangents/right
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

            // mesh
            int n = centers.Count;
            var verts = new List<Vector3>(n*4);
            var uvs   = new List<Vector2>(n*4);
            var tris  = new List<int>((n-1)*6);

            float total=0; var seg=new float[n]; seg[0]=0;
            for (int i=1;i<n;i++){ seg[i]=Vector3.Distance(centers[i-1],centers[i]); total+=seg[i]; }
            float acc=0;

            for (int i=0;i<n;i++){
                if (i>0) acc += seg[i]/Mathf.Max(0.0001f,total);
                verts.Add(lefts[i]);   uvs.Add(new Vector2(0,acc));
                verts.Add(rightsPts[i]); uvs.Add(new Vector2(1,acc));
                if (i<n-1){
                    int a=i*2;
                    tris.Add(a); tris.Add(a+1); tris.Add(a+2);
                    tris.Add(a+1); tris.Add(a+3); tris.Add(a+2);
                }
            }

            if (thickness>0.0001f){
                int baseTop=verts.Count;
                for (int i=0;i<n;i++){
                    verts.Add(lefts[i]+Vector3.down*thickness);   uvs.Add(new Vector2(0,uvs[i*2].y));
                    verts.Add(rightsPts[i]+Vector3.down*thickness); uvs.Add(new Vector2(1,uvs[i*2+1].y));
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
        public static void AppendStraight(float distance)
            => AppendStraight(distance, 1f);

        public static void AppendArc(string side, float deg)
            => AppendArc(side, deg, 0f, 0);
    }
}
#endif
