#if UNITY_EDITOR
using UnityEngine;

namespace Aim2Pro.TrackCreator {
  // Editor helpers invoked by Track Lab + NL adapters.
  public static class TrackOps {

    // Delete the whole Track GO.
    public static void ClearTrack() {
      var t = GameObject.Find("Track");
      if (t) Object.DestroyImmediate(t);
      Debug.Log("[TrackLab] Cleared Track");
    }

    // Delete specific tiles in a given row (by 1-based column index).
    // Tries by name "Tile_r{row}_c{col}", then falls back to child index (col-1).
    public static void DeleteTilesInRow(int row, int[] cols) {
      var track = GameObject.Find("Track");
      if (!track) { Debug.LogWarning("[TrackOps] No Track object."); return; }

      var rowTr = track.transform.Find("Row_" + row);
      if (!rowTr) { Debug.LogWarning("[TrackOps] Row_" + row + " not found."); return; }

      if (cols == null || cols.Length == 0) return;

      foreach (var col in cols) {
        if (col <= 0) continue;

        Transform t = rowTr.Find($"Tile_r{row}_c{col}");
        if (!t && col - 1 >= 0 && col - 1 < rowTr.childCount)
          t = rowTr.GetChild(col - 1);

        if (t) {
          Object.DestroyImmediate(t.gameObject);
        } else {
          Debug.LogWarning($"[TrackOps] Tile col {col} not found in Row_{row}");
        }
      }
      Debug.Log($"[TrackLab] Deleted tiles [{string.Join(",", cols)}] in row {row}");
    }
  }
}
#endif
