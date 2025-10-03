#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public class NLTesterWindow : EditorWindow {
        Vector2 _nlScroll, _canonScroll;
        string _nl = string.Join(Environment.NewLine, new[]{
            "build 200 by 3",
            "protect start 10 end 10",
            "random slopes 2 to 4 degrees, segment 2",
            "fork 120 to 160 widen to 3",
            "rejoin 200 to 240",
            "smooth heights"
        });
        string _canon = "";

        [MenuItem("Window/Aim2Pro/Track Creator/NL Tester")]
        public static void Open(){
            var w = GetWindow<NLTesterWindow>("NL Tester");
            w.minSize = new Vector2(620, 480);
        }

        void OnGUI(){
            GUILayout.Label("Natural Language → Canonical", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            // NL
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));
            GUILayout.Label("NL", EditorStyles.miniBoldLabel);
            _nlScroll = EditorGUILayout.BeginScrollView(_nlScroll, GUILayout.MinHeight(180));
            _nl = EditorGUILayout.TextArea(_nl ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Canonical
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));
            GUILayout.Label("Canonical (read-only)", EditorStyles.miniBoldLabel);
            _canonScroll = EditorGUILayout.BeginScrollView(_canonScroll, GUILayout.MinHeight(180));
            using (new EditorGUI.DisabledScope(true)){
                EditorGUILayout.TextArea(_canon ?? "", GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Parse Only", GUILayout.Height(26))) {
                    _canon = NLEngine.ParseNL(_nl ?? "");
                }
                if (GUILayout.Button("Write Plan", GUILayout.Height(26))) {
                    _canon = NLEngine.ParseNL(_nl ?? "");
                    NLEngine.WritePlan(_canon);
                }
                if (GUILayout.Button("Run Plan", GUILayout.Height(26))) {
                    _canon = NLEngine.ParseNL(_nl ?? "");
                    NLEngine.WritePlan(_canon);
                    NLEngine.RunPlan(false);
                }
            }
        }
    }
}
#endif
