#if UNITY_EDITOR
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG {
    public static class PlanMacros {
        static readonly Regex RxBuild = new Regex(@"\bbuildAbs\((\d+)\s*,\s*(\d+)\)", RegexOptions.IgnoreCase);
        static readonly Regex RxYSplit = new Regex(@"\bySplit\(\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*gapStart\s*=\s*(\d+))?\s*(?:,\s*gapEnd\s*=\s*(\d+))?\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxYMerge = new Regex(@"\byMerge\(\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*toGap\s*=\s*(\d+))?\s*\)", RegexOptions.IgnoreCase);
        static readonly Regex RxSmoothHeights = new Regex(@"\bsmoothHeights\((?:both|row|col)?(?:,\s*tolerance\s*=\s*[0-9.]+)?\)", RegexOptions.IgnoreCase);

        public static string Expand(string canon){
            if (string.IsNullOrWhiteSpace(canon)) return canon ?? "";
            var lines = (canon ?? "").Replace("\r","").Split(n);
            var sb = new StringBuilder();

            int width = 3;
            foreach (var raw in lines){ var m = RxBuild.Match(raw); if (m.Success){ int.TryParse(m.Groups[2].Value, out width); break; } }

            int currentGap = 0;
            int midLeft = (width + 1) / 2;

            foreach (var raw in lines){
                var line = (raw ?? "").Trim();
                if (line.Length == 0) continue;

                // smoothHeights -> smoothColumns()
                if (RxSmoothHeights.IsMatch(line)) { sb.AppendLine("smoothColumns()"); continue; }

                // ySplit
                var ms = RxYSplit.Match(line);
                if (ms.Success){
                    int a = int.Parse(ms.Groups[1].Value);
                    int b = int.Parse(ms.Groups[2].Value);
                    if (a > b){ var t=a; a=b; b=t; }
                    int gapStart = ms.Groups[3].Success ? int.Parse(ms.Groups[3].Value) : Math.Max(1, currentGap>0?currentGap:1);
                    int gapEnd   = ms.Groups[4].Success ? int.Parse(ms.Groups[4].Value) : Math.Max(gapStart, gapStart);
                    for (int r=a; r<=b; r++){
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

                // yMerge
                var mj = RxYMerge.Match(line);
                if (mj.Success){
                    int a = int.Parse(mj.Groups[1].Value);
                    int b = int.Parse(mj.Groups[2].Value);
                    if (a > b){ var t=a; a=b; b=t; }
                    int toGap = mj.Groups[3].Success ? int.Parse(mj.Groups[3].Value) : 0;
                    int fromGap = Math.Max(0, currentGap);
                    for (int r=a; r<=b; r++){
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

                // passthrough
                sb.AppendLine(line);
            }
            return sb.ToString().Trim();
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
