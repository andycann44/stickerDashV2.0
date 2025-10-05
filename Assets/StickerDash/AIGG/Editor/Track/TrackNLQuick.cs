using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace Aim2Pro.TrackCreator
{
    public class TrackNLQuick : EditorWindow
    {
        string nl = "3m wide, tile width 1m, straight 50m";
        float widthM = 3f, tileW = 1f, lengthM = 50f;
        const float tileThickness = 0.2f;
        const string rootName = "A2P_Track";

        [MenuItem("Window/Aim2Pro/Track Creator/Track NL (Quick)")]
        public static void Open() => GetWindow<TrackNLQuick>("Track NL (Quick)");

        void OnGUI()
        {
            EditorGUILayout.LabelField("Tiny NL → Straight builder (emergency minimal)");
            nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(60));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse NL")) ParseNL();
                if (GUILayout.Button("Clear Track")) ClearTrack();
                if (GUILayout.Button("Build")) { ParseNL(); BuildStraight(); }
            }
            EditorGUILayout.Space();
            widthM = EditorGUILayout.FloatField("Width (m)", widthM);
            tileW  = EditorGUILayout.FloatField("Tile Width (m)", tileW);
            lengthM= EditorGUILayout.FloatField("Straight Length (m)", lengthM);
            EditorGUILayout.HelpBox("Example: 3m wide, tile width 1m, straight 50m", MessageType.Info);
        }

        void ParseNL()
        {
            var rxWidth   = new Regex(@"\b(\d+(?:\.\d+)?)\s*m\s*(?:wide|width)\b", RegexOptions.IgnoreCase);
            var rxTileW   = new Regex(@"\btile\s*width\s*(\d+(?:\.\d+)?)m?\b", RegexOptions.IgnoreCase);
            var rxStraight= new Regex(@"\bstraight\s*(\d+(?:\.\d+)?)\s*m\b", RegexOptions.IgnoreCase);

            var m = rxWidth.Match(nl);     if (m.Success) float.TryParse(m.Groups[1].Value, out widthM);
            m     = rxTileW.Match(nl);     if (m.Success) float.TryParse(m.Groups[1].Value, out tileW);
            m     = rxStraight.Match(nl);  if (m.Success) float.TryParse(m.Groups[1].Value, out lengthM);

            widthM = Mathf.Max(0.5f, widthM);
            tileW  = Mathf.Max(0.1f, tileW);
            lengthM= Mathf.Max(tileW, lengthM);
        }

        void ClearTrack()
        {
            var old = GameObject.Find(rootName);
            if (old) Undo.DestroyObjectImmediate(old);
        }

        void BuildStraight()
        {
            ClearTrack();
            int cols = Mathf.Max(1, Mathf.RoundToInt(widthM / tileW));
            int rows = Mathf.Max(1, Mathf.RoundToInt(lengthM / tileW));

            var root = new GameObject(rootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Track Root");

            float halfW = (cols * tileW) * 0.5f;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Undo.RegisterCreatedObjectUndo(go, "Create Tile");
                    go.transform.SetParent(root.transform);
                    go.name = $"tile_r{r:000}_c{c:000}";
                    go.transform.localScale = new Vector3(tileW, tileThickness, tileW);
                    float x = (c + 0.5f) * tileW - halfW;
                    float z = r * tileW;
                    go.transform.localPosition = new Vector3(x, tileThickness * 0.5f, z);
                }
            }
            Selection.activeGameObject = root;
            Debug.Log($"[A2P] Built straight: rows={rows}, cols={cols}, tileW={tileW}m, length={lengthM}m, width={widthM}m.");
        }
    }
}
