#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG {
    public static class PlanMacros {
        static readonly Regex RxBuild = new Regex(@"\bbuildAbs\(\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxYSplit = new Regex(@"\bySplit\(\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*gapStart\s*=\s*(\d+))?\s*(?:,\s*gapEnd\s*=\s*(\d+))?\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxYMerge = new Regex(@"\byMerge\(\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*toGap\s*=\s*(\d+))?\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxSmoothHeights = new Regex(@"\bsmoothHeights\(", RegexOptions.IgnoreCase);
        static readonly Regex RxSmoothColumns = new Regex(@"\bsmoothColumns\(\)", RegexOptions.IgnoreCase);
        static readonly Regex RxSafeStart = new Regex(@"\bsafeMarginStart\(\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxSafeEnd   = new Regex(@"\bsafeMarginEnd\(\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxDelRows   = new Regex(@"\bdeleteRows\(\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxDelTiles  = new Regex(@"\bdeleteTiles\(\s*([0-9,\s]+)\s*,\s*row\s*=\s*(\d+)\s*\)", RegexOptions.IgnoreCase);

        public static string Expand(string canon){
            if (string.IsNullOrWhiteSpace(canon)) return canon ?? "";
            string src = (canon ?? "").Replace("\r", "");

            // Get track length/width from first buildAbs
            int length = 0, width = 3;
            var mB = RxBuild.Match(src);
            if (mB.Success){ int.TryParse(mB.Groups[1].Value, out length); int.TryParse(mB.Groups[2].Value, out width); if (width<=0) width=3; }

            int protectStart = 0; // rows 1..protectStart protected
            int protectEnd   = 0; // last protectEnd rows protected
            int currentGap = 0;
            int midLeft = (width + 1) / 2;

            // First pass: gather safe margins (we will DROP those lines)
            using (var reader = new StringReader(src)){
                string raw;
                while ((raw = reader.ReadLine()) != null){
                    var line = (raw ?? "").Trim();
                    if (line.Length==0) continue;
                    var ms = RxSafeStart.Match(line); if (ms.Success) int.TryParse(ms.Groups[1].Value, out protectStart);
                    var me = RxSafeEnd.Match(line);   if (me.Success) int.TryParse(me.Groups[1].Value, out protectEnd);
                }
            }

            var sb = new StringBuilder();
            using (var reader = new StringReader(src)){
                string raw;
                while ((raw = reader.ReadLine()) != null){
                    var line = (raw ?? "").Trim();
                    if (line.Length==0) continue;

                    // Drop placeholders / unsupported ops
                    if (RxSmoothHeights.IsMatch(line)) continue; // placeholder only
                    if (RxSmoothColumns.IsMatch(line)) continue; // legacy placeholder
                    if (RxSafeStart.IsMatch(line) || RxSafeEnd.IsMatch(line)) continue; // preprocess only

                    // ySplit -> deleteTiles per row
                    var ms = RxYSplit.Match(line);
                    if (ms.Success){
                        int a = int.Parse(ms.Groups[1].Value), b = int.Parse(ms.Groups[2].Value);
                        if (a>b){ var t=a; a=b; b=t; }
                        int gapStart = ms.Groups[3].Success ? int.Parse(ms.Groups[3].Value) : Math.Max(1, currentGap>0?currentGap:1);
                        int gapEnd   = ms.Groups[4].Success ? int.Parse(ms.Groups[4].Value) : Math.Max(gapStart, gapStart);
                        for (int r=a; r<=b; r++){
                            // respect protected rows
                            if (IsProtected(r, length, protectStart, protectEnd)) continue;
                            double tt = (b==a)?1.0:(double)(r-a)/(double)(b-a);
                            int gap = (int)Math.Round(gapStart + (gapEnd-gapStart)*tt);
                            if (gap < 1) gap = 1; if (gap > width) gap = width;

                            int startCol, endCol;
                            if (gap % 2 == 1){
                                int half=(gap-1)/2; startCol=midLeft-half; endCol=midLeft+half;
                            } else {
                                int half=gap/2; startCol=midLeft-half+1; endCol=midLeft+half;
                            }
                            if (startCol<1) startCol=1; if (endCol>width) endCol=width;
                            sb.AppendLine("deleteTiles(" + CsvRange(startCol,endCol) + ", row=" + r + ")");
                            currentGap = gap;
                        }
                        continue;
                    }

                    // yMerge -> deleteTiles per row (shrinking)
                    var mj = RxYMerge.Match(line);
                    if (mj.Success){
                        int a = int.Parse(mj.Groups[1].Value), b = int.Parse(mj.Groups[2].Value);
                        if (a>b){ var t=a; a=b; b=t; }
                        int toGap = mj.Groups[3].Success ? int.Parse(mj.Groups[3].Value) : 0;
                        int fromGap = Math.Max(0, currentGap);
                        for (int r=a; r<=b; r++){
                            if (IsProtected(r, length, protectStart, protectEnd)) continue;
                            double tt = (b==a)?1.0:(double)(r-a)/(double)(b-a);
                            int gap = (int)Math.Round(fromGap + (toGap-fromGap)*tt);
                            if (gap < 0) gap = 0; if (gap > width) gap = width;
                            if (gap == 0) { continue; }
                            int startCol, endCol;
                            if (gap % 2 == 1){
                                int half=(gap-1)/2; startCol=midLeft-half; endCol=midLeft+half;
                            } else {
                                int half=gap/2; startCol=midLeft-half+1; endCol=midLeft+half;
                            }
                            if (startCol<1) startCol=1; if (endCol>width) endCol=width;
                            sb.AppendLine("deleteTiles(" + CsvRange(startCol,endCol) + ", row=" + r + ")");
                            currentGap = gap;
                        }
                        continue;
                    }

                    // Clip deleteRows against protected ranges
                    var dr = RxDelRows.Match(line);
                    if (dr.Success){
                        int a = int.Parse(dr.Groups[1].Value), b = int.Parse(dr.Groups[2].Value);
                        if (a>b){ var t=a; a=b; b=t; }
                        // clip start-protected
                        if (protectStart > 0 && a <= protectStart) a = protectStart + 1;
                        // clip end-protected
                        if (length > 0 && protectEnd > 0){
                            int endStart = Math.Max(1, length - protectEnd + 1);
                            if (b >= endStart) b = endStart - 1;
                        }
                        if (a <= b) sb.AppendLine($"deleteRows({a},{b})");
                        continue;
                    }

                    // Drop deleteTiles on protected rows
                    var dt = RxDelTiles.Match(line);
                    if (dt.Success){
                        int row = int.Parse(dt.Groups[2].Value);
                        if (IsProtected(row, length, protectStart, protectEnd)) continue;
                        sb.AppendLine(line);
                        continue;
                    }

                    // passthrough
                    sb.AppendLine(line);
                }
            }
            return sb.ToString().Trim();
        }

        static bool IsProtected(int row, int length, int protectStart, int protectEnd){
            if (row <= Math.Max(0, protectStart)) return true;
            if (length > 0 && protectEnd > 0){
                int endStart = Math.Max(1, length - protectEnd + 1);
                if (row >= endStart) return true;
            }
            return false;
        }

        static string CsvRange(int a,int b){
            if (a>b){ var t=a; a=b; b=t; }
            var sb = new System.Text.StringBuilder();
            for (int i=a;i<=b;i++){ if (sb.Length>0) sb.Append(","); sb.Append(i.ToString()); }
            return sb.ToString();
        }
    }
}
#endif
