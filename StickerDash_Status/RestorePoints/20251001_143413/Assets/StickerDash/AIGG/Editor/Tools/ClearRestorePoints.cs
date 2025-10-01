
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;
namespace Aim2Pro.AIGG.Tools {
  public static class ClearRestorePoints {
    [MenuItem("Window/Aim2Pro/Tools/Clear All Restore Points…")]
    public static void Clear(){
      string root = Path.Combine("StickerDash_Status","RestorePoints");
      if(!Directory.Exists(root)){ Debug.Log("[A2P] No restore points."); return; }
      if(!EditorUtility.DisplayDialog("Clear Restore Points","Delete ALL restore points under "+root+" ?","Delete","Cancel")) return;
      Directory.Delete(root, true); AssetDatabase.Refresh();
      Debug.Log("[A2P] Cleared restore points.");
    }
  }
}
#endif
