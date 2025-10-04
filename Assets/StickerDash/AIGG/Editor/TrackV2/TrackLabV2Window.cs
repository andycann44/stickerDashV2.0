#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.TrackV2
{
    public class TrackLabV2Window : EditorWindow
    {
        private string nlInput =
@"create 300 m by 6 m track with 10% tiles missing, random bends up to 30 degrees either way, split min 0.5w, slight ups and downs, low speed, simple";
        private string log = "";
        private Vector2 logScroll;
        private V2CommandEngine engine;

        [MenuItem("Window/Aim2Pro/Track Creator/Track Lab v2")]
        public static void Open() => GetWindow<TrackLabV2Window>("Track Lab v2");

        private void OnEnable()
        {
            engine = new V2CommandEngine(AddLog);
            engine.LoadRules();
        }

        private void AddLog(string line) { log += line + "\n"; Repaint(); }

        private void OnGUI()
        {
            // Toolbar (window-only actions)
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Parse",    EditorStyles.toolbarButton)) engine.Parse(nlInput);
                if (GUILayout.Button("Apply",    EditorStyles.toolbarButton)) engine.Apply();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("Reload Rules", EditorStyles.toolbarButton)) { engine.LoadRules(); AddLog("Rules reloaded."); }
         if (GUILayout.Button("Analyze",  EditorStyles.toolbarButton)) TrackAnalyzer.AnalyzeAndWriteReport();
                if (GUILayout.Button("Snapshot", EditorStyles.toolbarButton)) TrackAnalyzer.SaveTopDownSnapshot();
                if (GUILayout.Button("Clear Tiles", EditorStyles.toolbarButton)) ClearTiles();
                if (GUILayout.Button("Clear Mesh", EditorStyles.toolbarButton)) Aim2Pro.AIGG.Kernel.ClearTrackMesh(true);


                // Bakes
                if (GUILayout.Button("Bake Mesh (preserve holes)", EditorStyles.toolbarButton))
                    Aim2Pro.AIGG.Kernel.BuildMeshFromTilesPreserveHoles(0.2f, true);
                if (GUILayout.Button("Bake Clean Mesh (no gaps)", EditorStyles.toolbarButton))
                    Aim2Pro.AIGG.Kernel.BuildSplineFromTrack(0f, 0f, 0.2f, true);

                // One-click preset that matches your description
                if (GUILayout.Button("Build First Level (preset)", EditorStyles.toolbarButton))
                    Aim2Pro.AIGG.Kernel.GenerateFirstLevelScenario();
            }

            GUILayout.Space(6);
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            nlInput = EditorGUILayout.TextArea(nlInput, GUILayout.MinHeight(90));

            GUILayout.Space(6);
            GUILayout.Label("Log", EditorStyles.boldLabel);
            logScroll = EditorGUILayout.BeginScrollView(logScroll);
            EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            EditorGUILayout.HelpBox("v2 is data-driven: edit Assets/StickerDash/AIGG/Resources/TrackV2/commands.json", MessageType.Info);
        }

        private static void ClearTiles()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go) { Debug.LogWarning("[TrackLabV2] Track root not found."); return; }
            var root = go.transform;
            var rx = new Regex(@"^tile_r\d+_c\d+$", RegexOptions.IgnoreCase);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Clear Track Tiles");
            var del = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in root) if (rx.IsMatch(c.name)) del.Add(c.gameObject);
            foreach (var g in del) Object.DestroyImmediate(g);
            Debug.Log($"[TrackLabV2] Cleared {del.Count} tiles under {root.name}.");
        }
    }
}
#endif
