using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// Progress through the night. Kept across a scene reload (death → retry) and saved to disk at every checkpoint (Continue).
    public static class Checkpoint
    {
        public static int stage;
        public static bool restart, startIntro;
        public static bool ghostReturned, ghostLoose;
        public static string scrawl1, scrawl2;
        public static readonly List<string> passed = new List<string>();
        public static readonly List<string> extra = new List<string>();
        public static int deaths;

        public static readonly string[] Names = { "Arrival", "The goods train", "The radio", "The Kalvadi Mail", "The Rockfort Express", "The 3:10" };
        const char Sep = '\u001f';

        public static void Clear()
        {
            stage = 0; restart = false; ghostReturned = ghostLoose = false;
            scrawl1 = scrawl2 = null; passed.Clear(); extra.Clear(); deaths = 0;
        }

        public static bool HasSave => PlayerPrefs.GetInt("sb_save", 0) == 1;
        public static string SaveName => Names[Mathf.Clamp(PlayerPrefs.GetInt("sb_stage", 0), 0, Names.Length - 1)];

        public static void SaveToPrefs()
        {
            PlayerPrefs.SetInt("sb_save", 1);
            PlayerPrefs.SetInt("sb_stage", stage);
            PlayerPrefs.SetInt("sb_ghost", (ghostReturned ? 1 : 0) | (ghostLoose ? 2 : 0));
            PlayerPrefs.SetString("sb_s1", scrawl1 ?? "");
            PlayerPrefs.SetString("sb_s2", scrawl2 ?? "");
            PlayerPrefs.SetString("sb_passed", string.Join(Sep.ToString(), passed));
            PlayerPrefs.SetString("sb_extra", string.Join(Sep.ToString(), extra));
            PlayerPrefs.SetInt("sb_deaths", deaths);
            PlayerPrefs.Save();
        }

        public static void LoadFromPrefs()
        {
            Clear();
            if (!HasSave) return;
            stage = PlayerPrefs.GetInt("sb_stage", 0);
            int g = PlayerPrefs.GetInt("sb_ghost", 0);
            ghostReturned = (g & 1) != 0; ghostLoose = (g & 2) != 0;
            var s1 = PlayerPrefs.GetString("sb_s1", ""); scrawl1 = s1.Length > 0 ? s1 : null;
            var s2 = PlayerPrefs.GetString("sb_s2", ""); scrawl2 = s2.Length > 0 ? s2 : null;
            foreach (var p in PlayerPrefs.GetString("sb_passed", "").Split(Sep)) if (p.Length > 0) passed.Add(p);
            foreach (var p in PlayerPrefs.GetString("sb_extra", "").Split(Sep)) if (p.Length > 0) extra.Add(p);
            deaths = PlayerPrefs.GetInt("sb_deaths", 0);
        }

        public static void DeleteSave()
        {
            foreach (var k in new[] { "sb_save", "sb_stage", "sb_ghost", "sb_s1", "sb_s2", "sb_passed", "sb_extra", "sb_deaths" }) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
        }
    }

    /// Player options, stored in PlayerPrefs.
    public static class Settings
    {
        public static float Volume = 1f, Sens = 1f;
        public static bool InvertY;

        public static void Load()
        {
            Volume = PlayerPrefs.GetFloat("sb_vol", 1f);
            Sens = PlayerPrefs.GetFloat("sb_sens", 1f);
            InvertY = PlayerPrefs.GetInt("sb_inv", 0) == 1;
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("sb_vol", Volume);
            PlayerPrefs.SetFloat("sb_sens", Sens);
            PlayerPrefs.SetInt("sb_inv", InvertY ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        public static void Apply() { AudioListener.volume = Volume; }
    }
}
