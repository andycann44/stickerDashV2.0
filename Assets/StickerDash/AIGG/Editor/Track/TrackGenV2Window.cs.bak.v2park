#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public class TrackGenV2Window : EditorWindow {
        // BUILD
        string _nl = "build 300 by 6\nprotect start 10 end 10\nrandom holes 10%";
        string _preview = "";
        Vector2 _scrollBuild, _scrollPreview;

        // AMEND
        string _amend = "remove rows 15 to 18";
        string _amendPreview = "";
        Vector2 _scrollAmend, _scrollAmendPrev;

        [MenuItem("Window/Aim2Pro/Track Creator/Track Gen V2")]
        public static void Open(){
            var w = GetWindow<TrackGenV2Window>("Track Gen V2");
            w.minSize = new Vector2(780, 560);
        }

        void OnGUI(){
            // ===== BUILD =====
            GUILayout.Label("Natural Language (Build)", EditorStyles.boldLabel);
            _scrollBuild = EditorGUILayout.BeginScrollView(_scrollBuild);
            _nl = EditorGUILayout.TextArea(_nl ?? "", GUILayout.MinHeight(80));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Parse -> Canonical", GUILayout.Height(24))){
                    try{
                        var canon = NLEngine.ParseNL(_nl ?? "");
                        _preview = (canon ?? "").Replace("\r\n","\n");
                        PlanIO.Overwrite(_preview);
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Build & Run", GUILayout.Height(24))){
                    try{
                        var canon = NLEngine.ParseNL(_nl ?? "");
                        _preview = (canon ?? "").Replace("\r\n","\n");
                        PlanIO.Overwrite(_preview);
                        RunLast();
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Rebuild Track (Run Last Canonical)", GUILayout.Height(24))){
                    RunLast();
                }
                if (GUILayout.Button("Reveal Plan", GUILayout.Height(24))){
                    PlanIO.Reveal();
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Canonical Preview", EditorStyles.boldLabel);
            _scrollPreview = EditorGUILayout.BeginScrollView(_scrollPreview, GUILayout.MinHeight(140));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(_preview ?? "", GUILayout.ExpandHeight(true));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();

            // ===== AMEND =====
            GUILayout.Space(10);
            GUILayout.Label("Amend Current Track via NL", EditorStyles.boldLabel);
            _scrollAmend = EditorGUILayout.BeginScrollView(_scrollAmend);
            _amend = EditorGUILayout.TextArea(_amend ?? "", GUILayout.MinHeight(60));
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Preview Amend", GUILayout.Height(22))){
                    try{
                        var canon = NLEngine.ParseNL(_amend ?? "");
                        _amendPreview = (canon ?? "").Replace("\r\n","\n");
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Append To Plan", GUILayout.Height(22))){
                    try{
                        PlanIO.Append(NLEngine.ParseNL(_amend ?? ""));
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
                if (GUILayout.Button("Append & Run", GUILayout.Height(22))){
                    try{
                        PlanIO.Append(NLEngine.ParseNL(_amend ?? ""));
                        RunLast();
                    } catch (Exception ex){ Debug.LogException(ex); }
                }
            }

            if (!string.IsNullOrEmpty(_amendPreview)){
                GUILayout.Space(4);
                GUILayout.Label("Amend Canonical Preview", EditorStyles.miniBoldLabel);
                _scrollAmendPrev = EditorGUILayout.BeginScrollView(_scrollAmendPrev, GUILayout.MinHeight(80));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextArea(_amendPreview, GUILayout.ExpandHeight(true));
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndScrollView();
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
