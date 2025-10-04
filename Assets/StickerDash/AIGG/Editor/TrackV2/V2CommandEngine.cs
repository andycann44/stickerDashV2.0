using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Aim2Pro.AIGG.TrackV2
{
    [Serializable] public class CommandCall { public string fn; public List<string> args; }
    [Serializable] public class CommandRule { public string name; public string regex; public CommandCall call; }
    [Serializable] public class CommandSpec { public List<CommandRule> commands; }

    public class V2CommandEngine
    {
        // Robust 'create <len> m by <width> m <extras>' recognizer
        // (also accepts 'build'/'make' and numbers like '250m')
        static readonly System.Text.RegularExpressions.Regex RxCreateLW =
            new System.Text.RegularExpressions.Regex(
                @"\b(?:create|build|make)\s+(\d+)\s*(?:m|meter|meters|metre|metres)?\s*(?:by|x)\s*(\d+)\s*(?:m|meter|meters|metre|metres)?(?:\s*(.*))?$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        void HardcodedCreateRecognizer(string nl, System.Collections.Generic.List<PlannedCall> planned, System.Action<string> log)
        {
            var text = nl ?? string.Empty;
            var m = RxCreateLW.Match(text);
            if (!m.Success) return;
            int length = int.Parse(m.Groups[1].Value);
            int width  = int.Parse(m.Groups[2].Value);
            string extras = (m.Groups[3].Success ? m.Groups[3].Value : string.Empty).Trim();
            planned.Add(new PlannedCall("GenerateScenarioFromPrompt", new object[]{ length, width, extras }));
            log?.Invoke($"Matched: scenario-create → GenerateScenarioFromPrompt({length}, {width}, {extras})");
        }
    
        private readonly Action<string> log;
        private CommandSpec spec;
        private readonly List<(CommandRule rule, object[] args)> planned = new();

        public V2CommandEngine(Action<string> logger) { log = logger; }

        public void LoadRules()
        {
            var ta = Resources.Load<TextAsset>("TrackV2/commands");
            if (ta == null) { log("ERROR: commands.json not found at Resources/TrackV2/commands.json"); return; }
            spec = JsonUtility.FromJson<CommandSpec>(ta.text);
            log($"Loaded {spec?.commands?.Count ?? 0} command rules.");
        }

        public void Parse(string nl)
        {
            try { HardcodedCreateRecognizer(nl, planned, Log); } catch {}

            if (spec?.commands == null) { log("No rules loaded."); return; }
            planned.Clear();

            var lines = nl.Split(new[] { '\r','\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var line = Regex.Replace(raw.Trim(), "\\s+", " ").ToLowerInvariant();
                bool matched = false;
                foreach (var rule in spec.commands)
                {
                    var m = new Regex(rule.regex, RegexOptions.IgnoreCase).Match(line);
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
                try { KernelInvokerV2.Call(rule.call.fn, args); log($"APPLIED: {rule.call.fn}()"); }
                catch (Exception ex) { log($"ERROR applying {rule.call.fn}: {ex.Message}"); }
            }
            if (planned.Count == 0) log("Nothing to apply.");
        }

        private static object[] BuildArgs(List<string> argSpecs, Match m)
        {
            var list = new List<object>();
            if (argSpecs == null) return list.ToArray();

            foreach (var spec in argSpecs)
            {
                // Accept both "$2?:float" and "$2?" styles; type is optional.
                bool optional = spec.Contains("?:") || spec.EndsWith("?");
                string core = spec.Contains("?:") ? spec.Replace("?:", ":") : spec.TrimEnd('?');

                string cap = null;
                string type = null;

                if (core.StartsWith("$", StringComparison.Ordinal))
                {
                    var after = core.Substring(1);
                    var parts = after.Split(':');          // ["2"] or ["2","float"]
                    var idxStr = parts[0].TrimEnd('?');    // tolerate "$2?"
                    if (!int.TryParse(idxStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx))
                    {
                        // Bad capture index; skip optional else null
                        if (optional) continue;
                        list.Add(null);
                        continue;
                    }
                    type = parts.Length > 1 ? parts[1] : null;
                    cap  = m.Groups.Count > idx ? m.Groups[idx].Value : null;
                }
                else
                {
                    cap = core;
                }

                if (string.IsNullOrEmpty(cap))
                {
                    if (optional) continue; // skip missing optional
                    list.Add(null);
                    continue;
                }

                try
                {
                    if (type == "int")
                        list.Add(int.Parse(cap, CultureInfo.InvariantCulture));
                    else if (type == "float")
                        list.Add(float.Parse(cap, CultureInfo.InvariantCulture));
                    else
                        list.Add(cap);
                }
                catch
                {
                    // Be forgiving: if cast fails, pass raw string
                    list.Add(cap);
                }
            }
            return list.ToArray();
        }
    }
}
