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
        string nlText = "create 120 m by 6 m\nstraighten rows 1-10\noffset rows 1-10 x 1\n";

        [MenuItem("Window/Aim2Pro/Track Creator/Track Lab (All-in-One)", priority = 10)]
        public static void Open() => GetWindow<TrackLab>("Track Lab");

        void OnGUI()
        {
            GUILayout.Label("Track Lab — Baseline", EditorStyles.boldLabel);
            GUILayout.Space(6);

            GUILayout.Label("Create Grid", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope()){
                lengthMeters = EditorGUILayout.IntField("Length (m → rows)", lengthMeters);
                widthTiles   = EditorGUILayout.IntField("Width (tiles)", widthTiles);
            }
            using (new EditorGUILayout.HorizontalScope()){
                tileSize  = EditorGUILayout.FloatField("Tile Size (m)", tileSize);
                thickness = EditorGUILayout.FloatField("Thickness (m)", thickness);
            }
            if (GUILayout.Button("Build Grid")) BuildGrid(lengthMeters, widthTiles, tileSize, thickness);

            EditorGUILayout.Space(10);

            GUILayout.Label("Row Edits (inclusive)", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope()){
                rangeA = EditorGUILayout.IntField("Start Row", rangeA);
                rangeB = EditorGUILayout.IntField("End Row", rangeB);
            }
            using (new EditorGUILayout.HorizontalScope()){
                offsetX = EditorGUILayout.FloatField("Offset X (m)", offsetX);
                if (GUILayout.Button("Apply X Offset", GUILayout.Width(140))) OffsetRowsX(rangeA, rangeB, offsetX);
            }
            using (new EditorGUILayout.HorizontalScope()){
                offsetY = EditorGUILayout.FloatField("Offset Y (m)", offsetY);
                if (GUILayout.Button("Apply Y Offset", GUILayout.Width(140))) OffsetRowsY(rangeA, rangeB, offsetY);
            }
            if (GUILayout.Button("Straighten Rows (match previous row X + tile heights)"))
                StraightenRows(rangeA, rangeB);

            EditorGUILayout.Space(10);

            GUILayout.Label("Append", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope()){
                appendLen  = EditorGUILayout.FloatField("Length (m)", appendLen);
                appendStep = EditorGUILayout.FloatField("Step (m)", appendStep);
            }
            if (GUILayout.Button("Append Straight")) AppendStraight(appendLen, appendStep, widthTiles, tileSize, thickness);

            EditorGUILayout.Space(10);

            GUILayout.Label("Delete", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Delete Row A"))   DeleteRow(rangeA);
                if (GUILayout.Button("Delete Rows A-B")) DeleteRowsRange(rangeA, rangeB);
            }

            EditorGUILayout.Space(12);
            DrawNLBox();
        }

        void DrawNLBox()
        {
            GUILayout.Label("Natural Language (tiny, safe)", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Examples:\\n• create 120 m by 6 m\\n• straighten rows 1-10\\n• offset rows 1-10 x 1\\n• offset rows 1-10 y 0.2\\n• append straight 50 m [step 1]\\n• delete row 3 / delete rows 2-5", MessageType.None);
            nlText = EditorGUILayout.TextArea(nlText, GUILayout.MinHeight(80));
            if (GUILayout.Button("Apply NL")) ApplyNL(nlText);
        }

        static int I(string s) => int.Parse(s);
        static float F(string s) => (float)double.Parse(s);

        void ApplyNL(string raw)
        {
            var text = raw.Replace("\\u000A", "\\n").Replace("\\n", "\n");
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int applied = 0;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try {
                foreach (var L in lines){
                    var line = L.Trim(); if (string.IsNullOrEmpty(line)) continue;
                    var mCreate = Regex.Match(line, @"^create\s+(\d+)\s*m\s+by\s+(\d+)\s*m?$", RegexOptions.IgnoreCase);
                    if (mCreate.Success){ BuildGrid(I(mCreate.Groups[1].Value), I(mCreate.Groups[2].Value), 1f, 0.2f); applied++; continue; }
                    var mStr = Regex.Match(line, @"^straighten\s+rows\s+(\d+)\s*-\s*(\d+)$", RegexOptions.IgnoreCase);
                    if (mStr.Success){ StraightenRows(I(mStr.Groups[1].Value), I(mStr.Groups[2].Value)); applied++; continue; }
                    var mOffX = Regex.Match(line, @"^offset\s+rows\s+(\d+)\s*-\s*(\d+)\s*x\s*(-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase);
                    if (mOffX.Success){ OffsetRowsX(I(mOffX.Groups[1].Value), I(mOffX.Groups[2].Value), F(mOffX.Groups[3].Value)); applied++; continue; }
                    var mOffY = Regex.Match(line, @"^offset\s+rows\s+(\d+)\s*-\s*(\d+)\s*y\s*(-?\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase);
                    if (mOffY.Success){ OffsetRowsY(I(mOffY.Groups[1].Value), I(mOffY.Groups[2].Value), F(mOffY.Groups[3].Value)); applied++; continue; }
                    var mAppend = Regex.Match(line, @"^append\s+straight\s+(\d+(?:\.\d+)?)\s*m(?:\s*step\s*(\d+(?:\.\d+)?))?$", RegexOptions.IgnoreCase);
                    if (mAppend.Success){ AppendStraight(F(mAppend.Groups[1].Value), mAppend.Groups[2].Success ? F(mAppend.Groups[2].Value) : 1f, widthTiles, tileSize, thickness); applied++; continue; }
                    var mDelRow = Regex.Match(line, @"^delete\s+row\s+(\d+)$", RegexOptions.IgnoreCase);
                    if (mDelRow.Success){ DeleteRow(I(mDelRow.Groups[1].Value)); applied++; continue; }
                    var mDelRows = Regex.Match(line, @"^delete\s+rows\s+(\d+)\s*-\s*(\d+)$", RegexOptions.IgnoreCase);
                    if (mDelRows.Success){ DeleteRowsRange(I(mDelRows.Groups[1].Value), I(mDelRows.Groups[2].Value)); applied++; continue; }
                    Debug.LogWarning($"[TrackLab/NL] Unrecognized: \"{line}\"");
                }
            } finally { Undo.CollapseUndoOperations(group); }
            Debug.Log($"[TrackLab/NL] Applied {applied} command(s).");
        }

        // Core helpers
        static GameObject EnsureTrack(){ var t = GameObject.Find("Track"); if (!t) t = new GameObject("Track"); return t; }
        static Transform EnsureRow(GameObject track, int row, bool createIfMissing=true){
            var tr = track.transform.Find($"Row_{row}");
            if (!tr && createIfMissing){ var go=new GameObject($"Row_{row}"); tr=go.transform; tr.parent=track.transform; }
            return tr;
        }
        static void ClearChildren(Transform t){ for (int i=t.childCount-1;i>=0;i--) DestroyImmediate(t.GetChild(i).gameObject); }

        public static void BuildGrid(int rows,int width,float tile,float thick){
            var track=EnsureTrack(); ClearChildren(track.transform);
            for(int r=1;r<=rows;r++){
                var row=EnsureRow(track,r,true); row.localPosition=new Vector3(0,0,(r-1)*tile);
                for(int c=1;c<=width;c++){
                    var t=GameObject.CreatePrimitive(PrimitiveType.Cube);
                    t.name=$"Tile_r{r}_c{c}"; t.transform.parent=row; t.transform.localScale=new Vector3(tile,thick,tile);
                    t.transform.localPosition=new Vector3((c-1)*tile, thick*0.5f, 0);
                }
            }
            Selection.activeGameObject=track; Debug.Log($"[TrackLab] Built {rows}×{width}, tile {tile}m, thick {thick}m.");
        }
        public static void OffsetRowsX(int a,int b,float m){ var tr=EnsureTrack(); for(int r=a;r<=b;r++){ var row=EnsureRow(tr,r,false); if(!row) continue; var p=row.localPosition; p.x+=m; row.localPosition=p; } }
        public static void OffsetRowsY(int a,int b,float m){ var tr=EnsureTrack(); for(int r=a;r<=b;r++){ var row=EnsureRow(tr,r,false); if(!row) continue; for(int i=0;i<row.childCount;i++){ var t=row.GetChild(i); var lp=t.localPosition; lp.y+=m; t.localPosition=lp; } } }
        public static void StraightenRows(int a,int b){ var tr=EnsureTrack(); for(int r=a+1;r<=b;r++){ var prev=EnsureRow(tr,r-1,false); var row=EnsureRow(tr,r,false); if(!row||!prev) continue; var p=row.localPosition; p.x=prev.localPosition.x; row.localPosition=p; int n=Math.Min(prev.childCount,row.childCount); for(int i=0;i<n;i++){ var pt=prev.GetChild(i); var rt=row.GetChild(i); var lp=rt.localPosition; lp.y=pt.localPosition.y; rt.localPosition=lp; } } }
        public static void AppendStraight(float L,float step,int width,float tile,float thick){
            int steps=Mathf.Max(1, Mathf.CeilToInt(L/Mathf.Max(0.0001f,step)));
            var tr=EnsureTrack(); int max=0;
            for(int i=0;i<tr.transform.childCount;i++){ var ch=tr.transform.GetChild(i); if(ch.name.StartsWith("Row_") && int.TryParse(ch.name.Substring(4), out int n) && n>max) max=n; }
            for(int s=1;s<=steps;s++){ int r=max+s; var row=EnsureRow(tr,r,true); row.localPosition=new Vector3(0,0,(r-1)*tile);
                for(int c=1;c<=width;c++){ var t=GameObject.CreatePrimitive(PrimitiveType.Cube); t.name=$"Tile_r{r}_c{c}"; t.transform.parent=row; t.transform.localScale=new Vector3(tile,thick,tile); t.transform.localPosition=new Vector3((c-1)*tile, thick*0.5f, 0); } }
        }
        public static void DeleteRow(int r){ var tr=EnsureTrack(); var row=EnsureRow(tr,r,false); if(row) DestroyImmediate(row.gameObject); }
        public static void DeleteRowsRange(int a,int b){ for(int r=a;r<=b;r++) DeleteRow(r); }
    }
}
