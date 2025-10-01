
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public static class NLFileRunner {
        const string NLPath = "StickerDash_Status/NL.input";
        const string Sample = "StickerDash_Status/NL.sample.txt";

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Parse From File")]
        public static void ParseFromFile(){
            EnsureSample();
            var nl = File.ReadAllText(NLPath);
            var canon = NLCore.ParseNL(nl);
            NLCore.WritePlan(canon);
            Debug.Log("[A2P:NL] Parsed NL.input to Canonical only.");
        }

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Run From File")]
        public static void RunFromFile(){
            EnsureSample();
            var nl = File.ReadAllText(NLPath);
            var canon = NLCore.ParseNL(nl);
            NLCore.WritePlan(canon);
            NLCore.RunPlan(false);
        }

        [MenuItem("Window/Aim2Pro/Track Creator/NL/Run From File (SBend Fix)")]
        public static void RunFromFileSBend(){
            EnsureSample();
            var nl = File.ReadAllText(NLPath);
            var canon = NLCore.ParseNL(nl);
            NLCore.WritePlan(canon);
            NLCore.RunPlan(true);
        }

        static void EnsureSample(){
            Directory.CreateDirectory("StickerDash_Status");
            if (!File.Exists(NLPath)){
                File.WriteAllText(NLPath,
@"seed 42
build 200 by 3
safe margin 10
s bend 60-120 at 25 degrees gain 2
random slopes 2 to 6 degrees, segment 12
random holes 4%
smooth columns");
            }
            if (!File.Exists(Sample)){
                File.WriteAllText(Sample,
@"# Copy this to NL.input then edit:
seed 7
build 220 by 3
safe start 5 and safe end 10
curve rows 50-80 left 25 deg
s bend 90-150 at 30 degrees gain 1.6 ratio 0.4
random holes 5%
smooth columns");
            }
        }
    }
}
#endif
