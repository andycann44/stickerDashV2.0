#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public static class NLFileRunner {
        const string NLPath = "StickerDash_Status/NL.input";
        const string Sheet  = "StickerDash_Status/NL_CheatSheet.md";

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Parse From File")]
        public static void ParseFromFile(){
            EnsureInputs();
            var nl = File.ReadAllText(NLPath);
            var canon = NLEngine.ParseNL(nl);
            NLEngine.WritePlan(canon);
            Debug.Log("[A2P:NL] Parsed NL.input → Canonical only.");
        }

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Run From File")]
        public static void RunFromFile(){
            EnsureInputs();
            var nl = File.ReadAllText(NLPath);
            var canon = NLEngine.ParseNL(nl);
            NLEngine.WritePlan(canon);
            NLEngine.RunPlan(false);
        }

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Open Cheat Sheet")]
        public static void OpenCheat(){
            EnsureInputs();
            EditorUtility.RevealInFinder(Path.GetFullPath(Sheet));
        }

        static void EnsureInputs(){
            Directory.CreateDirectory("StickerDash_Status");
            if (!File.Exists(NLPath)){
                File.WriteAllText(NLPath, string.Join(Environment.NewLine, new[]{
                    "build 200 by 3",
                    "protect start 10 end 10",
                    "random slopes 2 to 4 degrees, segment 2",
                    "smooth heights"
                }));
            }
            if (!File.Exists(Sheet)){
                File.WriteAllText(Sheet, DefaultSheet());
            }
        }

        static string DefaultSheet(){
            return string.Join(Environment.NewLine, new[]{
                "# NL Cheat Sheet",
                "",
                "## Rebuild (fresh)",
                "- rebuild 220 by 3 seed 11",
                "- build 200 by 3",
                "- safe start 10 end 10",
                "",
                "## Amend (edit current track)",
                "- remove rows 80 to 120",
                "- remove tiles 2,4,7 in row 95",
                "- curve rows 40 to 60 left 20 degrees",
                "- s bend 60 to 120 at 25 gain 2",
                "- random slopes 2 to 4 degrees, segment 2",
                "- random holes 5%",
                "- add 2 jump gaps",
                "- smooth heights",
                "- protect start 10 end 10",
                "",
                "## Fork / Rejoin (Y split)",
                "- fork 120 to 160 widen to 3",
                "- rejoin 200 to 240",
                "",
                "## How to run",
                "Window → Aim2Pro → Track Creator → NL Tester",
                "or NL → Parse/Run From File (uses StickerDash_Status/NL.input)"
            });
        }
    }
}
#endif
