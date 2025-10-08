using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

static class A2PJsonMini {
    public static object Parse(string s)=>MiniJSON.Json.Deserialize(s);
    public static string Stringify(object o){return MiniJSON.Json.Serialize(o);}
}

namespace MiniJSON {
    using System; using System.Collections; using System.Collections.Generic; using System.Text; using System.IO;
    public static class Json {
        public static object Deserialize(string json){ return Parser.Parse(json); }
        public static string Serialize(object obj){ return Serializer.Serialize(obj); }
        sealed class Parser : IDisposable {
            StringReader json; Parser(string j){ json=new StringReader(j); }
            public static object Parse(string j){ using(var p=new Parser(j)){ return p.ParseValue(); } }
            public void Dispose(){ json.Dispose(); }
            enum TOK{NONE,CURLY_OPEN,CURLY_CLOSE,SQUARE_OPEN,SQUARE_CLOSE,COMMA,COLON,STRING,NUMBER,TRUE,FALSE,NULL}
            object ParseValue(){ EatWS(); switch(NextToken){ case TOK.STRING: return ParseString();
                case TOK.NUMBER: return ParseNumber(); case TOK.CURLY_OPEN: return ParseObject();
                case TOK.SQUARE_OPEN: return ParseArray(); case TOK.TRUE: return true; case TOK.FALSE: return false; case TOK.NULL: return null; default: return null; } }
            Dictionary<string,object> ParseObject(){ var d=new Dictionary<string,object>(); NextToken; for(;;){ var t=NextToken;
                if(t==TOK.CURLY_CLOSE) return d; if(t!=TOK.STRING) return d; var k=ParseString(); if(NextToken!=TOK.COLON) return d; d[k]=ParseValue(); var nx=NextToken; if(nx==TOK.CURLY_CLOSE) return d; }
            }
            List<object> ParseArray(){ var a=new List<object>(); NextToken; for(;;){ var t=NextToken; if(t==TOK.SQUARE_CLOSE) break; json.Read(); a.Add(ParseValue()); } return a; }
            string ParseString(){ var sb=new StringBuilder(); json.Read(); for(;;){ if(json.Peek()==-1) break; var c=NextChar;
                if(c=='"') break; if(c=='\\'){ var n=NextChar; switch(n){ case '"': sb.Append('\"'); break; case '\\': sb.Append('\\'); break; case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break; case 'f': sb.Append('\f'); break; case 'n': sb.Append('\n'); break; case 'r': sb.Append('\r'); break; case 't': sb.Append('\t'); break;
                    case 'u': var hex=new char[4]; for(int i=0;i<4;i++) hex[i]=NextChar; sb.Append((char)Convert.ToInt32(new string(hex),16)); break; } } else sb.Append(c); } return sb.ToString(); }
            object ParseNumber(){ var w=NextWord; if(w.IndexOf('.')==-1){ long.TryParse(w,out var iv); return iv; } double.TryParse(w,out var dv); return dv; }
            void EatWS(){ while(char.IsWhiteSpace(Peek)) json.Read(); }
            char Peek{ get{ var c=json.Peek(); return c==-1?'\0':(char)c; } }
            char NextChar{ get{ var c=json.Read(); return c==-1?'\0':(char)c; } }
            string NextWord{ get{ var sb=new StringBuilder(); while(!char.IsWhiteSpace(Peek) && "{}[],:\"".IndexOf(Peek)==-1 && json.Peek()!=-1){ sb.Append(NextChar);} return sb.ToString(); } }
            TOK NextToken{ get{
                EatWS(); if(json.Peek()==-1) return TOK.NONE; switch(Peek){ case '{': json.Read(); return TOK.CURLY_OPEN; case '}': json.Read(); return TOK.CURLY_CLOSE;
                    case '[': json.Read(); return TOK.SQUARE_OPEN; case ']': json.Read(); return TOK.SQUARE_CLOSE; case ',': json.Read(); return TOK.COMMA; case ':': json.Read(); return TOK.COLON; case '"': return TOK.STRING;
                    case '-': case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9': return TOK.NUMBER; }
                var w=NextWord; switch(w){ case "true": return TOK.TRUE; case "false": return TOK.FALSE; case "null": return TOK.NULL; default: return TOK.NONE; } } }
        }
        sealed class Serializer {
            StringBuilder b=new StringBuilder();
            public static string Serialize(object o){ var s=new Serializer(); s.SerializeValue(o); return s.b.ToString(); }
            void SerializeValue(object v){ if(v==null){ b.Append("null"); return; }
                if(v is string) { SerializeString((string)v); return; } if(v is bool) { b.Append((bool)v?"true":"false"); return; }
                if(v is IDictionary dict){ SerializeObject(dict); return; } if(v is IList list){ SerializeArray(list); return; }
                if(v is IFormattable) { b.Append(Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)); return; }
                SerializeString(v.ToString());
            }
            void SerializeObject(IDictionary dct){ b.Append('{'); bool first=true; foreach(var k in dct.Keys){ if(!first) b.Append(','); SerializeString(k.ToString()); b.Append(':'); SerializeValue(dct[k]); first=false; } b.Append('}'); }
            void SerializeArray(IList arr){ b.Append('['); bool first=true; foreach(var o in arr){ if(!first) b.Append(','); SerializeValue(o); first=false; } b.Append(']'); }
            void SerializeString(string s){ b.Append('\"'); foreach(var c in s){ switch(c){ case '"': b.Append("\\\""); break; case '\\': b.Append("\\\\"); break;
                    case '\b': b.Append("\\b"); break; case '\f': b.Append("\\f"); break; case '\n': b.Append("\\n"); break; case '\r': b.Append("\\r"); break; case '\t': b.Append("\\t"); break;
                    default: if(c<' '||c>126) b.Append("\\u"+((int)c).ToString("x4")); else b.Append(c); break; } } b.Append('\"'); }
        }
    }
}

