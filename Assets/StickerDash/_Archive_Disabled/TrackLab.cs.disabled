using UnityEditor;
using UnityEngine;
using System;
using System.Text.RegularExpressions;

namespace Aim2Pro.TrackCreator
{
    public class TrackLab : EditorWindow
    {
        int lengthMeters = 120, widthTiles = 6;
        float tileSize = 1f, thickness = 0.2f;

        int rangeA = 1, rangeB = 10;
        float offsetX = 0f, offsetY = 0f;

        float appendLen = 50f, appendStep = 1f;

        // NL prompt
        string nlText = "create 120 m by 6 m\nstraighten rows 1-10\noffset rows 1-10 x 1\n";

        [MenuItem("Window/Aim2Pro/Track Creator/Track Lab (All-in-One)", priority = 10)]
        public static void Open() => GetWindow<TrackLab>("Track Lab");

        void OnGUI()
        {
            GUILayout.Label("Track Lab — Baseline", EditorStyles.boldLabel);
            GUILayout.Space(6);

            // Create Grid
            GUILayout.Label("Create Grid", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                lengthMeters = EditorGUILayout.IntField("Length (m → rows)", lengthMeters);
                widthTiles   = EditorGUILayout.IntField("Width (tiles)", widthTiles);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                tileSize  = EditorGUILayout.FloatField("Tile Size (m)", tileSize);
                thickness = EditorGUILayout.FloatField("Thickness (m)", thickness);
            }
            if (GUILayout.Button("Build Grid")) BuildGrid(lengthMeters, widthTiles, tileSize, thickness);

            EditorGUILayout.Space(10);

            // Row edits
            GUILayout.Label("Row Edits (inclusive)", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                rangeA = EditorGUILayout.IntField("Start Row", rangeA);
                rangeB = EditorGUILayout.IntField("End Row", rangeB);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                offsetX = EditorGUILayout.FloatField("Offset X (m)", offsetX);
                if (GUILayout.Button("Apply X Offset", GUILayout.Width(140))) OffsetRowsX(rangeA, rangeB, offsetX);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                offsetY = EditorGUILayout.FloatField("Offset Y (m)", offsetY);
                if (GUILayout.Button("Apply Y Offset", GUILayout.Width(140))) OffsetRowsY(rangeA, rangeB, offsetY);
            }
            if (GUILayout.Button("Straighten Rows (match previous row X + tile heights)"))
                StraightenRows(rangeA, rangeB);

            EditorGUILayout.Space(10);

            // Append
            GUILayout.Label("Append", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                appendLen  = EditorGUILayout.FloatField("Length (m)", appendLen);
                appendStep = EditorGUILayout.FloatField("Step (m)", appendStep);
            }
            if (GUILayout.Button("Append Straight")) AppendStraight(appendLen, appendStep, widthTiles, tileSize, thickness);

            EditorGUILayout.Space(12);

            // --- Natural Language box ---
            DrawNLBox();
        }

