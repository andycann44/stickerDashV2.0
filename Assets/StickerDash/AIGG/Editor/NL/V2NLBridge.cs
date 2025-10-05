#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Aim2Pro.AIGG.NL {
  public static class V2NLBridge {
    class Cand { public string T; public string M; public Cand(string t,string m){ T=t; M=m; } }

    // Known locations for your existing NL→canonical converter
    static readonly Cand[] Cands = new Cand[] {
      new Cand("Aim2Pro.TrackCreator.TrackGenV2Window","ParseToCanonical"),
      new Cand("Aim2Pro.TrackCreator.TrackGenV2Window","Parse"),
      new Cand("Aim2Pro.TrackCreator.V2CommandEngine","ParseToCanonical"),
      new Cand("V2CommandEngine","ParseToCanonical"),
      new Cand("Aim2Pro.AIGG.V2CommandEngine","ParseToCanonical"),
      new Cand("KernelInvokerV2","ParseToCanonical"),
      new Cand("RealKernelBridge","ParseToCanonical")
    };

    public static bool TryCanonical(string text, out string canonical, out string provider) {
      canonical = null; provider = null;
      if (string.IsNullOrEmpty(text)) return false;

      var asms = AppDomain.CurrentDomain.GetAssemblies();
      for (int ai = 0; ai < asms.Length; ai++) {
        var asm = asms[ai];
        for (int ci = 0; ci < Cands.Length; ci++) {
          var c = Cands[ci];

          // Try fully-qualified first, then simple name match
          Type t = asm.GetType(c.T, false, true);
          if (t == null) {
            string simple = c.T;
            int dot = simple.LastIndexOf(.);
            if (dot >= 0) simple = simple.Substring(dot + 1);
            var types = asm.GetTypes();
            for (int ti = 0; ti < types.Length && t == null; ti++) {
              if (types[ti].Name == simple) t = types[ti];
            }
          }
          if (t == null) continue;

          MethodInfo m = t.GetMethod(
            c.M,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance,
            null,
            new Type[] { typeof(string) },
            null
          );
          if (m == null) continue;

          object inst = m.IsStatic ? null : Activator.CreateInstance(t);
          object res  = m.Invoke(inst, new object[] { text });
          string s = res as string;
          if (!string.IsNullOrEmpty(s)) {
            canonical = s;
            provider  = ((t.FullName != null) ? t.FullName : t.Name) + "." + m.Name;
            return true;
          }
        }
      }
      return false;
    }
  }
}
#endif
