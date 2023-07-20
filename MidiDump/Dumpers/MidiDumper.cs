using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;

namespace MidiDump
{
    public abstract class MidiDumper
    {
        public static readonly ReadingSettings MidiSettings = new()
        {
            InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.ReadValid,
            InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
            NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
        };

        public virtual bool Initialize() => true;

        public virtual void ProcessMidi(string filePath)
        {
            Console.WriteLine($"Reading {filePath}");
            var midi = MidiFile.Read(filePath, MidiSettings);
            foreach (var track in midi.GetTrackChunks())
            {
                if (track.Events.Count < 1)
                    continue;

                if (track.Events[0] is not SequenceTrackNameEvent trackName)
                    continue;

                string name = trackName.Text;
                ProcessTrackChunk(filePath, name, track);
            }
        }

        protected abstract void ProcessTrackChunk(string filePath, string name, TrackChunk track);
        public abstract IEnumerable<string> DumpResults();
    }
}