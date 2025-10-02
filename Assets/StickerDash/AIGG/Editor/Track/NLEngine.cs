#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG {
    public static class NLEngine {
        public const string PlanPath = "StickerDash_Status/LastCanonical.plan";
        public const string SeedsLog = "StickerDash_Status/Seeds.log";

        // Entry: parse NL lines -> canonical, amending existing plan unless a rebuild/build is requested.
        public static string ParseNL(string src){
            string nl = Normalize(src);
            string basePlan = LoadPlanIfAny();

            bool doRebuild = Regex.IsMatch(nl, @"^\s*(rebuild|build\s+\d+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var sb = new StringBuilder();

            if (doRebuild){
                // fresh plan
                int L = 100, W = 3;
                var mBuild = Regex.Match(nl, @"\brebuild\s+(\d+(?:\.\d+)?)\s*(?:m)?\s*(?:by|x)\s*(\d+(?:\.\d+)?)|\bbuild\s+(\d+(?:\.\d+)?)\s*(?:m)?\s*(?:by|x)\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (mBuild.Success){
                    string l = mBuild.Groups[1].Success ? mBuild.Groups[1].Value : mBuild.Groups[3].Value;
                    string w = mBuild.Groups[2].Success ? mBuild.Groups[2].Value : mBuild.Groups[4].Value;
                    int.TryParse(TrimNum(l), out L); int.TryParse(TrimNum(w), out W);
                }
                var mSeed = Regex.Match(nl, @"\bseed\s+(-?\d+)\b", RegexOptions.IgnoreCase);
                if (mSeed.Success) sb.AppendLine("seed(" + mSeed.Groups[1].Value + ")");
                sb.AppendLine("buildAbs(" + L + "," + W + ")");

                // optional safe margins
                var mSafe = Regex.Match(nl, @"\bsafe\s+start\s+(\d+).*\bsafe\s+end\s+(\d+)", RegexOptions.IgnoreCase);
                if (mSafe.Success){ sb.AppendLine("safeMarginStart(" + mSafe.Groups[1].Value + ")"); sb.AppendLine("safeMarginEnd(" + mSafe.Groups[2].Value + ")"); }

                // slopes
                foreach (Match m in Regex.Matches(nl, @"\brandom\s+slopes?\s+(\d+(?:\.\d+)?)\s*(?:to|-)\s*(\d+(?:\.\d+)?)\s*(?:deg)?\s*,?\s*segment\s+(\d+)", RegexOptions.IgnoreCase)){
                    sb.AppendLine("slopesRandomAuto(" + TrimNum(m.Groups[1].Value) + "," + TrimNum(m.Groups[2].Value) + "," + m.Groups[3].Value + ")");
                }
                // s bends auto
                foreach (Match m in Regex.Matches(nl, @"\bauto\s*s[-\s]?bends?\s+(\d+)\s+(?:at\s+)?(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)){
                    sb.AppendLine("sBendAuto(" + m.Groups[1].Value + "," + TrimNum(m.Groups[2].Value) + ")");
                }
            } else {
                // start from existing plan if present
                sb.Append((basePlan ?? "").Trim());
                if (sb.Length > 0) sb.Append("\n");
            }

            // universal amendments (apply for both modes)
            // delete rows
            foreach (Match m in Regex.Matches(nl, @"\b(remove|delete)\s+rows?\s+(\d+)\s*(?:to|-)\s*(\d+)\b", RegexOptions.IgnoreCase)){
                sb.AppendLine("deleteRows(" + m.Groups[2].Value + "," + m.Groups[3].Value + ")");
            }
            // delete tiles X,Y in row R
            foreach (Match m in Regex.Matches(nl, @"\b(remove|delete)\s+tiles?\s+([\d,\s]+)\s+(?:in\s+)?row\s+(\d+)\b", RegexOptions.IgnoreCase)){
                sb.AppendLine("deleteTiles(" + CsvNums(m.Groups[2].Value) + ", row=" + m.Groups[3].Value + ")");
            }
            // curves
            foreach (Match m in Regex.Matches(nl, @"\bcurve\s+rows?\s+(\d+)\s*(?:to|-)\s*(\d+)\s+(left|right)\s+(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)){
                sb.AppendLine("curveRows(" + m.Groups[1].Value + "," + m.Groups[2].Value + "," + m.Groups[3].Value.ToLower() + "," + TrimNum(m.Groups[4].Value) + ")");
            }
            // s bend manual
            foreach (Match m in Regex.Matches(nl, @"\bs[-\s]?bend\s+(\d+)\s*(?:to|-)\s*(\d+)(?:.*?(?:at|@)\s*(\d+(?:\.\d+)?))?(?:.*?gain\s*(\d+(?:\.\d+)?))?(?:.*?ratio\s*(\d+(?:\.\d+)?))?", RegexOptions.IgnoreCase)){
                string deg = m.Groups[3].Success ? TrimNum(m.Groups[3].Value) : "25";
                string gain = m.Groups[4].Success ? ("," + TrimNum(m.Groups[4].Value)) : "";
                string ratio = m.Groups[5].Success ? ("," + TrimNum(m.Groups[5].Value)) : "";
                sb.AppendLine("sBend(" + m.Groups[1].Value + "," + m.Groups[2].Value + "," + deg + gain + ratio + ")");
            }
            // random slopes (amendments)
            foreach (Match m in Regex.Matches(nl, @"\brandom\s+slopes?\s+(\d+(?:\.\d+)?)\s*(?:to|-)\s*(\d+(?:\.\d+)?)\s*(?:deg)?\s*,?\s*segment\s+(\d+)", RegexOptions.IgnoreCase)){
                sb.AppendLine("slopesRandomAuto(" + TrimNum(m.Groups[1].Value) + "," + TrimNum(m.Groups[2].Value) + "," + m.Groups[3].Value + ")");
            }
            // random holes
            foreach (Match m in Regex.Matches(nl, @"\brandom\s+holes?\s+(\d+(?:\.\d+)?)\s*%?", RegexOptions.IgnoreCase)){
                sb.AppendLine("randomHoles(" + TrimNum(m.Groups[1].Value) + ")");
            }
            // jump gaps
            foreach (Match m in Regex.Matches(nl, @"\b(add\s+)?(\d+)\s+jump\s+gaps?\b", RegexOptions.IgnoreCase)){
                sb.AppendLine("insertJumpGaps(" + m.Groups[2].Value + ")");
            }
            // smooth heights
            if (Regex.IsMatch(nl, @"\bsmooth\s+heights?\b", RegexOptions.IgnoreCase)){
                sb.AppendLine("smoothHeights(both, tolerance=0.02)");
            }
            // fork / rejoin to macros
            foreach (Match m in Regex.Matches(nl, @"\bfork\s+(\d+)\s*(?:to|-)\s*(\d+)(?:.*?widen\s+to\s+(\d+))?", RegexOptions.IgnoreCase)){
                string gapEnd = m.Groups[3].Success ? m.Groups[3].Value : "3";
                sb.AppendLine("ySplit(" + m.Groups[1].Value + "," + m.Groups[2].Value + ", gapStart=1, gapEnd=" + gapEnd + ")");
            }
            foreach (Match m in Regex.Matches(nl, @"\b(rejoin|merge)\s+(\d+)\s*(?:to|-)\s*(\d+)(?:.*?to\s+gap\s+(\d+))?", RegexOptions.IgnoreCase)){
                string toGap = m.Groups[4].Success ? m.Groups[4].Value : "0";
                sb.AppendLine("yMerge(" + m.Groups[2].Value + "," + m.Groups[3].Value + ", toGap=" + toGap + ")");
            }
            // protect start/end
            var mStart = Regex.Match(nl, @"\bprotect\s+start\s+(\d+)", RegexOptions.IgnoreCase);
            if (mStart.Success) sb.AppendLine("safeMarginStart(" + mStart.Groups[1].Value + ")");
            var mEnd = Regex.Match(nl, @"\bprotect\s+.*?\bend\s+(\d+)", RegexOptions.IgnoreCase);
            if (mEnd.Success) sb.AppendLine("safeMarginEnd(" + mEnd.Groups[1].Value + ")");

            // FILL/SOLID (plan-level undo by filtering base plan)
            // collect fill directives
            var fillAll = Regex.IsMatch(nl, @"\b(solid\s+track|fill\s+all)\b", RegexOptions.IgnoreCase);
            var fillRows = new System.Collections.Generic.List<Tuple<int,int>>();
            foreach (Match m in Regex.Matches(nl, @"\bfill\s+rows?\s+(\d+)\s*(?:to|-)\s*(\d+)\b", RegexOptions.IgnoreCase)){
                int a=int.Parse(m.Groups[1].Value), b=int.Parse(m.Groups[2].Value); if (a>b){var t=a;a=b;b=t;} fillRows.Add(Tuple.Create(a,b));
            }
            foreach (Match m in Regex.Matches(nl, @"\bfill\s+row\s+(\d+)\b", RegexOptions.IgnoreCase)){
                int r=int.Parse(m.Groups[1].Value); fillRows.Add(Tuple.Create(r,r));
            }
            var fillTiles = new System.Collections.Generic.List<Tuple<int,string>>();
            foreach (Match m in Regex.Matches(nl, @"\bfill\s+tiles?\s+([\d,\s]+)\s+(?:in\s+)?row\s+(\d+)\b", RegexOptions.IgnoreCase)){
                fillTiles.Add(Tuple.Create(int.Parse(m.Groups[2].Value), CsvNums(m.Groups[1].Value)));
            }

            string planNow = sb.ToString().Trim();
            if (!string.IsNullOrEmpty(basePlan)){
                var amended = AmendBase(basePlan, fillAll, fillRows, fillTiles);
                if (planNow.Length > 0){
                    planNow = (amended.Trim() + "\n" + planNow).Trim();
                } else {
                    planNow = amended.Trim();
                }
            }

            // If we still have nothing, fall back to tiny safe plan
            if (string.IsNullOrWhiteSpace(planNow)){
                planNow = "buildAbs(100,3)";
            }

            // Auto-seed capture: if plan has random ops and no seed(), inject one
            if (!Regex.IsMatch(planNow, @"\bseed\(\s*-?\d+\s*\)", RegexOptions.IgnoreCase) &&
                Regex.IsMatch(planNow, @"\b(randomHoles|slopesRandomAuto|sBendAuto)\b", RegexOptions.IgnoreCase)){
                int autoSeed = (int)(DateTime.UtcNow.Ticks % 2000000000);
                planNow = "seed(" + autoSeed.ToString() + ")\n" + planNow;
                TryAppendSeedLog(autoSeed);
            }

            return planNow.Trim();
        }

        public static void WritePlan(string canon){
            string expanded = PlanMacros.Expand((canon ?? "").Trim());
            var dir = Path.GetDirectoryName(PlanPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(PlanPath, expanded + Environment.NewLine, new UTF8Encoding(false));
            Debug.Log("[A2P:NL] Wrote plan -> " + PlanPath);
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
            if (s == null) return "";
            s = s.Replace("\r", "");
            s = Regex.Replace(s, @"(\d+(?:\.\d+)?)\s*m\b", "$1");
            return s.ToLowerInvariant();
        }
        static string TrimNum(string x){ if (x.IndexOf(".") >= 0) x = x.TrimEnd(0).TrimEnd(.); return x; }
        static string CsvNums(string s){
            var list = new System.Collections.Generic.List<string>();
            foreach (Match m in Regex.Matches(s ?? "", @"\d+")) list.Add(m.Value);
            return string.Join(",", list.ToArray());
        }

        static string LoadPlanIfAny(){
            try{ if (File.Exists(PlanPath)) return File.ReadAllText(PlanPath); } catch{}
            return "";
        }

        // Remove destructive rows/tiles ops affecting target rows; drop randomHoles/jumpGaps if fillAll requested.
        static string AmendBase(string basePlan, bool fillAll, System.Collections.Generic.List<Tuple<int,int>> fillRows, System.Collections.Generic.List<Tuple<int,string>> fillTiles){
            if (string.IsNullOrWhiteSpace(basePlan)) return "";
            var sb = new StringBuilder();
            var lines = basePlan.Replace("\r","").Split(n);
            foreach (var raw in lines){
                var line = (raw ?? "").Trim();
                if (line.Length==0) continue;

                // Drop deleteRows that overlaps any fillRows
                var mr = Regex.Match(line, @"\bdeleteRows\(\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
                if (mr.Success){
                    int a=int.Parse(mr.Groups[1].Value), b=int.Parse(mr.Groups[2].Value); if (a>b){var t=a;a=b;b=t;}
                    bool overlap=false; foreach(var fr in fillRows){ if (!(b < fr.Item1 || a > fr.Item2)) { overlap=true; break; } }
                    if (fillAll || overlap) continue; // skip this deletion
                }

                // Drop deleteTiles that targets any fillTiles or any row in fillRows
                var mt = Regex.Match(line, @"\bdeleteTiles\(\s*([0-9,\s]+)\s*,\s*row\s*=\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
                if (mt.Success){
                    int row=int.Parse(mt.Groups[2].Value);
                    bool drop = fillAll;
                    if (!drop){
                        foreach(var fr in fillRows){ if (row>=fr.Item1 && row<=fr.Item2){ drop=true; break; } }
                        if (!drop){
                            foreach(var ft in fillTiles){ if (ft.Item1==row){ drop=true; break; } }
                        }
                    }
                    if (drop) continue; // skip this deletion
                }

                // Drop global random holes / jump gaps if solid requested
                if (fillAll && (Regex.IsMatch(line, @"\brandomHoles\(", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(line, @"\binsertJumpGaps\(", RegexOptions.IgnoreCase))) continue;

                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        static void TryAppendSeedLog(int seed){
            try{
                Directory.CreateDirectory(Path.GetDirectoryName(SeedsLog));
                File.AppendAllText(SeedsLog, DateTime.UtcNow.ToString("u") + " seed(" + seed.ToString() + ")" + Environment.NewLine, new UTF8Encoding(false));
            } catch {}
        }
    }
}
#endif
