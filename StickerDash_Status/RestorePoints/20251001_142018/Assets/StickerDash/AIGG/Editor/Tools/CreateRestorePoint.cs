
#if UNITY_EDITOR
using UnityEditor; using UnityEngine; using System.IO;
namespace Aim2Pro.AIGG.Tools {
  public static class CreateRestorePoint {
    [MenuItem("Window/Aim2Pro/Tools/Create Restore Point")]
    public static void Create(){
      string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
      string dest = Path.Combine("StickerDash_Status","RestorePoints", ts);
      Directory.CreateDirectory(dest);
      CopyDir("Assets", Path.Combine(dest,"Assets"));
      CopyDir("ProjectSettings", Path.Combine(dest,"ProjectSettings"));
      if(Directory.Exists("Packages")) CopyDir("Packages", Path.Combine(dest,"Packages"));
      AssetDatabase.Refresh();
      Debug.Log("[A2P] Restore Point created at: "+dest);
    }
    static void CopyDir(string src, string dst){
      foreach(var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        Directory.CreateDirectory(dir.Replace(src, dst));
      foreach(var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        File.Copy(file, file.Replace(src, dst), true);
    }
  }
}
#endif
