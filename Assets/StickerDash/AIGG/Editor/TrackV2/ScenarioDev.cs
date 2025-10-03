#if UNITY_EDITOR
using UnityEditor;

namespace Aim2Pro.AIGG.TrackV2
{
    public static class ScenarioDev
    {
        [MenuItem("Window/Aim2Pro/Track Creator/Generate Sample Level (300x6 simple)")]
        public static void GenSample()
        {
            Aim2Pro.AIGG.Kernel.GenerateScenarioFromPrompt(
                300f, 6f,
                "10% tiles missing, random bends up to 30 degrees either way, split, slight ups and downs, low speed, simple"
            );
        }

        [MenuItem("Window/Aim2Pro/Track Creator/Clear Track Tiles")]
        public static void Clear()
        {
            // Reuse bridge method by calling a no-op delete range across a huge span:
            // or implement a dedicated clearer if preferred.
            var t = UnityEngine.GameObject.Find("A2P_Track") ?? UnityEngine.GameObject.Find("Track");
            if (!t) return;
            var root = t.transform;
            var rx = new System.Text.RegularExpressions.Regex(@"^tile_r\d+_c\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Clear Track Tiles");
            var del = new System.Collections.Generic.List<UnityEngine.GameObject>();
            foreach (UnityEngine.Transform c in root) if (rx.IsMatch(c.name)) del.Add(c.gameObject);
            foreach (var g in del) UnityEngine.Object.DestroyImmediate(g);
        }
    }
}
#endif
