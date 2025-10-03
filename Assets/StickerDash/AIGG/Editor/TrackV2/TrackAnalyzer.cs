#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Aim2Pro.AIGG.TrackV2
{
    /// Analyzer + top-down snapshot for A2P_Track with tiles named: tile_r{row}_c{col}
    public static class TrackAnalyzer
    {
        static readonly Regex TileRx = new Regex(@"^tile_r(?<r>\d+)_c(?<c>\d+)$", RegexOptions.IgnoreCase);

        [MenuItem("Window/Aim2Pro/Track Creator/Analyze Track v2")]
        public static void AnalyzeMenu() => AnalyzeAndWriteReport();

        [MenuItem("Window/Aim2Pro/Track Creator/Top-Down Snapshot v2")]
        public static void SnapshotMenu() => SaveTopDownSnapshot();

        // ---------- Core finders ----------
        static Transform FindTrackRoot()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (go) return go.transform;

            // Heuristic: any root with multiple tile_r?_c? children
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                int seen = 0;
                foreach (Transform c in root.transform)
                    if (TileRx.IsMatch(c.name)) { seen++; if (seen >= 3) return root.transform; }
            }
            Debug.LogError("[TrackAnalyzer] Track root not found (expected 'A2P_Track' or 'Track').");
            return null;
        }

        static Dictionary<int, List<Transform>> GetRows(Transform trackRoot, out int globalMinCol, out int globalMaxCol)
        {
            var rows = new Dictionary<int, List<Transform>>();
            globalMinCol = int.MaxValue; globalMaxCol = int.MinValue;

            if (!trackRoot) return rows;
            foreach (Transform c in trackRoot)
            {
                var m = TileRx.Match(c.name);
                if (!m.Success) continue;
                int r = int.Parse(m.Groups["r"].Value);
                int col = int.Parse(m.Groups["c"].Value);
                if (!rows.TryGetValue(r, out var list)) rows[r] = list = new List<Transform>();
                list.Add(c);
                if (col < globalMinCol) globalMinCol = col;
                if (col > globalMaxCol) globalMaxCol = col;
            }
            if (globalMinCol == int.MaxValue) { globalMinCol = 0; globalMaxCol = -1; }
            return rows;
        }

        // ---------- Metrics helpers ----------
        static Vector3 Centroid(IList<Transform> ts)
        {
            if (ts == null || ts.Count == 0) return Vector3.zero;
            var s = Vector3.zero; for (int i = 0; i < ts.Count; i++) s += ts[i].position; return s / ts.Count;
        }
        static float Median(IList<float> vals)
        {
            if (vals == null || vals.Count == 0) return 0f;
            var a = vals.OrderBy(v => v).ToArray();
            int n = a.Length;
            return (n % 2 == 1) ? a[n/2] : 0.5f * (a[n/2 - 1] + a[n/2]);
        }
        static float StdDev(IList<float> vals)
        {
            if (vals == null || vals.Count < 2) return 0f;
            float mean = vals.Average();
            float sum = 0f; foreach (var v in vals) sum += (v - mean) * (v - mean);
            return Mathf.Sqrt(sum / (vals.Count - 1));
        }
        static Bounds ComputeBounds(IEnumerable<Transform> ts)
        {
            var e = ts.GetEnumerator();
            if (!e.MoveNext()) return new Bounds(Vector3.zero, Vector3.zero);
            var b = new Bounds(e.Current.position, Vector3.zero);
            foreach (var t in ts) b.Encapsulate(t.position);
            return b;
        }

        // cluster a row into one or two lateral groups based on X gaps
        static List<List<Transform>> ClusterRowByGaps(List<Transform> tiles)
        {
            if (tiles.Count <= 2) return new List<List<Transform>> { tiles };
            var sorted = tiles.OrderBy(t => t.position.x).ToList();
            var gaps = new List<float>();
            for (int i = 1; i < sorted.Count; i++) gaps.Add(Mathf.Abs(sorted[i].position.x - sorted[i-1].position.x));
            float medGap = Median(gaps);
            float splitGap = Mathf.Max(0.01f, medGap * 2.5f);

            var clusters = new List<List<Transform>> { new List<Transform>{ sorted[0] } };
            for (int i = 1; i < sorted.Count; i++)
            {
                float g = Mathf.Abs(sorted[i].position.x - sorted[i-1].position.x);
                if (g > splitGap) clusters.Add(new List<Transform>());
                clusters[clusters.Count - 1].Add(sorted[i]);
            }
            return clusters;
        }

        public static void AnalyzeAndWriteReport()
        {
            var root = FindTrackRoot(); if (!root) return;

            int minCol, maxCol;
            var rows = GetRows(root, out minCol, out maxCol);
            if (rows.Count == 0) { Debug.LogWarning("[TrackAnalyzer] No tiles found (tile_r*_c*)."); return; }

            var rowKeys = rows.Keys.OrderBy(k => k).ToList();
            int rowCount = rowKeys.Count;
            int firstRow = rowKeys.First();
            int lastRow  = rowKeys.Last();

            // tiles / holes
            int present = rows.Sum(kv => kv.Value.Count);
            int expectedPerRow = Math.Max(0, maxCol - minCol + 1);
            int expected = expectedPerRow * rowCount;
            int missing = Math.Max(0, expected - rows.Sum(kv => kv.Value.Select(t => TileRx.Match(t.name).Groups["c"].Value).Distinct().Count()));
            float missingPct = expected > 0 ? (float)missing / expected : 0f;

            // width (median X-span using adjacent deltas)
            var rowWidths = new List<float>();
            var allTiles = new List<Transform>();
            foreach (var r in rowKeys)
            {
                var tiles = rows[r];
                allTiles.AddRange(tiles);
                var cols = tiles
                    .Select(t => new { t, m = TileRx.Match(t.name) })
                    .Where(x => x.m.Success)
                    .Select(x => new { t = x.t, c = int.Parse(x.m.Groups["c"].Value) })
                    .OrderBy(x => x.c)
                    .ToList();

                if (cols.Count >= 2)
                {
                    float minX = cols.First().t.position.x;
                    float maxX = cols.Last().t.position.x;
                    var deltas = new List<float>();
                    for (int i = 1; i < cols.Count; i++)
                        deltas.Add(Mathf.Abs(cols[i].t.position.x - cols[i-1].t.position.x));
                    float pitchX = Median(deltas);
                    int spanCols = cols.Last().c - cols.First().c;
                    float width = (spanCols <= 0 || pitchX <= 0f) ? Mathf.Abs(maxX - minX) : spanCols * pitchX;
                    rowWidths.Add(width);
                }
            }
            float widthM = rowWidths.Count > 0 ? Median(rowWidths) : 1f;

            // mainline centers following nearest cluster (avoid hopping across split)
            var centers = new List<Vector3>();
            Vector3? prevCenter = null;
            int splitRows = 0;
            foreach (var r in rowKeys)
            {
                var tiles = rows[r];
                var clusters = ClusterRowByGaps(tiles);
                if (clusters.Count > 1) splitRows++;

                if (prevCenter == null)
                {
                    centers.Add(Centroid(clusters.OrderByDescending(c => c.Count).First()));
                    prevCenter = centers[centers.Count - 1];
                }
                else
                {
                    // choose cluster whose centroid is nearest to previous center
                    var cands = clusters.Select(c => new { c, cent = Centroid(c) }).ToList();
                    var pick = cands.OrderBy(k => Vector3.SqrMagnitude(k.cent - prevCenter.Value)).First();
                    centers.Add(pick.cent);
                    prevCenter = pick.cent;
                }
            }

            // length from mainline centers
            var pitches = new List<float>();
            for (int i = 1; i < centers.Count; i++)
                pitches.Add(Vector3.Distance(centers[i-1], centers[i]));
            float pitchZ = pitches.Count > 0 ? Median(pitches) : 1f;
            float lengthM = pitchZ * (centers.Count - 1);

            // curvature on mainline
            var headings = new List<Vector3>();
            for (int i = 1; i < centers.Count; i++)
            {
                var d = centers[i] - centers[i-1];
                d.y = 0f;
                if (d.sqrMagnitude > 1e-6f) headings.Add(d.normalized);
            }
            var curveDegs = new List<float>();
            for (int i = 1; i < headings.Count; i++)
            {
                float ang = Vector3.SignedAngle(headings[i-1], headings[i], Vector3.up);
                curveDegs.Add(Mathf.Abs(ang));
            }
            float meanCurv = curveDegs.Count > 0 ? curveDegs.Average() : 0f;
            float maxCurv = curveDegs.Count > 0 ? curveDegs.Max() : 0f;

            // write report
            string projRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(projRoot, "StickerDash_Status");
            Directory.CreateDirectory(outDir);
            string md = Path.Combine(outDir, "track_report.md");

            var sb = new StringBuilder();
            sb.AppendLine("# Track v2 Analysis");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"- Rows: **{rowCount}** (index {firstRow}..{lastRow})");
            sb.AppendLine($"- Est. length: **{lengthM:F1} m** (median row pitch {pitchZ:F2} m)");
            sb.AppendLine($"- Est. width: **{widthM:F1} m** (median X-span)");
            sb.AppendLine($"- Tiles present: **{present}**");
            if (expected > 0) sb.AppendLine($"- Estimated missing tiles: **{missing}** ({missingPct:P0})");
            sb.AppendLine($"- Curvature (mainline): mean **{meanCurv:F1}°**, max **{maxCurv:F1}°**");
            sb.AppendLine($"- Split section: **{(splitRows>0 ? "yes" : "no")}** (~{splitRows} rows)");
            sb.AppendLine();
            sb.AppendLine("## Notes");
            sb.AppendLine("- Length/width target the generator's requested meters using computed pitches.");
            sb.AppendLine("- Curvature follows the mainline through splits (no centroid jumping).");
            File.WriteAllText(md, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[TrackAnalyzer] Wrote report → {md}");
            EditorUtility.RevealInFinder(md);
        }

        // ---------- Snapshot ----------
        public static void SaveTopDownSnapshot(int pixels = 2048, float margin = 2f)
        {
            var root = FindTrackRoot(); if (!root) return;
            var tiles = new List<Transform>();
            foreach (Transform c in root) if (TileRx.IsMatch(c.name)) tiles.Add(c);
            if (tiles.Count == 0) { Debug.LogWarning("[TrackAnalyzer] No tiles to snapshot."); return; }

            var b = ComputeBounds(tiles);
            b.Expand(margin);

            var goCam = new GameObject("A2P_TopDownCam_TEMP");
            var cam = goCam.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
            cam.transform.position = new Vector3(b.center.x, b.size.y + 50f, b.center.z);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.orthographicSize = Mathf.Max(b.extents.x, b.extents.z);

            var rt = new RenderTexture(pixels, pixels, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);

            var prevTarget = RenderTexture.active;
            var prevCamRT = cam.targetTexture;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, pixels, pixels), 0, 0);
                tex.Apply();
            }
            finally
            {
                cam.targetTexture = prevCamRT;
                RenderTexture.active = prevTarget;
            }

            string projRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(projRoot, "StickerDash_Status");
            Directory.CreateDirectory(outDir);
            string png = Path.Combine(outDir, "TrackTopDown.png");

            File.WriteAllBytes(png, tex.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(goCam);

            Debug.Log($"[TrackAnalyzer] Saved snapshot → {png}");
            EditorUtility.RevealInFinder(png);
        }
    }
}
#endif