        void DrawNLBox()
        {
            GUILayout.Label("Natural Language (tiny, safe)", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(
                "Examples:\n• create 120 m by 6 m\n• straighten rows 1-10\n• offset rows 1-10 x 1\n• offset rows 1-10 y 0.2\n• append straight 50 m [step 1]\n• delete row 3 / delete rows 2-5",
                MessageType.None);
            nlText = EditorGUILayout.TextArea(nlText, GUILayout.MinHeight(80));
            if (GUILayout.Button("Apply NL")) ApplyNL(nlText);
        }

        static int I(string s) => int.Parse(s);
        static float F(string s) => (float)double.Parse(s);

        void ApplyNL(string raw)
        {
            // Optional preprocessor: NLPre.Normalize(raw) if present (no-op otherwise)
            try {
                var t = System.Type.GetType("Aim2Pro.NLPre, Assembly-CSharp-Editor") ??
                        System.Type.GetType("Aim2Pro.NL.NLPre, Assembly-CSharp-Editor");
                if (t != null)
                {
                    var m = t.GetMethod("Normalize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (m != null)
                    {
                        var o = m.Invoke(null, new object[]{ raw });
                        if (o is string s) raw = s;
                    }
                }
            } catch (System.Exception ex) {
                Debug.LogWarning("[TrackLab/NL] Preprocess failed: " + ex.Message);
            }

            var text = raw.Replace("\\u000A", "\n").Replace("\\n", "\n");
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int applied = 0;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            try
            {
                foreach (var L in lines)
                {
                    var line = L.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // create 120 m by 6 m
                    var mCreate = Regex.Match(line, @"^create\s+(\d+)\s*m\s+by\s+(\d+)\s*m?$", RegexOptions.IgnoreCase);
                    if (mCreate.Success) { BuildGrid(I(mCreate.Groups[1].Value), I(mCreate.Groups[2].Value), 1f, 0.2f); applied++; continue; }

                    // straighten rows 1-10
                    var mStr = Regex.Match(line, @"^straighten\s+rows\s+(\d+)\s*-\s*(\d+)$", RegexOptions.IgnoreCase);
                    if (mStr.Success) { StraightenRows(I(mStr.Groups[1].Value), I(mStr.Groups[2].Value)); applied++; continue; }

                    // offset rows 1-10 x 1.5
                    var mOffX = Regex.Match(line, @"^offset\s+rows\s+(\d+)\s*-\s*(\d+)\s*x\s*(-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase);
                    if (mOffX.Success) { OffsetRowsX(I(mOffX.Groups[1].Value), I(mOffX.Groups[2].Value), F(mOffX.Groups[3].Value)); applied++; continue; }

                    // offset rows 1-10 y 0.2
                    var mOffY = Regex.Match(line, @"^offset\s+rows\s+(\d+)\s*-\s*(\d+)\s*y\s*(-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase);
                    if (mOffY.Success) { OffsetRowsY(I(mOffY.Groups[1].Value), I(mOffY.Groups[2].Value), F(mOffY.Groups[3].Value)); applied++; continue; }

                    // append straight 50 m [step 1]
                    var mAppend = Regex.Match(line, @"^append\s+straight\s+(\d+(?:\.\d+)?)\s*m(?:\s*step\s*(\d+(?:\.\d+)?))?$", RegexOptions.IgnoreCase);
                    if (mAppend.Success) { AppendStraight(F(mAppend.Groups[1].Value), mAppend.Groups[2].Success ? F(mAppend.Groups[2].Value) : 1f, widthTiles, tileSize, thickness); applied++; continue; }

                    // delete row 3
                    var mDelRow = Regex.Match(line, @"^delete\s+row\s+(\d+)$", RegexOptions.IgnoreCase);
                    if (mDelRow.Success) { DeleteRow(I(mDelRow.Groups[1].Value)); applied++; continue; }

                    // delete rows 2-5
                    var mDelRows = Regex.Match(line, @"^delete\s+rows\s+(\d+)\s*-\s*(\d+)$", RegexOptions.IgnoreCase);
                    if (mDelRows.Success) { DeleteRowsRange(I(mDelRows.Groups[1].Value), I(mDelRows.Groups[2].Value)); applied++; continue; }

                    Debug.LogWarning($"[TrackLab/NL] Unrecognized: \"{line}\"");
                }
            }
            finally
            {
                Undo.CollapseUndoOperations(group);
            }

            Debug.Log($"[TrackLab/NL] Applied {applied} command(s).");
        }

        // ---------- helpers ----------
        static GameObject EnsureTrack()
        {
            var t = GameObject.Find("Track");
            if (!t) t = new GameObject("Track");
            return t;
        }

        static Transform EnsureRow(GameObject track, int row, bool createIfMissing = true)
        {
            string name = $"Row_{row}";
            var tr = track.transform.Find(name);
            if (!tr && createIfMissing)
            {
                var go = new GameObject(name);
                tr = go.transform;
                tr.parent = track.transform;
            }
            return tr;
        }

        static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                DestroyImmediate(t.GetChild(i).gameObject);
        }

        public static void BuildGrid(int rows, int width, float tileSize, float thickness)
        {
            var track = EnsureTrack();
            ClearChildren(track.transform);

            for (int r = 1; r <= rows; r++)
            {
                var row = EnsureRow(track, r, true);
                row.localPosition = new Vector3(0f, 0f, (r - 1) * tileSize);

                for (int c = 1; c <= width; c++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_r{r}_c{c}";
                    tile.transform.parent = row;
                    tile.transform.localScale = new Vector3(tileSize, thickness, tileSize);
                    tile.transform.localPosition = new Vector3((c - 1) * tileSize, thickness * 0.5f, 0f);
                }
            }
            Selection.activeGameObject = track;
            Debug.Log($"[TrackLab] Built {rows}×{width}, tile {tileSize}m, thick {thickness}m.");
        }

        public static void OffsetRowsX(int start, int end, float meters)
        {
            var track = EnsureTrack();
            for (int r = start; r <= end; r++)
            {
                var row = EnsureRow(track, r, false);
                if (!row) continue;
                var p = row.localPosition;
                p.x += meters;
                row.localPosition = p;
            }
            Debug.Log($"[TrackLab] OffsetRowsX {start}-{end} by {meters}m");
        }

        public static void OffsetRowsY(int start, int end, float meters)
        {
            var track = EnsureTrack();
            for (int r = start; r <= end; r++)
            {
                var row = EnsureRow(track, r, false);
                if (!row) continue;
                for (int i = 0; i < row.childCount; i++)
                {
                    var t = row.GetChild(i);
                    var lp = t.localPosition;
                    lp.y += meters;
                    t.localPosition = lp;
                }
            }
            Debug.Log($"[TrackLab] OffsetRowsY {start}-{end} by {meters}m");
        }

        public static void StraightenRows(int start, int end)
        {
            var track = EnsureTrack();
            for (int r = start + 1; r <= end; r++)
            {
                var prev = EnsureRow(track, r - 1, false);
                var row  = EnsureRow(track, r, false);
                if (!row || !prev) continue;

                var p = row.localPosition;
                p.x = prev.localPosition.x;
                row.localPosition = p;

                int count = Math.Min(prev.childCount, row.childCount);
                for (int i = 0; i < count; i++)
                {
                    var prevTile = prev.GetChild(i);
                    var rowTile  = row.GetChild(i);
                    var lp = rowTile.localPosition;
                    lp.y = prevTile.localPosition.y;
                    rowTile.localPosition = lp;
                }
            }
            Debug.Log($"[TrackLab] StraightenRows {start}-{end}.");
        }

        public static void AppendStraight(float distanceMeters, float stepMeters, int width, float tileSize, float thickness)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(distanceMeters / Mathf.Max(0.0001f, stepMeters)));
            var track = EnsureTrack();

            int maxRow = 0;
            for (int i = 0; i < track.transform.childCount; i++)
            {
                var ch = track.transform.GetChild(i);
                if (ch.name.StartsWith("Row_") && int.TryParse(ch.name.Substring(4), out int n))
                    if (n > maxRow) maxRow = n;
            }

            for (int s = 1; s <= steps; s++)
            {
                int r = maxRow + s;
                var row = EnsureRow(track, r, true);
                row.localPosition = new Vector3(0f, 0f, (r - 1) * tileSize);

                for (int c = 1; c <= width; c++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_r{r}_c{c}";
                    tile.transform.parent = row;
                    tile.transform.localScale = new Vector3(tileSize, thickness, tileSize);
                    tile.transform.localPosition = new Vector3((c - 1) * tileSize, thickness * 0.5f, 0f);
                }
            }
            Debug.Log($"[TrackLab] AppendStraight: +{steps} rows (~{distanceMeters:0.##}m).");
        }

        public static void DeleteRow(int row)
        {
            var track = EnsureTrack();
            var r = EnsureRow(track, row, false);
            if (r) DestroyImmediate(r.gameObject);
            Debug.Log($"[TrackLab] DeleteRow {row}");
        }

        public static void DeleteRowsRange(int start, int end)
        {
            for (int r = start; r <= end; r++) DeleteRow(r);
            Debug.Log($"[TrackLab] DeleteRowsRange {start}-{end}");
        }
    }
}
