#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

namespace Aim2Pro.AIGG.NL {
  public static class V2NLAdapter {
    public static bool TryApply(string text, Action<string> log = null) {
      if (string.IsNullOrWhiteSpace(text)) return false;
      var asms = AppDomain.CurrentDomain.GetAssemblies();

      Type engineType = null;
      foreach (var asm in asms) {
        var names = new string[] {
          "V2CommandEngine",
          "Aim2Pro.AIGG.TrackV2.V2CommandEngine",
          "Aim2Pro.TrackCreator.V2CommandEngine",
          "Aim2Pro.AIGG.V2CommandEngine"
        };
        foreach (var n in names) { engineType = asm.GetType(n, false); if (engineType!=null) break; }
        if (engineType!=null) break;
        foreach (var t in asm.GetTypes()) { if (t.Name=="V2CommandEngine") { engineType=t; break; } }
        if (engineType!=null) break;
      }
      if (engineType==null) { Debug.LogWarning("[V2NL] V2CommandEngine not found"); return false; }

      object inst = null;
      var ctorMsg = engineType.GetConstructor(new Type[]{ typeof(Action<string>) });
      if (ctorMsg!=null) inst = ctorMsg.Invoke(new object[]{ new Action<string>(s => { if (log!=null) log(s); }) });
      if (inst==null) {
        var ctor0 = engineType.GetConstructor(Type.EmptyTypes);
        if (ctor0!=null) inst = ctor0.Invoke(null);
      }
      if (inst==null) { Debug.LogWarning("[V2NL] Cannot construct V2CommandEngine"); return false; }

      var mLoad = engineType.GetMethod("LoadRules", System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
      var mParse= engineType.GetMethod("Parse",     System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance, null, new Type[]{ typeof(string) }, null);
      var mApply= engineType.GetMethod("Apply",     System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
      if (mLoad==null || mParse==null || mApply==null) { Debug.LogWarning("[V2NL] Engine methods missing"); return false; }

      try {
        mLoad.Invoke(inst, null);                 // reads Assets/Resources/TrackV2/commands.json
        mParse.Invoke(inst, new object[]{ text });
        mApply.Invoke(inst, null);                // should hit your Kernel (RealKernelBridge)
        return true;
      } catch (Exception ex) {
        Debug.LogError("[V2NL] " + ex.Message);
        return false;
      }
    }
  }
}
#endif
