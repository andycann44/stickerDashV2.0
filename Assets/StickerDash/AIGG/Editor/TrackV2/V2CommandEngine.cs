using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Aim2Pro.AIGG.TrackV2
{
    [Serializable]
    public class CommandCall { public string fn; public List<string> args; }
    [Serializable]
    public class CommandRule { public string name; public string regex; public CommandCall call; }
    [Serializable]
    public class CommandSpec { public List<CommandRule> commands; }

    public class V2CommandEngine
    {
        private readonly Action<string> log;
        private CommandSpec spec;
        private readonly List<(CommandRule rule, object[] args)> planned = new List<(CommandRule, object[])>();

        public V2CommandEngine(Action<string> logger) { log = logger; }

        public void LoadRules()
        {
            var ta = Resources.Load<TextAsset>("TrackV2/commands");
            if (ta == null)
            {
                log("ERROR: commands.json not found at Resources/TrackV2/commands.json");
                return;
            }
            spec = JsonUtility.FromJson<CommandSpec>(ta.text);
            log($"Loaded {spec?.commands?.Count ?? 0} command rules.");
        }

        public void Parse(string nl)
        {
            if (spec?.commands == null) { log("No rules loaded."); return; }
            planned.Clear();

            var lines = nl.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var line = Normalize(raw);
                bool matched = false;
                foreach (var rule in spec.commands)
                {
                    var rx = new Regex(rule.regex, RegexOptions.IgnoreCase);
                    var m = rx.Match(line);
                    if (!m.Success) continue;

                    var args = BuildArgs(rule.call.args, m);
                    planned.Add((rule, args));
                    log($"Matched: {rule.name} → {rule.call.fn}({string.Join(", ", args)})");
                    matched = true;
                    break;
                }
                if (!matched) log($"No match: \"{line}\"");
            }
            log($"Planned {planned.Count} kernel calls.");
        }

        public void Apply()
        {
            foreach (var (rule, args) in planned)
            {
                try
                {
                    KernelInvokerV2.Call(rule.call.fn, args);
                    log($"APPLIED: {rule.call.fn}()");
                }
                catch (Exception ex)
                {
                    log($"ERROR applying {rule.call.fn}: {ex.Message}");
                }
            }
            if (planned.Count == 0) log("Nothing to apply.");
        }

        private static string Normalize(string s)
        {
            return Regex.Replace(s.Trim(), "\\s+", " ").ToLowerInvariant();
        }

        private static object[] BuildArgs(List<string> argSpecs, Match m)
        {
            var list = new List<object>();
            if (argSpecs == null) return list.ToArray();

            foreach (var spec in argSpecs)
            {
                var optional = spec.Contains("?:");
                var core = spec.Replace("?:", ":");
                string cap;
                string type = null;

                if (core.StartsWith("$", StringComparison.Ordinal))
                {
                    var parts = core.Substring(1).Split(':');
                    var idx = int.Parse(parts[0], CultureInfo.InvariantCulture);
                    type = parts.Length > 1 ? parts[1] : null;
                    cap = m.Groups.Count > idx ? m.Groups[idx].Value : null;
                }
                else
                {
                    cap = spec;
                }

                if (string.IsNullOrEmpty(cap))
                {
                    if (optional) continue;
                    list.Add(null);
                    continue;
                }

                if (type == "int")
                    list.Add(int.Parse(cap, CultureInfo.InvariantCulture));
                else if (type == "float")
                    list.Add(float.Parse(cap, CultureInfo.InvariantCulture));
                else
                    list.Add(cap);
            }
            return list.ToArray();
        }
    }
}
