using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class A2P_DedupTileNames
{
    [MenuItem("Window/Aim2Pro/Tools/Dedup Tile Names (Selected Parent or Scene)")]
    public static void Dedup()
    {
        var roots = Selection.activeTransform
            ? new Transform[]{ Selection.activeTransform }
            : UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Select(g=>g.transform).ToArray();

        var tiles = new List<GameObject>();
        foreach (var r in roots)
            tiles.AddRange(r.GetComponentsInChildren<Transform>(true).Select(t=>t.gameObject).Where(go => go.name.StartsWith("tile")));

        var seen = new Dictionary<string,int>();
        int changes = 0;
        foreach (var go in tiles)
        {
            var n = go.name;
            if (!seen.ContainsKey(n)) { seen[n]=1; continue; }
            seen[n]++;
            Undo.RecordObject(go, "Dedup Name");
            go.name = n + "__" + seen[n].ToString("000");
            changes++;
        }
        Debug.Log($"[A2P] Dedup complete: {changes} renamed (added __NNN to duplicates).");
    }
}
