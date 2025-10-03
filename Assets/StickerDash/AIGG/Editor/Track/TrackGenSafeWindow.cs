#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public class TrackGenSafeWindow : EditorWindow {
        string _nl = "build 120 by 6\nprotect start 10 end 10\nrandom holes 25%";
        string _preview = "";
        string _amend = "remove rows 30 to 32";
        string _amendPreview = "";
        Vector2 _s1,_s2,_s3;

        [MenuItem("Window/Aim2Pro/Track Creator/Track Gen (Safe)")]
        public static void Open(){
            var w = GetWindow<TrackGenSafeWindow>("Track Gen (Safe)");
            w.minSize = new Vector2(760, 520);
        }

        void OnGUI(){
            GUILayout.Label("Natural Language (Build)", EditorStyles.boldLabel);
            _s1 = EditorGUILayout.BeginScrollView(_s1);
            _nl = EditorGUILayout.TextArea(_nl ?? "", GUILayout.MinHeight(90));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Parse -> Canonical", GUILayout.Height(24))){
                    try{
                        var canon = NLEngine.ParseNL(NLPre.Normalize(_nl ?? ""));
                        _preview = (canon ?? "").Replace("\r\n","\n").Replace("\r","\n");
                        PlanIO.Overwrite(_preview);
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Build & Run", GUILayout.Height(24))){
                    try{
                        var canon = NLEngine.ParseNL(NLPre.Normalize(_nl ?? ""));
                        _preview = (canon ?? "").Replace("\r\n","\n").Replace("\r","\n");
                        PlanIO.Overwrite(_preview);
                        RunLast();
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Rebuild (Run Last Canonical)", GUILayout.Height(24))){
                    RunLast();
                }
                if (GUILayout.Button("Reveal Plan", GUILayout.Height(24))){
                    PlanIO.Reveal();
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Canonical Preview", EditorStyles.boldLabel);
            _s2 = EditorGUILayout.BeginScrollView(_s2, GUILayout.MinHeight(140));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(_preview ?? "", GUILayout.ExpandHeight(true));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.Label("Amend Current Track via NL", EditorStyles.boldLabel);
            _s3 = EditorGUILayout.BeginScrollView(_s3);
            _amend = EditorGUILayout.TextArea(_amend ?? "", GUILayout.MinHeight(70));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Preview Amend", GUILayout.Height(22))){
                    try{
                        var canon = NLEngine.ParseNL(NLPre.Normalize(_amend ?? ""));
                        _amendPreview = (canon ?? "").Replace("\r\n","\n").Replace("\r","\n");
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Append To Plan", GUILayout.Height(22))){
                    try{
                        PlanIO.Append(NLEngine.ParseNL(NLPre.Normalize(_amend ?? "")));
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Append & Run", GUILayout.Height(22))){
                    try{
                        PlanIO.Append(NLEngine.ParseNL(NLPre.Normalize(_amend ?? "")));
                        RunLast();
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
            }

            if (!string.IsNullOrEmpty(_amendPreview)){
                GUILayout.Space(4);
                GUILayout.Label("Amend Canonical Preview", EditorStyles.miniBoldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(_amendPreview, GUILayout.MinHeight(80));
                EditorGUI.EndDisabledGroup();
            }
        }

        static void RunLast(){
            if (!EditorApplication.ExecuteMenuItem("Window/Aim2Pro/Track Creator/Run Last Canonical")){
                Debug.LogWarning("[A2P] Could not trigger Run Last Canonical.");
            }
        }
    }
}
#endif
