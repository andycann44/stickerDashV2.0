using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.TrackV2
{
    public static class KernelInvokerV2
    {
        private static Type _targetType;
        private static bool _resolved;

        public static void Call(string fn, object[] args)
        {
            var t = ResolveTargetType();
            Debug.Log($"[TrackV2] Invoking {fn} on {(t != null ? t.FullName : "<stub>")} with {(args?.Length ?? 0)} arg(s)");

            if (t == null)
            {
                Debug.LogWarning("[TrackV2] No real kernel type discovered. Using stub logger.");
                InvokeOn(typeof(StubKernel), fn, args, allowOptional: true);
                return;
            }

            try
            {
                InvokeOn(t, fn, args, allowOptional: true);
            }
            catch (TargetInvocationException tie)
            {
                var ie = tie.InnerException;
                Debug.LogError($"[TrackV2] {fn} threw {ie?.GetType().Name}: {ie?.Message}\n{ie?.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrackV2] {fn} failed: {ex}");
                throw;
            }
        }

        private static Type ResolveTargetType()
        {
            if (_resolved) return _targetType;
            _resolved = true;

            _targetType = FindTypeInAllAssemblies("Aim2Pro.AIGG.Kernel");
            if (IsGood(_targetType))
            {
                Debug.Log($"[TrackV2] Using real kernel (full-name): {_targetType.FullName}");
                return _targetType;
            }

            _targetType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.IsClass && t.Namespace == "Aim2Pro.AIGG" && t.Name == "Kernel");
            if (IsGood(_targetType))
            {
                Debug.Log($"[TrackV2] Using real kernel (ns scan): {_targetType.FullName}");
                return _targetType;
            }

            string[] mustHave = { "AppendStraight", "AppendArc", "DeleteRowsRange", "OffsetRowsX", "OffsetRowsY", "StraightenRows" };
            foreach (var t in AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeGetTypes))
            {
                if (!t.IsClass) continue;
                var names = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).ToHashSet();
                if (mustHave.Any(n => names.Contains(n)))
                {
                    _targetType = t;
                    Debug.Log($"[TrackV2] Using real kernel (discovered): {_targetType.FullName}");
                    return _targetType;
                }
            }

            _targetType = null;
            return null;
        }

        private static Type FindTypeInAllAssemblies(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { var t = asm.GetType(fullName, false); if (t != null) return t; } catch { }
            }
            return null;
        }

        private static Type[] SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
        }

        private static bool IsGood(Type t)
            => t != null && t.IsClass && t.GetMethods(BindingFlags.Public | BindingFlags.Static).Length > 0;

        private static void InvokeOn(Type type, string fn, object[] args, bool allowOptional)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == fn);
            MethodInfo match = null; object[] finalArgs = null;

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                int provided = args?.Length ?? 0;
                if (ps.Length < provided) continue;

                bool extrasOptional = true;
                for (int i = provided; i < ps.Length; i++) if (!ps[i].IsOptional) { extrasOptional = false; break; }
                if (!extrasOptional && allowOptional) continue;

                finalArgs = new object[ps.Length];
                for (int i = 0; i < provided; i++) finalArgs[i] = ConvertArg(args[i], ps[i].ParameterType);
                for (int i = provided; i < ps.Length; i++) finalArgs[i] = Type.Missing;
                match = m; break;
            }

            if (match == null)
                throw new MissingMethodException($"Kernel fn {fn} with {(args?.Length ?? 0)} args not found in {type.FullName}.");

            match.Invoke(null, finalArgs);
        }

        private static object ConvertArg(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;
            try
            {
                if (targetType == typeof(float)) return Convert.ToSingle(value);
                if (targetType == typeof(double)) return Convert.ToDouble(value);
                if (targetType == typeof(int)) return Convert.ToInt32(value);
                if (targetType.IsEnum) return Enum.Parse(targetType, value.ToString(), true);
            }
            catch { }
            return value;
        }

        // Fallback stub (logs only)
        public static class StubKernel
        {
            public static void DeleteRowsRange(int start, int end) => Debug.Log($"[KernelStub] DeleteRowsRange({start},{end})");
            public static void OffsetRowsX(int start, int end, float meters) => Debug.Log($"[KernelStub] OffsetRowsX({start},{end},{meters})");
            public static void OffsetRowsY(int start, int end, float meters) => Debug.Log($"[KernelStub] OffsetRowsY({start},{end},{meters})");
            public static void StraightenRows(int start, int end) => Debug.Log($"[KernelStub] StraightenRows({start},{end})");
            public static void AppendStraight(float distance, float step = 1f) => Debug.Log($"[KernelStub] AppendStraight(distance:{distance},step:{step})");
            public static void AppendArc(string side, float deg, float arcLen = 0f, int steps = 0) => Debug.Log($"[KernelStub] AppendArc(side:{side},deg:{deg},arcLen:{arcLen},steps:{steps})");
            public static void SetWidth(float width) => Debug.Log($"[KernelStub] SetWidth({width})");
            public static void SetThickness(float thickness) => Debug.Log($"[KernelStub] SetThickness({thickness})");
            public static void Resample(float density) => Debug.Log($"[KernelStub] Resample({density})");
        }
    }
}
