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
                InvokeOn(typeof(StubKernel), fn, args);
                return;
            }

            try
            {
                InvokeOn(t, fn, args);
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
            if (IsGood(_targetType)) { Debug.Log($"[TrackV2] Using real kernel (full-name): {_targetType.FullName}"); return _targetType; }

            _targetType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.IsClass && t.Namespace == "Aim2Pro.AIGG" && t.Name == "Kernel");
            if (IsGood(_targetType)) { Debug.Log($"[TrackV2] Using real kernel (ns scan): {_targetType.FullName}"); return _targetType; }

            string[] mustHave = { "AppendStraight", "AppendArc", "DeleteRowsRange", "OffsetRowsX", "OffsetRowsY", "StraightenRows" };
            foreach (var t in AppDomain.CurrentDomain.GetAssemblies().SelectMany(SafeGetTypes))
            {
                if (!t.IsClass) continue;
                var names = t.GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).ToHashSet();
                if (mustHave.Any(n => names.Contains(n))) { _targetType = t; Debug.Log($"[TrackV2] Using real kernel (discovered): {_targetType.FullName}"); return _targetType; }
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

        private static Type[] SafeGetTypes(Assembly a) { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } }
        private static bool IsGood(Type t) => t != null && t.IsClass;

        // ----- robust binder: accepts fewer args, fills defaults/optionals, converts types -----
        private static void InvokeOn(Type type, string fn, object[] providedArgs)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                              .Where(m => m.Name == fn)
                              .ToArray();
            if (methods.Length == 0)
                throw new MissingMethodException($"Kernel fn {fn} not found in {type.FullName}.");

            int provided = providedArgs?.Length ?? 0;

            foreach (var m in methods)
            {
                var ps = m.GetParameters();
                if (provided > ps.Length) continue; // too many provided

                try
                {
                    var finalArgs = new object[ps.Length];

                    // supplied args
                    for (int i = 0; i < provided; i++)
                        finalArgs[i] = ConvertArg(providedArgs[i], ps[i].ParameterType);

                    // trailing args
                    for (int i = provided; i < ps.Length; i++)
                    {
                        if (ps[i].IsOptional)
                        {
                            // Unity's reflection sometimes reports optional without DefaultValue; Type.Missing is fine
                            finalArgs[i] = ps[i].DefaultValue is DBNull ? Type.Missing : ps[i].DefaultValue;
                        }
                        else
                        {
                            // fall back to default(T) so we can still call methods that didn't mark defaults as optional
                            finalArgs[i] = DefaultOf(ps[i].ParameterType);
                        }
                    }

                    m.Invoke(null, finalArgs);
                    return; // success
                }
                catch
                {
                    // try next overload
                }
            }

            throw new MissingMethodException($"Kernel fn {fn} with {provided} args not found in {type.FullName}.");
        }

        private static object DefaultOf(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;

        private static object ConvertArg(object value, Type targetType)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            try
            {
                // numeric strings -> numbers
                if (value is string s)
                {
                    if (targetType == typeof(int)   && int.TryParse(s,   out var ii)) return ii;
                    if (targetType == typeof(float) && float.TryParse(s, out var ff)) return ff;
                    if (targetType == typeof(double)&& double.TryParse(s,out var dd)) return dd;
                    if (targetType.IsEnum) return Enum.Parse(targetType, s, true);
                }

                if (targetType == typeof(float))  return Convert.ToSingle(value);
                if (targetType == typeof(double)) return Convert.ToDouble(value);
                if (targetType == typeof(int))    return Convert.ToInt32(value);
                if (targetType.IsEnum)            return Enum.Parse(targetType, value.ToString(), true);
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
