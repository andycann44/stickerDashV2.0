#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Aim2Pro.AIGG.NL; // uses the IntentEngine from NLTester

namespace Aim2Pro.AIGG.NLBridge
{
    public class NLToTrackLabBridge : EditorWindow
    {
        [MenuItem("Window/Aim2Pro/Test/NL → TrackLab Bridge")]
        public static void Open() => GetWindow<NLToTrackLabBridge>("NL → TrackLab");

        private string _nl = "120m by 3m, left curve 30°, seed 101, add start countdown, add dartboards";
        private string _json = "";
        private string _status = "Ready.";

        private void OnGUI()
        {
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            _nl = EditorGUILayout.TextArea(_nl, GUILayout.MinHeight(60));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse NL → JSON", GUILayout.Height(26))) TryParse();
                if (GUILayout.Button("Copy JSON to Clipboard", GUILayout.Height(26)))
                {
                    EditorGUIUtility.systemCopyBuffer = _json;
                    _status = string.IsNullOrEmpty(_json) ? "Nothing to copy (parse first)." : "Copied JSON. Paste into Track Lab.";
                }
                if (GUILayout.Button("Send to Track Lab (try)", GUILayout.Height(26))) _status = TrySendToTrackLab(_json);
                if (GUILayout.Button("Inspect Track Lab", GUILayout.Height(26))) _status = InspectTrackLab();
            }

            GUILayout.Space(6);
            GUILayout.Label("Canonical JSON (preview)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_json, GUILayout.MinHeight(140));
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        private void TryParse()
        {
            try
            {
                var engine = new IntentEngine();
                var spec = engine.Parse(_nl, out _);
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

        private string TrySendToTrackLab(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return "No JSON to send. Parse first.";
            try
            {
                var trackLabWin = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .FirstOrDefault(w => w.titleContent != null && w.titleContent.text.IndexOf("Track Lab", StringComparison.OrdinalIgnoreCase) >= 0);
                if (trackLabWin == null) return "Track Lab window not found. Open it and try again.";

                var t = trackLabWin.GetType();
                string[] candidates = { "jsonSpecInline","JsonSpecInline","m_JsonSpecInline","jsonSpecString","currentJson","CurrentJson","m_JSON" };

                foreach (var name in candidates)
                {
                    var f = t.GetField(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(string)) { f.SetValue(trackLabWin, json); trackLabWin.Repaint(); return $"Set field '{name}'."; }
                }
                foreach (var name in candidates)
                {
                    var p = t.GetProperty(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                    if (p != null && p.CanWrite && p.PropertyType == typeof(string)) { p.SetValue(trackLabWin, json, null); trackLabWin.Repaint(); return $"Set property '{name}'."; }
                }
                return "Could not find a string field/property to set. Use Clipboard for now, or read name via Inspect.";
            }
            catch (Exception ex) { Debug.LogException(ex); return "Reflection send failed; use Clipboard."; }
        }

        private string InspectTrackLab()
        {
            try
            {
                var win = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .FirstOrDefault(w => w.titleContent != null && w.titleContent.text.IndexOf("Track Lab", StringComparison.OrdinalIgnoreCase) >= 0);
                if (win == null) return "Track Lab window not found.";

                var t = win.GetType();
                var fields = t.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                              .Where(f => f.FieldType == typeof(string)).ToArray();
                var props  = t.GetProperties(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                              .Where(p => p.PropertyType == typeof(string) && p.CanRead).ToArray();

                Debug.Log($"[Bridge] TrackLab type: {t.FullName}");
                foreach (var f in fields) Debug.Log($"[Bridge] string field: {f.Name}");
                foreach (var p in props)  Debug.Log($"[Bridge] string prop:  {p.Name}");
                return $"Logged {fields.Length} fields and {props.Length} props to Console. Use the JSON-looking name in candidates if needed.";
            }
            catch (Exception ex) { Debug.LogException(ex); return "Inspect failed (see Console)."; }
        }
    }
}
#endif
