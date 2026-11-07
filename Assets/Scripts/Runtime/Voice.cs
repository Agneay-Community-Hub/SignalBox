using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// Voice acting. Clips are generated into Assets/Resources/Voice by "Signal Box → Generate Voices",
    /// one per (speaker, line), named by a hash of both so the text in code is the single source of truth.
    public static class Voice
    {
        public const string Raghavan = "R", Ghost = "Q", You = "Y", Passengers = "P", Whisper = "W", Narrator = "N", Letter = "L", Anand = "A", Radio = "B";

        /// Speaker for an on-screen label (used by Director.Speak and the voice generator alike).
        public static string ForLabel(string label)
        {
            if (label.StartsWith("RAGHAVAN")) return Raghavan;
            if (label == "???") return Ghost;
            if (label == "A VOICE YOU KNOW") return You;
            if (label == "CHILD") return Whisper;
            if (label == "RADIO") return Radio;
            return Passengers;
        }

        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();

        public static string Key(string speaker, string text)
        {
            uint h = 2166136261;
            foreach (char c in speaker + "|" + text) { h ^= c; h *= 16777619; }
            return "v_" + h.ToString("x8");
        }

        public static bool Speakable(string text) => !string.IsNullOrEmpty(text) && text[0] != '[' && text[0] != '(';

        public static AudioClip Get(string speaker, string text)
        {
            if (!Speakable(text)) return null;
            string k = Key(speaker, text);
            if (Cache.TryGetValue(k, out var c)) return c;
            c = Resources.Load<AudioClip>("Voice/" + k);
            Cache[k] = c;
            return c;
        }
    }
}
