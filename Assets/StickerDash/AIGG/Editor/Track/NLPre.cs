#if UNITY_EDITOR
using System.Text.RegularExpressions;

namespace Aim2Pro.AIGG {
    public static class NLPre {
        public static string Normalize(string src){
            string s = (src ?? "").Replace("\r\n","\n").Replace("\r","\n");
            s = Regex.Replace(s, @"\b(?:protect|safe)\s+start\s+(\d+)\s+end\s+(\d+)\b", "safeMarginStart($1)\nsafeMarginEnd($2)", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(?:protect|safe)\s+start\s+(\d+)\b", "safeMarginStart($1)", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\b(?:protect|safe)\s+end\s+(\d+)\b", "safeMarginEnd($1)", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"random\s+tiles?\s+missing\s+(\d+(?:\.\d+)?)\s*%?", "random holes $1", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"\bs\s*bends?\s+(\d+)\s+(?:at\s+)?(\d+(?:\.\d+)?)", "auto s-bends $1 at $2", RegexOptions.IgnoreCase);
            return s;
        }
    }
}
#endif
