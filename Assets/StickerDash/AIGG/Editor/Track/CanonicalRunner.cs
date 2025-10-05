#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Aim2Pro.TrackCreator {
  // No MenuItem: TrackLab calls this via reflection.
  public static class CanonicalRunner {
    public static void RunLast() {
      var path = "StickerDash_Status/LastCanonical.plan";
      if (!File.Exists(path)) { Debug.LogWarning("[CanonicalRunner] No plan at " + path); return; }
      var can = File.ReadAllText(path);
      Debug.Log("[CanonicalRunner] (stub) would execute canonical:\n" + can);
    }
  }
}
#endif
