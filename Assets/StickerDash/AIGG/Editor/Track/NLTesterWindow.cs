
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public class NLTesterWindow : EditorWindow {
        Vector2 _nlScroll, _canonScroll;
        string _nl = @"seed 7
build 220 by 3
safe margin 10
s bend 60-120 at 25 degrees gain 2
random holes 5%
smooth columns";
        string _canon = "";

        [MenuItem("Window/Aim2Pro/Track Creator/NL Tester")]
        public static void Open(){
            var w = GetWindow<NLTesterWindow>("NL Tester");
            w.minSize = new Vector2(620, 480);
        }

        void OnGUI(){
            GUILayout.Label("Natural Language → Canonical", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));
            GUILayout.Label("NL", EditorStyles.miniBoldLabel);
            _nlScroll = EditorGUILayout.BeginScrollView(_nlScroll, GUILayout.MinHeight(180));
            _nl = EditorGUILayout.TextArea(_nl ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

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
                    _canon = NLCore.ParseNL(_nl ?? "");
                }
                if (GUILayout.Button("Write Plan", GUILayout.Height(26))) {
                    _canon = NLCore.ParseNL(_nl ?? "");
                    NLCore.WritePlan(_canon);
                }
                if (GUILayout.Button("Run Plan", GUILayout.Height(26))) {
                    _canon = NLCore.ParseNL(_nl ?? "");
                    NLCore.WritePlan(_canon);
                    NLCore.RunPlan(false);
                }
                if (GUILayout.Button("Run (SBend Fix)", GUILayout.Height(26))) {
                    _canon = NLCore.ParseNL(_nl ?? "");
                    NLCore.WritePlan(_canon);
                    NLCore.RunPlan(true);
                }
            }
        }
    }
}
#endif
