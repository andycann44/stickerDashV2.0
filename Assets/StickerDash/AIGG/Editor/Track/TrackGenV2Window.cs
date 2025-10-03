#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public class TrackGenV2Window : EditorWindow {
        // NL
        string _nl = string.Join(Environment.NewLine, new[]{
            "build 300 by 6",
            "protect start 10 end 10",
            "random holes 5%",
            "auto s-bends 2 at 25"
        });
        Vector2 _scroll;
        // Quick amend
        string _amend = "remove rows 15 to 15";
        // Edit panel fields
        // Delete rows
        int drA = 10, drB = 12;
        // Delete tiles
        string dtCSV = "1,3,5";
        int dtRow = 20;
        // Curve rows
        int cvA = 80, cvB = 120, cvDeg = 20; bool cvLeft = true;
        // S-bends auto
        int sbCount = 2, sbDeg = 25;
        // Slopes random auto
        float slMin = 2f, slMax = 4f; int slSeg = 2;
        // Jump gaps
        int jgCount = 2;

        [MenuItem("Window/Aim2Pro/Track Creator/Track Gen V2")]
        public static void Open(){
            var w = GetWindow<TrackGenV2Window>("Track Gen V2");
            w.minSize = new Vector2(760, 520);
        }

        void OnGUI(){
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            _nl = EditorGUILayout.TextArea(_nl ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            // Main actions
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Parse -> Canonical", GUILayout.Height(26))){
                    var canon = NLEngine.ParseNL(_nl ?? "");
                    NLEngine.WritePlan(canon);
                }
                if (GUILayout.Button("Build & Run", GUILayout.Height(26))){
                    var canon = NLEngine.ParseNL(_nl ?? "");
                    NLEngine.WritePlan(canon);
                    NLEngine.RunPlan(false);
                }
                if (GUILayout.Button("Run Last Canonical", GUILayout.Height(26))){
                    EditorApplication.ExecuteMenuItem("Window/Aim2Pro/Track Creator/Run Last Canonical");
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("+","Insert NL snippet"), GUILayout.Width(28), GUILayout.Height(26))){
                    try { NLInsertLibrary.ShowInsertMenu(sn => { _nl = NLInsertLibrary.AppendWithNewline(_nl ?? "", sn); }); } catch {}
                }
            }

            // Quick amend line
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope()){
                GUILayout.Label("Quick amend:", GUILayout.Width(90));
                _amend = EditorGUILayout.TextField(_amend ?? "");
                if (GUILayout.Button("Apply & Run", GUILayout.Width(120), GUILayout.Height(22))){
                    RunSingleNL(_amend ?? "");
                }
            }

            // File ops
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("Open NL.input", GUILayout.Height(22))) { NLFileRunner.OpenNLInput(); }
                if (GUILayout.Button("Parse From File", GUILayout.Height(22))) { NLFileRunner.ParseFromFile(); }
                if (GUILayout.Button("Run From File", GUILayout.Height(22))) { NLFileRunner.RunFromFile(); }
            }

            // Edit Panel
            EditorGUILayout.Space(8);
            GUILayout.Label("Edit Panel (applies on existing track)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            // Delete rows
            EditorGUILayout.LabelField("Delete rows (remove rows A to B)");
            using (new EditorGUILayout.HorizontalScope()){
                drA = EditorGUILayout.IntField("A", drA);
                drB = EditorGUILayout.IntField("B", drB);
                if (GUILayout.Button("Append", GUILayout.Width(90))){
                    AppendNL($"remove rows {Mathf.Min(drA,drB)} to {Mathf.Max(drA,drB)}");
                }
                if (GUILayout.Button("Run", GUILayout.Width(70))){
                    RunSingleNL($"remove rows {Mathf.Min(drA,drB)} to {Mathf.Max(drA,drB)}");
                }
            }
            EditorGUILayout.Space(4);
            // Delete tiles
            EditorGUILayout.LabelField("Delete tiles (delete tiles CSV in row R)");
            using (new EditorGUILayout.HorizontalScope()){
                dtCSV = EditorGUILayout.TextField("CSV", dtCSV);
                dtRow = EditorGUILayout.IntField("Row", dtRow);
                if (GUILayout.Button("Append", GUILayout.Width(90))){
                    AppendNL($"delete tiles {dtCSV} in row {dtRow}");
                }
                if (GUILayout.Button("Run", GUILayout.Width(70))){
                    RunSingleNL($"delete tiles {dtCSV} in row {dtRow}");
                }
            }
            EditorGUILayout.Space(4);
            // Curve rows
            EditorGUILayout.LabelField("Curve rows");
            using (new EditorGUILayout.HorizontalScope()){
                cvA = EditorGUILayout.IntField("A", cvA);
                cvB = EditorGUILayout.IntField("B", cvB);
                cvLeft = EditorGUILayout.ToggleLeft("Left", cvLeft, GUILayout.Width(60));
                cvDeg = EditorGUILayout.IntField("Deg", cvDeg);
                if (GUILayout.Button("Append", GUILayout.Width(90))){
                    var side = cvLeft ? "left" : "right";
                    AppendNL($"curve rows {Mathf.Min(cvA,cvB)} to {Mathf.Max(cvA,cvB)} {side} {cvDeg}");
                }
                if (GUILayout.Button("Run", GUILayout.Width(70))){
                    var side = cvLeft ? "left" : "right";
                    RunSingleNL($"curve rows {Mathf.Min(cvA,cvB)} to {Mathf.Max(cvA,cvB)} {side} {cvDeg}");
                }
            }
            EditorGUILayout.Space(4);
            // S-Bends auto
            EditorGUILayout.LabelField("Auto S-bends");
            using (new EditorGUILayout.HorizontalScope()){
                sbCount = EditorGUILayout.IntField("Count", sbCount);
                sbDeg = EditorGUILayout.IntField("Deg", sbDeg);
                if (GUILayout.Button("Append", GUILayout.Width(90))){
                    AppendNL($"auto s-bends {Mathf.Max(1,sbCount)} at {sbDeg}");
                }
                if (GUILayout.Button("Run", GUILayout.Width(70))){
                    RunSingleNL($"auto s-bends {Mathf.Max(1,sbCount)} at {sbDeg}");
                }
            }
            EditorGUILayout.Space(4);
            // Slopes random auto
            EditorGUILayout.LabelField("Random slopes");
            using (new EditorGUILayout.HorizontalScope()){
                slMin = EditorGUILayout.FloatField("Min deg", slMin);
                slMax = EditorGUILayout.FloatField("Max deg", slMax);
                slSeg = EditorGUILayout.IntField("Segment len", slSeg);
                if (GUILayout.Button("Append", GUILayout.Width(90))){
                    AppendNL($"random slopes {slMin} to {slMax} degrees, segment {Mathf.Max(1,slSeg)}");
                }
                if (GUILayout.Button("Run", GUILayout.Width(70))){
                    RunSingleNL($"random slopes {slMin} to {slMax} degrees, segment {Mathf.Max(1,slSeg)}");
                }
            }
            EditorGUILayout.Space(4);
            // Jump gaps
            EditorGUILayout.LabelField("Insert jump gaps");
            using (new EditorGUILayout.HorizontalScope()){
                jgCount = EditorGUILayout.IntField("Count", jgCount);
                if (GUILayout.Button("Append", GUILayout.Width(90))){
                    AppendNL($"add {Mathf.Max(1,jgCount)} jump gaps");
                }
                if (GUILayout.Button("Run", GUILayout.Width(70))){
                    RunSingleNL($"add {Mathf.Max(1,jgCount)} jump gaps");
                }
            }
            EditorGUILayout.EndVertical();
        }

        void AppendNL(string line){
            if (string.IsNullOrEmpty(_nl)) _nl = line;
            else {
                if (!_nl.EndsWith("\n")) _nl += "\n";
                _nl += line;
            }
        }

        void RunSingleNL(string nl){
            if (string.IsNullOrWhiteSpace(nl)) return;
            var canon = NLEngine.ParseNL(nl);
            NLEngine.WritePlan(canon);
            NLEngine.RunPlan(false);
        }
    }
}
#endif
