
// NLCore — shared NL→Canonical parser + plan writer/runner.
// Used by NL Tester window and NL File Runner menus.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    [InitializeOnLoad]
    public static class NLCore {
        public const string PlanPath = "StickerDash_Status/LastCanonical.plan";
        static NLCore(){
            Debug.Log("[A2P:NL] Loaded — menus under Window → Aim2Pro → Track Creator. Use NL Tester or NL → Run From File.");
        }

        public static string ParseNL(string nlRaw){
            if (string.IsNullOrWhiteSpace(nlRaw)) return "";
            string nl = Normalize(nlRaw);
            var sb = new StringBuilder();
            foreach (var raw in nl.Split(new[]{n}, StringSplitOptions.None)){
                string line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                Match m;
                m = Regex.Match(line, @"\bseed\s+(\d+)\b");
                if (m.Success){ sb.AppendLine($"seed({m.Groups[1].Value})"); continue; }

                m = Regex.Match(line, @"\bbuild\s+(\d+(?:\.\d+)?)\s*(?:m)?\s*(?:by|x|×)\s*(\d+(?:\.\d+)?)\s*(?:m)?\b");
                if (m.Success){ sb.AppendLine($"buildAbs({T(m.Groups[1].Value)},{T(m.Groups[2].Value)})"); continue; }

                m = Regex.Match(line, @"\brandom\s+holes?\s+(\d+(?:\.\d+)?)\s*%?");
                if (m.Success){ sb.AppendLine($"randomHoles({T(m.Groups[1].Value)})"); continue; }

                m = Regex.Match(line, @"\b(add\s+)?(\d+)\s+jump\s+gaps?\b");
                if (m.Success){ sb.AppendLine($"insertJumpGaps({m.Groups[2].Value})"); continue; }

                m = Regex.Match(line, @"\bdelete\s+rows?\s+(\d+)\s*(?:-|to|–|—)\s*(\d+)\b");
                if (m.Success){ sb.AppendLine($"deleteRows({m.Groups[1].Value},{m.Groups[2].Value})"); continue; }

                m = Regex.Match(line, @"\bdelete\s+tiles?\s+([\d,\s]+)\s+in\s+row\s+(\d+)\b");
                if (m.Success){ sb.AppendLine($"deleteTiles({CsvNums(m.Groups[1].Value)}, row={m.Groups[2].Value})"); continue; }

                m = Regex.Match(line, @"\bcurve\s+rows?\s+(\d+)\s*(?:-|to|–|—)\s*(\d+)\s+(left|right)\s+(\d+(?:\.\d+)?)");
                if (m.Success){ sb.AppendLine($"curveRows({m.Groups[1].Value},{m.Groups[2].Value},{m.Groups[3].Value},{T(m.Groups[4].Value)})"); continue; }

                m = Regex.Match(line, @"\bauto\s*s[-\s]?bends?\s+(\d+)\s+(?:at\s+)?(\d+(?:\.\d+)?)\s*(?:deg(?:rees)?)?(?:\s*gain\s*(\d+(?:\.\d+)?))?");
                if (m.Success){
                    string gain = m.Groups[3].Success ? $",{T(m.Groups[3].Value)}" : "";
                    sb.AppendLine($"sBendAuto({m.Groups[1].Value},{T(m.Groups[2].Value)}{gain})"); continue;
                }

                m = Regex.Match(line, @"\bs[-\s]?bend\s+(\d+)\s*(?:-|to|–|—)\s*(\d+)(?:.*?(?:at|@)\s*(\d+(?:\.\d+)?)\s*(?:deg(?:rees)?)?)?(?:.*?gain\s*(\d+(?:\.\d+)?))?(?:.*?ratio\s*(\d+(?:\.\d+)?))?");
                if (m.Success){
                    string a = m.Groups[1].Value, b = m.Groups[2].Value;
                    string deg = m.Groups[3].Success ? T(m.Groups[3].Value) : "25";
                    string gain = m.Groups[4].Success ? $",{T(m.Groups[4].Value)}" : "";
                    string ratio = m.Groups[5].Success ? $",{T(m.Groups[5].Value)}" : "";
                    sb.AppendLine($"sBend({a},{b},{deg}{gain}{ratio})"); continue;
                }

                m = Regex.Match(line, @"\brandom\s+slopes?\s+(\d+(?:\.\d+)?)\s*(?:to|-)\s*(\d+(?:\.\d+)?)\s*(?:deg(?:rees)?)?\s*,?\s*segment\s+(\d+)");
                if (m.Success){ sb.AppendLine($"slopesRandomAuto({T(m.Groups[1].Value)},{T(m.Groups[2].Value)},{m.Groups[3].Value})"); continue; }

                m = Regex.Match(line, @"\bsafe\s+margin\s+(\d+)\b");
                if (m.Success){ sb.AppendLine($"safeMargin({m.Groups[1].Value})"); continue; }
                m = Regex.Match(line, @"\bsafe\s+start\s+(\d+)");
                if (m.Success){ sb.AppendLine($"safeMarginStart({m.Groups[1].Value})"); }
                m = Regex.Match(line, @"\bsafe\s+end\s+(\d+)");
                if (m.Success){ sb.AppendLine($"safeMarginEnd({m.Groups[1].Value})"); continue; }

                if (Regex.IsMatch(line, @"\bno\s*smooth\b")) { sb.AppendLine("noSmooth()"); continue; }
                if (Regex.IsMatch(line, @"\bsmooth\s+columns?\b")) { sb.AppendLine("smoothColumns()"); continue; }
            }
            return sb.ToString().Trim();
        }

        public static void WritePlan(string canon){
            Directory.CreateDirectory(Path.GetDirectoryName(PlanPath));
            File.WriteAllText(PlanPath, (canon ?? "").Trim() + "\n", new UTF8Encoding(false));
            Debug.Log("[A2P:NL] Wrote " + PlanPath);
        }

        public static void RunPlan(bool viaSBendFix){
            string menu = viaSBendFix
                ? "Window/Aim2Pro/Track Creator/Track Gen V2 (SBend Fix)"
                : "Window/Aim2Pro/Track Creator/Run Last Canonical";
            if (!EditorApplication.ExecuteMenuItem(menu)){
                Debug.LogWarning("[A2P:NL] Could not trigger menu: " + menu);
            }
        }

        static string Normalize(string s){
            s = s.Replace(\u00D7,x);
            s = Regex.Replace(s, @"[–—]", "-");
            s = s.Replace("\\r","");
            s = Regex.Replace(s, @"(\\d+(?:\\.\\d+)?)\\s*m\\b", "$1");
            return s.ToLowerInvariant();
        }
        static string T(string x){ if (x.IndexOf(".") >= 0) x = x.TrimEnd(0).TrimEnd(.); return x; }
        static string CsvNums(string s){ var L=new List<string>(); foreach (Match m in Regex.Matches(s,"(\\d+)+")) L.Add(m.Groups[1].Value); return string.Join(",",L); }
    }
}
#endif
