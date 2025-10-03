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
            "fork same width 140 to 200 gap 1",
            "auto s-bends 2 at 25"
        });
        string _canon = "";

        [MenuItem("Window/Aim2Pro/Track Creator/NL/NL Tester")]
        public static void Open(){
            var w = GetWindow<NLTesterWindow>("NL Tester");
            w.minSize = new Vector2(680, 520);
        }

        void OnGUI(){
            GUILayout.Label("Natural Language → Canonical", EditorStyles.boldLabel);

            // NL + Canonical side by side
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(320));
            GUILayout.Label("NL", EditorStyles.miniBoldLabel);
            _nlScroll = EditorGUILayout.BeginScrollView(_nlScroll, GUILayout.MinHeight(200));
            _nl = EditorGUILayout.TextArea(_nl ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(320));
            GUILayout.Label("Canonical (read-only)", EditorStyles.miniBoldLabel);
            _canonScroll = EditorGUILayout.BeginScrollView(_canonScroll, GUILayout.MinHeight(200));
            using (new EditorGUI.DisabledScope(true)){
                EditorGUILayout.TextArea(_canon ?? "", GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse Only", GUILayout.Height(24))) {
                    _canon = NLEngine.ParseNL(_nl ?? "");
                }
                if (GUILayout.Button("Write Plan", GUILayout.Height(24))) {
                    _canon = NLEngine.ParseNL(_nl ?? "");
                    NLEngine.WritePlan(_canon);
                }
                if (GUILayout.Button("Run Plan", GUILayout.Height(24))) {
                    _canon = NLEngine.ParseNL(_nl ?? "");
                    NLEngine.WritePlan(_canon);
                    NLEngine.RunPlan(false);
                }
                GUILayout.FlexibleSpace();
               
                }
                if (GUILayout.Button("Open NL.input", GUILayout.Height(24))) {
                    NLFileRunner.OpenNLInput();
                }
                if (GUILayout.Button("Parse From File", GUILayout.Height(24))) {
                    NLFileRunner.ParseFromFile();
                }
                if (GUILayout.Button("Run From File", GUILayout.Height(24))) {
                    NLFileRunner.RunFromFile();
                }
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope()){
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("+", "Insert NL snippet"), GUILayout.Width(24), GUILayout.Height(22))){
                    NLInsertLibrary.ShowInsertMenu(sn => { _nl = NLInsertLibrary.AppendWithNewline(_nl ?? "", sn); });
                }
                if (GUILayout.Button("Open NL.input", GUILayout.Height(22))) { NLFileRunner.OpenNLInput(); }
                if (GUILayout.Button("Parse From File", GUILayout.Height(22))) { NLFileRunner.ParseFromFile(); }
                if (GUILayout.Button("Run From File", GUILayout.Height(22))) { NLFileRunner.RunFromFile(); }
            }
        
        }
    }
}
#endif
