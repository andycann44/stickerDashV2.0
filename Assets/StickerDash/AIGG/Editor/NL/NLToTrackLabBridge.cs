#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Aim2Pro.AIGG.NL; // uses the NLTester engine we added

namespace Aim2Pro.AIGG.NLBridge
{
    public class NLToTrackLabBridge : EditorWindow
    {
        [MenuItem("Window/Aim2Pro/Test/NL -> TrackLab Bridge")]
        public static void Open() => GetWindow<NLToTrackLabBridge>("NL -> TrackLab");

        string _nl = "120m by 3m, left curve 30Â°, seed 101, add start countdown, add dartboards";
        string _json = "";
        string _status = "Ready.";

        void OnGUI()
        {
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            _nl = EditorGUILayout.TextArea(_nl, GUILayout.MinHeight(60));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse NL "†’ JSON", GUILayout.Height(26)))
                {
                    try
                    {
                        var engine = new IntentEngine();
                        var spec = engine.Parse(_nl, out var _);
                        _json = spec.ToJson();
                        _status = "Parsed.";
                    }
                    catch (Exception ex)
                    {
                        _json = "";
                        _status = "Parse error: " + ex.Message;
                        Debug.LogException(ex);
                    }
                }
                if (GUILayout.Button("Copy JSON to Clipboard", GUILayout.Height(26)))
                {
                    EditorGUIUtility.systemCopyBuffer = _json;
                    _status = string.IsNullOrEmpty(_json) ? "Nothing to copy (parse first)." : "Copied JSON. Paste into Track Lab.";
                }
                if (GUILayout.Button("Send to Track Lab (try)", GUILayout.Height(26)))
                {
                    _status = TrySendToTrackLab(_json);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Canonical JSON (preview)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_json, GUILayout.MinHeight(140));

            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        string InspectTrackLab()
        {
            try
            {
                var win = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .FirstOrDefault(w => w.titleContent != null && w.titleContent.text.IndexOf("Track Lab", StringComparison.OrdinalIgnoreCase) >= 0);
                if (win == null) return "Track Lab window not found.";
                var t = win.GetType();
                var fields = t.GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)
                                .Where(f => f.FieldType == typeof(string)).ToArray();
                var props  = t.GetProperties(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)
                                .Where(p => p.PropertyType == typeof(string) && p.CanRead).ToArray();
                Debug.Log( TrackLab type: {t.FullName}");
                foreach (var f in fields) Debug.Log( string field: {f.Name} = "{(f.GetValue(win) as string ?? "")}"");
                foreach (var p in props)  Debug.Log( string prop:  {p.Name} = "{(p.GetValue(win,null) as string ?? "")}"");
                return  Logged {fields.Length} string fields and {props.Length} string props to Console. Look for JSON-looking ones.";
            }
            catch (Exception ex) { Debug.LogException(ex); return "Inspect failed (see Console)."; }
        }

    string TrySendToTrackLab(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "No JSON to send.";
            try
            {
                // Find an open EditorWindow with title containing "Track Lab"
                var trackLabWin = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .FirstOrDefault(w => w.titleContent != null && w.titleContent.text.IndexOf("Track Lab", StringComparison.OrdinalIgnoreCase) >= 0);

                if (trackLabWin == null) return "Track Lab window not found. (Open it and try again.)";

                var t = trackLabWin.GetType();
                // Try common field/property names
                string[] candidates = { "jsonSpecInline", "JsonSpecInline", "m_JsonSpecInline" };
                foreach (var name in candidates)
                {
                    var f = t.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(string))
                    { f.SetValue(trackLabWin, json); trackLabWin.Repaint(); return $"Set {name} on Track Lab."; }

                    var p = t.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                    { p.SetValue(trackLabWin, json); trackLabWin.Repaint(); return $"Set {name} on Track Lab."; }
                }
                return "Could not find jsonSpecInline on Track Lab. Clipboard copy works ""” paste manually.";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return "Reflection send failed; use Clipboard.";
            }
        }
    }
}
#endif
