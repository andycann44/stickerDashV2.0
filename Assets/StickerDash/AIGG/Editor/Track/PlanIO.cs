#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public static class PlanIO {
        public const string PlanPath = "StickerDash_Status/LastCanonical.plan";
        static string Norm(string s) { return (s ?? "").Replace("\r\n","\n").Replace("\r","\n").Replace("\\n","\n"); }

        public static void Overwrite(string canon){
            string text = Norm(canon).TrimEnd() + "\n";
            string dir = Path.GetDirectoryName(PlanPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(PlanPath, text, new System.Text.UTF8Encoding(false));
            Debug.Log("[A2P:PlanIO] Overwrite -> " + PlanPath);
        }

        public static void Append(string canon){
            string add = Norm(canon).Trim();
            if (string.IsNullOrEmpty(add)) return;
            string dir = Path.GetDirectoryName(PlanPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string existing = File.Exists(PlanPath) ? Norm(File.ReadAllText(PlanPath)) : "";
            string joiner = existing.EndsWith("\n") ? "" : "\n";
            File.WriteAllText(PlanPath, existing + joiner + add + "\n", new System.Text.UTF8Encoding(false));
            Debug.Log("[A2P:PlanIO] Append -> " + PlanPath);
        }

        public static void Reveal(){ EditorUtility.RevealInFinder(Path.GetFullPath(PlanPath)); }
    }
}
#endif
