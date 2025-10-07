#if false
#if UNITY_EDITOR
using System;
using System.Reflection;

namespace Aim2Pro.AIGG.NL {
  public static class V2NLBridge {
    class Cand { public string T; public string M; public Cand(string t,string m){ T=t; M=m; } }

    // Likely locations for your NL->canonical converter
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
      if (text == null || text.Trim().Length == 0) return false;

      Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
      for (int ai=0; ai<asms.Length; ai++) {
        Assembly asm = asms[ai];
        Type[] allTypes;
        try { allTypes = asm.GetTypes(); } catch { continue; }

        for (int ci=0; ci<Cands.Length; ci++) {
          Cand c = Cands[ci];

          // Try fully qualified type, then fallback to simple name match
          Type t = asm.GetType(c.T, false);
          if (t == null) {
            string simple = c.T;
            int dot = simple.LastIndexOf(.);
            if (dot >= 0) simple = simple.Substring(dot+1);
            for (int ti=0; ti<allTypes.Length && t==null; ti++) {
              if (allTypes[ti].Name == simple) t = allTypes[ti];
            }
          }
          if (t == null) continue;

          MethodInfo[] meths = t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);
          for (int mi=0; mi<meths.Length; mi++) {
            MethodInfo m = meths[mi];
            if (m.Name != c.M) continue;
            ParameterInfo[] ps = m.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(string)) continue;

            object inst = m.IsStatic ? null : Activator.CreateInstance(t);
            object res;
            try { res = m.Invoke(inst, new object[]{ text }); }
            catch { continue; }

            string s = res as string;
            if (s != null && s.Trim().Length > 0) {
              canonical = s;
              provider  = ((t.FullName != null) ? t.FullName : t.Name) + "." + m.Name;
              return true;
            }
          }
        }
      }
      return false;
    }
  }
}
#endif

#endif
