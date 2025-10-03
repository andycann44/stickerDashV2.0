using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Aim2Pro.AIGG.TrackV2
{
    public class TrackLabV2Window : EditorWindow
    {
        private string nl = "append straight 20m\nappend arc left 45deg\noffset x rows 3..8 by 0.5m\ndelete rows 10..12";
        private Vector2 scroll;
        private List<string> log = new List<string>();
        private V2CommandEngine engine;

        [MenuItem("Window/Aim2Pro/Track Creator/Track Lab v2")]
        public static void Open() => GetWindow<TrackLabV2Window>("Track Lab v2");

        private void OnEnable() => engine = new V2CommandEngine(Log);

        private void Log(string msg)
        {
            log.Add(msg);
            Repaint();
        }

                  id OnGUI()
        {
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(140));

            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse"))
                {
                    log.Clear();
                    engine.LoadRules();
                    engine.Parse(nl);
                }
                if (GUILayout.Button("Apply"))
                {
                    engine.Apply();
                }
                if (GUILayout.Button("Clear Log"))
                {
                    log.Clear();
                }
            }

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(180));
            foreach (var line in log) EditorGUILayout.LabelField(line);
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox("v2 is data-driven: edit Assets/StickerDash/AIGG/Resources/TrackV2/commands.json", MessageType.Info);
        }
    }
}
