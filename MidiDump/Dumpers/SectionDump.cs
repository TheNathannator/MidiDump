using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.Core;

namespace MidiDump
{
    public partial class SectionDump : MidiDumper
    {
        // Isolates the text inside brackets of text events, removing any garbage whitespace
        // '[section Practice]' -> 'section Practice'
        // '[prc_intro] - "Intro"' -> 'prc_intro'
        // '[prc__verse] - "Verse"' -> -> 'prc__verse'
        [GeneratedRegex(@"\[\s*(.*?)\s*\]", RegexOptions.Singleline)]
		public static partial Regex TextEventRegex();

        // Isolates the single name of a 
        // 'section Practice' -> 'Practice'
        // 'prc_intro' -> 'intro'
        // 'prc__verse' -> 'verse'
        // Harmonix dun-goofed with some sections, here's some real edge-cases some charts have:
        // 'section_outro ' -> 'outro'
        // 'section  chorus_1' -> 'chorus_1'
        // 'seciton outro_b' -> 'outro_b'
        [GeneratedRegex(@"(?:section|seciton|prc)[\s_]*(.*)", RegexOptions.Singleline)]
		public static partial Regex SectionRegex();

        // For loading known sections and names
        // '[prc_intro] - "Intro"' -> 'intro', 'Intro'
        // '[prc__verse] - "Verse"' -> -> 'verse', 'Verse'
        [GeneratedRegex(@"\[\s*(?:section|seciton|prc)[\s_]*(\S*)\s*\].*""(.*)""", RegexOptions.Singleline)]
		public static partial Regex SectionAndNameRegex();

        // For loading .dta locale information
        // '(bass_solo "Bass Solo")' -> 'bass_solo', 'Bass Solo"
        // '(bass_solo "Bass Solo" "Bass Solo 2")' -> not matched
        [GeneratedRegex(@"\(\s*([^""\s]*)\s*""([^""]*?)""\s*\)", RegexOptions.Multiline)]
		public static partial Regex DtaLocaleRegex();

        private const string KnownSectionsFile = "known_sections.txt";
        private const string DtaFolder = "DTA";
        private const string DtaListFile = "dta.txt";

        private readonly Dictionary<string, List<string>> _dtaLocale = new();

        private readonly Dictionary<string, string> _knownSections = new();
        private readonly Dictionary<string, List<string>> _newSections = new();

        public override bool Initialize()
        {
            if (File.Exists(KnownSectionsFile))
            {
                Console.WriteLine("Loading known sections...");
                foreach (var line in File.ReadAllLines(KnownSectionsFile))
                {
                    if (SectionAndNameRegex().Match(line) is not { Success: true } sectionMatch)
                        continue;
                    string section = sectionMatch.Groups[1].Value;
                    string name = sectionMatch.Groups[2].Value;

                    if (!_knownSections.ContainsKey(section))
                        _knownSections.Add(section, name);
                }
            }
            else
            {
                if (!Program.YesNo($"No {KnownSectionsFile} file provided. All sections found from this point forward will be treated as new.\nDo you want to continue?"))
                    return false;
            }

            IEnumerable<string> dtaFiles = null;
            if (Directory.Exists(DtaFolder))
                dtaFiles = Directory.EnumerateFiles(DtaFolder, "*.dta", Program.FileOptions);
            else if (File.Exists(DtaListFile))
                dtaFiles = File.ReadAllLines(DtaListFile).Where((path) => Path.GetExtension(path) == ".dta");
            else if (!Program.YesNo("No .dta files provided! The list of sections created from this point forward will only include the files in which the sections exist, which will get very large.\nDo you want to continue?"))
                return false;

            if (dtaFiles != null)
            {
                Console.WriteLine("Loading .dta locale info...");
                foreach (var dtaFile in dtaFiles)
                {
                    Console.WriteLine($"Loading {dtaFile}");
                    foreach (var line in File.ReadAllLines(dtaFile))
                    {
                        if (DtaLocaleRegex().Match(line) is not { Success: true } dtaMatch)
                            continue;
                        string symbol = dtaMatch.Groups[1].Value;
                        string name = dtaMatch.Groups[2].Value;

                        // Ignore symbols/names that most likely aren't sections
                        if (name.Length > 50)
                            continue;

                        if (!_dtaLocale.TryGetValue(symbol, out var names))
                        {
                            names = new() { name };
                            _dtaLocale.Add(symbol, names);
                        }
                        else if (!names.Contains(name))
                        {
                            Debug.WriteLine($"Warning: Found extra name '{name}' for {symbol}");
                            names.Add(name);
                        }
                    }
                }
                Console.WriteLine();
            }

            return true;
        }

        protected override void ProcessTrackChunk(string filePath, string name, TrackChunk track)
        {
            if (name != "EVENTS")
                return;

            foreach (var midiEvent in track.Events)
            {
                if (midiEvent is not TextEvent textEvent)
                    continue;
                string text = textEvent.Text;

                string section = GetSectionName(text);
                if (string.IsNullOrEmpty(section))
                    continue;

                if (_knownSections.ContainsKey(section))
                    continue;

                if (_newSections.TryGetValue(section, out var files))
                {
                    files.Add(filePath);
                    continue;
                }

                _newSections.Add(section, new() { filePath });
                if (!_dtaLocale.TryGetValue(section, out var names))
                {
                    // No names available
                    Debug.WriteLine($"Found new section {section}, no names found");
                    continue;
                }

                Debug.WriteLine($"Found new section {section}, {names.Count} names found");
            }
        }

        private string GetSectionName(string text)
        {
            if (TextEventRegex().Match(text) is not { Success: true } textMatch)
                return null;
            string eventText = textMatch.Groups[1].Value;

            if (SectionRegex().Match(eventText) is not { Success: true } sectionMatch)
                return null;
            string section = sectionMatch.Groups[1].Value;

            return section;
        }

        public override IEnumerable<string> DumpResults()
        {
            yield return "Found sections:";
            var newSections = _newSections.Keys.ToList();
            newSections.Sort();
            foreach (var section in newSections)
            {
                if (!_dtaLocale.TryGetValue(section, out var names))
                    continue;

                yield return $"{section} - {string.Join(", ", names.ToArray())}";
            }

            yield return "\nFound sections (no name found):";
            foreach (var section in newSections)
            {
                if (_dtaLocale.ContainsKey(section))
                    continue;

                var paths = _newSections[section].ToArray();
                if (paths.Length == 1)
                    yield return $"{section} - found in {paths[0]}";
                else
                    yield return $"{section} - found in:\n - {string.Join("\n - ", paths)}\n";
            }

            yield return "\nKnown sections:";
            var knownSections = _knownSections.Keys.ToList();
            knownSections.Sort();
            foreach (string section in knownSections)
            {
                string name = _knownSections[section];
                yield return $"{section} - {name}";
            }
        }
    }
}