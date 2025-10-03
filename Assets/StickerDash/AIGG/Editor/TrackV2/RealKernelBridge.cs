#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG
{
    /// <summary>
    /// Real kernel bridge for Track Gen v2 using tile children named: tile_r{row}_c{col}
    /// Parent is typically "A2P_Track".
    /// Forward axis assumed +Z (meters).
    /// </summary>
    public static class Kernel
    {
        private static readonly Regex TileRx = new Regex(@"^tile_r(?<r>\d+)_c(?<c>\d+)$", RegexOptions.IgnoreCase);

        private static Transform FindTrack()
        {
            // 1) Preferred names
            var go = GameObject.Find("A2P_Track") ?? GameObject.Find("Track");
            if (go) return go.transform;

            // 2) Tag
            try { var tagged = GameObject.FindGameObjectWithTag("Track"); if (tagged) return tagged.transform; } catch {}

            // 3) Heuristic: any root with many tile_r?_c? children
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var t = root.transform;
                int matches = 0;
                foreach (Transform c in t)
                    if (TileRx.IsMatch(c.name)) { matches++; if (matches >= 3) return t; }
            }

            Debug.LogWarning("[Kernel] Track root not found (looked for 'A2P_Track' / 'Track' / tag 'Track').");
            return null;
        }

        private static Dictionary<int, List<Transform>> GetRowGroups(Transform trackRoot)
        {
            var rows = new Dictionary<int, List<Transform>>();
            if (!trackRoot) return rows;

            foreach (Transform c in trackRoot)
            {
                var m = TileRx.Match(c.name);
                if (!m.Success) continue;
                int r = int.Parse(m.Groups["r"].Value);
                if (!rows.TryGetValue(r, out var list)) rows[r] = list = new List<Transform>();
                list.Add(c);
            }
            return rows;
        }

        private static bool TryParseRC(string name, out int r, out int c)
        {
            var m = TileRx.Match(name);
            if (m.Success)
            {
                r = int.Parse(m.Groups["r"].Value);
                c = int.Parse(m.Groups["c"].Value);
                return true;
            }
            r = c = 0; return false;
        }

        // --------- Ops ----------

        public static void DeleteRowsRange(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "DeleteRowsRange");
            for (int r = start; r <= end; r++)
            {
                if (!rows.TryGetValue(r, out var tiles)) continue;
                foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
            }
            EditorUtility.SetDirty(root);
        }

        public static void OffsetRowsX(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsX");
            for (int r = start; r <= end; r++)
            {
                if (!rows.TryGetValue(r, out var tiles)) continue;
                foreach (var t in tiles)
                {
                    t.position += new Vector3(meters, 0f, 0f);
                    EditorUtility.SetDirty(t.gameObject);
                }
            }
        }

        public static void OffsetRowsY(int start, int end, float meters)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "OffsetRowsY");
            for (int r = start; r <= end; r++)
            {
                if (!rows.TryGetValue(r, out var tiles)) continue;
                foreach (var t in tiles)
                {
                    t.position += new Vector3(0f, meters, 0f);
                    EditorUtility.SetDirty(t.gameObject);
                }
            }
        }

        public static void StraightenRows(int start, int end)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "StraightenRows");
            for (int r = start; r <= end; r++)
            {
                if (!rows.TryGetValue(r, out var tiles)) continue;
                foreach (var t in tiles)
                {
                    t.rotation = Quaternion.identity;
                    EditorUtility.SetDirty(t.gameObject);
                }
            }
        }

        public static void AppendStraight(float distance, float step = 1f)
        {
            var root = FindTrack(); if (!root) return;
            var rows = GetRowGroups(root);
            if (rows.Count == 0) { Debug.LogWarning("[Kernel] No tiles named tile_r*_c* under track root."); return; }

            int lastRow = rows.Keys.Max();
            var lastTiles = rows[lastRow];
            if (lastTiles == null || lastTiles.Count == 0) { Debug.LogWarning("[Kernel] Last row empty."); return; }

            int copies = Mathf.Max(1, Mathf.RoundToInt(distance / Mathf.Max(0.0001f, step)));
            float dz = step; // forward is +Z

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "AppendStraight");
            for (int i = 1; i <= copies; i++)
            {
                foreach (var src in lastTiles)
                {
                    if (!TryParseRC(src.name, out var _, out var col)) continue;
                    var clone = Object.Instantiate(src.gameObject, src.parent);
                    int newRow = lastRow + i;
                    clone.name = $"tile_r{newRow}_c{col}";
                    var p = src.position + new Vector3(0f, 0f, dz * i);
                    clone.transform.position = p;
                    clone.transform.rotation = src.rotation;
                    clone.transform.localScale = src.localScale;
                    EditorUtility.SetDirty(clone);
                }
            }
            EditorUtility.SetDirty(root);
        }

        // Stubs we’ll fill later
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