static class A2PNLCore {
    static string SpecDir => "Assets/AIGG/Spec";
    static Dictionary<string,string> Synonyms = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    static List<Dictionary<string,object>> Intents = new List<Dictionary<string,object>>();
    static List<Dictionary<string,object>> Commands = new List<Dictionary<string,object>>();
    static List<Dictionary<string,object>> Macros = new List<Dictionary<string,object>>();

    static bool loaded=false;
    public static void EnsureLoaded(){
        if(loaded) return;
        string L(string f)=>Path.Combine(SpecDir,f);
        Synonyms = LoadSynonyms(L("lexicon.json"));
        Intents = LoadArray(L("intents.json"), "intents");
        Commands= LoadArray(L("commands.json"),"commands");
        Macros  = LoadArray(L("macros.json"),  "commands");
        loaded=true;
    }
    static Dictionary<string,string> LoadSynonyms(string p){
        var root = A2PJsonMini.Parse(File.ReadAllText(p)) as Dictionary<string,object>;
        var d = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        if(root!=null && root.TryGetValue("synonyms", out var s) && s is Dictionary<string,object> syn){
            foreach(var kv in syn) d[kv.Key]=kv.Value.ToString();
        }
        return d;
    }
    static List<Dictionary<string,object>> LoadArray(string p, string key){
        var root = A2PJsonMini.Parse(File.ReadAllText(p)) as Dictionary<string,object>;
        var list = new List<Dictionary<string,object>>();
        if(root!=null && root.TryGetValue(key, out var arr) && arr is List<object> a){
            foreach(var o in a) list.Add(o as Dictionary<string,object>);
        }
        return list;
    }
    public static string Normalize(string s){
        s = s.Trim().ToLowerInvariant();
        foreach(var kv in Synonyms){
            var pat="\\b"+Regex.Escape(kv.Key.ToLowerInvariant())+"\\b";
            s = Regex.Replace(s, pat, kv.Value.ToLowerInvariant());
        }
        s = Regex.Replace(s, "\\s+", " ").Trim();
        return s;
    }

    static object Cast(string spec, GroupCollection g){
        if(string.IsNullOrEmpty(spec)) return null;
        if(!spec.StartsWith("$")) return spec;
        var core = spec.Substring(1);
        var parts = core.Split(':');
        int idx = int.Parse(parts[0]);
        var typ = parts.Length>1?parts[1]:"string";
        var txt = g[idx].Value;
        switch(typ){
            case "int": return int.Parse(txt);
            case "float": return double.Parse(txt, System.Globalization.CultureInfo.InvariantCulture);
            case "bool": return txt.Equals("true", StringComparison.OrdinalIgnoreCase);
            case "int_list":
                var l=new List<object>(); foreach(var t in txt.Split(',')){ var t2=t.Trim(); if(int.TryParse(t2,out var iv)) l.Add(iv); } return l;
            default: return txt;
        }
    }

