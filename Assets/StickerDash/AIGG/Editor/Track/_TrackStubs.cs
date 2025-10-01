#if UNITY_EDITOR
using UnityEditor; using UnityEngine;
namespace Aim2Pro.AIGG.Track {
  public class TrackGenV2Window : EditorWindow {
    [MenuItem("Window/Aim2Pro/Track Creator/Track Gen V2")]
    public static void Open(){ GetWindow<TrackGenV2Window>("Track Gen V2"); }
  }
  public class TrackGeneratorWindow : EditorWindow {
    [MenuItem("Window/Aim2Pro/Track Creator/Track Generator")]
    public static void Open(){ GetWindow<TrackGeneratorWindow>("Track Generator"); }
  }
  public static class CanonicalRunner {
    [MenuItem("Window/Aim2Pro/Track Creator/Run Last Canonical")]
    public static void RunLast(){ Debug.Log("[A2P] CanonicalRunner stub."); }
  }
  public static class TrackSceneBuilder {}
}
#endif
