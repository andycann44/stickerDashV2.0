#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Text;
using Aim2Pro.AIGG.NL; // for V2CanonicalParser (optional)

namespace Aim2Pro.TrackCreator
{
    public static class TrackLab_NLTests
    {
        [MenuItem("Window/Aim2Pro/Track Creator/Run NL Tests")]
        public static void RunAll()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0, warn = 0;

            void PASS(string m){ sb.AppendLine("PASS  " + m); pass++; }
            void FAIL(string m){ sb.AppendLine("FAIL  " + m); fail++; }
            void WARN(string m){ sb.AppendLine("WARN  " + m); warn++; }

            // Open Track Lab + get ApplyNL
            var tlType = Type.GetType("Aim2Pro.TrackCreator.TrackLab, Assembly-CSharp-Editor");
            if (tlType == null) { Debug.LogError("[NLTests] TrackLab type not found"); return; }
            var win = EditorWindow.GetWindow(tlType, false, "Track Lab");
            var apply = tlType.GetMethod("ApplyNL", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (apply == null) { Debug.LogError("[NLTests] ApplyNL method not found"); return; }

            // Helpers
            void Clean()
            {
                var old = GameObject.Find("Track");
                if (old) UnityEngine.Object.DestroyImmediate(old);
            }
            GameObject Track() => GameObject.Find("Track");
            Transform Row(int n)
            {
                var t = Track();
                return t ? t.transform.Find("Row_" + n) : null;
            }
            int RowCount()
            {
                var t = Track();
                return t ? t.transform.childCount : 0;
            }

            sb.AppendLine("=== TRACK LAB NL TESTS === " + DateTime.Now.ToString("u"));
            try
            {
                // 1) create grid
                Clean();
                apply.Invoke(win, new object[]{ "create 12 m by 6 m\n" });
                if (Track() && RowCount()==12 && (Row(1)?.childCount ?? 0)==6) PASS("create 12x6");
                else FAIL($"create 12x6 -> rows={RowCount()}, row1Tiles={(Row(1)?.childCount ?? -1)}");

                // 2) offset rows x
                apply.Invoke(win, new object[]{ "offset rows 1-10 x 2\n" });
                float x1 = Row(1) ? Row(1).localPosition.x : float.NaN;
                float x11= Row(11)? Row(11).localPosition.x: float.NaN;
                if (Mathf.Abs(x1-2f)<0.01f && Mathf.Abs(x11-0f)<0.01f) PASS("offset rows 1–10 x 2");
                else FAIL($"offset rows -> Row_1.x={x1:0.###}, Row_11.x={x11:0.###}");

                // 3) append straight
                apply.Invoke(win, new object[]{ "append straight 5 m step 1\n" });
                int rc = RowCount();
                if (rc>=17 && rc<=17) PASS("append straight +5 rows");
                else FAIL($"append straight expected 17 rows, got {rc}");

                // 4) delete row
                apply.Invoke(win, new object[]{ "delete row 3\n" });
                if (Row(3)==null) PASS("delete row 3");
                else FAIL("delete row 3");

                // 5) straighten rows alignment
                // prepare: skew Row_2.x=1, Row_3.x=3 then straighten 2-4
                Clean();
                apply.Invoke(win, new object[]{ "create 4 m by 4 m\n" });
                if (Row(2)) { var p=Row(2).localPosition; p.x=1f; Row(2).localPosition=p; }
                if (Row(3)) { var p=Row(3).localPosition; p.x=3f; Row(3).localPosition=p; }
                apply.Invoke(win, new object[]{ "straighten rows 2-4\n" });
                float sx2 = Row(2)? Row(2).localPosition.x : 999f;
                float sx3 = Row(3)? Row(3).localPosition.x : -999f;
                if (Mathf.Abs(sx3 - sx2) < 0.001f) PASS("straighten rows 2–4 aligns X to previous");
                else FAIL($"straighten -> Row_2.x={sx2}, Row_3.x={sx3}");

                // 6) NLPre normalization (em-dash)
                bool hasPre = Type.GetType("Aim2Pro.NLPre, Assembly-CSharp-Editor") != null
                           || Type.GetType("Aim2Pro.NL.NLPre, Assembly-CSharp-Editor") != null;
                Clean();
                apply.Invoke(win, new object[]{ "create 4 m by 2 m\noffset rows 1—2 x 1\n" }); // em-dash
                float ex1 = Row(1)? Row(1).localPosition.x : 0f;
                if (hasPre)
                {
                    if (Mathf.Abs(ex1-1f)<0.01f) PASS("NLPre: em-dash normalized");
                    else FAIL("NLPre present but em-dash not normalized");
                }
                else
                {
                    if (Mathf.Abs(ex1)<0.01f) WARN("No NLPre.cs (expected) — em-dash not applied");
                }

                // 7) V2 parser canonical smoke (no CanonicalRunner required)
                string v2 = V2CanonicalParser.ParseToCanonical("build 60 by 6 random tiles missing 10% row gaps for jumps 2");
                bool v2ok = v2.Contains("buildAbs(60,6)") && v2.Contains("randomHoles(10)") && v2.Contains("insertJumpGaps(2)");
                if (v2ok) PASS("V2 canonical parse basic");
                else FAIL("V2 canonical parse");

            }
            catch (Exception ex)
            {
                FAIL("Unhandled test exception: " + ex.Message);
            }

            var dir = "StickerDash_Status/Diagnostics";
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, "nl_tests.txt");
            var summary = $"=== SUMMARY: PASS={pass} FAIL={fail} WARN={warn} ===";
            sb.AppendLine(summary);
            System.IO.File.WriteAllText(path, sb.ToString());

            if (fail>0) Debug.LogError("[NLTests]\n" + sb.ToString() + "\nWrote: " + path);
            else Debug.Log("[NLTests]\n" + sb.ToString() + "\nWrote: " + path);
        }
    }
}
#endif
