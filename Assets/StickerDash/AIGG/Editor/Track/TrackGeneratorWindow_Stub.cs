#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.Track {
  // Stub to satisfy legacy references. No MenuItem (we keep single-window policy).
  public class TrackGeneratorWindow : EditorWindow {
    public static void Open()      => GetWindow<TrackGeneratorWindow>("Track Generator (stub)");
    public static void ShowWindow()=> Open();

    void OnGUI() {
      EditorGUILayout.HelpBox(
        "Track Generator is retired. Use Window → Aim2Pro → Track Creator → Track Lab (All-in-One).",
        MessageType.Info);
      if (GUILayout.Button("Open Track Lab")) Aim2Pro.TrackCreator.TrackLab.Open();
    }
  }
}
#endif
