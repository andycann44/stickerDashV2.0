#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Aim2Pro.AIGG.NL {
  public static class V2NLBridge {
    class Cand {
      public string T; public string M;
      public Cand(string t, string m){ T=t; M=m; }
    }

    // Known places your existing converter might live.
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
      if (string.IsNullOrWhiteSpace(text)) return false;

      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
        foreach (var c in Cands) {
          // Try fully-qualified first, then by simple name match.
          Type t = asm.GetType(c.T, false, true);
          if (t == null) {
            string simple = c.T.LastIndexOf(.) >= 0 ? c.T.Substring(c.T.LastIndexOf(.)+1) : c.T;
            foreach (var tt in asm.GetTypes()) { if (tt.Name == simple) { t = tt; break; } }
          }
          if (t == null) continue;

          var m = t.GetMethod(c.M,
                    BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance,
                    binder: null, types: new Type[]{ typeof(string) }, modifiers: null);
          if (m == null) continue;

          object inst = m.IsStatic ? null : Activator.CreateInstance(t);
          object res  = m.Invoke(inst, new object[]{ text });
          if (res is string s && !string.IsNullOrWhiteSpace(s)) {
            canonical = s;
            provider  = (t.FullName ?? t.Name) + "." + m.Name;
            return true;
          }
        }
      }
      return false;
    }
  }
}
#endif
