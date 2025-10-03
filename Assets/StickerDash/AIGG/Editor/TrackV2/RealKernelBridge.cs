#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG
{
    public static class Kernel
    {
        private static Transform FindTrack()
        {
            var go = GameObject.Find("Track");
            if (go) return go.transform;
            try { var tagged = GameObject.FindGameObjectWithTag("Track"); if (tagged) return tagged.transform; } catch {}
            Debug.LogWarning("[Kernel] Track root not found (name 'Track' or tag 'Track').");
            return null;
        }

        private static SortedDictionary<int, Transform> GetRows(Transform root)
        {
            var dict = new SortedDictionary<int, Transform>();
            if (!root) return dict;
            var rx = new Regex(@"\b(\d+)\b");
            foreach (Transform c in root)
            {
                var m = rx.Match(c.name);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var idx)) dict[idx] = c;
            }
            return dict;
        }

        public static void DeleteRowsRange(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "DeleteRowsRange");
            for (int i = start; i <= end; i++)
            {
                Transform t = root.Find($"Row_{i}") ?? root.Find($"Row {i}");
                if (!t)
                    foreach (Transform c in root) if (c.name.EndsWith("_"+i) || c.name.EndsWith(" "+i)) { t = c; break; }
                if (t) Object.DestroyImmediate(t.gameObject);
            }
            EditorUtility.SetDirty(root);
        }

        public static void OffsetRowsX(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsX");
            var rows = GetRows(root);
            for (int i = start; i <= end; i++) if (rows.TryGetValue(i, out var r)) { r.position += new Vector3(meters,0,0); EditorUtility.SetDirty(r.gameObject); }
        }

        public static void OffsetRowsY(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsY");
            var rows = GetRows(root);
            for (int i = start; i <= end; i++) if (rows.TryGetValue(i, out var r)) { r.position += new Vector3(0,meters,0); EditorUtility.SetDirty(r.gameObject); }
        }

        public static void StraightenRows(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "StraightenRows");
            var rows = GetRows(root);
            for (int i = start; i <= end; i++) if (rows.TryGetValue(i, out var r)) { r.rotation = Quaternion.identity; EditorUtility.SetDirty(r.gameObject); }
        }

        public static void AppendStraight(float distance, float step = 1f)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRows(root); if (rows.Count == 0) { Debug.LogWarning("[Kernel] No rows under Track."); return; }
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "AppendStraight");
            int lastIndex = 0; Transform lastRow = null; foreach (var kv in rows) { lastIndex = kv.Key; lastRow = kv.Value; }
            int count = Mathf.Max(1, Mathf.RoundToInt(distance / Mathf.Max(0.0001f, step)));
            float dz = step;
            for (int i = 1; i <= count; i++)
            {
                var clone = Object.Instantiate(lastRow.gameObject, lastRow.parent);
                clone.name = $"Row_{lastIndex + i}";
                clone.transform.position = lastRow.position + new Vector3(0,0,dz * i);
                EditorUtility.SetDirty(clone);
            }
            EditorUtility.SetDirty(root);
        }

        public static void AppendArc(string side, float deg, float arcLen = 0f, int steps = 0)
            => Debug.Log($"[Kernel] AppendArc(side:{side},deg:{deg}) — TODO");

        public static void BuildSplineFromTrack(float width = 3f, float top = 0f, float thickness = 0.2f, bool replace = false)
            => Debug.Log("[Kernel] BuildSplineFromTrack — TODO");

        public static void SetWidth(float width) => Debug.Log($"[Kernel] SetWidth({width})");
        public static void SetThickness(float thickness) => Debug.Log($"[Kernel] SetThickness({thickness})");
        public static void Resample(float density) => Debug.Log($"[Kernel] Resample({density})");
    }
}
#endif
