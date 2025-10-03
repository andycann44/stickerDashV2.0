#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public static class PlanIO {
        public const string PlanPath = "StickerDash_Status/LastCanonical.plan";

        static string Norm(string s) => (s ?? "").Replace("\r\n","\n").Replace("\r","\n").Replace("\\n","\n");

        // Expand NL macros and remove directives the runner does not implement.
        static string ExpandAndClean(string canon){
            try {
                string expanded = PlanMacros.Expand((canon ?? "").Trim());
                string s = Norm(expanded).Trim();
                // Drop lines like: safeMarginStart(N), safeMarginEnd(N), safeMargin(N), noSmooth(), smoothColumns()
                s = Regex.Replace(s, @"^\s*safeMargin(Start|End)?\s*\([^\)]*\)\s*$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"^\s*noSmooth\s*\(\s*\)\s*$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                s = Regex.Replace(s, @"^\s*smoothColumns\s*\(\s*\)\s*$", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                // Remove blank lines left behind
                s = Regex.Replace(s, @"^\s*$\n?", "", RegexOptions.Multiline);
                return s.Trim();
            } catch (Exception ex) {
                Debug.LogWarning("[A2P:PlanIO] Expand failed, writing raw canonical. " + ex.Message);
                return Norm(canon).Trim();
            }
        }

        public static void Overwrite(string canon){
            string text = ExpandAndClean(canon);
            string dir = Path.GetDirectoryName(PlanPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(PlanPath, (text + "\n"), new UTF8Encoding(false));
            Debug.Log("[A2P:PlanIO] Overwrite -> " + PlanPath);
        }

        public static void Append(string canon){
            string add = ExpandAndClean(canon);
            if (string.IsNullOrEmpty(add)) return;
            string dir = Path.GetDirectoryName(PlanPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string existing = File.Exists(PlanPath) ? Norm(File.ReadAllText(PlanPath)) : "";
            string joiner = existing.EndsWith("\n") || existing.Length==0 ? "" : "\n";
            File.WriteAllText(PlanPath, existing + joiner + add + "\n", new UTF8Encoding(false));
            Debug.Log("[A2P:PlanIO] Append -> " + PlanPath);
        }

        public static void Reveal(){ EditorUtility.RevealInFinder(Path.GetFullPath(PlanPath)); }
    }
}
#endif
