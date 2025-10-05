#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Aim2Pro.AIGG.NL {
  /// SpecNL: load delete-tiles patterns from your spec JSON (commands/registry/etc.).
  /// If none found, falls back to built-in patterns.
  public static class SpecNL {
    static bool _loaded;
    static readonly List<Regex> _delTiles = new List<Regex>();

    static void LoadOnce() {
      if (_loaded) return;
      _loaded = true;

      // Find a spec directory:
      // Prefer StickerDash_Status/NL/spec/, otherwise newest StickerDash_Status/NL_Snapshot_*/spec/
      string specDir = null;
      var direct = Path.Combine("StickerDash_Status", "NL", "spec");
      if (Directory.Exists(direct)) specDir = direct;
      if (specDir == null) {
        var root = "StickerDash_Status";
        if (Directory.Exists(root)) {
          var snaps = Directory.GetDirectories(root, "NL_Snapshot_*")
                               .OrderByDescending(p => p)
                               .ToList();
          foreach (var s in snaps) {
            var cand = Path.Combine(s, "spec");
            if (Directory.Exists(cand)) { specDir = cand; break; }
          }
        }
      }

      // Try to extract regexes from any *.json in specDir
      if (!string.IsNullOrEmpty(specDir)) {
        try {
          foreach (var file in Directory.GetFiles(specDir, "*.json", SearchOption.TopDirectoryOnly)) {
            var text = File.ReadAllText(file);
            // Heuristic: grab any quoted strings that look like regex for delete tiles + row
            // e.g. "^delete\\s+tiles? ... in\\s+row ..."
            foreach (Match m in Regex.Matches(
                text,
                "\"([^\"]*delete[^\"]*tile[^\"]*row[^\"]*)\"",
                RegexOptions.IgnoreCase)) {
              var pattern = m.Groups[1].Value;
              // Must contain \d or ( ) to be a regex-y thing
              if (!Regex.IsMatch(pattern, @"\\d|\(|\^|\\s", RegexOptions.IgnoreCase)) continue;
              TryAddRegex(pattern);
            }
            // Also accept objects like {"re":"...","action":"deleteTilesInRow"...}
            foreach (Match m in Regex.Matches(
                text,
                "\"re\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)+)\"\\s*,\\s*\"action\"\\s*:\\s*\"([^\"]+)\"",
                RegexOptions.IgnoreCase)) {
              var pattern = m.Groups[1].Value;
              var action  = m.Groups[2].Value;
              if (action.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0 &&
                  action.IndexOf("tile",   StringComparison.OrdinalIgnoreCase) >= 0) {
                TryAddRegex(pattern);
              }
            }
          }
        } catch (Exception ex) {
          Debug.LogWarning("[SpecNL] Failed reading spec: " + ex.Message);
        }
      }

      // Fallbacks if spec had nothing useful
      if (_delTiles.Count == 0) {
        TryAddRegex(@"^delete\s+tile\s+(\d+)\s+in\s+row\s+(\d+)$");
        TryAddRegex(@"^delete\s+tiles?\s+([\d,\s]+)\s+in\s+row\s+(\d+)$");
      }
    }

    static void TryAddRegex(string pattern) {
      if (string.IsNullOrEmpty(pattern)) return;
      try {
        // Unescape any JSON-escaped backslashes
        pattern = pattern.Replace("\\\\", "\\");
        var re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        _delTiles.Add(re);
      } catch (Exception ex) {
        Debug.LogWarning("[SpecNL] Skip bad regex: " + pattern + " (" + ex.Message + ")");
      }
    }

    public static bool TryApplyDeleteTiles(string line) {
      LoadOnce();
      foreach (var re in _delTiles) {
        var m = re.Match(line);
        if (!m.Success) continue;

        // Prefer named groups if present
        int row = -1;
        var tilesList = new List<int>();

        Group gRow   = m.Groups["row"];
        Group gTiles = m.Groups["tiles"];

        if (gRow != null && gRow.Success) int.TryParse(gRow.Value, out row);
        if (gTiles != null && gTiles.Success) {
          foreach (var p in gTiles.Value.Split(new[]{","," ","\t"}, StringSplitOptions.RemoveEmptyEntries)) {
            if (int.TryParse(p, out var n) && n > 0) tilesList.Add(n);
          }
        }

        // If unnamed, guess: first group = tiles (single or list), second = row
        if (row <= 0) {
          var nums = m.Groups.Cast<Group>().Skip(1).Where(g=>g.Success).Select(g=>g.Value).ToList();
          if (nums.Count >= 2) {
            // last looks like row
            int.TryParse(nums.Last(), out row);
            var tilesStr = string.Join(" ", nums.Take(nums.Count - 1));
            foreach (var p in tilesStr.Split(new[]{","," ","\t"}, StringSplitOptions.RemoveEmptyEntries)) {
              if (int.TryParse(p, out var n) && n > 0) tilesList.Add(n);
            }
          }
        }

        if (row > 0 && tilesList.Count > 0) {
          Aim2Pro.TrackCreator.TrackOps.DeleteTilesInRow(row, tilesList.ToArray());
          return true;
        }
      }
      return false;
    }
  }
}
#endif
