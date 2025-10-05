#if UNITY_EDITOR
using UnityEngine;

namespace Aim2Pro.TrackCreator {
  public static class TrackOps {
    public static void ClearTrack() {
      var t = GameObject.Find("Track");
      if (t) Object.DestroyImmediate(t);
      Debug.Log("[TrackLab] Cleared Track");
    }
  }
}
#endif
