using UnityEngine;

namespace SignalBox
{
    /// Full-screen jump scares: a face painted in code at start-up slams onto the screen with a screech.
    public class JumpScare : MonoBehaviour
    {
        public static JumpScare I;
        public const int FaceCount = 3;
        public bool Active => Time.unscaledTime < until;

        Texture2D[] faces;
        Texture2D white;
        int face;
        float start, until, dur;
        AudioSource src;

        void Awake()
        {
            I = this;
            white = Texture2D.whiteTexture;
            faces = new Texture2D[FaceCount];
            for (int i = 0; i < FaceCount; i++) faces[i] = Paint(i);
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 1f;
            src.priority = 0;
        }

        /// variant: 0 = hollow-eyed scream, 1 = white eyes, 2 = burned/red.
        public void Play(int variant = 0, float seconds = 0.9f)
        {
            face = Mathf.Clamp(variant, 0, FaceCount - 1);
            dur = seconds;
            start = Time.unscaledTime;
            until = start + seconds;
            src.Stop();
            src.PlayOneShot(Synth.Screech(), 1f);
            src.PlayOneShot(Synth.Boom(), 0.8f);
            if (Player.I) Player.I.AddShake(1.6f);
            if (Hud.I) Hud.I.Flash(new Color(0f, 0f, 0f, 1f), seconds + 0.7f);
        }

