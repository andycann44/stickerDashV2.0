using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Aim2Pro
{
    [InitializeOnLoad]
    public static class A2PToast
    {
        class Entry { public string msg; public double until; }
        static readonly List<Entry> entries = new List<Entry>();
        static GUIStyle style;

        static A2PToast()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += _ => SceneView.RepaintAll();
        }

        public static void Show(string msg, float seconds = 2f)
        {
            entries.Add(new Entry { msg = msg, until = EditorApplication.timeSinceStartup + seconds });
            SceneView.RepaintAll();
            EditorApplication.Beep();
            Debug.Log("[A2P] " + msg);
        }

        static void OnSceneGUI(SceneView sv)
        {
            entries.RemoveAll(e => e.until < EditorApplication.timeSinceStartup);
            if (entries.Count == 0) return;

            if (style == null)
            {
                style = new GUIStyle(EditorStyles.helpBox)
                { fontSize = 12, wordWrap = true, alignment = TextAnchor.UpperLeft, padding = new RectOffset(8,8,8,8) };
            }

            Handles.BeginGUI();
            float y = 8f;
            foreach (var e in entries)
            {
                var content = new GUIContent(e.msg);
                var size = style.CalcSize(content);
                var rect = new Rect(8, y, Mathf.Min(420, size.x + 16), Mathf.Max(28, size.y + 8));
                GUI.Label(rect, content, style);
                y += rect.height + 6;
            }
            Handles.EndGUI();
        }
    }

    public static class A2PToastMenu
    {
        [MenuItem("Window/Aim2Pro/Terminal/Ping Toast")]
        public static void Ping() => A2PToast.Show("Toast OK — editor overlay working.");
    }
}
