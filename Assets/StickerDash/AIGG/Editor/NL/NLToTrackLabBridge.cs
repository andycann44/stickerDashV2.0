#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.NLBridge
{
    public class NLToTrackLabBridge : EditorWindow
    {
        [MenuItem("Window/Aim2Pro/Test/NL -> TrackLab Bridge")]
        public static void Open() => GetWindow<NLToTrackLabBridge>("NL -> TrackLab");

        string _nl = "120m by 3m, left curve 30 deg, seed 101, add start countdown, add dartboards";
        string _json = "";
        string _status = "Ready.";

        void OnGUI()
        {
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            _nl = EditorGUILayout.TextArea(_nl, GUILayout.MinHeight(60));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Parse NL -> JSON", GUILayout.Height(26))) TryParse();
                if (GUILayout.Button("Copy JSON to Clipboard", GUILayout.Height(26)))
                {
                    EditorGUIUtility.systemCopyBuffer = _json;
                    _status = string.IsNullOrEmpty(_json)
                        ? "Nothing to copy (parse first)."
                        : "Copied JSON. Paste into Track Lab.";
                }
                if (GUILayout.Button("Send to Track Lab (try)", GUILayout.Height(26))) _status = TrySendToTrackLab(_json);
                if (GUILayout.Button("Inspect Track Lab", GUILayout.Height(26))) _status = InspectTrackLab();
            }

            GUILayout.Space(6);
            GUILayout.Label("Canonical JSON (preview)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_json, GUILayout.MinHeight(120));
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }

        void TryParse()
        {
            try
            {
                // TODO: wire to your real NL->JSON parser. Placeholder so the window compiles.
                _json = "{\"track\":{\"length\":120,\"width\":3}}";
                _status = "Parsed.";
            }
            catch (Exception ex)
            {
                _json = "";
                _status = "Parse error: " + ex.Message;
                Debug.LogException(ex);
            }
        }

        string TrySendToTrackLab(string json)
        {
            try
            {
                var trackLabWin = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .FirstOrDefault(w => w.titleContent != null &&
                                         w.titleContent.text.IndexOf("Track Lab", StringComparison.OrdinalIgnoreCase) >= 0);

                if (trackLabWin == null)
                    return "Track Lab window not found. Open it and try again.";

                var t = trackLabWin.GetType();
                string[] candidates =
                {
                    "jsonSpecInline", "JsonSpecInline", "m_JsonSpecInline",
                    "jsonSpecString", "currentJson", "CurrentJson", "m_JSON"
                };

                foreach (var name in candidates)
                {
                    var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null && f.FieldType == typeof(string))
                    {
                        f.SetValue(trackLabWin, json);
                        trackLabWin.Repaint();
                        return "Set Track Lab field '" + name + "'.";
                    }
                }

                foreach (var name in candidates)
                {
                    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                    {
                        p.SetValue(trackLabWin, json, null);
                        trackLabWin.Repaint();
                        return "Set Track Lab property '" + name + "'.";
                    }
                }

                return "Could not find a string field/property to set. Use Clipboard copy as fallback, or tell me the name from Inspect.";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return "Reflection send failed; use Clipboard for now.";
            }
        }

        string InspectTrackLab()
        {
            try
            {
                var win = Resources.FindObjectsOfTypeAll<EditorWindow>()
                    .FirstOrDefault(w => w.titleContent != null &&
                                         w.titleContent.text.IndexOf("Track Lab", StringComparison.OrdinalIgnoreCase) >= 0);

                if (win == null) return "Track Lab window not found.";

                var t = win.GetType();
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => f.FieldType == typeof(string)).ToArray();
                var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => p.PropertyType == typeof(string) && p.CanRead).ToArray();

                Debug.Log("[Bridge] TrackLab type: " + t.FullName);
                foreach (var f in fields)
                {
                    string val = f.GetValue(win) as string ?? "";
                    Debug.Log("[Bridge] string field: " + f.Name + " = \"" + (val.Length > 80 ? val.Substring(0, 80) + "…" : val) + "\"");
                }
                foreach (var p in props)
                {
                    string val = p.GetValue(win, null) as string ?? "";
                    Debug.Log("[Bridge] string prop:  " + p.Name + " = \"" + (val.Length > 80 ? val.Substring(0, 80) + "…" : val) + "\"");
                }

                return "Logged " + fields.Length + " string fields and " + props.Length +
                       " string props to Console. Find the JSON-looking one and tell me its exact name.";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return "Inspect failed (see Console).";
            }
        }
    }
}
#endif