    public static Dictionary<string,object> Run(string prompt, out List<string> matched, out List<string> unmatched){
        EnsureLoaded();
        var norm = Normalize(prompt);
        matched = new List<string>();
        var exec = new List<object>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Scan(List<Dictionary<string,object>> arr, string kind){
            foreach(var it in arr){
                var name = it.TryGetValue("name", out var n)? n.ToString() : kind;
                var rx   = it.TryGetValue("regex", out var r)? r.ToString() : null;
                if(string.IsNullOrEmpty(rx)) continue;
                var m = Regex.Match(norm, rx, RegexOptions.IgnoreCase);
                if(!m.Success) continue;
                seen.Add(name);
                matched.Add($"[{kind}] {name}");
                if(kind=="commands" || kind=="macros"){
                    if(it.TryGetValue("kernel", out var kern) && kern is List<object> kl){
                        foreach(var ko in kl){
                            var d = ko as Dictionary<string,object>;
                            var opname = d["op"].ToString();
                            var argSpec = d.ContainsKey("args")? d["args"] as Dictionary<string,object> : new Dictionary<string,object>();
                            var args = new Dictionary<string,object>();
                            foreach(var kv in argSpec) args[kv.Key]=Cast(kv.Value?.ToString()??"", m.Groups);
                            exec.Add(new Dictionary<string,object>{{"op",opname},{"args",args}});
                        }
                    }
                } else if(kind=="intents"){
                    if(it.TryGetValue("ops", out var ops) && ops is List<object> ol){
                        foreach(var oo in ol){
                            var d = oo as Dictionary<string,object>;
                            var path = d["path"].ToString();
                            var val  = Cast(d["value"]?.ToString()??"", m.Groups);
                            exec.Add(new Dictionary<string,object>{{"op","set"},{"path",path},{"value",val}});
                        }
                    }
                }
            }
        }

        Scan(Intents, "intents");
        Scan(Macros,  "macros");
        Scan(Commands,"commands");

        var toks = Regex.Split(norm, "[^a-z0-9\\.]+");
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var k in Synonyms.Keys) known.Add(k); foreach(var v in Synonyms.Values) known.Add(v);
        unmatched = new List<string>();
        foreach(var t in toks){
            if(string.IsNullOrEmpty(t)) continue;
            if(Regex.IsMatch(t,"^[0-9]+(\\.[0-9]+)?$")) continue;
            if(new[]{ "m","deg","by","x","rows","row","to","len","steps" }.Contains(t)) continue;
            if(!known.Contains(t) && !seen.Contains(t)) unmatched.Add(t);
        }

        return new Dictionary<string,object>{
            {"normalized", norm},
            {"exec", exec},
            {"matched", matched},
            {"unmatched", unmatched}
        };
    }
}

public class NLGeneratorWindow : EditorWindow {
    string nl = "build 40 m by 3 m, curve rows 10-20 left 15 deg";
    string jsonOut = "";
    Vector2 s1, s2;

    [MenuItem("Window/Aim2Pro/Track Creator/NL Generator")]
    public static void Open(){ GetWindow<NLGeneratorWindow>("NL Generator"); }

    void OnGUI(){
        GUILayout.Label("Natural Language Prompt", EditorStyles.boldLabel);
        nl = EditorGUILayout.TextArea(nl, GUILayout.MinHeight(60));

        if(GUILayout.Button("NL > Json", GUILayout.Height(28))){
            try{
                var res = A2PNLCore.Run(nl, out var matched, out var unmatched);
                jsonOut = MiniJSON.Json.Serialize(res);
            }catch(Exception ex){
                jsonOut = MiniJSON.Json.Serialize(new Dictionary<string,object>{{"error",ex.Message}});
            }
        }

        GUILayout.Space(6);
        GUILayout.Label("Matched / Unmatched", EditorStyles.boldLabel);
        s1 = EditorGUILayout.BeginScrollView(s1, GUILayout.MinHeight(90), GUILayout.MaxHeight(180));
        try{
            var res = A2PJsonMini.Parse(string.IsNullOrEmpty(jsonOut)?"{}":jsonOut) as Dictionary<string,object>;
            var matched = (res!=null && res.TryGetValue("matched", out var m) && m is List<object> ml)? ml : new List<object>();
            var unmatched = (res!=null && res.TryGetValue("unmatched", out var u) && u is List<object> ul)? ul : new List<object>();
            foreach(var x in matched) GUILayout.Label("• "+x.ToString(), EditorStyles.miniBoldLabel);
            if(unmatched.Count>0){
                GUILayout.Space(4);
                GUILayout.Label("Unmatched tokens:", EditorStyles.miniBoldLabel);
                GUILayout.Label(string.Join(", ", unmatched), EditorStyles.wordWrappedMiniLabel);
            }
        } catch { GUILayout.Label("— run NL > Json first —", EditorStyles.miniLabel); }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(6);
        GUILayout.Label("Generated JSON", EditorStyles.boldLabel);
        s2 = EditorGUILayout.BeginScrollView(s2, GUILayout.MinHeight(140));
        EditorGUILayout.TextArea(string.IsNullOrEmpty(jsonOut)?"{}":Pretty(jsonOut), GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    string Pretty(string raw){
        try{
            var obj = A2PJsonMini.Parse(raw);
            var s = MiniJSON.Json.Serialize(obj);
            // simple prettifier:
            var sb=new StringBuilder(); int ind=0; bool q=false;
            foreach(char c in s){
                if(c=='\"') q=!q;
                if(!q && (c==',')){ sb.Append(c); sb.Append('\n'); sb.Append(new string(' ',ind*2)); continue; }
                if(!q && (c=='{'||c=='[')){ sb.Append(c); sb.Append('\n'); ind++; sb.Append(new string(' ',ind*2)); continue; }
                if(!q && (c=='}'||c==']')){ sb.Append('\n'); ind=Math.Max(0,ind-1); sb.Append(new string(' ',ind*2)); sb.Append(c); continue; }
                sb.Append(c);
            }
            return sb.ToString();
        }catch{ return raw; }
    }
}
