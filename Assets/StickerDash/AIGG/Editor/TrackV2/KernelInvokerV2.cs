using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.TrackV2
{
    public static class KernelInvokerV2
    {
        public static void Call(string fn, object[] args)
        {
            var mi = typeof(Kernel).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                   .Where(m => m.Name == fn)
                                   .FirstOrDefault(m => m.GetParameters().Length == args.Length);
            if (mi == null)
                throw new MissingMethodException($"Kernel fn {fn} with {args.Length} args not found.");

            mi.Invoke(null, args);
        }

        // TEMP KERNEL STUBS (logs). Wire to real kernel later.
        public static class Kernel
        {
            public static void DeleteRow(int n) => Debug.Log($"[Kernel] DeleteRow({n})");
            public static void DeleteRowsRange(int start, int end) => Debug.Log($"[Kernel] DeleteRowsRange({start},{end})");
            public static void DeleteRowsList(int[] rows) => Debug.Log($"[Kernel] DeleteRowsList([{string.Join(",", rows)}])");
            public static void DeleteTilesInRow(int row, int[] tiles) => Debug.Log($"[Kernel] DeleteTilesInRow({row},[{string.Join(",", tiles)}])");
            public static void DeleteTilesInRows(int start, int end, int[] tiles) => Debug.Log($"[Kernel] DeleteTilesInRows({start},{end},[{string.Join(",", tiles)}])");
            public static void CurveRows(int start, int end, string side, float deg) => Debug.Log($"[Kernel] CurveRows({start},{end},{side},{deg})");
            public static void StraightenRows(int start, int end) => Debug.Log($"[Kernel] StraightenRows({start},{end})");
            public static void OffsetRowsX(int start, int end, float meters) => Debug.Log($"[Kernel] OffsetRowsX({start},{end},{meters})");
            public static void OffsetRowsY(int start, int end, float meters) => Debug.Log($"[Kernel] OffsetRowsY({start},{end},{meters})");
            public static void BuildSplineFromTrack(float width = 3f, float top = 0f, float thickness = 0.2f, bool replace = false)
                => Debug.Log($"[Kernel] BuildSplineFromTrack(width:{width},top:{top},thickness:{thickness},replace:{replace})");
            public static void AppendStraight(float distance, float step = 1f)
                => Debug.Log($"[Kernel] AppendStraight(distance:{distance},step:{step})");
            public static void AppendArc(string side, float deg, float arcLen = 0f, int steps = 0)
                => Debug.Log($"[Kernel] AppendArc(side:{side},deg:{deg},arcLen:{arcLen},steps:{steps})");
            public static void SetWidth(float width) => Debug.Log($"[Kernel] SetWidth({width})");
            public static void SetThickness(float thickness) => Debug.Log($"[Kernel] SetThickness({thickness})");
            public static void Resample(float density) => Debug.Log($"[Kernel] Resample({density})");
        }
    }
}
