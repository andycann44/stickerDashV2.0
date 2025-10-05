#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Aim2Pro.AIGG.NL {
  public static class V2CanonicalParser {
    public static string ParseToCanonical(string text) {
      var s = (text ?? "").ToLowerInvariant();
      string can = "seed(12345)\n";

      var m = Regex.Match(s, @"\b(?:build|rebuild)\s+(\d+)\s*(?:m|meter|meters)?\s*by\s*(\d+)\s*(?:m|meter|meters)?");
      if (m.Success) can += $"buildAbs({m.Groups[1].Value},{m.Groups[2].Value})\n";

      m = Regex.Match(s, @"random\s+tiles\s+missing\s+(\d+)%");
      if (m.Success) can += $"randomHoles({m.Groups[1].Value})\n";

      m = Regex.Match(s, @"row\s+gaps?\s+for\s+jumps?\s+(\d+)");
      if (m.Success) can += $"insertJumpGaps({m.Groups[1].Value})\n";

      m = Regex.Match(s, @"curve\s+rows?\s+(\d+)\s*(?:-|to)\s*(\d+)\s+(left|right)\s+(\d+)\s*(?:deg|degree|degrees|°)?");
      if (m.Success) can += $"curveRows({m.Groups[1].Value},{m.Groups[2].Value},{m.Groups[3].Value},{m.Groups[4].Value})\n";

      int sCount = 0;
      var mc = Regex.Match(s, @"(\d+)\s*(?:s\s*-?bends?|s\s*bend\s*curves?)");
      if (mc.Success) sCount = int.Parse(mc.Groups[1].Value);
      if (sCount == 0) {
        if (Regex.IsMatch(s, @"a\s+couple\s+of\s+s\s*-?bend")) sCount = 2;
        else if (Regex.IsMatch(s, @"several\s+s\s*-?bend")) sCount = 3;
        else if (Regex.IsMatch(s, @"s\s*-?bend")) sCount = 1;
      }
      if (sCount > 0) can += $"sBendAuto({sCount},8)\n";

      if (Regex.IsMatch(s, @"random\s+slopes?\s+on\s+the\s+straights?")) {
        can += "slopesRandomAuto(2,8,10)\n";
      }

      m = Regex.Match(s, @"remove\s+rows?\s+(\d+)\s*-\s*(\d+)");
      if (m.Success) can += $"deleteRows({m.Groups[1].Value},{m.Groups[2].Value})\n";

      m = Regex.Match(s, @"remove\s+tiles?\s+([\d,\\s]+)\s+row\s+(\d+)");
      if (m.Success) can += $"deleteTiles({m.Groups[1].Value}, row={m.Groups[2].Value})\n";

      return can.Trim();
    }

    public static string SaveCanonical(string content) {
      var dir = "StickerDash_Status";
      if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
      var path = Path.Combine(dir, "LastCanonical.plan");
      File.WriteAllText(path, content ?? "");
      return path;
    }

    public static bool TryRunLast() {
      try {
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in asms) {
          foreach (var t in asm.GetTypes()) {
            if (t.Name == "CanonicalRunner") {
              var m = t.GetMethod("RunLast", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
              if (m != null) { m.Invoke(null, null); return true; }
            }
          }
        }
      } catch (Exception ex) {
        Debug.LogWarning("[V2CanonicalParser] RunLast failed: " + ex.Message);
      }
      return false;
    }
  }
}
#endif