        void OnGUI()
        {
            GUI.depth = -100;
            if (!Active || Event.current.type != EventType.Repaint) return;
            float W = Screen.width, H = Screen.height;
            float k = Mathf.Clamp01((Time.unscaledTime - start) / dur);
            // Slam in huge, then keep pushing toward the camera.
            float s = H * (k < 0.07f ? Mathf.Lerp(1.8f, 1.02f, k / 0.07f) : Mathf.Lerp(1.02f, 1.22f, (k - 0.07f) / 0.93f));
            float jx = (Random.value - 0.5f) * W * 0.035f, jy = (Random.value - 0.5f) * H * 0.035f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
            // Frame drops and colour stutters sell the "broken tape" feel.
            bool red = Random.value < 0.1f;
            bool neg = Random.value < 0.06f;
            GUI.color = neg ? new Color(1f, 1f, 1f, 1f) : red ? new Color(1f, 0.25f, 0.2f) : Color.white;
            if (neg) GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
            else GUI.DrawTexture(new Rect(W * 0.5f - s * 0.5f + jx, H * 0.5f - s * 0.5f + jy, s, s), faces[face]);
            // Scanline tear.
            if (Random.value < 0.35f)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.8f);
                GUI.DrawTexture(new Rect(0f, Random.value * H, W, H * Random.Range(0.02f, 0.08f)), white);
            }
            GUI.color = Color.white;
        }

        // ---------------- Face painter ----------------

        static float Hash(float x, float y) { float h = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f; return h - Mathf.Floor(h); }

        static float Noise(float x, float y)
        {
            float ix = Mathf.Floor(x), iy = Mathf.Floor(y), fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy), b = Hash(ix + 1f, iy), c = Hash(ix, iy + 1f), d = Hash(ix + 1f, iy + 1f);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float x, float y) { float v = 0f, a = 0.5f; for (int i = 0; i < 5; i++) { v += Noise(x, y) * a; x *= 2.03f; y *= 2.03f; a *= 0.5f; } return v; }

        static float Ell(float u, float v, float cx, float cy, float rx, float ry) { float dx = (u - cx) / rx, dy = (v - cy) / ry; return Mathf.Sqrt(dx * dx + dy * dy); }

        static Texture2D Paint(int variant)
        {
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[S * S];
            float seed = variant * 17.3f;
            Color skin = variant == 2 ? new Color(0.48f, 0.3f, 0.26f) : new Color(0.62f, 0.63f, 0.56f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = x / (S - 1f) * 2f - 1f, v = y / (S - 1f) * 2f - 1f;
                    // Warp the whole face so nothing is quite symmetric or human.
                    float wu = u + (Fbm(u * 2f + seed, v * 2f) - 0.5f) * 0.12f;
                    float wv = v + (Fbm(u * 2f, v * 2f + seed) - 0.5f) * 0.1f;
                    wu *= 1f + 0.18f * Mathf.Max(0f, -wv);           // jaw stretched wide and long

                    Color c = Color.black;
                    float head = Ell(wu, wv, 0f, -0.08f, 0.6f, 0.92f);
                    if (head < 1f)
                    {
                        float shade = Mathf.Clamp01(1.05f - head * head * 0.9f);
                        float blot = Fbm(wu * 5f + seed, wv * 5f);
                        c = skin * (0.35f + 0.75f * shade) * (0.75f + 0.5f * blot);
                        // Bruised hollows under the cheekbones.
                        float cheek = Mathf.Min(Ell(wu, wv, -0.33f, -0.18f, 0.16f, 0.22f), Ell(wu, wv, 0.33f, -0.18f, 0.16f, 0.22f));
                        if (cheek < 1f) c *= Mathf.Lerp(0.45f, 1f, cheek);
                        // Veins / cracks.
                        float crack = Mathf.Abs(Fbm(wu * 9f + seed, wv * 9f) - 0.5f);
                        if (crack < 0.018f) c = Color.Lerp(c, new Color(0.12f, 0.08f, 0.1f), 0.8f);
                        // Edge falloff into darkness.
                        c *= Mathf.SmoothStep(1f, 0.15f, head);
                    }

                    // Eye sockets: huge, deep, slightly uneven.
                    float el = Ell(wu, wv, -0.24f, 0.2f, 0.17f, 0.15f + 0.02f * variant);
                    float er = Ell(wu, wv, 0.25f, 0.23f, 0.16f, 0.14f);
                    float eye = Mathf.Min(el, er);
                    if (eye < 1.6f) c *= Mathf.SmoothStep(0.05f, 1f, (eye - 0.7f) / 0.9f);
                    if (eye < 0.8f)
                    {
                        c = Color.black;
                        float pl = Ell(wu, wv, -0.22f, 0.19f, 0.035f, 0.035f), pr = Ell(wu, wv, 0.23f, 0.22f, 0.03f, 0.03f);
                        if (variant == 1)
                        {
                            // Milky white eyes with pinprick pupils.
                            float ew = eye / 0.8f;
                            c = new Color(0.86f, 0.87f, 0.8f) * (1f - ew * ew * 0.5f);
                            if (Mathf.Min(pl, pr) < 0.6f) c = Color.black;
                        }
                        else if (Mathf.Min(pl, pr) < 1f)
                            c = variant == 2 ? new Color(1f, 0.15f, 0.05f) : new Color(1f, 1f, 0.92f);
                    }
                    // Blood running from the eyes.
                    for (int s = 0; s < 2; s++)
                    {
                        float ex = s == 0 ? -0.24f : 0.25f;
                        float streak = Mathf.Abs(wu - ex - 0.04f * Mathf.Sin(wv * 9f + s) + (Hash(s, seed) - 0.5f) * 0.06f);
                        float len = 0.55f + 0.35f * Hash(s + 3f, seed);
                        if (wv < 0.12f && wv > 0.12f - len && streak < 0.022f * (1f - (0.12f - wv) / len * 0.6f))
                            c = Color.Lerp(c, new Color(0.25f, 0.01f, 0.01f), 0.92f);
                    }

                    // Nose: two slits.
                    if (Ell(wu, wv, -0.045f, -0.06f, 0.025f, 0.05f) < 1f || Ell(wu, wv, 0.05f, -0.06f, 0.025f, 0.05f) < 1f) c = Color.black;

                    // Mouth: a long, screaming hole with broken teeth.
                    float m = Ell(wu, wv, 0.01f, -0.52f, 0.19f, 0.33f);
                    if (m < 1.25f && m >= 1f) c *= 0.4f;
                    if (m < 1f)
                    {
                        c = new Color(0.03f, 0f, 0f);
                        float top = -0.52f + 0.33f * Mathf.Sqrt(Mathf.Max(0f, 1f - (wu * wu) / (0.19f * 0.19f)));
                        float bot = -0.52f - (top + 0.52f);
                        float tooth = Mathf.Abs(Mathf.Repeat(wu * 22f + seed, 1f) - 0.5f);
                        float th = 0.11f * (1f - tooth * 2f) * (0.6f + 0.6f * Hash(Mathf.Floor(wu * 22f + seed), 1f));
                        if (wv > top - th || wv < bot + th * 0.8f) c = new Color(0.66f, 0.6f, 0.45f) * (0.6f + 0.4f * Hash(Mathf.Floor(wu * 22f), 2f));
                    }

                    // Wet black hair hanging over the face.
                    float strand = Fbm(u * 18f + seed, 0.4f);
                    float hairLen = 0.25f + 0.5f * Fbm(u * 3f + seed, 1.7f);
                    if (v > 0.95f - hairLen && strand > 0.53f && Mathf.Abs(u) > 0.12f) c = Color.Lerp(c, new Color(0.01f, 0.01f, 0.012f), 0.94f);
                    if (v > 0.62f && head > 0.8f) c = Color.Lerp(c, Color.black, 0.85f);

                    // Grain, vignette, contrast.
                    float r = Mathf.Sqrt(u * u + v * v);
                    c *= Mathf.SmoothStep(1.25f, 0.55f, r);
                    c += Color.white * (Hash(x + seed, y) - 0.5f) * 0.06f;
                    c.r = Mathf.Pow(Mathf.Clamp01(c.r), 1.25f); c.g = Mathf.Pow(Mathf.Clamp01(c.g), 1.25f); c.b = Mathf.Pow(Mathf.Clamp01(c.b), 1.25f);
                    c.a = 1f;
                    px[(S - 1 - y) * S + x] = c;
                }
            // Texture rows go bottom-up; we painted v up, so flip back.
            var flip = new Color[S * S];
            for (int y = 0; y < S; y++) System.Array.Copy(px, y * S, flip, (S - 1 - y) * S, S);
            tex.SetPixels(flip);
            tex.Apply();
            return tex;
        }
    }
}
