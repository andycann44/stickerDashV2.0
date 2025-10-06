#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Aim2Pro.AIGG.NL
{
    public class NLTesterWindow : EditorWindow
    {
        [MenuItem("Window/Aim2Pro/Test/NL Tester")]
        public static void Open() => GetWindow<NLTesterWindow>("NL Tester");

        string _nl = "120m by 3m, left curve 30°, seed 101, add start countdown, add dartboards";
        string _json = "";
        string _log = "";

        void OnGUI()
        {
            GUILayout.Label("Natural Language", EditorStyles.boldLabel);
            _nl = EditorGUILayout.TextArea(_nl, GUILayout.MinHeight(60));

            if (GUILayout.Button("Parse NL → JSON"))
            {
                try
                {
                    var engine = new IntentEngine();
                    var spec = engine.Parse(_nl, out var log);
                    _json = spec.ToJson(); // return compact JSON (no pretty dependency)
                    _log = log;
                }
                catch (Exception ex)
                {
                    _json = "";
                    _log = "ERROR: " + ex.Message;
                    Debug.LogException(ex);
                }
            }

            GUILayout.Space(6);
            GUILayout.Label("Canonical JSON (preview)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_json, GUILayout.MinHeight(120));

            GUILayout.Space(6);
            GUILayout.Label("Parse Log", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(_log, GUILayout.MinHeight(80));
        }
    }

    class IntentEngine
    {
        readonly IntentSpec _spec;
        public IntentEngine() { _spec = IntentSpec.Load(); }

        public CanonicalSpec Parse(string nl, out string log)
        {
            var sb = new StringBuilder();
            var result = new CanonicalSpec();
            int matches = 0;

            foreach (var i in _spec.Intents)
            {
                var rx = new Regex(i.regex, RegexOptions.IgnoreCase);
                var m = rx.Match(nl);
                if (!m.Success) continue;

                matches++;
                sb.AppendLine($"match: {i.name}");
                foreach (var op in i.ops)
                {
                    ApplyOp(result, op, m);
                    sb.AppendLine($"  op {op.op} {op.path} = {op.value}");
                }
            }

            log = matches > 0 ? sb.ToString() : "No intents matched.";
            return result;
        }

        void ApplyOp(CanonicalSpec dst, IntentOp op, Match m)
        {
            string val = ExpandValue(op.value, m);
            switch (op.op)
            {
                case "set":
                    dst.Set(op.path, ParseTyped(val));
                    break;
                case "append":
                    dst.Append(op.path, ExpandValue(op.value, m));
                    break;
                case "offset":
                case "custom":
                    // Not used in tester
                    break;
            }
        }

        static object ParseTyped(string v)
        {
            int idx = v.LastIndexOf(':');
            if (idx > 0)
            {
                string core = v.Substring(0, idx);
                string type = v.Substring(idx + 1);
                switch (type)
                {
                    case "int": if (int.TryParse(core, out var ii)) return ii; break;
                    case "float": if (float.TryParse(core, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ff)) return ff; break;
                    case "bool": if (bool.TryParse(core, out var bb)) return bb; break;
                }
            }
            return v;
        }

        static string ExpandValue(string v, Match m)
        {
            for (int i = 1; i < m.Groups.Count; i++)
                v = v.Replace($"${i}", m.Groups[i].Value);
            return v;
        }
    }

    class CanonicalSpec
    {
        readonly Dictionary<string, object> root = new Dictionary<string, object> {
            { "track", new Dictionary<string, object>() },
            { "rules", new Dictionary<string, object>() },
            { "commands", new List<object>() }
        };

        public void Set(string path, object value)
        {
            var parts = path.Trim().TrimStart('$','.').Split('.');
            Dictionary<string, object> cur = root;
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                bool last = i == parts.Length - 1;
                if (p.EndsWith("[]"))
                {
                    var key = p.Substring(0, p.Length - 2);
                    if (!cur.TryGetValue(key, out var listObj) || listObj is not List<object> list)
                    { list = new List<object>(); cur[key] = list; }
                    if (last) list.Add(value); else throw new Exception("Unsupported nested [] set");
                    return;
                }
                else
                {
                    if (last) { cur[p] = value; }
                    else
                    {
                        if (!cur.TryGetValue(p, out var next) || next is not Dictionary<string, object> dict)
                        { dict = new Dictionary<string, object>(); cur[p] = dict; }
                        cur = dict;
                    }
                }
            }
        }

        public void Append(string path, string value)
        {
            var parts = path.Trim().TrimStart('$','.').Split('.');
            Dictionary<string, object> cur = root;
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                bool last = i == parts.Length - 1;

                if (p.EndsWith("[]"))
                {
                    var key = p.Substring(0, p.Length - 2);
                    if (!cur.TryGetValue(key, out var listObj))
                    { listObj = new List<object>(); cur[key] = listObj; }
                    var list = (List<object>)listObj;
                    if (!last) throw new Exception("Unsupported nested [] append");
                    list.Add(value);
                    return;
                }
                else
                {
                    if (!cur.TryGetValue(p, out var next) || next is not Dictionary<string, object> dict)
                    { dict = new Dictionary<string, object>(); cur[p] = dict; }
                    cur = dict;
                }
            }
        }

        public string ToJson() => MiniJson.Serialize(root);
    }

    [Serializable] class IntentSpec { public List<Intent> intents;
        public static IntentSpec Load()
        {
            var ta = Resources.Load<TextAsset>("spec/intents");
            if (ta == null) throw new Exception("Resources/spec/intents.json not found.");
            return JsonUtility.FromJson<IntentSpec>(ta.text);
        }
        public List<Intent> Intents => intents ?? new List<Intent>();
    }
    [Serializable] class Intent { public string name; public string regex; public List<IntentOp> ops; }
    [Serializable] class IntentOp { public string op; public string path; public string value; }

    static class MiniJson
    {
        public static string Serialize(object obj)
        {
            var sb = new StringBuilder();
            Write(obj, sb);
            return sb.ToString();
        }
        static void Write(object o, StringBuilder sb)
        {
            switch (o)
            {
                case null: sb.Append("null"); break;
                case string s: sb.Append('\"').Append(s.Replace("\"","\\\"")).Append('\"'); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int or long or float or double or decimal:
                    sb.Append(Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture)); break;
                case Dictionary<string, object> dict:
                    sb.Append('{'); bool first=true;
                    foreach (var kv in dict) { if(!first) sb.Append(','); first=false; sb.Append('\"').Append(kv.Key).Append("\":"); Write(kv.Value,sb); }
                    sb.Append('}'); break;
                case List<object> list:
                    sb.Append('['); for(int i=0;i<list.Count;i++){ if(i>0) sb.Append(','); Write(list[i],sb); } sb.Append(']'); break;
                default: sb.Append('\"').Append(o.ToString()).Append('\"'); break;
            }
        }
    }
}
#endif
