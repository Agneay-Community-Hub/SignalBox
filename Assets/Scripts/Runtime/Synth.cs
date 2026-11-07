using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// Game audio. Every sound is generated in code so the game ships with no audio assets,
    /// but any clip dropped into Assets/Resources/Audio/<name>.wav (or .ogg/.mp3) replaces the generated one.
    public static class Synth
    {
        const int SR = 44100;
        const float TAU = Mathf.PI * 2f;
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>();
        static readonly System.Random Rng = new System.Random(1981);

        static float N() => (float)(Rng.NextDouble() * 2.0 - 1.0);
        static float Saw(float ph) => 2f * (ph - Mathf.Floor(ph + 0.5f));
        static float Tanh(float x) { float e = Mathf.Exp(2f * Mathf.Clamp(x, -9f, 9f)); return (e - 1f) / (e + 1f); }

        /// RBJ biquad filter (band-pass, low-pass or high-pass).
        class BQ
        {
            float b0, b1, b2, a1, a2, x1, x2, y1, y2;
            readonly int mode;
            public BQ(int mode, float f, float q) { this.mode = mode; Set(f, q); }
            public void Set(float f, float q)
            {
                float w = TAU * Mathf.Clamp(f, 20f, SR * 0.45f) / SR, cs = Mathf.Cos(w), al = Mathf.Sin(w) / (2f * q), a0 = 1f + al;
                if (mode == 0) { b0 = al; b1 = 0f; b2 = -al; }
                else if (mode == 1) { b0 = (1f - cs) * 0.5f; b1 = 1f - cs; b2 = b0; }
                else { b0 = (1f + cs) * 0.5f; b1 = -(1f + cs); b2 = b0; }
                a1 = -2f * cs; a2 = 1f - al;
                b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
            }
            public float P(float x)
            {
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                return y;
            }
        }
        static BQ Band(float f, float q) => new BQ(0, f, q);
        static BQ Low(float f, float q = 0.707f) => new BQ(1, f, q);
        static BQ High(float f, float q = 0.707f) => new BQ(2, f, q);

        /// Schroeder reverb: a small, hard, concrete room — the cabin, the cutting, the night.
        static void Reverb(float[] d, float wet, float room)
        {
            int[] cd = { 1557, 1617, 1491, 1422, 1277, 1356 };
            int[] ad = { 225, 556, 441 };
            var combs = new float[cd.Length][];
            var ci = new int[cd.Length];
            var cl = new float[cd.Length];
            for (int k = 0; k < cd.Length; k++) combs[k] = new float[Mathf.RoundToInt(cd[k] * room)];
            var aps = new float[ad.Length][];
            var ai = new int[ad.Length];
            for (int k = 0; k < ad.Length; k++) aps[k] = new float[ad[k]];
            for (int i = 0; i < d.Length; i++)
            {
                float x = d[i], s = 0f;
                for (int k = 0; k < combs.Length; k++)
                {
                    var b = combs[k];
                    float y = b[ci[k]];
                    cl[k] = y * 0.72f + cl[k] * 0.28f;
                    b[ci[k]] = x * 0.02f + cl[k] * 0.84f;
                    ci[k] = (ci[k] + 1) % b.Length;
                    s += y;
                }
                for (int k = 0; k < aps.Length; k++)
                {
                    var b = aps[k];
                    float bo = b[ai[k]];
                    float o = -s + bo;
                    b[ai[k]] = s + bo * 0.5f;
                    ai[k] = (ai[k] + 1) % b.Length;
                    s = o;
                }
                d[i] = x * (1f - wet * 0.4f) + s * wet * 3f;
            }
        }

        static AudioClip Make(string name, float secs, System.Func<float, float> gen, float wet = 0f, float tail = 0f, float room = 1f)
        {
            if (Cache.TryGetValue(name, out var c) && c) return c;
            var ov = Resources.Load<AudioClip>("Audio/" + name);
            if (ov) { Cache[name] = ov; return ov; }
            int n = Mathf.CeilToInt(secs * SR), m = n + Mathf.CeilToInt(tail * SR);
            var d = new float[m];
            for (int i = 0; i < n; i++) d[i] = gen((float)i / SR);
            if (wet > 0f) Reverb(d, wet, room);
            float peak = 1e-4f;
            for (int i = 0; i < m; i++) peak = Mathf.Max(peak, Mathf.Abs(d[i]));
            float k = 0.92f / peak;
            for (int i = 0; i < m; i++) d[i] *= k;
            c = AudioClip.Create(name, m, 1, SR, false);
            c.SetData(d, 0);
            Cache[name] = c;
            return c;
        }

        // ---------------- Ambience (loops: no reverb tail, so they loop cleanly) ----------------

        public static AudioClip Wind()
        {
            var lp = Low(420f); var bp = Band(900f, 3f);
            return Make("wind", 10f, t =>
            {
                float n = N();
                float gust = 0.55f + 0.45f * Mathf.Sin(TAU * t / 10f) * Mathf.Sin(TAU * t / 5f + 1f);
                bp.Set(700f + 500f * gust, 4f);
                return lp.P(n) * 2.2f * gust + bp.P(n) * 0.5f * gust * gust;
            });
        }

        public static AudioClip Rumble()
        {
            var lp = Low(180f); var lp2 = Low(900f);
            return Make("rumble", 4f, t =>
            {
                float hum = Mathf.Sin(TAU * 38f * t) * 0.35f + Mathf.Sin(TAU * 57f * t) * 0.15f;
                float ph = (t * 2.5f) % 1f;
                float clack = (Mathf.Exp(-ph * 30f) + Mathf.Exp(-Mathf.Abs(ph - 0.18f) * 60f) * 0.7f) * lp2.P(N()) * 1.8f;
                return lp.P(N()) * 3f + hum + clack;
            });
        }

        public static AudioClip Crickets() => Make("crickets", 6f, t =>
        {
            float v = 0f;
            for (int k = 0; k < 3; k++)
            {
                float per = 0.75f + k * 0.17f;
                float lt = (t + k * 0.31f) % per;
                int pulse = (int)(lt / 0.055f);
                float pt = lt - pulse * 0.055f;
                if (pulse < 3 && pt < 0.035f)
                    v += Mathf.Sin(TAU * (4100f + k * 350f) * t) * Mathf.Sin(Mathf.PI * pt / 0.035f) * (0.6f - k * 0.15f);
            }
            return v;
        });

        /// Low, sick drone with slow beating and a far-off metallic whine.
        public static AudioClip Drone()
        {
            var lp = Low(120f);
            return Make("drone", 12f, t =>
            {
                float sub = Mathf.Sin(TAU * 41.2f * t) + Mathf.Sin(TAU * 41.7f * t) + 0.5f * Mathf.Sin(TAU * 61.8f * t);
                float whine = 0.08f * Mathf.Sin(TAU * (1320f + 6f * Mathf.Sin(TAU * 0.25f * t)) * t) * (0.5f + 0.5f * Mathf.Sin(TAU * t / 12f * 3f));
                return sub * 0.6f + lp.P(N()) * 2.5f + whine;
            });
        }

        public static AudioClip Ring()
        {
            // Old electromechanical bell: two clappers on a metal dome.
            return Make("ring", 3f, t =>
            {
                bool on = t < 0.4f || (t > 0.6f && t < 1.0f);
                if (!on) return 0f;
                float strike = (t * 22f) % 1f;
                float e = Mathf.Exp(-strike * 4f);
                return (Mathf.Sin(TAU * 640f * t) + 0.7f * Mathf.Sin(TAU * 1712f * t) + 0.4f * Mathf.Sin(TAU * 2410f * t)) * (0.35f + 0.65f * e);
            });
        }

        /// Muffled, syllabic noise that reads as a voice down a bad phone line.
        public static AudioClip Murmur()
        {
            var f1 = Band(600f, 4f); var f2 = Band(1300f, 5f); var hp = High(300f);
            return Make("murmur", 4f, t =>
            {
                float syl = Mathf.Max(0f, Mathf.Sin(TAU * 4.3f * t) * Mathf.Sin(TAU * 0.7f * t + 0.6f));
                float src = Saw(t * (115f + 12f * Mathf.Sin(TAU * 0.9f * t))) * 0.6f + N() * 0.3f;
                f1.Set(500f + 250f * Mathf.Sin(TAU * 3.1f * t), 4f);
                f2.Set(1200f + 400f * Mathf.Sin(TAU * 2.3f * t + 1f), 5f);
                return hp.P(f1.P(src) + f2.P(src) * 0.7f) * syl + N() * 0.03f;
            });
        }

        public static AudioClip Static()
        {
            var hp = High(1500f); var bp = Band(1000f, 0.8f);
            return Make("static", 3f, t =>
            {
                float crack = Rng.NextDouble() < 0.003 ? N() * 6f : 0f;
                return (hp.P(N()) * 0.4f + bp.P(N()) * 0.6f) * (0.6f + 0.4f * Mathf.Sin(TAU * 0.7f * t)) + crack;
            });
        }

        public static AudioClip Heart()
        {
            var lp = Low(140f);
            return Make("heart", 0.9f, t =>
            {
                float a = Mathf.Exp(-t * 18f);
                float b = t > 0.28f ? Mathf.Exp(-(t - 0.28f) * 18f) * 0.75f : 0f;
                return lp.P(Mathf.Sin(TAU * 48f * t) * (a + b) * 2f + N() * (a + b) * 0.3f);
            });
        }

        /// A rope taking weight, creaking as it turns.
        public static AudioClip Rope()
        {
            var bp = Band(260f, 6f);
            return Make("rope", 4f, t =>
            {
                float sw = Mathf.Max(0f, Mathf.Sin(TAU * t / 2f));
                float grit = (Rng.NextDouble() < 0.04 * sw ? N() * 3f : 0f);
                bp.Set(220f + 120f * sw, 6f);
                return bp.P(grit + N() * 0.05f) * sw;
            });
        }

        // ---------------- One-shots ----------------

        public static AudioClip Horn() => Make("horn", 2.2f, t =>
        {
            float env = Mathf.Clamp01(t / 0.07f) * Mathf.Clamp01((2.2f - t) / 0.4f);
            return Tanh((Saw(311f * t) + 0.8f * Saw(392f * t) + 0.5f * Saw(466f * t)) * 0.8f) * env;
        }, 0.35f, 1.6f, 1.6f);

        public static AudioClip Clank() => Make("clank", 0.6f, t =>
            N() * Mathf.Exp(-t * 45f) * 1.2f +
            (Mathf.Sin(TAU * 620f * t) + 0.7f * Mathf.Sin(TAU * 1130f * t) + 0.45f * Mathf.Sin(TAU * 1870f * t)) * Mathf.Exp(-t * 11f) * 0.45f, 0.25f, 0.6f);

        public static AudioClip Knock()
        {
            var lp = Low(900f);
            return Make("knock", 0.25f, t => Mathf.Sin(TAU * 85f * t) * Mathf.Exp(-t * 28f) + lp.P(N()) * Mathf.Exp(-t * 60f) * 3f, 0.3f, 0.5f);
        }

        public static AudioClip Step()
        {
            var lp = Low(1200f);
            return Make("step", 0.14f, t => lp.P(N()) * Mathf.Exp(-t * 32f), 0.15f, 0.2f);
        }

        public static AudioClip Sting() => Make("sting", 3f, t =>
        {
            float env = Mathf.Clamp01(t / 0.01f) * Mathf.Exp(-t * 1.1f);
            float s = Mathf.Sin(TAU * 220f * t) + Mathf.Sin(TAU * 233.1f * t) + Mathf.Sin(TAU * 311.1f * t) + 0.6f * Mathf.Sin(TAU * 349f * t * (1f + t * 0.03f));
            float scrape = Saw(t * (1800f + 300f * Mathf.Sin(TAU * 11f * t))) * Mathf.Exp(-t * 2.5f) * 0.35f;
            return Tanh((s * 0.6f + scrape + N() * Mathf.Exp(-t * 3f)) * 1.5f) * env;
        }, 0.45f, 2f, 1.4f);

        /// Deep impact: a body hitting steel, a door slammed by something heavy.
        public static AudioClip Thud()
        {
            var lp = Low(500f);
            return Make("thud", 1.4f, t =>
                Mathf.Sin(TAU * 52f * t * (1f - t * 0.35f)) * Mathf.Exp(-t * 4f) * 1.6f + lp.P(N()) * Mathf.Exp(-t * 14f) * 2.5f +
                N() * Mathf.Exp(-t * 40f), 0.4f, 1.4f, 1.3f);
        }

        public static AudioClip Boom()
        {
            var lp = Low(220f);
            return Make("boom", 2.5f, t =>
                Tanh((Mathf.Sin(TAU * (60f - 28f * Mathf.Clamp01(t)) * t) * Mathf.Exp(-t * 1.8f) * 2f + lp.P(N()) * Mathf.Exp(-t * 3f) * 4f) * 1.4f),
                0.5f, 2.5f, 1.8f);
        }

        public static AudioClip Slam()
        {
            var lp = Low(1400f);
            return Make("slam", 0.8f, t =>
                Mathf.Sin(TAU * 70f * t) * Mathf.Exp(-t * 9f) * 1.5f + lp.P(N()) * Mathf.Exp(-t * 25f) * 4f + Mathf.Sin(TAU * 180f * t) * Mathf.Exp(-t * 18f) * 0.6f,
                0.55f, 1.6f, 1.2f);
        }

        /// Slow, wet breathing down a phone line.
        public static AudioClip Breath()
        {
            var f1 = Band(700f, 1.4f); var f2 = Band(1900f, 2.5f); var hp = High(250f);
            return Make("breath", 7.2f, t =>
            {
                float cyc = (t % 2.4f) / 2.4f;
                bool inhale = cyc < 0.4f;
                float env = inhale ? Mathf.Sin(Mathf.PI * cyc / 0.4f) : (cyc < 0.55f ? 0f : Mathf.Sin(Mathf.PI * (cyc - 0.55f) / 0.45f) * 0.75f);
                env *= env;
                float n = N();
                float rasp = inhale ? 0f : Mathf.Max(0f, Saw(t * 62f)) * 0.25f;
                return hp.P(f1.P(n + rasp) * 1.4f + f2.P(n) * (inhale ? 0.9f : 0.5f)) * env;
            }, 0.25f, 1f, 0.8f);
        }

        public static AudioClip Whisper()
        {
            var f1 = Band(1800f, 3f); var f2 = Band(3200f, 4f); var hp = High(900f);
            float[] syl = { 0f, 0.28f, 0.52f, 0.71f, 1.05f, 1.3f, 1.62f, 1.9f };
            return Make("whisper", 2.5f, t =>
            {
                float g = 0f;
                for (int i = 0; i < syl.Length; i++) { float d = t - syl[i]; if (d > 0f && d < 0.26f) g = Mathf.Max(g, Mathf.Sin(Mathf.PI * d / 0.26f)); }
                f1.Set(1500f + 900f * Mathf.Sin(TAU * 3.7f * t), 3f);
                f2.Set(3000f + 900f * Mathf.Sin(TAU * 2.9f * t + 1f), 4f);
                float n = N();
                float s = (t % 0.62f) < 0.08f ? hp.P(n) * 1.5f : 0f;
                return (f1.P(n) + f2.P(n) * 0.6f) * g * 2f + s * g;
            }, 0.45f, 1.5f, 1.1f);
        }

        /// A child humming an aimless tune in an empty room.
        public static AudioClip Hum()
        {
            int[] notes = { 0, 3, 7, 5, 3, 2, 0, -2, 0 };
            float ph = 0f;
            var f1 = Band(500f, 3f); var f2 = Band(1600f, 5f);
            return Make("hum", 9f, t =>
            {
                int i = Mathf.Min(notes.Length - 1, (int)t);
                float lt = t - i;
                float f = 330f * Mathf.Pow(2f, notes[i] / 12f) * (1f + 0.012f * Mathf.Sin(TAU * 5.5f * t));
                ph += f / SR;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(lt));
                float src = Saw(ph) * 0.6f + Mathf.Sin(TAU * ph) * 0.8f;
                return (f1.P(src) + f2.P(src) * 0.4f + Mathf.Sin(TAU * ph) * 0.25f) * env + N() * 0.01f;
            }, 0.55f, 2.5f, 1.6f);
        }

        /// Formant-synthesised scream: a real throat shape ("ee" opening to "aah"), rough and clipped.
        public static AudioClip Scream()
        {
            float ph = 0f;
            var f1 = Band(800f, 5f); var f2 = Band(1200f, 7f); var f3 = Band(2800f, 9f); var nb = Band(3200f, 1.5f);
            return Make("scream", 2.8f, t =>
            {
                float shape = Mathf.Clamp01(t / 0.45f);
                float pitch = 640f + 480f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 2.6f)) + 28f * Mathf.Sin(TAU * 6.3f * t) + 18f * N();
                ph += pitch / SR;
                float src = Saw(ph) + 0.4f * Saw(ph * 1.003f);
                f1.Set(Mathf.Lerp(380f, 880f, shape), 5f);
                f2.Set(Mathf.Lerp(2300f, 1250f, shape), 7f);
                float v = f1.P(src) * 1.4f + f2.P(src) + f3.P(src) * 0.5f + nb.P(N()) * 0.45f;
                float env = Mathf.Clamp01(t / 0.06f) * Mathf.Clamp01((2.8f - t) / 0.9f);
                return Tanh(v * 2.5f) * env;
            }, 0.5f, 2.2f, 1.7f);
        }

        /// The jump-scare hit: a shrieking, distorted wall of noise over a sub-bass punch.
        public static AudioClip Screech()
        {
            float p1 = 0f, p2 = 0f, p3 = 0f;
            var hp = High(1800f); var f1 = Band(1100f, 4f); var f2 = Band(2600f, 6f);
            return Make("screech", 1.8f, t =>
            {
                float sweep = Mathf.Clamp01(t / 1.4f);
                float f = Mathf.Lerp(900f, 2300f, Mathf.Sqrt(sweep)) + 60f * Mathf.Sin(TAU * 13f * t);
                p1 += f / SR; p2 += f * 1.013f / SR; p3 += f * 0.497f / SR;
                float src = Saw(p1) + Saw(p2) + 0.6f * Saw(p3);
                float ring = Mathf.Sin(TAU * 73f * t);
                float shriek = f1.P(src * ring) * 1.5f + f2.P(src) + hp.P(N()) * 0.8f;
                float sub = Mathf.Sin(TAU * (55f - 20f * t) * t) * Mathf.Exp(-t * 3f) * 2.2f;
                float env = Mathf.Clamp01(t / 0.004f) * Mathf.Clamp01((1.8f - t) / 0.6f);
                return Tanh((shriek * 1.8f + sub) * 2.2f) * env;
            }, 0.35f, 1.3f, 1.2f);
        }

        public static AudioClip Type()
        {
            var hp = High(2000f);
            return Make("type", 0.06f, t => hp.P(N()) * Mathf.Exp(-t * 120f) * 2f + Mathf.Sin(TAU * 1300f * t) * Mathf.Exp(-t * 90f) * 0.6f, 0.2f, 0.15f, 0.6f);
        }

        public static AudioClip Creak()
        {
            float ph = 0f;
            var bp = Band(600f, 3f);
            return Make("creak", 1.6f, t =>
            {
                float f = 120f + 70f * Mathf.Sin(TAU * 1.3f * t) + 30f * Mathf.Sin(TAU * 9f * t);
                ph += f / SR;
                float stick = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(TAU * 27f * t)), 6f);
                float env = Mathf.Clamp01(t / 0.1f) * Mathf.Clamp01((1.6f - t) / 0.3f);
                return (bp.P(Saw(ph)) * 1.6f + Saw(ph) * stick * 0.5f) * env;
            }, 0.4f, 1.2f, 1f);
        }
    }
}
