using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.TrackV2
{
    /// <summary>
    /// Resolves and dispatches to your existing static Kernel class (if present),
    /// otherwise falls back to a stub that just logs.
    /// </summary>
    public static class KernelInvokerV2
    {
        // Known names we try first (you can add more here if needed)
        private static readonly string[] CandidateTypeNames = new[]
        {
            "Aim2Pro.AIGG.Kernel",
            "Aim2Pro.AIGG.TrackKernel",
            "Aim2Pro.AIGG.TrackEditApplier",
            "Aim2Pro.AIGG.TrackOps",
            "Aim2Pro.AIGG.EditorKernel",
            "Aim2Pro.AIGG.KernelV1"
        };

        private static Type _targetType;   // discovered kernel type (cached)
        private static bool _resolved;

        public static void Call(string fn, object[] args)
        {
            var t = ResolveTargetType();
            if (t == null)
            {
                // No real kernel found → use stub
                InvokeOn(typeof(StubKernel), fn, args, allowOptional:true);
                return;
            }

            try
            {
                InvokeOn(t, fn, args, allowOptional:true);
            }
            catch (MissingMethodException)
            {
                // Fall back to stub if the exact fn signature isn't found
                Debug.LogWarning($"[TrackV2] Real kernel missing method '{fn}({args.Length} args)'. Using stub.");
                InvokeOn(typeof(StubKernel), fn, args, allowOptional:true);
            }
        }

        private static void InvokeOn(Type type, string fn, object[] args, bool allowOptional)
        {
            // find a static public method named fn that can accept our args, and where extra parameters are optional
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == fn);
            MethodInfo match = null;
            object[] finalArgs = null;

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (ps.Length < (args?.Length ?? 0)) continue;

                bool extrasOptional = true;
                for (int i = (args?.Length ?? 0); i < ps.Length; i++)
                {
                    if (!ps[i].IsOptional) { extrasOptional = false; break; }
                }
                if (!extrasOptional && allowOptional) continue;

                // Convert provided args to parameter types
                finalArgs = new object[ps.Length];
                int provided = args?.Length ?? 0;
                for (int i = 0; i < provided; i++)
                {
                    finalArgs[i] = ConvertArg(args[i], ps[i].ParameterType);
                }
                // Fill remaining with defaults
                for (int i = provided; i < ps.Length; i++)
                {
                    finalArgs[i] = Type.Missing; // let reflection use default value
                }

                match = m;
                break;
            }

            if (match == null)
                throw new MissingMethodException($"Kernel fn {fn} with {(args?.Length ?? 0)} args not found in {type.FullName}.");

            match.Invoke(null, finalArgs);
        }

        private static object ConvertArg(object value, Type targetType)
        {
            if (value == null) return null;
            var vType = value.GetType();

            // Direct assignable (string, etc.)
            if (targetType.IsAssignableFrom(vType)) return value;

            try
            {
                if (targetType == typeof(float)) return Convert.ToSingle(value);
                if (targetType == typeof(double)) return Convert.ToDouble(value);
                if (targetType == typeof(int)) return Convert.ToInt32(value);
                if (targetType.IsEnum) return Enum.Parse(targetType, value.ToString(), true);
            }
            catch { /* fall through to default */ }

            return value;
        }

        private static Type ResolveTargetType()
        {
            if (_resolved) return _targetType;
            _resolved = true;

            // 1) Try known names
            foreach (var name in CandidateTypeNames)
            {
                var t = Type.GetType(name);
                if (IsSuitableKernel(t)) { _targetType = t; LogTarget("named", t); return _targetType; }
            }

            // 2) Search assemblies for a type that has at least one of our known method names
            string[] mustHave = new[]
            {
                "AppendStraight", "AppendArc", "DeleteRowsRange",
                "OffsetRowsX", "OffsetRowsY", "StraightenRows",
                "BuildSplineFromTrack", "SetWidth", "SetThickness", "Resample"
            };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = Array.Empty<Type>();
                try { types = asm.GetTypes(); } catch { /* dynamic assemblies may fail */ }

                foreach (var t in types)
                {
                    if (!IsSuitableKernel(t)) continue;
                    var names = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).ToHashSet();
                    if (mustHave.Any(n => names.Contains(n)))
                    {
                        _targetType = t;
                        LogTarget("discovered", t);
                        return _targetType;
                    }
                }
            }

            // 3) None found
            Debug.LogWarning("[TrackV2] No real kernel type discovered. Using stub logger.");
            _targetType = null;
            return null;
        }

        private static bool IsSuitableKernel(Type t)
        {
            if (t == null) return false;
            if (t == typeof(StubKernel)) return false; // not our own stub
            if (!t.IsClass) return false;
            // Must have at least one public static method
            return t.GetMethods(BindingFlags.Public | BindingFlags.Static).Length > 0;
        }

        private static void LogTarget(string how, Type t)
        {
            Debug.Log($"[TrackV2] Using real kernel ({how}): {t.FullName}");
        }

        // ---- Fallback stub (logs only) ----
        public static class StubKernel
        {
            public static void DeleteRow(int n) => Debug.Log($"[KernelStub] DeleteRow({n})");
            public static void DeleteRowsRange(int start, int end) => Debug.Log($"[KernelStub] DeleteRowsRange({start},{end})");
            public static void DeleteRowsList(int[] rows) => Debug.Log($"[KernelStub] DeleteRowsList([{string.Join(",", rows)}])");
            public static void DeleteTilesInRow(int row, int[] tiles) => Debug.Log($"[KernelStub] DeleteTilesInRow({row},[{string.Join(",", tiles)}])");
            public static void DeleteTilesInRows(int start, int end, int[] tiles) => Debug.Log($"[KernelStub] DeleteTilesInRows({start},{end},[{string.Join(",", tiles)}])");

            public static void CurveRows(int start, int end, string side, float deg) => Debug.Log($"[KernelStub] CurveRows({start},{end},{side},{deg})");
            public static void StraightenRows(int start, int end) => Debug.Log($"[KernelStub] StraightenRows({start},{end})");
            public static void OffsetRowsX(int start, int end, float meters) => Debug.Log($"[KernelStub] OffsetRowsX({start},{end},{meters})");
            public static void OffsetRowsY(int start, int end, float meters) => Debug.Log($"[KernelStub] OffsetRowsY({start},{end},{meters})");

            public static void BuildSplineFromTrack(float width = 3f, float top = 0f, float thickness = 0.2f, bool replace = false)
                => Debug.Log($"[KernelStub] BuildSplineFromTrack(width:{width},top:{top},thickness:{thickness},replace:{replace})");

            public static void AppendStraight(float distance, float step = 1f)
                => Debug.Log($"[KernelStub] AppendStraight(distance:{distance},step:{step})");

            public static void AppendArc(string side, float deg, float arcLen = 0f, int steps = 0)
                => Debug.Log($"[KernelStub] AppendArc(side:{side},deg:{deg},arcLen:{arcLen},steps:{steps})");

            public static void SetWidth(float width) => Debug.Log($"[KernelStub] SetWidth({width})");
            public static void SetThickness(float thickness) => Debug.Log($"[KernelStub] SetThickness({thickness})");
            public static void Resample(float density) => Debug.Log($"[KernelStub] Resample({density})");
        }
    }
}
