
#if UNITY_EDITOR
using UnityEditor; using UnityEditor.Compilation; using UnityEngine;
namespace Aim2Pro.AIGG {
  public static class ForceRecompileMenu {
    [MenuItem("Window/Aim2Pro/Terminal/Force Recompile")]
    public static void ForceRecompile(){ AssetDatabase.SaveAssets(); CompilationPipeline.RequestScriptCompilation(); Debug.Log("[A2P] Requested script recompilation."); }
  }
}
#endif
