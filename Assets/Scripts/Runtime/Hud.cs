using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalBox
{
    public enum HudState { Title, Intro, Playing, Reading, Ending }

    /// Low-res pixelated presentation, grain, vignette, plus all text UI (IMGUI, so no UI assets needed).
    public class Hud : MonoBehaviour
    {
        public static Hud I;
        /// Automated play-testing only: treat the cursor as locked so the shift runs without a focused Game view.
        public static bool TestMode;
        public const int EndingCount = 6;
        const int PixelHeight = 360;

        public HudState State { get; private set; } = HudState.Title;
        public bool ReadyForInput => Time.frameCount > stateFrame + 1;

        int stateFrame;
        Camera worldCam;
        RenderTexture rt;
        Texture2D white, vignette, grain;
        string subWho, subText;
        float subUntil;
        string readTitle, readBody;
        float fade = 1f, fadeTarget = 0.45f;
        string endTitle, endBody;
        int endIndex;
        float endTime;
        GUIStyle sHead, sToast;
        GUIStyle sTitle, sBig, sBody, sSmall, sWho, sSub, sCenter;
        float styleScale = -1f;
        // ---------- Intro (driven by Director's prologue) ----------
        struct Card { public int style; public string speaker, head, body; public Card(int s, string sp, string h, string b) { style = s; speaker = sp; head = h; body = b; } }
        static readonly Card[] Cards =
        {
            new Card(1, "", Director.LetterHead, Director.LetterBody),
            new Card(2, Voice.Narrator, "", Director.DateCard),
        };
        int card;
        bool cardActive;
        float typed, typeTick, cardTime, typeSpeed = 55f;
        string capText;
        float capUntil;
        public Color tint;
        public bool letterbox;

        public void Caption(string text, float dur) { capText = text; capUntil = Time.unscaledTime + dur; }
        public void FadeTo(float a) { fadeTarget = a; }

        public void StartIntro()
        {
            fadeTarget = 1f;
            cardActive = false;
            SetState(HudState.Intro);
            Director.I.StartPrologue();
        }

        public System.Collections.IEnumerator ShowCard(int i)
        {
            card = i; typed = 0f; cardTime = 0f; cardActive = true;
            fadeTarget = 1f;
            var c = Cards[i];
            typeSpeed = c.style == 2 ? 30f : 110f;
            var clip = string.IsNullOrEmpty(c.speaker) ? null : Voice.Get(c.speaker, c.body);
            if (clip)
            {
                Director.I.ReadAloud(c.speaker, c.body);
                typeSpeed = Mathf.Max(8f, CardLen(c) / Mathf.Max(1f, clip.length - 0.6f));
            }
            while (cardActive) yield return null;
            Director.I.StopReading();
        }

        static int CardLen(Card c) => c.head.Length + c.body.Length;

        public void EndIntro()
        {
            cardActive = false;
            SetState(HudState.Playing);
            Lock();
            fadeTarget = 0f;
            fade = 1f;
            Director.I.Begin();
        }

        void UpdateIntro()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (ReadyForInput && Inp.Escape()) { Director.I.SkipPrologue(); return; }
            if (!cardActive) return;
            var c = Cards[card];
            int len = CardLen(c);
            int before = Mathf.FloorToInt(typed);
            typed = Mathf.Min(len, typed + Time.unscaledDeltaTime * typeSpeed);
            cardTime += Time.unscaledDeltaTime;
            typeTick -= Time.unscaledDeltaTime;
            if (Mathf.FloorToInt(typed) > before && typeTick <= 0f && typed < len)
            {
                typeTick = c.style == 2 ? 0.07f : 0.05f;
                char ch = (c.head + c.body)[Mathf.Min(len - 1, Mathf.FloorToInt(typed))];
                if (ch != ' ' && ch != '\n' && World.Sfx) World.Sfx.PlayOneShot(Synth.Type(), 0.3f);
            }
            if (!ReadyForInput) return;
            if (Inp.Click() || Inp.Confirm())
            {
                if (typed < len) typed = len;
                else cardActive = false;
            }
            else if (c.style == 2 && typed >= len && cardTime > len / typeSpeed + 1.2f) cardActive = false;
        }

        void DrawIntro(float W, float H, float u)
        {
            if (cardActive)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
                GUI.color = Color.white;
                var c = Cards[card];
                int shown = Mathf.FloorToInt(typed);
                string head = c.head.Substring(0, Mathf.Min(shown, c.head.Length));
                string body = shown > c.head.Length ? c.body.Substring(0, shown - c.head.Length) : "";
                bool done = shown >= CardLen(c);
                if (c.style == 2) Label(new Rect(0f, H * 0.34f, W, H * 0.34f), body, sBig, Paper);
                else
                {
                    var r = new Rect(W * 0.5f - 430f * u, H * 0.5f - 300f * u, 860f * u, 600f * u);
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.DrawTexture(new Rect(r.x + 10f * u, r.y + 12f * u, r.width, r.height), white);
                    GUI.color = new Color(0.84f, 0.81f, 0.72f);
                    GUI.DrawTexture(r, white);
                    GUI.color = new Color(0.35f, 0.27f, 0.15f, 0.15f);
                    GUI.DrawTexture(new Rect(r.x, r.y + r.height * 0.48f, r.width, 2f * u), white);
                    GUI.color = Color.white;
                    var ink = new Color(0.08f, 0.07f, 0.06f);
                    Ink(new Rect(r.x + 40f * u, r.y + 30f * u, r.width - 80f * u, 110f * u), head, sBody, ink);
                    Ink(new Rect(r.x + 40f * u, r.y + 140f * u, r.width - 80f * u, r.height - 180f * u), body, sBody, ink);
                }
                if (done && c.style != 2)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                    Label(new Rect(0f, H - 70f * u, W, 30f * u), "Click to continue", sCenter, new Color(Amber.r, Amber.g, Amber.b, pulse));
                }
            }
            else
            {
                if (Time.unscaledTime < capUntil && !string.IsNullOrEmpty(capText))
                    Label(new Rect(0f, H * 0.12f + 30f * u, W, 40f * u), capText, sWho, new Color(Paper.r, Paper.g, Paper.b, Mathf.Clamp01(capUntil - Time.unscaledTime)));
                if (Time.unscaledTime < subUntil && !string.IsNullOrEmpty(subText))
                {
                    float y = H - 150f * u;
                    if (!string.IsNullOrEmpty(subWho)) Label(new Rect(W * 0.15f, y - 28f * u, W * 0.7f, 26f * u), subWho, sWho, Amber);
                    Label(new Rect(W * 0.15f, y, W * 0.7f, 120f * u), subText, sSub, Color.white);
                }
            }
            Label(new Rect(0f, H - 38f * u, W - 24f * u, 30f * u), "Esc — skip", sSmall, new Color(1f, 1f, 1f, 0.3f));
        }

        static void Ink(Rect r, string text, GUIStyle s, Color c)
        {
            GUI.color = c;
            GUI.Label(r, text, s);
            GUI.color = Color.white;
        }

        // ---------- Menus ----------
        bool paused;
        int page;          // 0 main, 1 controls, 2 options
        float readScroll;
        GUIStyle sButton;
        Texture2D clear;

        public bool Paused => paused;

        void Pause()
        {
            paused = true; page = 0;
            Unlock();
        }

        void Resume()
        {
            paused = false; page = 0;
            Settings.Save();
            Lock();
        }

        void NewGame()
        {
            Checkpoint.Clear();
            Checkpoint.DeleteSave();
            if (State == HudState.Title) { StartIntro(); return; }
            Checkpoint.startIntro = true;
            Reload();
        }

        void ContinueGame()
        {
            Checkpoint.LoadFromPrefs();
            Checkpoint.restart = true;
            if (State != HudState.Title) Reload();
        }

        void Reload()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            var sc = SceneManager.GetActiveScene();
            if (sc.buildIndex >= 0) SceneManager.LoadScene(sc.buildIndex); else SceneManager.LoadScene(sc.name);
        }

        bool Btn(float y, float u, string label, float W)
        {
            var r = new Rect(W * 0.5f - 200f * u, y, 400f * u, 36f * u);
            bool hot = r.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint && hot)
            {
                GUI.color = new Color(1f, 0.72f, 0.3f, 0.12f);
                GUI.DrawTexture(r, white);
                GUI.color = Color.white;
            }
            Label(r, hot ? "›  " + label + "  ‹" : label, sButton, hot ? Amber : Paper);
            return Event.current.type == EventType.MouseDown && Event.current.button == 0 && hot && Use();
        }

        static bool Web => Application.platform == RuntimePlatform.WebGLPlayer;

        static bool Use() { Event.current.Use(); return true; }

        void Bar(Rect r, float t)
        {
            if (Event.current.type != EventType.Repaint) return;
            var tr = new Rect(r.x, r.center.y - 3f, r.width, 6f);
            GUI.color = new Color(1f, 1f, 1f, 0.15f); GUI.DrawTexture(tr, white);
            GUI.color = new Color(Amber.r, Amber.g, Amber.b, 0.85f); GUI.DrawTexture(new Rect(tr.x, tr.y, tr.width * Mathf.Clamp01(t), tr.height), white);
            GUI.color = Color.white;
        }

        void DrawMenu(float W, float H, float u, bool title)
        {
            if (sButton == null)
            {
                sButton = new GUIStyle(sBig) { fontSize = Mathf.RoundToInt(22f * Screen.height / 720f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            float y = title ? H * 0.43f : H * 0.3f, step = 42f * u;
            if (page == 0)
            {
                if (title)
                {
                    if (Checkpoint.HasSave && Btn(y, u, "Continue   (" + Checkpoint.SaveName + ")", W)) ContinueGame();
                    if (Checkpoint.HasSave) y += step;
                    if (Btn(y, u, "New Game", W)) NewGame(); y += step;
                    if (Btn(y, u, "Controls", W)) page = 1; y += step;
                    if (Btn(y, u, "Options", W)) page = 2; y += step;
                    if (!Web && Btn(y, u, "Quit", W)) Application.Quit();
                }
                else
                {
                    if (Btn(y, u, "Continue", W)) Resume(); y += step;
                    if (Btn(y, u, "Save", W)) { Checkpoint.SaveToPrefs(); Toast("Saved — " + Checkpoint.Names[Mathf.Clamp(Checkpoint.stage, 0, Checkpoint.Names.Length - 1)], 3f); } y += step;
                    if (Btn(y, u, "Load last checkpoint", W)) ContinueGame(); y += step;
                    if (Btn(y, u, "New Game", W)) NewGame(); y += step;
                    if (Btn(y, u, "Controls", W)) page = 1; y += step;
                    if (Btn(y, u, "Options", W)) page = 2; y += step;
                    if (Btn(y, u, "Main Menu", W)) { Checkpoint.restart = false; Reload(); } y += step;
                    if (!Web && Btn(y, u, "Quit Game", W)) Application.Quit();
                }
                if (!title && Time.unscaledTime < toastUntil && !string.IsNullOrEmpty(toastText))
                    Label(new Rect(0f, y + step * 1.2f, W, 30f * u), toastText, sCenter, Amber);
            }
            else if (page == 1)
            {
                Label(new Rect(0f, y - 6f * u, W, 30f * u), "CONTROLS", sButton, Amber);
                Label(new Rect(W * 0.5f - 260f * u, y + 34f * u, 520f * u, 260f * u),
                    "WASD / arrows        walk\nMouse                     look\nLeft Shift              run\nE / left click         interact, answer the phone, levers\nF                             torch\nMouse wheel, W S  scroll the register\nEsc                         pause menu", sBody, Paper);
                if (Btn(y + 290f * u, u, "Back", W)) page = 0;
            }
            else
            {
                Label(new Rect(0f, y - 6f * u, W, 30f * u), "OPTIONS", sButton, Amber);
                float x = W * 0.5f - 200f * u, sw = 400f * u;
                Label(new Rect(x, y + 34f * u, sw, 26f * u), "Volume   " + Mathf.RoundToInt(Settings.Volume * 100f) + "%", sBody, Paper);
                Bar(new Rect(x, y + 62f * u, sw, 20f * u), Settings.Volume / 1f);
                Settings.Volume = GUI.HorizontalSlider(new Rect(x, y + 62f * u, sw, 20f * u), Settings.Volume, 0f, 1f);
                Label(new Rect(x, y + 92f * u, sw, 26f * u), "Mouse sensitivity   " + Settings.Sens.ToString("0.0") + "x", sBody, Paper);
                Bar(new Rect(x, y + 120f * u, sw, 20f * u), (Settings.Sens - 0.2f) / 2.8f);
                Settings.Sens = GUI.HorizontalSlider(new Rect(x, y + 120f * u, sw, 20f * u), Settings.Sens, 0.2f, 3f);
                Settings.Apply();
                if (Btn(y + 152f * u, u, "Invert mouse:  " + (Settings.InvertY ? "ON" : "OFF"), W)) Settings.InvertY = !Settings.InvertY;
                if (Btn(y + 194f * u, u, "Fullscreen:  " + (Screen.fullScreen ? "ON" : "OFF"), W)) Screen.fullScreen = !Screen.fullScreen;
                if (Btn(y + 248f * u, u, "Back", W)) { Settings.Save(); page = 0; }
            }
        }

        string toastText;
        float toastUntil;
        bool isDeath;
        public void Toast(string t, float d) { toastText = t; toastUntil = Time.unscaledTime + d; }

        public void ShowDeath(string title, string body)
        {
            ShowEnding(0, title, body);
            isDeath = true;
        }

        Color flashC;
        float flashUntil, flashDur = 1f;

        /// A full-screen colour hit that fades out (blood-red for impacts, white for jump scares).
        public void Flash(Color c, float dur)
        {
            flashC = c;
            flashDur = Mathf.Max(0.01f, dur);
            flashUntil = Time.unscaledTime + dur;
        }

        static readonly Color Amber = new Color(1f, 0.72f, 0.3f);
        static readonly Color Paper = new Color(0.95f, 0.9f, 0.8f);
        static readonly Color Dim = new Color(0.75f, 0.72f, 0.65f, 0.9f);

        void Awake() { I = this; TestMode = false; Settings.Load(); }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && State == HudState.Playing && !TestMode && !paused) Pause();
        }

        public void Init(Camera cam)
        {
            worldCam = cam;
            white = Texture2D.whiteTexture;
            vignette = MakeVignette();
            grain = MakeGrain();
            Unlock();
        }

        public void TestStart() { TestMode = true; SetState(HudState.Playing); fadeTarget = 0f; Director.I.Begin(); }
        public void TestIntro() { TestMode = true; StartIntro(); }
        public bool CardActive => cardActive;

        void SetState(HudState s) { State = s; stateFrame = Time.frameCount; }

        public void Say(string who, string text, float dur)
        {
            subWho = who;
            subText = text;
            subUntil = Time.unscaledTime + dur;
        }

        public void Read(string title, string body)
        {
            readTitle = title;
            readBody = body;
            readScroll = 0f;
            SetState(HudState.Reading);
        }

        public void ShowEnding(int idx, string title, string body)
        {
            endIndex = idx;
            isDeath = false;
            endTitle = title;
            endBody = body;
            endTime = Time.unscaledTime;
            fadeTarget = 1f;
            subUntil = 0f;
            SetState(HudState.Ending);
            Unlock();
            if (idx > 0)
            {
                PlayerPrefs.SetInt("sb_endings", PlayerPrefs.GetInt("sb_endings", 0) | (1 << idx));
                PlayerPrefs.Save();
            }
        }

        public static int Found()
        {
            int m = PlayerPrefs.GetInt("sb_endings", 0), n = 0;
            for (int i = 1; i <= EndingCount; i++) if ((m & (1 << i)) != 0) n++;
            return n;
        }

        static void Lock() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        static void Unlock() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

        void Update()
        {
            int w = Mathf.Max(64, Mathf.RoundToInt(PixelHeight * (float)Screen.width / Mathf.Max(1, Screen.height)));
            if (worldCam && (rt == null || rt.width != w || rt.height != PixelHeight))
            {
                if (rt) { worldCam.targetTexture = null; rt.Release(); Destroy(rt); }
                rt = new RenderTexture(w, PixelHeight, 24) { filterMode = FilterMode.Point };
                rt.Create();
                worldCam.targetTexture = rt;
            }

            fade = Mathf.MoveTowards(fade, fadeTarget, Time.unscaledDeltaTime * (State == HudState.Ending ? 0.5f : 0.8f));

            switch (State)
            {
                case HudState.Title:
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    if (Checkpoint.restart && Director.I)
                    {
                        Checkpoint.restart = false;
                        SetState(HudState.Playing);
                        Lock();
                        fadeTarget = 0f; fade = 1f;
                        Director.I.BeginAt(Checkpoint.stage);
                        break;
                    }
                    if (Checkpoint.startIntro) { Checkpoint.startIntro = false; StartIntro(); break; }
                    if (page == 0 && Inp.Confirm()) { if (Checkpoint.HasSave) ContinueGame(); else NewGame(); }
                    if (page != 0 && Inp.Escape()) page = 0;
                    break;
                case HudState.Intro:
                    UpdateIntro();
                    break;
                case HudState.Playing:
                    if (ReadyForInput && Inp.Escape())
                    {
                        if (!paused) Pause();
                        else if (page != 0) page = 0;
                        else Resume();
                    }
                    else if (!paused && !TestMode && Cursor.lockState != CursorLockMode.Locked && Inp.Click()) Lock();
                    Time.timeScale = paused ? 0f : 1f;
                    AudioListener.pause = paused;
                    break;
                case HudState.Reading:
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    readScroll += Inp.Scroll();
                    if (ReadyForInput && (Inp.Interact() || Inp.Escape()))
                    {
                        if (Director.I) Director.I.StopReading();
                        SetState(HudState.Playing);
                        if (Cursor.lockState != CursorLockMode.Locked) Lock();
                    }
                    break;
                case HudState.Ending:
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                    if (Time.unscaledTime - endTime > 3.5f && (Inp.Click() || Inp.Confirm()))
                    {
                        if (isDeath) Checkpoint.restart = true;
                        var sc = SceneManager.GetActiveScene();
                        if (sc.buildIndex >= 0) SceneManager.LoadScene(sc.buildIndex);
                        else SceneManager.LoadScene(sc.name);
                    }
                    break;
            }
        }

        void OnDestroy()
        {
            if (worldCam) worldCam.targetTexture = null;
            if (rt) rt.Release();
        }

        void OnGUI()
        {
            Styles();
            float W = Screen.width, H = Screen.height, u = H / 720f;
            var full = new Rect(0f, 0f, W, H);

            if (Event.current.type == EventType.Repaint)
            {
                if (rt) GUI.DrawTexture(full, rt, ScaleMode.StretchToFill, false);
                GUI.color = new Color(1f, 1f, 1f, 0.07f);
                GUI.DrawTextureWithTexCoords(full, grain, new Rect(Random.value, Random.value, W / 384f, H / 384f));
                GUI.color = Color.white;
                GUI.DrawTexture(full, vignette, ScaleMode.StretchToFill, true);
                if (tint.a > 0f) { GUI.color = tint; GUI.DrawTexture(full, white); GUI.color = Color.white; }
                if (letterbox) { GUI.color = Color.black; GUI.DrawTexture(new Rect(0f, 0f, W, H * 0.11f), white); GUI.DrawTexture(new Rect(0f, H * 0.89f, W, H * 0.11f), white); GUI.color = Color.white; }
                if (Time.unscaledTime < flashUntil)
                {
                    float fa = (flashUntil - Time.unscaledTime) / flashDur;
                    GUI.color = new Color(flashC.r, flashC.g, flashC.b, flashC.a * fa);
                    GUI.DrawTexture(full, white);
                    GUI.color = Color.white;
                }
                if (fade > 0.001f)
                {
                    GUI.color = new Color(0f, 0f, 0f, fade);
                    GUI.DrawTexture(full, white);
                    GUI.color = Color.white;
                }
            }

            switch (State)
            {
                case HudState.Title: DrawTitle(W, H, u); break;
                case HudState.Intro: DrawIntro(W, H, u); break;
                case HudState.Playing: DrawPlay(W, H, u, true); break;
                case HudState.Reading: DrawPlay(W, H, u, false); DrawReading(W, H, u); break;
                case HudState.Ending: DrawEnding(W, H, u); break;
            }
        }

        void DrawTitle(float W, float H, float u)
        {
            Label(new Rect(0f, H * 0.2f, W, 90f * u), "SIGNAL BOX", sTitle, Paper);
            Label(new Rect(0f, H * 0.2f + 88f * u, W, 30f * u), "Kalvadi Halt  ·  night shift  ·  14 November", sCenter, Dim);
            DrawMenu(W, H, u, true);
            Label(new Rect(0f, H * 0.84f, W, 30f * u), "Endings found: " + Found() + " / " + EndingCount, sCenter, Dim);
            Label(new Rect(0f, H - 40f * u, W - 20f * u, 30f * u), "GitHub Game Off 2026  ·  theme: SHIFT", sSmall, new Color(1f, 1f, 1f, 0.35f));
            Label(new Rect(0f, H - 64f * u, W * 0.7f, 30f * u), "Models: Quaternius (CC0), puck_psx (CC BY)  ·  Textures: Poly Haven (CC0)", sSmall, new Color(1f, 1f, 1f, 0.3f));
        }

        void DrawPlay(float W, float H, float u, bool interactive)
        {
            var p = Player.I;
            bool locked = Cursor.lockState == CursorLockMode.Locked || TestMode;
            float cs = (p && p.focus) ? 6f * u : 3f * u;
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(W * 0.5f - cs * 0.5f, H * 0.5f - cs * 0.5f, cs, cs), white);
            GUI.color = Color.white;

            if (interactive && p && p.focus && p.focus.prompt != null)
                Label(new Rect(W * 0.2f, H * 0.5f + 22f * u, W * 0.6f, 60f * u), p.focus.prompt(), sCenter, new Color(1f, 0.95f, 0.8f));

            if (Time.unscaledTime < toastUntil && !string.IsNullOrEmpty(toastText))
                Label(new Rect(W * 0.5f, 20f * u, W * 0.5f - 24f * u, 30f * u), toastText, sToast, new Color(1f, 0.85f, 0.55f, Mathf.Clamp01(toastUntil - Time.unscaledTime)));
            var d = Director.I;
            if (d && !string.IsNullOrEmpty(d.objective))
                Label(new Rect(24f * u, 20f * u, W * 0.45f, 90f * u), d.objective, sSmall, new Color(0.88f, 0.86f, 0.8f, 0.8f));

            if (Time.unscaledTime < subUntil && !string.IsNullOrEmpty(subText))
            {
                float y = H - 150f * u;
                if (!string.IsNullOrEmpty(subWho)) Label(new Rect(W * 0.15f, y - 28f * u, W * 0.7f, 26f * u), subWho, sWho, Amber);
                Label(new Rect(W * 0.15f, y, W * 0.7f, 120f * u), subText, sSub, Color.white);
            }

            if (interactive && paused)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.72f);
                GUI.DrawTexture(new Rect(0f, 0f, W, H), white);
                GUI.color = Color.white;
                Label(new Rect(0f, H * 0.16f, W, 50f * u), "PAUSED", sBig, Paper);
                Label(new Rect(0f, H * 0.16f + 44f * u, W, 30f * u), (Director.I ? Director.I.ClockString : "") + "   ·   last checkpoint: " + Checkpoint.Names[Mathf.Clamp(Checkpoint.stage, 0, Checkpoint.Names.Length - 1)], sCenter, Dim);
                DrawMenu(W, H, u, false);
            }
        }

        void DrawReading(float W, float H, float u)
        {
            var r = new Rect(W * 0.5f - 400f * u, H * 0.5f - 280f * u, 800f * u, 560f * u);
            GUI.color = new Color(0.03f, 0.03f, 0.03f, 0.92f);
            GUI.DrawTexture(r, white);
            GUI.color = Color.white;
            Label(new Rect(r.x + 36f * u, r.y + 26f * u, r.width - 72f * u, 36f * u), readTitle, sBody, Amber);
            var view = new Rect(r.x + 36f * u, r.y + 72f * u, r.width - 88f * u, r.height - 124f * u);
            float ch = sBody.CalcHeight(new GUIContent(readBody), view.width) + 12f * u;
            float max = Mathf.Max(0f, ch - view.height);
            readScroll = Mathf.Clamp(readScroll, 0f, max);
            GUI.BeginGroup(view);
            Label(new Rect(0f, -readScroll, view.width, ch), readBody, sBody, Paper);
            GUI.EndGroup();
            if (max > 0f)
            {
                var track = new Rect(view.xMax + 14f * u, view.y, 5f * u, view.height);
                GUI.color = new Color(1f, 1f, 1f, 0.12f); GUI.DrawTexture(track, white);
                float th = Mathf.Max(30f * u, track.height * view.height / ch);
                GUI.color = new Color(Amber.r, Amber.g, Amber.b, 0.8f);
                GUI.DrawTexture(new Rect(track.x, track.y + (track.height - th) * (readScroll / max), track.width, th), white);
                GUI.color = Color.white;
            }
            Label(new Rect(r.x, r.yMax - 40f * u, r.width, 30f * u), max > 0f ? "[E] close   ·   mouse wheel / W S to scroll" : "[E] close", sCenter, Dim);
        }

        void DrawEnding(float W, float H, float u)
        {
            if (fade < 0.9f) return;
            float a = Mathf.Clamp01((Time.unscaledTime - endTime - 1.5f) / 1.5f);
            if (endIndex > 0) Label(new Rect(0f, H * 0.2f, W, 30f * u), "ENDING " + endIndex + " / " + EndingCount, sCenter, new Color(Dim.r, Dim.g, Dim.b, a));
            Label(new Rect(0f, H * 0.2f + 34f * u, W, 50f * u), endTitle, sBig, new Color(Amber.r, Amber.g, Amber.b, a));
            Label(new Rect(W * 0.2f, H * 0.36f, W * 0.6f, H * 0.36f), endBody, sCenter, new Color(Paper.r, Paper.g, Paper.b, a));
            if (Time.unscaledTime - endTime > 3.5f)
            {
                if (isDeath)
                    Label(new Rect(0f, H * 0.84f, W, 30f * u), "Click to try again from the last checkpoint  (" + Checkpoint.Names[Mathf.Clamp(Checkpoint.stage, 0, Checkpoint.Names.Length - 1)] + ")", sCenter, Amber);
                else
                {
                    Label(new Rect(0f, H * 0.78f, W, 30f * u), "Endings found: " + Found() + " / " + EndingCount, sCenter, Dim);
                    Label(new Rect(0f, H * 0.84f, W, 30f * u), "Click to begin another shift", sCenter, Amber);
                }
            }
        }

        void Styles()
        {
            float sc = Screen.height / 720f;
            if (sTitle != null && Mathf.Abs(sc - styleScale) < 0.001f) return;
            styleScale = sc;
            GUIStyle Make(int size, FontStyle fs, TextAnchor a)
            {
                var s = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.Max(8, Mathf.RoundToInt(size * sc)),
                    fontStyle = fs,
                    alignment = a,
                    wordWrap = true,
                    richText = false
                };
                s.normal.textColor = Color.white;
                return s;
            }
            sTitle = Make(68, FontStyle.Bold, TextAnchor.MiddleCenter);
            sBig = Make(30, FontStyle.Bold, TextAnchor.MiddleCenter);
            sHead = Make(24, FontStyle.Bold, TextAnchor.UpperLeft);
            sToast = Make(16, FontStyle.Bold, TextAnchor.UpperRight);
            sBody = Make(19, FontStyle.Normal, TextAnchor.UpperLeft);
            sSmall = Make(16, FontStyle.Normal, TextAnchor.UpperLeft);
            sWho = Make(16, FontStyle.Bold, TextAnchor.LowerCenter);
            sSub = Make(21, FontStyle.Normal, TextAnchor.UpperCenter);
            sCenter = Make(19, FontStyle.Normal, TextAnchor.UpperCenter);
        }

        static void Label(Rect r, string text, GUIStyle s, Color c)
        {
            var o = r;
            o.x += 2f;
            o.y += 2f;
            GUI.color = new Color(0f, 0f, 0f, c.a * 0.85f);
            GUI.Label(o, text, s);
            GUI.color = c;
            GUI.Label(r, text, s);
            GUI.color = Color.white;
        }

        static Texture2D MakeVignette()
        {
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x / (n - 1f) - 0.5f) * 2f, dy = (y / (n - 1f) - 0.5f) * 2f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    t.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0.35f, 1.05f, d) * 0.85f));
                }
            t.Apply();
            return t;
        }

        static Texture2D MakeGrain()
        {
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = Random.value;
                    t.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            t.Apply();
            return t;
        }
    }
}
