#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;

namespace Aim2Pro.AIGG.Tools {
  public static class CreateRestorePoint {
    [MenuItem("Window/Aim2Pro/Tools/Create Restore Point")]
    public static void Create(){
      string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string destRoot = Path.Combine("StickerDash_Status","RestorePoints", ts);
      try {
        Directory.CreateDirectory(destRoot);
        CopyDir("Assets", Path.Combine(destRoot,"Assets"));
        CopyDir("ProjectSettings", Path.Combine(destRoot,"ProjectSettings"));
        if (Directory.Exists("Packages")) CopyDir("Packages", Path.Combine(destRoot,"Packages"));
        AssetDatabase.Refresh();
        Debug.Log("[A2P] Restore Point created at: " + Path.GetFullPath(destRoot));
      } catch (System.Exception e) {
        Debug.LogError("[A2P] Restore Point failed: " + e.Message);
      }
    }

    static void CopyDir(string src, string dst){
      if (!Directory.Exists(src)) return;
      Directory.CreateDirectory(dst); // ensure root exists
      foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories)) {
        var d = dir.Replace(src, dst);
        Directory.CreateDirectory(d);
      }
      foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories)) {
        var target = file.Replace(src, dst);
        var parent = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        File.Copy(file, target, true);
      }
      // Note: GetFiles(... AllDirectories) already includes files at the src root.
    }
  }
}
#endif
