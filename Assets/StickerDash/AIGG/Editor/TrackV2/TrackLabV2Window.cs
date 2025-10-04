#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG.TrackV2
{
    public class TrackLabV2Window : EditorWindow
    {
        private string nlInput =
@"append straight 20m
append arc left 45deg
offset x rows 3..8 by 0.5m
delete rows 10..12";
        private string log = "";
        private Vector2 logScroll;
        private V2CommandEngine engine;
        private bool rulesLoaded;
        private bool showTools;

        [MenuItem("Window/Aim2Pro/Track Creator/Track Lab v2")]
        public static void Open() => GetWindow<TrackLabV2Window>("Track Lab v2");

        private void OnEnable()
        {
            engine = new V2CommandEngine(AddLog);
            engine.LoadRules();
            rulesLoaded = true;
        }

        private void AddLog(string line)
        {
            log += line + "\n";
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            nlInput = EditorGUILayout.TextArea(nlInput, GUILayout.MinHeight(90));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse", GUILayout.Height(24))) engine.Parse(nlInput);
                if (GUILayout.Button("Apply", GUILayout.Height(24))) engine.Apply();
                if (GUILayout.Button("Clear Log", GUILayout.Height(24))) log = "";
            }

            GUILayout.Space(6);
            showTools = EditorGUILayout.Foldout(showTools, "Tools (in-window)");
            if (showTools)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Analyze Track", GUILayout.Height(20)))
                        TrackAnalyzer.AnalyzeAndWriteReport();

                    if (GUILayout.Button("Top-Down Snapshot", GUILayout.Height(20)))
                        TrackAnalyzer.SaveTopDownSnapshot();

                    if (GUILayout.Button("Clear Tiles", GUILayout.Height(20)))
                        ClearTilesInTrackRoot();
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Log", EditorStyles.boldLabel);
            logScroll = EditorGUILayout.BeginScrollView(logScroll);
            EditorGUILayout.TextArea(log, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            EditorGUILayout.HelpBox("v2 is data-driven: edit Assets/StickerDash/AIGG/Resources/TrackV2/commands.json", MessageType.Info);
            if (!rulesLoaded)
                EditorGUILayout.HelpBox("Commands not loaded.", MessageType.Warning);
        }

        private static void ClearTilesInTrackRoot()
        {
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (!go) { Debug.LogWarning("[TrackLabV2] Track root not found."); return; }
            var root = go.transform;
            var rx = new Regex(@"^tile_r\d+_c\d+$", RegexOptions.IgnoreCase);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Clear Track Tiles");
            var del = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in root) if (rx.IsMatch(c.name)) del.Add(c.gameObject);
            foreach (var g in del) DestroyImmediate(g);
            Debug.Log($"[TrackLabV2] Cleared {del.Count} tiles under {root.name}.");
        }
    }
}
#endif
