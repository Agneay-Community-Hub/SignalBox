using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace SignalBox.EditorTools
{
    /// Generates every voiced line with the Windows speech voices installed on this PC, then processes each
    /// (phone line, ghost pitch, whispering crowd, room reverb) and writes it to Assets/Resources/Voice.
    public static class VoiceGen
    {
        class Job { public string key, spk, text; }
        class Take { public string voice, rate, pitch; public Take(string v, string r, string p) { voice = v; rate = r; pitch = p; } }

        [MenuItem("Signal Box/Generate Voices")]
        public static void Menu() => Debug.Log("[Signal Box] " + Run(false));

        [MenuItem("Signal Box/Regenerate All Voices")]
        public static void MenuAll() => Debug.Log("[Signal Box] " + Run(true));

        static Take[] Takes(string spk)
        {
            switch (spk)
            {
                case Voice.Raghavan: return new[] { new Take("Mark", "+2%", "-6%") };
                case Voice.Ghost: return new[] { new Take("David", "-14%", "-22%") };
                case Voice.You: return new[] { new Take("David", "-8%", "-5%") };
                case Voice.Passengers: return new[] { new Take("Kalpana", "-25%", "-15%"), new Take("Zira", "-28%", "-20%"), new Take("Mark", "-22%", "-25%") };
                case Voice.Whisper: return new[] { new Take("Kalpana", "-30%", "-20%") };
                case Voice.Narrator: return new[] { new Take("Mark", "+4%", "-12%") };
                case Voice.Letter: return new[] { new Take("Hemant", "-4%", "0%") };
                case Voice.Radio: return new[] { new Take("Kalpana", "+2%", "-4%") };
                default: return new[] { new Take("David", "-14%", "-10%") };      // Anand
            }
        }

        // ---------------- Collecting lines straight from the game code ----------------

        static string Lit(string s) => s.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");

        static List<Job> Collect()
        {
            var jobs = new List<Job>();
            var seen = new HashSet<string>();
            void Add(string spk, string text)
            {
                if (!Voice.Speakable(text)) return;
                var k = Voice.Key(spk, text);
                if (seen.Add(k)) jobs.Add(new Job { key = k, spk = spk, text = text });
            }
            string dir = Path.Combine(Application.dataPath, "Scripts/Runtime");
            const string S = "\"((?:[^\"\\\\]|\\\\.)*)\"";
            string director = File.ReadAllText(Path.Combine(dir, "Director.cs"));
            foreach (Match m in Regex.Matches(director, "(?:L|PhoneSay)\\((R|Q|Y), " + S + "\\)"))
                Add(m.Groups[1].Value, Lit(m.Groups[2].Value));
            foreach (Match m in Regex.Matches(director, "Narrate\\(" + S + "\\)")) Add(Voice.Narrator, Lit(m.Groups[1].Value));
            string scares = File.ReadAllText(Path.Combine(dir, "Scares.cs"));
            foreach (Match m in Regex.Matches(director + scares, "Speak\\(\"([^\"]+)\", " + S)) Add(Voice.ForLabel(m.Groups[1].Value), Lit(m.Groups[2].Value));
            foreach (Match m in Regex.Matches(director, "Broadcast\\(" + S + "\\)")) Add(Voice.Radio, Lit(m.Groups[1].Value));
            foreach (Match m in Regex.Matches(scares, "Whisper\\(" + S + "\\)")) Add(Voice.Whisper, Lit(m.Groups[1].Value));
            foreach (var s in (string[])typeof(Director).GetField("Whispers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)) Add(Voice.Passengers, s);
            foreach (var s in (string[])typeof(Scares).GetField("WhisperLines", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)) Add(Voice.Whisper, s);
            Add(Voice.Narrator, Director.DateCard);
            foreach (var s in (string[])typeof(Scares).GetField("Pleas", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)) { var b = s.Split('|'); Add(Voice.ForLabel(b[0]), b[1]); }
            Add(Voice.Anand, Director.HandoverBody);
            return jobs;
        }

        // ---------------- Text → speech-friendly text ----------------

        static readonly string[] Ones = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty" };
        static string Num(int n) => n < 20 ? Ones[n] : Tens[n / 10] + (n % 10 == 0 ? "" : "-" + Ones[n % 10]);

        static string Speech(string t)
        {
            t = Regex.Replace(t, "\\[[^\\]]*\\]", " ");
            t = Regex.Replace(t, "\\([^)]*\\)", " ");
            t = Regex.Replace(t, "\\b(\\d{1,2}):(\\d{2})\\b", m =>
            {
                int h = int.Parse(m.Groups[1].Value), mm = int.Parse(m.Groups[2].Value);
                return Num(h) + (mm == 0 ? " o'clock" : mm < 10 ? " oh " + Num(mm) : " " + Num(mm));
            });
            t = t.Replace("1981", "nineteen eighty-one").Replace("11 km", "eleven kilometres");
            t = Regex.Replace(t, "\\b\\d{5}\\b", m => string.Join(" ", m.Value.ToCharArray()));
            t = t.Replace("—", ", ").Replace("·", ", ").Replace("\n", " ");
            return Regex.Replace(t, "\\s+", " ").Trim();
        }

        static string Xml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("\"", "&quot;");
        static string Ps(string s) => s.Replace("'", "''");

        const string Header = @"$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
[Windows.Media.SpeechSynthesis.SpeechSynthesizer,Windows.Media.SpeechSynthesis,ContentType=WindowsRuntime] | Out-Null
[Windows.Storage.Streams.DataReader,Windows.Storage.Streams,ContentType=WindowsRuntime] | Out-Null
$asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | ? { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
function Await($op, [Type]$t) { $task = $asTask.MakeGenericMethod($t).Invoke($null, @($op)); $task.Wait() | Out-Null; $task.Result }
$syn = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
$all = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices
function Say($name, $rate, $pitch, $text, $path) {
  $v = $all | ? { $_.DisplayName -like ('*' + $name + '*') } | Select -First 1
  if (-not $v) { $v = $all | Select -First 1 }
  $syn.Voice = $v
  $ssml = ""<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='"" + $v.Language + ""'><prosody rate='"" + $rate + ""' pitch='"" + $pitch + ""'>"" + $text + ""</prosody></speak>""
  try { $st = Await ($syn.SynthesizeSsmlToStreamAsync($ssml)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream]) }
  catch { $st = Await ($syn.SynthesizeTextToStreamAsync($text)) ([Windows.Media.SpeechSynthesis.SpeechSynthesisStream]) }
  $r = New-Object Windows.Storage.Streams.DataReader($st.GetInputStreamAt(0))
  Await ($r.LoadAsync([uint32]$st.Size)) ([uint32]) | Out-Null
  $b = New-Object byte[] $st.Size; $r.ReadBytes($b)
  [IO.File]::WriteAllBytes($path, $b)
}
";

        public static string Run(bool force)
        {
            var jobs = Collect();
            string proj = Directory.GetParent(Application.dataPath).FullName;
            string raw = Path.Combine(proj, "Temp", "voice_raw");
            Directory.CreateDirectory(raw);
            string outDir = Path.Combine(Application.dataPath, "Resources", "Voice");
            Directory.CreateDirectory(outDir);

            var todo = new List<Job>();
            var sb = new StringBuilder(Header);
            foreach (var j in jobs)
            {
                if (!force && File.Exists(Path.Combine(outDir, j.key + ".wav"))) continue;
                todo.Add(j);
                var takes = Takes(j.spk);
                string text = Xml(Speech(j.text));
                for (int i = 0; i < takes.Length; i++)
                {
                    var p = Path.Combine(raw, j.key + "_" + i + ".wav");
                    if (File.Exists(p)) File.Delete(p);
                    sb.AppendLine("Say '" + takes[i].voice + "' '" + takes[i].rate + "' '" + takes[i].pitch + "' '" + Ps(text) + "' '" + Ps(p) + "'");
                }
            }
            var keep = new HashSet<string>(); foreach (var j in jobs) keep.Add(j.key + ".wav");
            foreach (var f in Directory.GetFiles(outDir, "v_*.wav")) if (!keep.Contains(Path.GetFileName(f))) { File.Delete(f); if (File.Exists(f + ".meta")) File.Delete(f + ".meta"); }
            if (todo.Count == 0) { AssetDatabase.Refresh(); return "Voices up to date (" + jobs.Count + " lines).";}

            string ps1 = Path.Combine(raw, "gen.ps1");
            File.WriteAllText(ps1, sb.ToString(), new UTF8Encoding(true));
            var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + ps1 + "\"")
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            var pr = System.Diagnostics.Process.Start(psi);
            string err = pr.StandardError.ReadToEnd();
            pr.WaitForExit(15 * 60 * 1000);

            int made = 0;
            var failed = new List<string>();
            foreach (var j in todo)
            {
                var takes = Takes(j.spk);
                var parts = new List<float[]>();
                int sr = 16000;
                for (int i = 0; i < takes.Length; i++)
                {
                    var p = Path.Combine(raw, j.key + "_" + i + ".wav");
                    if (File.Exists(p) && Dsp.ReadWav(p, out var d, out sr)) parts.Add(d);
                }
                if (parts.Count == 0) { failed.Add(j.spk + ": " + j.text.Substring(0, Mathf.Min(40, j.text.Length))); continue; }
                var outp = Dsp.Process(j.spk, parts, sr);
                Dsp.WriteWav(Path.Combine(outDir, j.key + ".wav"), outp, sr);
                made++;
            }
            AssetDatabase.Refresh();
            return "Generated " + made + " voice lines (" + jobs.Count + " total)." + (failed.Count > 0 ? " FAILED: " + string.Join(" | ", failed) + " " + err : "");
        }

        // ---------------- Processing ----------------

        static class Dsp
        {
            public static bool ReadWav(string path, out float[] data, out int sr)
            {
                data = null; sr = 16000;
                var b = File.ReadAllBytes(path);
                if (b.Length < 44 || Encoding.ASCII.GetString(b, 0, 4) != "RIFF") return false;
                int ch = 1, bits = 16, pos = 12;
                while (pos + 8 <= b.Length)
                {
                    string id = Encoding.ASCII.GetString(b, pos, 4);
                    int len = System.BitConverter.ToInt32(b, pos + 4);
                    if (id == "fmt ") { ch = System.BitConverter.ToInt16(b, pos + 10); sr = System.BitConverter.ToInt32(b, pos + 12); bits = System.BitConverter.ToInt16(b, pos + 22); }
                    else if (id == "data")
                    {
                        len = Mathf.Min(len, b.Length - pos - 8);
                        if (bits != 16) return false;
                        int n = len / 2 / ch;
                        data = new float[n];
                        for (int i = 0; i < n; i++) data[i] = System.BitConverter.ToInt16(b, pos + 8 + i * 2 * ch) / 32768f;
                        return true;
                    }
                    pos += 8 + len + (len & 1);
                }
                return false;
            }

            public static void WriteWav(string path, float[] d, int sr)
            {
                using (var w = new BinaryWriter(File.Create(path)))
                {
                    w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + d.Length * 2); w.Write(Encoding.ASCII.GetBytes("WAVE"));
                    w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
                    w.Write(Encoding.ASCII.GetBytes("data")); w.Write(d.Length * 2);
                    foreach (var s in d) w.Write((short)Mathf.Clamp(s * 32767f, -32767f, 32767f));
                }
            }

            class BQ
            {
                float b0, b1, b2, a1, a2, x1, x2, y1, y2;
                public BQ(int mode, float f, float q, int sr)
                {
                    float w = 2f * Mathf.PI * Mathf.Clamp(f, 20f, sr * 0.45f) / sr, cs = Mathf.Cos(w), al = Mathf.Sin(w) / (2f * q), a0 = 1f + al;
                    if (mode == 0) { b0 = al; b1 = 0f; b2 = -al; }
                    else if (mode == 1) { b0 = (1f - cs) * 0.5f; b1 = 1f - cs; b2 = b0; }
                    else { b0 = (1f + cs) * 0.5f; b1 = -(1f + cs); b2 = b0; }
                    a1 = -2f * cs; a2 = 1f - al;
                    b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
                }
                public float P(float x) { float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2; x2 = x1; x1 = x; y2 = y1; y1 = y; return y; }
            }

            static float[] Filter(float[] d, int mode, float f, int sr, float q = 0.707f) { var bq = new BQ(mode, f, q, sr); var o = new float[d.Length]; for (int i = 0; i < d.Length; i++) o[i] = bq.P(d[i]); return o; }

            static float[] Resample(float[] d, float rate)
            {
                int n = Mathf.FloorToInt(d.Length / rate);
                var o = new float[n];
                for (int i = 0; i < n; i++) { float x = i * rate; int a = (int)x; float f = x - a; o[i] = a + 1 < d.Length ? d[a] * (1f - f) + d[a + 1] * f : d[Mathf.Min(a, d.Length - 1)]; }
                return o;
            }

            static float[] Pad(float[] d, float secs, int sr) { var o = new float[d.Length + (int)(secs * sr)]; System.Array.Copy(d, o, d.Length); return o; }

            static void Reverb(float[] d, float wet, float room, int sr)
            {
                float sc = sr / 44100f * room;
                int[] cd = { 1557, 1617, 1491, 1422, 1277, 1356 };
                int[] ad = { 225, 556, 441 };
                var combs = new float[cd.Length][]; var ci = new int[cd.Length]; var cl = new float[cd.Length];
                for (int k = 0; k < cd.Length; k++) combs[k] = new float[Mathf.Max(8, Mathf.RoundToInt(cd[k] * sc))];
                var aps = new float[ad.Length][]; var ai = new int[ad.Length];
                for (int k = 0; k < ad.Length; k++) aps[k] = new float[Mathf.Max(8, Mathf.RoundToInt(ad[k] * sr / 44100f))];
                for (int i = 0; i < d.Length; i++)
                {
                    float x = d[i], s = 0f;
                    for (int k = 0; k < combs.Length; k++)
                    {
                        var b = combs[k]; float y = b[ci[k]];
                        cl[k] = y * 0.7f + cl[k] * 0.3f;
                        b[ci[k]] = x * 0.02f + cl[k] * 0.84f;
                        ci[k] = (ci[k] + 1) % b.Length; s += y;
                    }
                    for (int k = 0; k < aps.Length; k++) { var b = aps[k]; float bo = b[ai[k]]; float o = -s + bo; b[ai[k]] = s + bo * 0.5f; ai[k] = (ai[k] + 1) % b.Length; s = o; }
                    d[i] = x * (1f - wet * 0.4f) + s * wet * 3f;
                }
            }

            static float Tanh(float x) { float e = Mathf.Exp(2f * Mathf.Clamp(x, -9f, 9f)); return (e - 1f) / (e + 1f); }

            static float[] Phone(float[] d, int sr, float drive)
            {
                d = Filter(Filter(d, 2, 330f, sr), 1, 3200f, sr);
                d = Filter(d, 0, 1400f, sr, 0.6f);
                var rnd = new System.Random(7);
                for (int i = 0; i < d.Length; i++) d[i] = Tanh(d[i] * drive) / Tanh(drive) + (float)(rnd.NextDouble() - 0.5) * 0.012f;
                return d;
            }

            static void Normalize(float[] d, float peak) { float m = 1e-4f; foreach (var s in d) m = Mathf.Max(m, Mathf.Abs(s)); float k = peak / m; for (int i = 0; i < d.Length; i++) d[i] *= k; }

            public static float[] Process(string spk, List<float[]> parts, int sr)
            {
                float[] d = parts[0];
                Normalize(d, 0.9f);
                switch (spk)
                {
                    case Voice.Raghavan:
                        d = Phone(d, sr, 1.4f);
                        break;
                    case Voice.Ghost:
                    {
                        var lo = Resample(d, 0.8f);
                        var sub = Resample(d, 0.62f);
                        var m = new float[sub.Length];
                        for (int i = 0; i < m.Length; i++) m[i] = (i < lo.Length ? lo[i] : 0f) + sub[i] * 0.35f;
                        d = Pad(Phone(m, sr, 3f), 1.2f, sr);
                        Reverb(d, 0.35f, 1.5f, sr);
                        break;
                    }
                    case Voice.You:
                    {
                        var ch = Resample(d, 0.985f);
                        var m = new float[ch.Length + 200];
                        for (int i = 0; i < m.Length; i++) m[i] = (i < d.Length ? d[i] : 0f) + (i >= 200 && i - 200 < ch.Length ? ch[i - 200] * 0.5f : 0f);
                        d = Pad(Phone(m, sr, 2f), 0.6f, sr);
                        Reverb(d, 0.15f, 1f, sr);
                        break;
                    }
                    case Voice.Passengers:
                    {
                        float[] rates = { 0.86f, 0.9f, 0.78f };
                        int[] delay = { 0, (int)(0.14f * sr), (int)(0.3f * sr) };
                        var layers = new List<float[]>();
                        int len = 0;
                        for (int i = 0; i < parts.Count; i++)
                        {
                            Normalize(parts[i], 0.9f);
                            var l = Resample(parts[i], rates[i % 3]);
                            layers.Add(l); len = Mathf.Max(len, l.Length + delay[i % 3]);
                        }
                        d = new float[len];
                        for (int i = 0; i < layers.Count; i++) for (int s = 0; s < layers[i].Length; s++) d[s + delay[i % 3]] += layers[i][s] * (i == 2 ? 0.7f : 0.55f);
                        d = Pad(Filter(d, 1, 3400f, sr), 2.2f, sr);
                        Reverb(d, 0.55f, 1.8f, sr);
                        break;
                    }
                    case Voice.Whisper:
                    {
                        d = Resample(d, 0.9f);
                        var env = 0f; var rnd = new System.Random(3);
                        var hp = new BQ(2, 1500f, 0.7f, sr);
                        for (int i = 0; i < d.Length; i++)
                        {
                            env = Mathf.Max(Mathf.Abs(d[i]), env * 0.999f);
                            d[i] = d[i] * 0.35f + hp.P((float)(rnd.NextDouble() * 2.0 - 1.0)) * env * 1.4f;
                        }
                        d = Pad(Filter(d, 2, 350f, sr), 1.5f, sr);
                        Reverb(d, 0.45f, 1.2f, sr);
                        break;
                    }
                    case Voice.Narrator:
                        d = Pad(Filter(d, 1, 6500f, sr), 0.8f, sr);
                        Reverb(d, 0.14f, 0.9f, sr);
                        break;
                    case Voice.Radio:
                    {
                        d = Filter(Filter(d, 2, 450f, sr), 1, 2900f, sr);
                        var rnd = new System.Random(11);
                        for (int i = 0; i < d.Length; i++)
                        {
                            float crackle = rnd.NextDouble() < 0.0015 ? (float)(rnd.NextDouble() - 0.5) * 0.8f : 0f;
                            d[i] = Tanh(d[i] * 2.6f) / Tanh(2.6f) * (0.85f + 0.15f * Mathf.Sin(i * 0.0009f)) + (float)(rnd.NextDouble() - 0.5) * 0.03f + crackle;
                        }
                        d = Pad(d, 0.4f, sr);
                        Reverb(d, 0.08f, 0.7f, sr);
                        break;
                    }
                    case Voice.Letter:
                        d = Pad(d, 0.5f, sr);
                        Reverb(d, 0.08f, 0.8f, sr);
                        break;
                    default:
                        d = Pad(Filter(Resample(d, 0.97f), 1, 4800f, sr), 0.8f, sr);
                        Reverb(d, 0.22f, 1f, sr);
                        break;
                }
                Normalize(d, 0.92f);
                return d;
            }
        }
    }
}
