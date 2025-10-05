#if false // A2P_DISABLED: single Track Lab menu only
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
            EditorGUILayout.HelpBox("Type simple NL then Build. You will get a popup + on-screen toast.", MessageType.Info);
            nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(60));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse NL")) ParseNL();
                if (GUILayout.Button("Clear")) ClearTrack();
                if (GUILayout.Button("Build")) { ParseNL(); TryBuild(); }
            }
            widthM = EditorGUILayout.FloatField("Width (m)", widthM);
            tileW  = EditorGUILayout.FloatField("Tile Width (m)", tileW);
            lengthM= EditorGUILayout.FloatField("Straight Length (m)", lengthM);
            EditorGUILayout.LabelField("Example:", "3m wide, tile width 1m, straight 50m");
        }

        void ParseNL()
        {
            var rxW   = new Regex(@"\b(\d+(?:\.\d+)?)\s*m\s*(?:wide|width)\b", RegexOptions.IgnoreCase);
            var rxTW  = new Regex(@"\btile\s*width\s*(\d+(?:\.\d+)?)m?\b",     RegexOptions.IgnoreCase);
            var rxLen = new Regex(@"\bstraight\s*(\d+(?:\.\d+)?)\s*m\b",       RegexOptions.IgnoreCase);

            var m = rxW.Match(nl);    if (m.Success) float.TryParse(m.Groups[1].Value, out widthM);
            m     = rxTW.Match(nl);   if (m.Success) float.TryParse(m.Groups[1].Value, out tileW);
            m     = rxLen.Match(nl);  if (m.Success) float.TryParse(m.Groups[1].Value, out lengthM);

            widthM = Mathf.Max(0.5f, widthM);
            tileW  = Mathf.Max(0.1f, tileW);
            lengthM= Mathf.Max(tileW, lengthM);
        }

        void TryBuild()
        {
            try
            {
                var result = BuildStraight();
                Aim2Pro.A2PToast.Show(result);
                EditorUtility.DisplayDialog("Track NL (Quick)", result, "OK");
            }
            catch (System.Exception ex)
            {
                var msg = "Build FAILED: " + ex.Message;
                Aim2Pro.A2PToast.Show(msg, 4f);
                EditorUtility.DisplayDialog("Track NL (Quick)", msg, "OK");
                throw;
            }
        }

        void ClearTrack()
        {
            var old = GameObject.Find(rootName);
            if (old) Undo.DestroyObjectImmediate(old);
        }

        string BuildStraight()
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
            return $"Built straight OK • rows={rows}, cols={cols}, tileW={tileW}m, length={lengthM}m, width={widthM}m";
        }
    }
}
#endif // A2P_DISABLED
