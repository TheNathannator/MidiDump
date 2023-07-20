using System;
using System.Diagnostics;
using System.IO;

namespace MidiDump
{
    public class Program
    {
        private const string DirectoriesFile = "search_paths.txt";

        public static readonly EnumerationOptions FileOptions = new()
        {
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false,
            IgnoreInaccessible = true,
        };

        public static void Main(string[] args)
        {
            var dumper = new SectionDump();
            try
            {
                if (!dumper.Initialize())
                    return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during initialization: {ex.Message}");
                Debug.WriteLine(ex);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            string[] directories;
            if (args.Length > 0)
            {
                directories = args;
            }
            else if (File.Exists(DirectoriesFile))
            {
                directories = File.ReadAllLines(DirectoriesFile);
            }
            else
            {
                Console.WriteLine($"Please provide some search directories, either as command-line arguments or via a {DirectoriesFile} file.");
                return;
            }

            foreach (string directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine($"Directory {directory} does not exist, skipping.");
                    continue;
                }

                Console.WriteLine($"Reading charts from {directory}");
                foreach (var midiFile in Directory.EnumerateFiles(directory, "notes.mid", FileOptions))
                {
                    try
                    {
                        dumper.ProcessMidi(midiFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing {midiFile}: {ex.Message}");
                        Debug.WriteLine(ex);
                        continue;
                    }
                }
            }

            Console.WriteLine("\n\nWriting results...");
            File.WriteAllLines("results.txt", dumper.DumpResults());

            Console.WriteLine("Finished.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static bool YesNo(string message)
        {
            Console.Write(message + " (yes/no):");
            while (true)
            {
                string line = Console.ReadLine().ToLowerInvariant();
                if (line is "y" or "yes")
                    return true;
                else if (line is "n" or "no")
                    return false;

                Console.Write("Invalid response! yes/no:");
            }
        }
    }
}
