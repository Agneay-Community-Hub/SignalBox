using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SignalBox
{
    /// Runs the night shift: clock, phone calls, trains, scares and the six endings.
    public class Director : MonoBehaviour
    {
        public static Director I;

        const string R = "RAGHAVAN  ·  Kalvadi Junction";
        const string Q = "???";
        const string Y = "A VOICE YOU KNOW";

        public const string NoticeTitle = "CAUTION ORDER No. 114 / 1981";
        public const string NoticeBody =
            "Consequent to the accident of 06110 Kalvadi Mail at the Kalvadi river bridge on the night of 14 November 1981, " +
            "the BRANCH LINE beyond Points No. 1 is CLOSED to all traffic.\n\n" +
            "Points No. 1 are to remain set NORMAL (main line) at all times.\n\n" +
            "SIGNALMAN'S DUTIES\n" +
            "1.  Answer the control phone from the Junction.\n" +
            "2.  Set Points No. 1 with the BLACK lever.\n" +
            "3.  Clear the home signal with the RED lever. Points are locked while the signal is clear.\n" +
            "4.  Restore the signal to danger once the train has passed.\n" +
            "5.  Record every train in the register.\n\n" +
            "— Divisional Engineer";

        public const string LetterHead = "KALVADI DIVISION  ·  RAILWAY ESTABLISHMENT BRANCH\nNo. EST/N/1126          Dated 12 November";
        public const string LetterBody =
            "Sub:  Posting of Night Signalman — Kalvadi Halt\n\n" +
            "You are posted as Night Signalman, Kalvadi Halt, from the night of 14 November, replacing Shri S. Anand, who has not reported for duty since 7 November. His whereabouts are not known.\n\n" +
            "Single-man cabin. Shift 22:30 to 05:00. His handover note will be found at the cabin.\n\n" +
            "Staff are requested not to discuss the 1981 accident.";
        public const string DateCard = "14 November. Present day.\n22:24.\nKalvadi Halt.";

        public const string HandoverTitle = "HANDOVER — to the new night man";
        public const string HandoverBody =
            "Fourteen nights I have done this. Here is what nobody told me.\n\n" +
            "1.  Answer every call. If nobody speaks, do not speak first.\n" +
            "2.  The man on the line is not your business. Let the train take him. He will be there again tomorrow.\n" +
            "3.  If the cabin lights go out, do not look up.\n" +
            "4.  Never stand with your back to the register.\n" +
            "5.  The Kalvadi Mail will ask to go home. Let it.\n" +
            "6.  Whatever it says, whoever's voice it uses — do not clear the 3:10.\n\n" +
            "I have asked for a transfer. The clerk at the Junction laughed. He said nobody has ever been transferred out of Kalvadi.\n\n" +
            "If you are reading this, I did not come back for it.\n\n" +
            "— S. Anand, 7 November";

        static readonly string[][] Endings =
        {
            new[] { "STRUCK", "You were standing on the running line.\n\nThe driver never saw you." },
            new[] { "RELIEVED", "At five o'clock the relief signalman climbs the steps, whistling. The signal is red. The line is empty.\n\nThere is no 3:10 in any timetable, and the handwriting in the register is gone. You go home.\n\nYou never take a night shift again." },
            new[] { "THE JUNCTION", "You cleared the 3:10 onto the main line.\n\nAt dawn, Kalvadi Junction was found with its lamps still burning and its platform empty — Raghavan, the porters, fourteen waiting passengers.\n\nThe 3:10 was never seen again. Neither were they." },
            new[] { "THE SHIFT NEVER ENDS", "You sent the 3:10 down the closed branch, toward the river bridge.\n\nThe phone rings. A calm voice asks for the signalman on duty, 14 November 1981.\n\nThe clock on the wall says 22:30. Your shift is starting." },
            new[] { "GOING HOME", "You step aboard. The seats are warm. Someone moves their bag so you can sit.\n\nOutside, the signal turns green on its own.\n\nNobody at Kalvadi Halt ever saw the new signalman again." },
            new[] { "INQUIRY", "You sent a train full of living passengers onto a line closed since 1981. It found the broken bridge in the dark.\n\nYou are suspended pending inquiry. The Divisional Engineer keeps asking who told you to do it.\n\nYou keep telling him: the points moved by themselves." },
            new[] { "SAME NAME", "At five the relief signalman arrives and signs the register — with your name, in your handwriting.\n\nHe talks to the empty cabin as if you are not there. The night man, he says, died here in 1981.\n\nSomewhere on the main line, a train with no driver is still running." },
        };

        static readonly string[] Whispers =
        {
            "Signalman... let us through.",
            "The children are cold.",
            "We have been waiting here since 1981.",
            "Your name is on the passenger list.",
            "Come and sit with us. There is room.",
            "Clear it. Clear it. Clear it."
        };

        World w;
        Player p;
        Scares sc;
        AudioSource talk;
        bool talkReading, prologue;
        Coroutine prologueCo;
        public float clock = M(22, 30);
        float gate = M(22, 30), rate = 0.5f;
        public string objective = "";
        public bool ghostReturned, ghostLoose;
        bool lastAnswered;
        bool ringing, answered, ending, started, anomalyFired, holding, inCall;
        readonly List<string> passed = new List<string>();
        string scrawl1, scrawl2;
        readonly List<string> extra = new List<string>();
        public void AddRegisterLine(string s) { extra.Add(s); }

        public static float M(int h, int m) => (h < 12 ? h + 24 : h) * 60f + m;
        public bool PhoneRinging => ringing;
        public bool Ending => ending;
        public string ClockString
        {
            get
            {
                int m = Mathf.FloorToInt(clock) % 1440;
                return (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
            }
        }

        const string RestoreObj = "Restore the signal to danger.";
        const string WaitObj = "Wait for the next call. The register and notice are on the desk and wall.";

        void Awake() { I = this; }

        public void Init(World world, Player player)
        {
            w = world;
            p = player;
            sc = world.scares;
            talk = gameObject.AddComponent<AudioSource>();
            talk.playOnAwake = false;
            talk.spatialBlend = 0f;
            talk.volume = 1f;
            talk.priority = 1;
        }

        static string Spk(string who) => who == R ? Voice.Raghavan : who == Q ? Voice.Ghost : who == Y ? Voice.You : string.IsNullOrEmpty(who) ? Voice.Narrator : Voice.ForLabel(who);

        /// Subtitle + voice clip if one was generated.
        public float Speak(string who, string text, float dur)
        {
            var clip = Voice.Get(Spk(who), text);
            if (clip) { talk.PlayOneShot(clip, 1f); dur = Mathf.Max(dur, clip.length + 0.3f); }
            Hud.I.Say(who, text, dur);
            return dur;
        }

        /// Reads a document aloud (the letter, Anand's note). Stops when the document is closed.
        public void ReadAloud(string speaker, string text)
        {
            var clip = Voice.Get(speaker, text);
            if (!clip) return;
            talk.Stop();
            talk.clip = clip;
            talk.Play();
            talkReading = true;
        }

        public void StopReading()
        {
            if (talkReading) talk.Stop();
            talkReading = false;
        }

        public void Begin()
        {
            if (started) return;
            started = true;
            Checkpoint.Clear();
            clock = gate = M(22, 24);
            StartCoroutine(Run(0));
        }

        static readonly float[] CpClock = { M(22, 24), M(23, 0), M(0, 16), M(0, 38), M(2, 0), M(3, 5) };
        static readonly float[] CpDread = { 0.05f, 0.18f, 0.32f, 0.34f, 0.45f, 0.7f };

        /// Restart after a death, from the last checkpoint.
        public void BeginAt(int k)
        {
            if (started) return;
            started = true;
            k = Mathf.Clamp(k, 0, CpClock.Length - 1);
            ghostReturned = Checkpoint.ghostReturned; ghostLoose = Checkpoint.ghostLoose;
            scrawl1 = Checkpoint.scrawl1; scrawl2 = Checkpoint.scrawl2;
            passed.Clear(); passed.AddRange(Checkpoint.passed);
            extra.Clear(); extra.AddRange(Checkpoint.extra);
            clock = gate = CpClock[k];
            w.dread = CpDread[k];
            if (k > 0)
            {
                p.Teleport(new Vector3(0.6f, 0.35f, -8.7f));
                p.Yaw = 0f; p.Pitch = 0f;
                sc.hangerDone = true; sc.bodyExamined = true;
                sc.ropeStub.SetActive(true);
            }
            if (k >= 4) sc.ShowHands();
            Hud.I.Toast("Checkpoint — " + Checkpoint.Names[k] + "   ·   " + ClockString, 4f);
            StartCoroutine(Run(k));
        }

        void Save(int k)
        {
            Checkpoint.stage = k;
            Checkpoint.ghostReturned = ghostReturned; Checkpoint.ghostLoose = ghostLoose;
            Checkpoint.scrawl1 = scrawl1; Checkpoint.scrawl2 = scrawl2;
            Checkpoint.passed.Clear(); Checkpoint.passed.AddRange(passed);
            Checkpoint.extra.Clear(); Checkpoint.extra.AddRange(extra);
            Checkpoint.SaveToPrefs();
            if (k > 0) Hud.I.Toast("Checkpoint saved", 2.5f);
        }

        /// A death: not an ending. The player retries from the last checkpoint.
        public void Die(string title, string body)
        {
            if (ending) return;
            ending = true;
            holding = false;
            if (p) p.frozen = true;
            if (w) { w.phoneRing.Stop(); w.voice.Stop(); w.SetRadio(false); }
            talk.Stop();
            Checkpoint.deaths++;
            Hud.I.ShowDeath(title, body);
        }

        IEnumerator Broadcast(string text)
        {
            var clip = Voice.Get(Voice.Radio, text);
            float d = clip ? clip.length + 0.4f : 2.4f + text.Length * 0.06f;
            if (clip) { w.radio.Stop(); w.radio.clip = clip; w.radio.Play(); }
            Hud.I.Say("RADIO", text, d);
            yield return Wait(d);
        }

        /// 00:16 — the radio switches itself on. An emergency bulletin, then the Walkers.
        IEnumerator Bulletin()
        {
            if (!Scares.PlayerInside()) objective = "Something is happening in the cabin. Go back inside.";
            yield return HoldUntil(() => Scares.PlayerInside(), 90f);
            if (ending) yield break;
            w.SetRadio(true);
            objective = "The radio has switched itself on.";
            Hud.I.Say("", "(The radio on the desk crackles into life. Nobody touched it.)", 3.5f);
            yield return Wait(3f);
            yield return Broadcast("This is Kalvadi District Radio, with an emergency bulletin.");
            yield return Broadcast("Police have received reports of people walking out of the Kalvadi river, near the old railway bridge.");
            yield return Broadcast("They are walking along the railway line, towards Kalvadi Halt.");
            yield return Broadcast("Do not open your door to anyone tonight. Whatever they say. Whoever they sound like.");
            yield return Broadcast("Stay away from your windows. This bulletin will repeat.");
            if (ending) yield break;
            yield return Wait(1.5f);
            Hud.I.Say("", "(Footsteps on the ballast. Many of them. Coming up the line.)", 4f);
            yield return sc.Walkers(60f);
            if (ending || sc.doorFailed) yield break;
            yield return Broadcast("...latest reports say they have turned back toward the river. Stay indoors until morning.");
            w.SetRadio(false);
            objective = WaitObj;
        }

        void Update()
        {
            float r = rate;
#if UNITY_EDITOR
            // Editor-only test aid: hold ] to fast-forward the shift clock (events still fire in order).
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rightBracketKey.isPressed) r *= 12f;
#endif
            if (started && !ending) clock = Mathf.Min(gate, clock + r * Time.deltaTime);
            if (w) w.SetClock(clock);
            if (w && objective == RestoreObj && !w.SignalClear) objective = WaitObj;
        }

        struct Line
        {
            public string who, text;
            public Line(string w, string t) { who = w; text = t; }
        }

        static Line L(string who, string text) => new Line(who, text);

        IEnumerator Wait(float secs)
        {
            float t = 0f;
            while (t < secs) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator Until(float t)
        {
            gate = Mathf.Max(gate, t);
            while (clock < t - 0.01f && !ending) yield return null;
        }

        IEnumerator HoldUntil(System.Func<bool> cond, float timeout)
        {
            float t = 0f;
            while (!cond() && t < timeout && !ending) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator Call(Line[] lines, float giveUp = 45f, bool ominous = false, AudioClip under = null)
        {
            inCall = true;
            ringing = true;
            answered = false;
            w.phoneRing.pitch = ominous ? 0.85f : 1f;
            w.phoneRing.Play();
            string prev = objective;
            objective = "The control phone is ringing. Answer it.";
            float t = 0f;
            while (!answered && t < giveUp && !ending) { t += Time.deltaTime; yield return null; }
            w.phoneRing.Stop();
            ringing = false;
            objective = prev;
            lastAnswered = answered;
            if (!answered)
            {
                Hud.I.Say("", "(The ringing stops.)", 3f);
                inCall = false;
                yield break;
            }
            var normal = w.voice.clip;
            bool bed = under != null;
            if (bed) { w.voice.clip = under; w.voice.volume = 0.45f; w.voice.pitch = 1f; w.voice.Play(); }
            StopReading();
            foreach (var l in lines)
            {
                if (ending) break;
                float d = 2.2f + l.text.Length * 0.05f;
                var clip = Voice.Get(Spk(l.who), l.text);
                if (clip)
                {
                    if (!bed) w.voice.Stop();
                    talk.Stop(); talk.clip = clip; talk.Play();
                    d = clip.length + 0.35f;
                }
                else if (!bed && !w.voice.isPlaying) { w.voice.pitch = ominous ? 0.8f : 1f; w.voice.Play(); }
                Hud.I.Say(l.who, l.text, d);
                yield return Wait(d + 0.2f);
            }
            talk.Stop();
            w.voice.Stop();
            w.voice.clip = normal;
            w.voice.volume = 0.4f;
            inCall = false;
        }

        public void AnswerPhone()
        {
            if (ringing) answered = true;
            else Hud.I.Say("", "The line is dead. Only a faint hum.", 2.5f);
        }

        /// A single ring with nobody on the line.
        public IEnumerator GhostRing()
        {
            if (ringing || inCall || ending) yield break;
            w.phoneRing.pitch = 0.9f;
            w.phoneRing.Play();
            yield return Wait(1.15f);
            w.phoneRing.Stop();
            Hud.I.Say("", "(The phone rings once. Then nothing.)", 3f);
        }

        /// Background haunting between the scripted beats; gets more frequent as dread rises.
        IEnumerator Haunter()
        {
            while (!ending)
            {
                yield return Wait(Random.Range(Mathf.Lerp(38f, 16f, w.dread), Mathf.Lerp(60f, 30f, w.dread)));
                if (ending) yield break;
                if (inCall || ringing || holding || w.otherSide) continue;
                sc.RandomScare();
            }
        }

        // ======================= PROLOGUE: 14 November 1981 =======================

        public void StartPrologue() { prologueCo = StartCoroutine(Prologue()); }

        public void SkipPrologue()
        {
            if (!prologue) return;
            if (prologueCo != null) StopCoroutine(prologueCo);
            talk.Stop();
            EndPrologue();
        }

        IEnumerator Narrate(string text)
        {
            var clip = Voice.Get(Voice.Narrator, text);
            float d = clip ? clip.length + 0.5f : 2.4f + text.Length * 0.06f;
            if (clip) { talk.Stop(); talk.clip = clip; talk.Play(); }
            Hud.I.Say("", text, d);
            yield return Wait(d);
        }

        IEnumerator PhoneSay(string who, string text)
        {
            var clip = Voice.Get(Spk(who), text);
            float d = clip ? clip.length + 0.4f : 2.2f + text.Length * 0.06f;
            if (clip) { talk.Stop(); talk.clip = clip; talk.Play(); }
            else { w.voice.pitch = 0.8f; w.voice.Play(); }
            Hud.I.Say(who, text, d);
            yield return Wait(d);
            w.voice.Stop();
        }

        IEnumerator LookAt(Vector3 target, float secs)
        {
            var cam = p.cam.transform;
            var to = target - cam.position;
            float ty = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float tp = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            float y0 = p.Yaw, p0 = p.Pitch;
            float dy = Mathf.DeltaAngle(y0, ty);
            float t = 0f;
            while (t < secs)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / secs);
                p.Yaw = y0 + dy * k;
                p.Pitch = Mathf.Lerp(p0, tp, k);
                yield return null;
            }
        }

        void Track(Vector3 target, float speed)
        {
            var cam = p.cam.transform;
            var to = target - cam.position;
            float ty = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float tp = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            p.Yaw = Mathf.LerpAngle(p.Yaw, ty, Time.deltaTime * speed);
            p.Pitch = Mathf.Lerp(p.Pitch, tp, Time.deltaTime * speed);
        }

        Color fog0, amb0;

        IEnumerator Prologue()
        {
            prologue = true;
            var hud = Hud.I;
            p.frozen = true;
            fog0 = RenderSettings.fogColor; amb0 = RenderSettings.ambientLight;
            RenderSettings.fogColor = new Color(0.09f, 0.07f, 0.04f);
            RenderSettings.ambientLight = new Color(0.09f, 0.07f, 0.05f);
            hud.tint = new Color(0.55f, 0.38f, 0.18f, 0.28f);
            hud.letterbox = true;
            w.SetLights(true);
            w.dread = 0.15f;
            clock = M(3, 2); w.SetClock(clock);
            p.Teleport(new Vector3(-1.55f, 0.1f, -8.45f));
            p.Yaw = 20f; p.Pitch = 12f;
            hud.Caption("KALVADI HALT  ·  14 NOVEMBER 1981  ·  03:02", 5f);
            hud.FadeTo(0f);
            yield return Narrate("Forty-five years ago tonight, Velan was the night signalman here.");

            // The call.
            w.phoneRing.pitch = 0.9f;
            w.phoneRing.Play();
            yield return LookAt(new Vector3(-2.6f, 1.1f, -7.3f), 1.4f);
            yield return Wait(1.6f);
            w.phoneRing.Stop();
            w.PlayAt(Synth.Clank(), new Vector3(-2.6f, 1.1f, -7.3f), 0.3f);
            yield return PhoneSay(Q, "Kalvadi. Set the Mail for the branch line.");
            yield return PhoneSay(Q, "It only wants to go home.");
            yield return Narrate("The branch was closed. Its bridge was half torn down. Velan said no.");

            // The levers.
            yield return LookAt(new Vector3(-0.2f, 1.3f, -6.2f), 1.1f);
            w.ForcePoints(true);
            w.Sting();
            p.AddShake(0.4f);
            yield return Wait(0.7f);
            w.signalLever.reversed = true;
            w.signal.Set(true);
            w.PlayAt(Synth.Clank(), w.signalLever.transform.position, 1f);
            yield return Narrate("The levers moved on their own.");

            // The Mail.
            var mail = Train.Spawn(w, TrainKind.Express, "06110 Kalvadi Mail", 28f);
            mail.s = 330f;
            mail.Horn();
            // Cut outside: the platform, the lamp post, the signal showing green.
            p.Teleport(new Vector3(9f, 0.1f, -4.8f));
            p.Yaw = -96f; p.Pitch = -2f;
            yield return null;
            StartCoroutine(Narrate("The Kalvadi Mail took the branch at sixty. Two hundred and twelve people on board."));
            float tt = 0f;
            while (mail && !mail.done && tt < 30f)
            {
                tt += Time.deltaTime;
                Track(mail.route.Pos(mail.s - 8f) + Vector3.up * 2f, 2.5f);
                if (mail.decided && tt > 4f && mail.route.Pos(mail.s).z > 25f) break;
                yield return null;
            }
            yield return Wait(0.6f);
            // The bridge.
            w.sfx.PlayOneShot(Synth.Boom(), 1f);
            p.AddShake(0.8f);
            yield return Wait(0.6f);
            w.PlayAt(Synth.Scream(), new Vector3(200f, 5f, 150f), 1f);
            w.sfx.PlayOneShot(Synth.Scream(), 0.25f);
            StartCoroutine(w.Flicker(1.2f));
            yield return Wait(1f);
            // Cut back inside, into the dark.
            w.SetLights(false);
            if (mail) Destroy(mail.gameObject);
            p.Teleport(new Vector3(-1.55f, 0.1f, -8.45f));
            p.Yaw = 10f; p.Pitch = 5f;
            yield return Narrate("They found Velan dead at his desk. Nobody knew whose handwriting was in the register.");

            // Something at the window.
            yield return LookAt(new Vector3(-0.75f, 1.65f, -5.2f), 1.2f);
            var pe = sc.peeper;
            pe.SetActive(true);
            float y = 0.9f;
            while (y < 1.72f) { y += Time.deltaTime * 0.9f; pe.transform.localPosition = new Vector3(-0.75f, y, -5.02f); yield return null; }
            yield return Wait(0.5f);
            pe.SetActive(false);
            JumpScare.I.Play(0, 0.9f);
            yield return Wait(0.9f);
            hud.FadeTo(1f);
            yield return Narrate("Every year since, Kalvadi Halt has had a new night man. None of them has finished the shift.");

            // Present day: the posting letter, then the date.
            ResetWorld();
            yield return hud.ShowCard(0);
            yield return hud.ShowCard(1);
            EndPrologue();
        }

        void ResetWorld()
        {
            foreach (var t in new List<Train>(Train.All)) if (t) Destroy(t.gameObject);
            w.pointsLever.reversed = false;
            w.signalLever.reversed = false;
            w.signal.Set(false);
            w.SetLights(true);
            w.dread = 0f;
            sc.peeper.SetActive(false);
            RenderSettings.fogColor = fog0 != default ? fog0 : RenderSettings.fogColor;
            RenderSettings.ambientLight = amb0 != default ? amb0 : RenderSettings.ambientLight;
            Hud.I.tint = default;
            Hud.I.letterbox = false;
        }

        void EndPrologue()
        {
            ResetWorld();
            prologue = false;
            p.frozen = false;
            p.Teleport(w.playerStart);
            p.Yaw = w.playerYaw; p.Pitch = 0f;
            Hud.I.EndIntro();
        }

        IEnumerator Run(int from)
        {
            switch (from) { case 1: goto CP1; case 2: goto CP2; case 3: goto CP3; case 4: goto CP4; case 5: goto CP5; }
            // Arrival: on foot, in the dark, toward the one lit window for miles.
            objective = "Walk up to the signal cabin. Your shift starts at 22:30.";
            Hud.I.Say("", "The jeep that brought you is already gone. Its tail lights vanish into the trees.", 5f);
            w.dread = 0.05f;
            StartCoroutine(Figure());
            bool rang = false, noted = false;
            float arrive = 0f;
            var doorPos = new Vector3(2.1f, 0f, -5f);
            while (!Scares.PlayerInside() && arrive < 150f && !ending)
            {
                arrive += Time.deltaTime;
                var pp = p.transform.position; pp.y = 0f;
                float dd = Vector3.Distance(pp, doorPos);
                if (!rang && dd < 8f)
                {
                    rang = true;
                    StartCoroutine(w.Flicker(0.9f));
                    StartCoroutine(GhostRing());
                }
                if (!noted && dd < 3.2f)
                {
                    noted = true;
                    Hud.I.Say("", "(A note is pinned to the wall beside the door.)", 3.5f);
                }
                yield return null;
            }
            if (ending) yield break;
            clock = gate = M(22, 30);
            objective = "Your shift has begun. Wait for the control phone.";
            Hud.I.Say("", "Kalvadi Halt signal cabin. 22:30. Your first night shift.", 4.5f);
            w.dread = 0.1f;
            yield return Wait(5f);
            StartCoroutine(w.Flicker(0.6f));
            Hud.I.Say("", "(The bulb buzzes and stutters.)", 2.5f);

            // 0. Wrong number — breathing on the line within the first twenty seconds.
            yield return Until(M(22, 33));
            yield return Call(new[]
            {
                L(Q, "[slow breathing]"),
                L(Q, "...signalman..."),
                L(Q, "...you came back. We waited so long for you."),
                L("", "(click)")
            }, 30f, true, Synth.Breath());

            // 1. Coastal Express — tutorial, and a man on the line.
            yield return Until(M(22, 36));
            yield return Call(new[]
            {
                L(R, "Kalvadi cabin? Raghavan here, from the Junction. New man, ah? Good, good."),
                L(R, "Did you call me just now? ...No? The line is bad tonight."),
                L(R, "12677 Coastal Express is coming to you. Points NORMAL — that is the main line. Then pull the red lever to clear your signal."),
                L(R, "Once it passes, put the signal back to danger and it goes in the register. That is the whole job.")
            }, 600f);
            objective = "12677 Coastal Express: points NORMAL (main line), then clear the signal.";
            bool stepped = false, warned = false, hit = false;
            float hornT = 0f;
            yield return RunTrain(TrainKind.Express, "12677 Coastal Express", t =>
            {
                if (hit) return;
                float fx = t.FrontX();
                // He steps out of the dark only when the train is about two and a half seconds away.
                if (!stepped && fx > Scares.JumperX - Mathf.Max(18f, t.speed * 2.6f))
                {
                    stepped = true;
                    StartCoroutine(sc.StepOut());
                }
                if (stepped && sc.jumper.activeSelf)
                {
                    if (!warned)
                    {
                        warned = true;
                        Hud.I.Say("", "A man has walked out onto the line. The driver is leaning on his horn.", 3f);
                    }
                    hornT -= Time.deltaTime;
                    if (hornT <= 0f) { hornT = 0.9f; t.Horn(); }
                }
                if (stepped && fx >= Scares.JumperX - 0.3f) { hit = true; sc.HitJumper(t); }
            });
            if (ending) yield break;

            yield return Call(new[]
            {
                L(R, "Kalvadi! My driver says he hit a man. Right in front of your cabin!"),
                L(R, "Go down and look. Take your torch — press F. Quickly!")
            }, 600f);
            objective = "Someone was hit by the train. Go outside and check the track.  (F = torch)";
            yield return HoldUntil(() => sc.bodyExamined, 80f);
            if (sc.bodyExamined) yield return Wait(9f);
            else yield return sc.VanishBodyUnseen();
            if (ending) yield break;
            w.dread = 0.18f;
            StartCoroutine(sc.HangerScene());
            yield return Wait(5f);
            yield return Call(new[]
            {
                L(R, "Kalvadi? Your line went dead for a minute. Are you there?"),
                L(R, "Well? ...Nothing?"),
                L(R, "My driver checked his engine. No blood. No dent. Nothing."),
                L(R, "...He says the man wore the old khaki uniform. The old men at the Junction say Velan still walks the line. Every year, on this night."),
                L(R, "Go back inside, ah. And keep the door shut.")
            }, 150f);
            yield return HoldUntil(() => !sc.hanger.activeSelf, 60f);
            objective = WaitObj;

        CP1:
            Save(1);
            objective = WaitObj;
            StartCoroutine(Haunter());

            // 2. Goods — the face at the window.
            yield return Until(M(23, 8));
            yield return Call(new[]
            {
                L(R, "Goods train coming to you. Main line."),
                L(R, "And don't touch the branch. Nobody uses the branch.")
            });
            objective = "Goods train: points NORMAL, then clear the signal.";
            bool peeped = false;
            yield return RunTrain(TrainKind.Goods, "Goods (BCN rake)", t =>
            {
                if (!peeped && t.FrontX() > -260f && Scares.PlayerInside()) { peeped = true; StartCoroutine(sc.Peep(40f)); }
            });
            if (ending) yield break;
            if (!peeped) StartCoroutine(sc.Peep(40f));

            // Phantom call.
            yield return Until(M(23, 32));
            yield return Call(new[] { L(Q, "[static, and running water]"), L(Q, "...is the signal clear?"), L(Q, "...it's so cold in the river..."), L(Q, "[a child is humming]") }, 20f, true, Synth.Static());
            w.PlayAt(Synth.Hum(), new Vector3(-2.6f, 1.1f, -7.3f), 0.6f);
            Hud.I.Say("", "(Humming, from the dead receiver.)", 3.5f);
            if (lastAnswered)
            {
                yield return Wait(4.2f);
                JumpScare.I.Play(2, 0.75f);
            }
            w.dread = 0.25f;

            yield return Until(M(23, 48));
            if (Scares.PlayerInside()) StartCoroutine(sc.RunPast());
            else sc.Whisper();

            // 3. The quiet passenger.
            yield return Until(M(0, 5));
            yield return Call(new[]
            {
                L(R, "56713 Passenger. Main line."),
                L(R, "Strange train, this one. Nobody gets off at the Junction. Nobody gets on. Still it runs.")
            });
            objective = "56713 Passenger: main line.";
            yield return RunTrain(TrainKind.Passenger, "56713 Kalvadi Passenger");
            if (ending) yield break;
            w.dread = 0.32f;
            StartCoroutine(sc.RoofSteps());

        CP2:
            Save(2);
            yield return Until(M(0, 18));
            yield return Bulletin();
            if (ending) yield break;

        CP3:
            Save(3);
            objective = WaitObj;
            yield return Until(M(0, 40));
            StartCoroutine(Figure());

            // A glimpse of the other side.
            yield return Until(M(0, 58));
            yield return sc.OtherSide(1.4f, true);
            Hud.I.Say("", "(For a second the world was red. And there were people standing outside.)", 4f);
            w.dread = 0.38f;

            // 4. The Kalvadi Mail — the first real choice.
            yield return Until(M(1, 10));
            yield return Call(new[]
            {
                L(R, "...cabin. Cabin, are you there."),
                L(R, "06110 Kalvadi Mail is coming to you. Set it for the BRANCH line."),
                L(R, "It only wants to go home. Don't let it come to the Junction.")
            }, 45f, true);
            sc.ArmBehind();
            scrawl1 = "01:12   06110 Kalvadi Mail ........ BRANCH      (shaky handwriting — not yours)";
            objective = "06110 Kalvadi Mail is not in the timetable. Check the register and the notice.";
            yield return RunTrain(TrainKind.Ghost, "06110 Kalvadi Mail");
            if (ending) yield break;
            w.dread = 0.45f;
            Hud.I.Say("", "(The crickets have stopped.)", 3.5f);

            yield return Until(M(1, 45));
            yield return Knocks();

        CP4:
            Save(4);
            // 5. Rockfort Express — the points move by themselves.
            yield return Until(M(2, 5));
            if (ghostLoose)
                yield return Call(new[]
                {
                    L(R, "A train went through the Junction just now. No engine number. No driver. My porter got on it..."),
                    L(R, "He has not come back. 22631 Rockfort Express to you. Main line. Real passengers on this one. Quickly.")
                });
            else
                yield return Call(new[]
                {
                    L(R, "Who sent a train down the branch? ...Never mind. Never mind."),
                    L(R, "22631 Rockfort Express to you. Main line. Real passengers on this one.")
                });
            objective = "22631 Rockfort Express: main line.";
            anomalyFired = false;
            yield return RunTrain(TrainKind.Express, "22631 Rockfort Express", ExpressAnomaly, 15f);
            if (ending) yield break;
            w.dread = 0.6f;

            // The other side, properly.
            yield return Until(M(2, 35));
            yield return sc.OtherSide(16f, false);
            scrawl2 = "03:10   DO NOT CLEAR THE SIGNAL.   DO NOT CLEAR IT.      (your handwriting)";
            Hud.I.Say("", "Something has been written in the register.", 3.5f);
            w.dread = 0.7f;

        CP5:
            Save(5);
            // 6. The 3:10.
            yield return Until(M(3, 8));
            yield return Call(new[]
            {
                L(Q, "Cabin. ...You know this voice."),
                L(Y, "It's me. The 3:10 is coming. Do not clear the signal. Whatever it says to you."),
                L(Y, "Hold it at danger until morning. The relief comes at five.")
            }, 45f, true);
            yield return FinalTrain();
        }

        IEnumerator RunTrain(TrainKind kind, string id, System.Action<Train> tick = null, float cruise = 0f)
        {
            var t = Train.Spawn(w, kind, id, cruise);
            t.onDecided = OnDecided;
            gate = Mathf.Max(gate, clock + 25f);
            rate = 0.25f;
            while (t && !t.done && !ending)
            {
                tick?.Invoke(t);
                yield return null;
            }
            rate = 0.5f;
            if (t && !ending) Destroy(t.gameObject);
            if (!ending)
                objective = w.SignalClear ? RestoreObj : WaitObj;
        }

        void OnDecided(Train t)
        {
            passed.Add(ClockString + "   " + t.id + "  →  " + (t.toBranch ? "BRANCH" : "MAIN"));
            if (t.kind == TrainKind.Ghost)
            {
                if (t.toBranch) ghostReturned = true;
                else ghostLoose = true;
            }
            else if (t.living && t.toBranch)
            {
                objective = "";
                Hud.I.Say("", "The train swings onto the closed branch line, toward the river.", 4f);
                StartCoroutine(Delayed(5.5f, () => EndingFor(5)));
            }
        }

        void ExpressAnomaly(Train t)
        {
            if (anomalyFired || ending || t.decided) return;
            float fx = t.FrontX();
            bool approaching = fx > World.SignalX - 260f && fx < World.SignalX - 60f;
            bool slowNearSignal = fx >= World.SignalX - 60f && fx < World.SignalX + 2f && t.speed < 8f;
            if (w.SignalClear && !w.PointsReversed && (approaching || slowNearSignal))
            {
                anomalyFired = true;
                w.ForcePoints(true);
                w.Sting();
                Hud.I.Flash(new Color(1f, 1f, 1f, 0.2f), 0.2f);
                Hud.I.Say("", "CLANK — the points lever moved by itself. REVERSE. The branch line.", 4.5f);
                objective = "The points flipped to the BRANCH! Signal to DANGER, then points back to NORMAL!";
            }
        }

        IEnumerator FinalTrain()
        {
            objective = "The 3:10 is coming.";
            var t = Train.Spawn(w, TrainKind.ThreeTen, "3:10");
            t.onDecided = OnDecided;
            gate = M(3, 40);
            rate = 0.3f;
            Hud.I.Say("", "A long, low horn somewhere in the fog.", 3f);
            bool waiting = false, secondCall = false;
            float holdT = 0f;
            while (!ending)
            {
                if (t.decided)
                {
                    yield return Wait(4.5f);
                    EndingFor(t.toBranch ? 3 : 2);
                    yield break;
                }
                if (t.stopped && !waiting)
                {
                    waiting = true;
                    holding = true;
                    objective = "The 3:10 is standing at your signal with its doors open. Hold it until 05:00.";
                    gate = M(5, 0);
                    rate = 1.1f;
                    StartCoroutine(HoldEvents(t));
                }
                if (waiting)
                {
                    holdT += Time.deltaTime;
                    if (t.stopped && !t.doorsOpen && !w.SignalClear) t.SetDoors(true);
                    if (t.doorsOpen && w.SignalClear) t.SetDoors(false);
                    if (!secondCall && holdT > 22f)
                    {
                        secondCall = true;
                        StartCoroutine(Call(new[]
                        {
                            L(R, "Cabin! Why is the 3:10 standing at your signal? Clear it! Passengers are waiting here!"),
                            L(R, "...Clear it, Signalman. Or come and sit with us.")
                        }, 25f, true));
                    }
                }
                if (clock >= M(5, 0) - 0.01f && !t.decided)
                {
                    yield return Dawn(t);
                    yield break;
                }
                yield return null;
            }
        }

        IEnumerator Dawn(Train t)
        {
            holding = false;
            Hud.I.Say("", "The sky is greying. The 3:10's lamps go out, one by one.", 4f);
            yield return Wait(3.5f);
            if (t) Destroy(t.gameObject);
            yield return Wait(1.5f);
            EndingFor(ghostReturned ? 1 : 6);
        }

        public void Board()
        {
            if (ending || !holding) return;
            EndingFor(4);
        }

        public void Struck()
        {
            Die(Endings[0][0], Endings[0][1]);
        }

        void EndingFor(int i)
        {
            if (ending) return;
            ending = true;
            holding = false;
            if (p) p.frozen = true;
            if (w) { w.phoneRing.Stop(); w.voice.Stop(); }
            Hud.I.ShowEnding(i, Endings[i][0], Endings[i][1]);
        }

        IEnumerator Delayed(float secs, System.Action a)
        {
            yield return Wait(secs);
            a();
        }

        IEnumerator Figure()
        {
            w.figure.SetActive(true);
            float t = 0f, look = 0f;
            while (t < 18f && !ending)
            {
                t += Time.deltaTime;
                var cam = p.cam.transform;
                var to = w.figure.transform.position + Vector3.up * 0.6f - cam.position;
                if (Vector3.Dot(cam.forward, to.normalized) > 0.985f && to.magnitude < 140f) look += Time.deltaTime;
                if (look > 1.2f || to.magnitude < 14f) break;
                yield return null;
            }
            w.figure.SetActive(false);
            if (look > 1.2f) w.Sting();
        }

        IEnumerator Knocks()
        {
            Hud.I.Say("", "Someone is knocking on the cabin door.", 3f);
            for (int i = 0; i < 3; i++) { w.knock.Play(); yield return Wait(0.55f); }
            yield return Wait(5f);
            if (!w.door.open)
                for (int i = 0; i < 2; i++) { w.knock.volume = 0.6f; w.knock.Play(); yield return Wait(0.8f); }
            w.knock.volume = 1f;
            sc.ShowHands();
            Hud.I.Say("", "There are handprints on the window glass, on the outside. They are wet. River water.", 4.5f);
            yield return Wait(7f);
            yield return sc.DoorSlam();
        }

        IEnumerator HoldEvents(Train t)
        {
            int i = 0;
            while (!ending && holding && t && !t.decided)
            {
                yield return Wait(Random.Range(8f, 13f));
                if (ending || !holding || !t || t.decided) yield break;
                switch (i % 6)
                {
                    case 0: Speak("PASSENGERS", Whispers[(i / 6) % Whispers.Length], 4f); break;
                    case 1: w.PlayAt(Synth.Knock(), w.windowPos, 1f); Hud.I.Say("", "Something taps on the window glass.", 2.5f); break;
                    case 2: t.Horn(); StartCoroutine(sc.Peep(15f)); break;
                    case 3: StartCoroutine(w.Flicker(0.6f)); Speak("PASSENGERS", Whispers[(i / 6 + 3) % Whispers.Length], 4f); break;
                    case 4: sc.Whisper("...come and sit with us..."); break;
                    case 5: StartCoroutine(sc.OtherSide(1.2f, true)); break;
                }
                i++;
            }
        }

        public string RegisterText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("(previous page)   night of 07 / 08 November  ·  S. Anand");
            sb.AppendLine("03:10   3:10 ............................ CLEARED      (the pen has torn through the paper)");
            sb.AppendLine();
            sb.AppendLine("Kalvadi Halt  ·  night of 14 / 15 November  ·  Signalman on duty: ________");
            sb.AppendLine();
            sb.AppendLine("WORKING TIMETABLE");
            sb.AppendLine("22:40   12677 Coastal Express ........ MAIN");
            sb.AppendLine("23:12   Goods (BCN rake) ............. MAIN");
            sb.AppendLine("00:08   56713 Kalvadi Passenger ...... MAIN");
            if (scrawl1 != null) sb.AppendLine(scrawl1);
            sb.AppendLine("02:10   22631 Rockfort Express ....... MAIN");
            if (scrawl2 != null) sb.AppendLine(scrawl2);
            foreach (var e in extra) sb.AppendLine(e);
            sb.AppendLine("05:00   Relief signalman takes charge.");
            sb.AppendLine();
            sb.AppendLine("TRAINS PASSED");
            if (passed.Count == 0) sb.AppendLine("—");
            foreach (var l in passed) sb.AppendLine(l);
            return sb.ToString();
        }
    }
}
