using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// Builds Kalvadi Halt from primitives: signal cabin, lever frame, main line, the closed branch line, home signal and forest.
    /// Trains run along +X. The cabin sits on the -Z side of the track.
    public class World : MonoBehaviour
    {
        public const float SpawnX = -520f, SignalX = 28f, PointsX = 70f, EndX = 600f;
        public static float SignalS => SignalX - SpawnX;
        public static float SignalStopS => SignalX - 4f - SpawnX;
        public static float PointsS => PointsX - SpawnX;

        public Route mainRoute, branchRoute;
        public List<Vector3> branchPath;
        public Lever pointsLever, signalLever;
        public SignalPost signal;
        public Door door;
        public AudioSource phoneRing, voice, knock, wind, crickets, drone, sfx;
        public Light cabinLight, lampLight;
        public GameObject figure;
        public Light moon, deskLight;
        public bool otherSide, clockSpin;
        public Scares scares;
        public AudioSource radio, radioStatic;
        public bool radioOn;
        Renderer radioDial;

        public void SetRadio(bool on)
        {
            radioOn = on;
            radioStatic.volume = on ? 0.22f : 0f;
            if (radioDial) radioDial.sharedMaterial = Mats.Unlit(on ? new Color(1f, 0.6f, 0.2f) : new Color(0.15f, 0.08f, 0.03f));
        }
        public Vector3 playerStart = new Vector3(-11f, 0.05f, -5.2f);
        public float playerYaw = 96f;
        public Vector3 windowPos = new Vector3(-0.8f, 1.9f, -5.3f);
        [Range(0f, 1f)] public float dread;

        Transform root;
        Renderer bulb, ptsLamp, sigLamp, phoneLamp;
        Transform hourHand, minuteHand;
        Material bulbOn, bulbOff;
        bool lightsOn = true;
        float flick = 5f, lampBase = 2.2f;

        public bool PointsReversed => pointsLever.reversed;
        public bool SignalClear => signal.clear;

        public static World Build(Transform parent)
        {
            var go = new GameObject("World");
            go.transform.SetParent(parent, false);
            var w = go.AddComponent<World>();
            w.root = go.transform;
            w.BuildRoutes();
            w.BuildGround();
            w.BuildTrack();
            w.BuildCabin();
            w.BuildSignal();
            w.BuildTrees();
            w.BuildAudio();
            w.scares = go.AddComponent<Scares>();
            w.scares.Build(w);
            return w;
        }

        void BuildRoutes()
        {
            mainRoute = new Route(new List<Vector3> { new Vector3(SpawnX, 0f, 0f), new Vector3(PointsX, 0f, 0f), new Vector3(EndX, 0f, 0f) });
            var ctrl = new List<Vector3>
            {
                new Vector3(PointsX, 0f, 0f), new Vector3(120f, 0f, 1.5f), new Vector3(170f, 0f, 22f), new Vector3(210f, 0f, 65f),
                new Vector3(238f, 0f, 135f), new Vector3(255f, 0f, 240f), new Vector3(262f, 0f, 420f)
            };
            branchPath = Route.CatmullRom(ctrl, 5f);
            var br = new List<Vector3> { new Vector3(SpawnX, 0f, 0f) };
            br.AddRange(branchPath);
            branchRoute = new Route(br);
        }

        void BuildGround()
        {
            Geo.Box(root, new Vector3(0f, -0.5f, 0f), new Vector3(1400f, 1f, 1400f), Tex.Mat("forest_ground_04", new Color(0.32f, 0.34f, 0.26f), new Vector2(420f, 420f)), true, 0f, "Ground");

            var moonGo = new GameObject("Moon");
            moonGo.transform.SetParent(root, false);
            moonGo.transform.rotation = Quaternion.Euler(35f, -150f, 0f);
            moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.62f, 0.85f);
            moon.intensity = 0.18f;
            moon.shadows = LightShadows.None;

            // Sodium lamp post by the cabin steps.
            var post = Mats.Lit(new Color(0.15f, 0.15f, 0.16f));
            Geo.Box(root, new Vector3(4.6f, 2.5f, -4.4f), new Vector3(0.14f, 5f, 0.14f), post);
            Geo.Box(root, new Vector3(4.6f, 4.95f, -3.9f), new Vector3(0.1f, 0.1f, 1.0f), post, false);
            Geo.Box(root, new Vector3(4.6f, 4.85f, -3.45f), new Vector3(0.35f, 0.12f, 0.25f), Mats.Unlit(new Color(1f, 0.62f, 0.25f)), false);
            lampLight = Geo.Point(root, new Vector3(4.6f, 4.5f, -3.4f), new Color(1f, 0.58f, 0.24f), lampBase, 16f);

            // Yellow station name board (Indian Railways style) on two posts.
            var yellow = Mats.Lit(new Color(0.85f, 0.72f, 0.1f));
            var blackM = Mats.Lit(new Color(0.03f, 0.03f, 0.03f));
            Geo.Box(root, new Vector3(-9.5f, 1.2f, -3.6f), new Vector3(0.1f, 2.4f, 0.1f), blackM);
            Geo.Box(root, new Vector3(-6.5f, 1.2f, -3.6f), new Vector3(0.1f, 2.4f, 0.1f), blackM);
            Geo.Box(root, new Vector3(-8f, 2.2f, -3.6f), new Vector3(3.4f, 0.9f, 0.06f), yellow, false);
            for (int i = 0; i < 6; i++) Geo.Box(root, new Vector3(-9.2f + i * 0.32f, 2.2f, -3.56f), new Vector3(0.16f, 0.36f, 0.02f), blackM, false);

            // The figure under the signal.
            figure = Geo.Prim(PrimitiveType.Capsule, root, new Vector3(SignalX - 3f, 1.05f, 4.8f), new Vector3(0.42f, 1.1f, 0.42f), Mats.Lit(new Color(0.005f, 0.005f, 0.005f)), Quaternion.identity);
            figure.name = "Figure";
            figure.SetActive(false);
        }

        void BuildTrack()
        {
            var t = new GameObject("Track").transform;
            t.SetParent(root, false);
            var ballast = Tex.Mat("gravel_floor", new Color(0.55f, 0.52f, 0.48f), new Vector2(400f, 1.4f));
            var rail = Tex.Mat("rusty_metal_02", new Color(0.75f, 0.72f, 0.7f), new Vector2(40f, 0.2f), 0.5f);
            var rust = Mats.Lit(new Color(0.3f, 0.15f, 0.08f), 0.2f);
            var sleeper = Tex.Mat("dark_wooden_planks", new Color(0.6f, 0.5f, 0.42f), new Vector2(0.4f, 1f));

            Geo.Box(t, new Vector3(40f, 0.1f, 0f), new Vector3(1300f, 0.2f, 4.2f), ballast, false);
            for (float x = -620f; x < 680f; x += 100f)
                foreach (float z in new[] { -0.84f, 0.84f })
                    Geo.Box(t, new Vector3(x + 50f, 0.29f, z), new Vector3(100f, 0.14f, 0.08f), rail, false);
            for (float x = -140f; x <= 160f; x += 1.1f)
                Geo.Box(t, new Vector3(x, 0.22f, 0f), new Vector3(0.26f, 0.06f, 2.7f), sleeper, false);

            // Branch line: rusted, overgrown, closed since 1981.
            for (int i = 1; i < branchPath.Count; i++)
            {
                Vector3 a = branchPath[i - 1], b = branchPath[i];
                var dir = (b - a).normalized;
                var n = Vector3.Cross(Vector3.up, dir).normalized;
                Vector3 a2 = a - dir * 0.15f, b2 = b + dir * 0.15f;
                if (i > 2) Geo.Beam(t, a2 + Vector3.up * 0.09f, b2 + Vector3.up * 0.09f, 3.6f, 0.18f, ballast);
                foreach (float off in new[] { -0.84f, 0.84f })
                    Geo.Beam(t, a2 + n * off + Vector3.up * 0.29f, b2 + n * off + Vector3.up * 0.29f, 0.08f, 0.14f, rust);
                if (i < 40)
                {
                    var g = Geo.Box(t, (a + b) * 0.5f + Vector3.up * 0.2f, new Vector3(2.7f, 0.06f, 0.26f), sleeper, false);
                    g.transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // Broken barrier across the branch, ~45 m past the points.
            float acc = 0f; int bi = 1;
            for (; bi < branchPath.Count - 1; bi++)
            {
                acc += Vector3.Distance(branchPath[bi - 1], branchPath[bi]);
                if (acc > 45f) break;
            }
            var bp = branchPath[bi];
            var bd = (branchPath[bi] - branchPath[bi - 1]).normalized;
            var bn = Vector3.Cross(Vector3.up, bd).normalized;
            var white = Mats.Lit(new Color(0.8f, 0.8f, 0.75f));
            var red = Mats.Lit(new Color(0.6f, 0.06f, 0.05f));
            Geo.Box(t, bp + bn * 2.3f + Vector3.up * 0.6f, new Vector3(0.15f, 1.2f, 0.15f), white);
            Geo.Box(t, bp - bn * 2.3f + Vector3.up * 0.6f, new Vector3(0.15f, 1.2f, 0.15f), white);
            var bar = Geo.Box(t, bp + Vector3.up * 1.0f, new Vector3(4.8f, 0.18f, 0.12f), red, false);
            bar.transform.rotation = Quaternion.LookRotation(bd) * Quaternion.Euler(0f, 0f, 12f);

            var weed = Mats.Lit(new Color(0.09f, 0.14f, 0.06f));
            var rnd = new System.Random(81);
            for (int i = 0; i < 90; i++)
            {
                var p = branchPath[rnd.Next(3, branchPath.Count)];
                p += new Vector3((float)rnd.NextDouble() * 3f - 1.5f, 0f, (float)rnd.NextDouble() * 3f - 1.5f);
                float h = 0.2f + (float)rnd.NextDouble() * 0.7f;
                Geo.Box(t, p + Vector3.up * h * 0.5f, new Vector3(0.5f, h, 0.5f), weed, false, (float)rnd.NextDouble() * 90f);
            }
        }

        void BuildCabin()
        {
            var c = new GameObject("Cabin").transform;
            c.SetParent(root, false);
            var wall = Tex.Mat("painted_plaster_wall", new Color(0.92f, 0.82f, 0.62f), new Vector2(2.5f, 1.2f));
            var trim = Mats.Lit(new Color(0.34f, 0.12f, 0.07f));
            var floor = Tex.Mat("dark_wooden_planks", new Color(0.7f, 0.62f, 0.55f), new Vector2(3f, 2.5f));
            var roofM = Tex.Mat("corrugated_iron", new Color(0.5f, 0.48f, 0.46f), new Vector2(3f, 3f), 0.3f);
            var wood = Tex.Mat("dark_wooden_planks", new Color(0.8f, 0.6f, 0.45f), new Vector2(1f, 1f));
            var black = Mats.Lit(new Color(0.03f, 0.03f, 0.03f), 0.5f);
            var steel = Mats.Lit(new Color(0.6f, 0.6f, 0.62f), 0.7f);
            var paper = Mats.Lit(new Color(0.82f, 0.77f, 0.62f));

            Geo.Box(c, new Vector3(0f, 0.15f, -8f), new Vector3(6.4f, 0.3f, 5.4f), floor);
            Geo.Box(c, new Vector3(0f, 1.8f, -10.6f), new Vector3(6.4f, 3f, 0.2f), wall);
            Geo.Box(c, new Vector3(-3.1f, 1.8f, -8f), new Vector3(0.2f, 3f, 5.4f), wall);
            Geo.Box(c, new Vector3(3.1f, 1.8f, -8f), new Vector3(0.2f, 3f, 5.4f), wall);
            // North wall (faces the track): big window + door.
            Geo.Box(c, new Vector3(-2.95f, 1.8f, -5.4f), new Vector3(0.5f, 3f, 0.2f), wall);
            Geo.Box(c, new Vector3(-0.75f, 0.75f, -5.4f), new Vector3(3.9f, 0.9f, 0.2f), wall);
            Geo.Box(c, new Vector3(-0.75f, 2.9f, -5.4f), new Vector3(3.9f, 0.8f, 0.2f), wall);
            Geo.Box(c, new Vector3(1.4f, 1.8f, -5.4f), new Vector3(0.4f, 3f, 0.2f), wall);
            Geo.Box(c, new Vector3(2.1f, 2.85f, -5.4f), new Vector3(1.0f, 0.9f, 0.2f), wall);
            Geo.Box(c, new Vector3(2.9f, 1.8f, -5.4f), new Vector3(0.6f, 3f, 0.2f), wall);
            var blocker = Geo.Box(c, new Vector3(-0.75f, 1.85f, -5.4f), new Vector3(3.9f, 1.3f, 0.1f), wall, true, 0f, "WindowBlocker");
            blocker.GetComponent<Renderer>().enabled = false;
            Geo.Box(c, new Vector3(-0.75f, 1.22f, -5.3f), new Vector3(4.0f, 0.06f, 0.35f), trim, false);
            foreach (float x in new[] { -2.05f, -0.75f, 0.55f })
                Geo.Box(c, new Vector3(x, 1.85f, -5.4f), new Vector3(0.06f, 1.3f, 0.08f), trim, false);
            Geo.Box(c, new Vector3(0f, 3.42f, -8f), new Vector3(7.2f, 0.18f, 6.4f), roofM);
            Geo.Box(c, new Vector3(0f, 3.3f, -4.82f), new Vector3(7.2f, 0.1f, 0.05f), trim, false);
            Geo.Box(c, new Vector3(0f, 0.45f, -5.28f), new Vector3(6.4f, 0.3f, 0.04f), trim, false);
            Geo.Box(c, new Vector3(2.1f, 0.08f, -4.95f), new Vector3(1.2f, 0.16f, 0.8f), floor);

            // Door (opens outward toward the track).
            var dp = new GameObject("DoorPivot").transform;
            dp.SetParent(c, false);
            dp.localPosition = new Vector3(1.6f, 0.3f, -5.4f);
            var panel = Geo.Box(dp, new Vector3(0.5f, 1.05f, 0f), new Vector3(1.0f, 2.1f, 0.08f), wood, true, 0f, "Door");
            door = dp.gameObject.AddComponent<Door>();
            Interactable.Add(panel, () => door.open ? "[E] Close door" : "[E] Open door", () =>
            {
                door.speed = 260f;
                door.open = !door.open;
                PlayAt(Synth.Knock(), dp.position, 0.3f);
            });

            // Anand's handover note, pinned outside beside the door.
            var note = Geo.Box(c, new Vector3(2.88f, 1.55f, -5.285f), new Vector3(0.26f, 0.34f, 0.02f), paper, true, 0f, "HandoverNote");
            Geo.Box(c, new Vector3(2.88f, 1.7f, -5.274f), new Vector3(0.03f, 0.03f, 0.01f), Mats.Lit(new Color(0.5f, 0.05f, 0.05f)), false);
            Interactable.Add(note, () => "[E] Read the note pinned by the door", () => { Hud.I.Read(Director.HandoverTitle, Director.HandoverBody); Director.I.ReadAloud(Voice.Anand, Director.HandoverBody); });

            // Lever frame: black = Points No.1, red = Home signal.
            Geo.Box(c, new Vector3(-0.2f, 0.45f, -6.2f), new Vector3(2.2f, 0.3f, 0.5f), Mats.Lit(new Color(0.12f, 0.12f, 0.13f)));
            pointsLever = MakeLever(c, -0.7f, Mats.Lit(new Color(0.05f, 0.05f, 0.05f), 0.4f), steel);
            signalLever = MakeLever(c, 0.3f, Mats.Lit(new Color(0.62f, 0.05f, 0.04f), 0.4f), steel);
            ptsLamp = Geo.Box(c, new Vector3(-0.35f, 0.63f, -6.0f), new Vector3(0.12f, 0.06f, 0.12f), Mats.Unlit(Color.white), false).GetComponent<Renderer>();
            sigLamp = Geo.Box(c, new Vector3(0.65f, 0.63f, -6.0f), new Vector3(0.12f, 0.06f, 0.12f), Mats.Unlit(Color.red), false).GetComponent<Renderer>();
            Interactable.Add(pointsLever.hit, () => pointsLever.reversed
                ? "Points No.1 — REVERSE (branch line)\n[E] set NORMAL"
                : "Points No.1 — NORMAL (main line)\n[E] set REVERSE", TogglePoints);
            Interactable.Add(signalLever.hit, () => signal.clear
                ? "Home signal — CLEAR\n[E] restore to DANGER"
                : "Home signal — DANGER\n[E] clear the signal", ToggleSignal);

            // Desk, phone, register.
            Geo.Box(c, new Vector3(-2.55f, 0.95f, -8.2f), new Vector3(0.9f, 0.06f, 2.6f), wood);
            Geo.Box(c, new Vector3(-2.55f, 0.62f, -7.0f), new Vector3(0.8f, 0.62f, 0.06f), wood, false);
            Geo.Box(c, new Vector3(-2.55f, 0.62f, -9.4f), new Vector3(0.8f, 0.62f, 0.06f), wood, false);
            Geo.Box(c, new Vector3(-2.6f, 1.03f, -7.3f), new Vector3(0.26f, 0.1f, 0.22f), black, false);
            Geo.Box(c, new Vector3(-2.6f, 1.12f, -7.3f), new Vector3(0.07f, 0.07f, 0.3f), black, false);
            var phoneHit = new GameObject("PhoneHit");
            phoneHit.transform.SetParent(c, false);
            phoneHit.transform.localPosition = new Vector3(-2.6f, 1.1f, -7.3f);
            var pbc = phoneHit.AddComponent<BoxCollider>();
            pbc.size = new Vector3(0.45f, 0.3f, 0.45f);
            pbc.isTrigger = true;
            Interactable.Add(phoneHit, () => Director.I.PhoneRinging ? "[E] Answer the control phone" : "Control phone", () => Director.I.AnswerPhone());
            // Call lamp on the phone: blinks red while it rings, so players can find it in the dark.
            phoneLamp = Geo.Box(c, new Vector3(-2.47f, 1.09f, -7.3f), new Vector3(0.05f, 0.04f, 0.05f), Mats.Unlit(new Color(0.12f, 0.02f, 0.02f)), false).GetComponent<Renderer>();
            // Small desk lamp over the phone and register.
            Geo.Box(c, new Vector3(-2.75f, 1.45f, -7.85f), new Vector3(0.03f, 0.95f, 0.03f), black, false);
            Geo.Prim(PrimitiveType.Cylinder, c, new Vector3(-2.62f, 1.9f, -7.85f), new Vector3(0.26f, 0.08f, 0.26f), Mats.Lit(new Color(0.12f, 0.2f, 0.12f), 0.5f), Quaternion.identity);
            deskLight = Geo.Point(c, new Vector3(-2.55f, 1.72f, -7.85f), new Color(1f, 0.78f, 0.5f), 1.8f, 3.2f);

            var reg = Geo.Box(c, new Vector3(-2.55f, 1.0f, -8.55f), new Vector3(0.45f, 0.04f, 0.34f), paper, true, 8f, "Register");
            Interactable.Add(reg, () => "[E] Read the train register", () => Hud.I.Read("TRAIN REGISTER", Director.I.RegisterText()));

            // An old valve radio at the end of the desk.
            var radioBody = Geo.Box(c, new Vector3(-2.62f, 1.07f, -9.1f), new Vector3(0.2f, 0.17f, 0.36f), Mats.Lit(new Color(0.28f, 0.17f, 0.1f), 0.35f), true, 0f, "Radio");
            Geo.Box(c, new Vector3(-2.515f, 1.07f, -9.15f), new Vector3(0.01f, 0.11f, 0.2f), Mats.Lit(new Color(0.1f, 0.09f, 0.07f)), false);
            radioDial = Geo.Box(c, new Vector3(-2.512f, 1.12f, -8.98f), new Vector3(0.01f, 0.03f, 0.07f), Mats.Unlit(new Color(0.15f, 0.08f, 0.03f)), false).GetComponent<Renderer>();
            Interactable.Add(radioBody, () => radioOn ? "Radio — it will not switch off" : "[E] Radio", () => { if (!radioOn) Hud.I.Say("", "Only static. Nobody broadcasts this late.", 2.5f); });

            Geo.Box(c, new Vector3(-1.6f, 0.65f, -8.4f), new Vector3(0.45f, 0.06f, 0.45f), wood);
            Geo.Box(c, new Vector3(-1.6f, 0.47f, -8.4f), new Vector3(0.06f, 0.34f, 0.06f), wood, false);

            // Notice board, poster, clock.
            var notice = Geo.Box(c, new Vector3(-1.1f, 1.9f, -10.48f), new Vector3(1.3f, 0.85f, 0.04f), paper, true, 0f, "Notice");
            Geo.Box(c, new Vector3(-1.1f, 2.25f, -10.455f), new Vector3(1.3f, 0.14f, 0.02f), Mats.Lit(new Color(0.6f, 0.06f, 0.05f)), false);
            Interactable.Add(notice, () => "[E] Read the notice", () => Hud.I.Read(Director.NoticeTitle, Director.NoticeBody));
            Geo.Box(c, new Vector3(2.3f, 1.8f, -10.48f), new Vector3(0.55f, 0.75f, 0.03f), Mats.Lit(new Color(0.45f, 0.18f, 0.12f)), false);

            var face = Geo.Prim(PrimitiveType.Cylinder, c, new Vector3(1.0f, 2.45f, -10.47f), new Vector3(0.5f, 0.02f, 0.5f), Mats.Lit(new Color(0.88f, 0.86f, 0.8f)), Quaternion.Euler(90f, 0f, 0f), true);
            Interactable.Add(face, () => "Clock — " + Director.I.ClockString, () => { });
            hourHand = new GameObject("Hour").transform;
            hourHand.SetParent(c, false);
            hourHand.localPosition = new Vector3(1.0f, 2.45f, -10.44f);
            Geo.Box(hourHand, new Vector3(0f, 0.065f, 0f), new Vector3(0.025f, 0.13f, 0.01f), black, false);
            minuteHand = new GameObject("Minute").transform;
            minuteHand.SetParent(c, false);
            minuteHand.localPosition = new Vector3(1.0f, 2.45f, -10.435f);
            Geo.Box(minuteHand, new Vector3(0f, 0.095f, 0f), new Vector3(0.018f, 0.19f, 0.01f), black, false);

            // Ceiling bulb.
            bulbOn = Mats.Unlit(new Color(1f, 0.9f, 0.6f));
            bulbOff = Mats.Lit(new Color(0.2f, 0.2f, 0.18f));
            bulb = Geo.Prim(PrimitiveType.Sphere, c, new Vector3(0f, 3.12f, -8f), Vector3.one * 0.16f, bulbOn, Quaternion.identity).GetComponent<Renderer>();
            Geo.Box(c, new Vector3(0f, 3.27f, -8f), new Vector3(0.01f, 0.3f, 0.01f), black, false);
            cabinLight = Geo.Point(c, new Vector3(0f, 2.95f, -8f), new Color(1f, 0.8f, 0.55f), 4.2f, 10f);
        }

        Lever MakeLever(Transform parent, float x, Material m, Material grip)
        {
            var pv = new GameObject("Lever").transform;
            pv.SetParent(parent, false);
            pv.localPosition = new Vector3(x, 0.6f, -6.2f);
            Geo.Box(pv, new Vector3(0f, 0.65f, 0f), new Vector3(0.07f, 1.3f, 0.07f), m, false);
            Geo.Box(pv, new Vector3(0f, 1.32f, 0f), new Vector3(0.1f, 0.16f, 0.1f), grip, false);
            var hit = new GameObject("Hit");
            hit.transform.SetParent(pv, false);
            var bc = hit.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.75f, 0f);
            bc.size = new Vector3(0.4f, 1.5f, 0.4f);
            bc.isTrigger = true;
            var l = pv.gameObject.AddComponent<Lever>();
            l.hit = hit;
            return l;
        }

        void BuildSignal()
        {
            var s = new GameObject("HomeSignal").transform;
            s.SetParent(root, false);
            s.localPosition = new Vector3(SignalX, 0f, 2.8f);
            var dark = Mats.Lit(new Color(0.08f, 0.08f, 0.08f));
            Geo.Box(s, new Vector3(0f, 3.5f, 0f), new Vector3(0.2f, 7f, 0.2f), dark);
            Geo.Box(s, new Vector3(-0.1f, 6.4f, 0f), new Vector3(0.5f, 1.0f, 0.5f), dark, false);
            Geo.Box(s, new Vector3(-0.1f, 7.0f, 0f), new Vector3(0.7f, 0.08f, 0.6f), dark, false);
            var lamp = Geo.Prim(PrimitiveType.Sphere, s, new Vector3(-0.38f, 6.45f, 0f), Vector3.one * 0.3f, Mats.Unlit(Color.red), Quaternion.identity);
            var glow = Geo.Point(s, new Vector3(-0.8f, 6.45f, 0f), Color.red, 2.5f, 8f);
            signal = s.gameObject.AddComponent<SignalPost>();
            signal.Init(lamp.GetComponent<Renderer>(), glow);
        }

        void BuildTrees()
        {
            var rnd = new System.Random(14);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var bark = Mats.Lit(new Color(0.12f, 0.09f, 0.07f));
            var leaf1 = Mats.Lit(new Color(0.05f, 0.09f, 0.05f));
            var leaf2 = Mats.Lit(new Color(0.07f, 0.11f, 0.06f));
            var tr = new GameObject("Trees").transform;
            tr.SetParent(root, false);
            int placed = 0, guard = 0;
            while (placed < 280 && guard++ < 5000)
            {
                float x = R(-230f, 330f), z = R(-200f, 260f);
                if (Mathf.Abs(z) < 10f) continue;
                if (x > -16f && x < 16f && z > -20f && z < 0f) continue;
                if (NearBranch(new Vector3(x, 0f, z), 10f)) continue;
                float h = R(4f, 9f), s = R(2.2f, 3.6f);
                var m = rnd.NextDouble() < 0.5 ? leaf1 : leaf2;
                Geo.Box(tr, new Vector3(x, h * 0.25f, z), new Vector3(0.35f, h * 0.5f, 0.35f), bark);
                Geo.Box(tr, new Vector3(x, h * 0.5f, z), new Vector3(s, h * 0.3f, s), m, false, R(0f, 90f));
                Geo.Box(tr, new Vector3(x, h * 0.72f, z), new Vector3(s * 0.7f, h * 0.28f, s * 0.7f), m, false, R(0f, 90f));
                Geo.Box(tr, new Vector3(x, h * 0.92f, z), new Vector3(s * 0.4f, h * 0.2f, s * 0.4f), m, false, R(0f, 90f));
                placed++;
            }
        }

        bool NearBranch(Vector3 p, float d)
        {
            foreach (var b in branchPath) if ((b - p).sqrMagnitude < d * d) return true;
            return false;
        }

        void BuildAudio()
        {
            wind = Geo.Audio(root, Vector3.zero, Synth.Wind(), 0.22f, true, false);
            crickets = Geo.Audio(root, Vector3.zero, Synth.Crickets(), 0.18f, true, false);
            drone = Geo.Audio(root, Vector3.zero, Synth.Drone(), 0f, true, false);
            sfx = Geo.Audio(root, Vector3.zero, null, 0.8f, false, false);
            Sfx = sfx;
            phoneRing = Geo.Audio(root, new Vector3(-2.6f, 1.1f, -7.3f), Synth.Ring(), 0.7f, false, true, 35f);
            phoneRing.loop = true;
            voice = Geo.Audio(root, new Vector3(-2.6f, 1.1f, -7.3f), Synth.Murmur(), 0.4f, false, true, 6f);
            voice.loop = true;
            knock = Geo.Audio(root, new Vector3(1.9f, 1.3f, -5.3f), Synth.Knock(), 1f, false, true, 30f);
            radio = Geo.Audio(root, new Vector3(-2.6f, 1.07f, -9.1f), null, 1f, false, true, 16f);
            radio.spatialBlend = 0.6f;
            radioStatic = Geo.Audio(root, new Vector3(-2.6f, 1.07f, -9.1f), Synth.Static(), 0f, true, true, 12f);
        }

        void Update()
        {
            ptsLamp.sharedMaterial = Mats.Unlit(pointsLever.reversed ? new Color(1f, 0.6f, 0.1f) : new Color(0.9f, 0.9f, 0.85f));
            sigLamp.sharedMaterial = Mats.Unlit(signal.clear ? new Color(0.2f, 1f, 0.5f) : new Color(1f, 0.1f, 0.05f));
            crickets.volume = otherSide ? 0f : Mathf.Lerp(0.18f, 0f, dread * 2.2f);
            drone.volume = otherSide ? 0.75f : dread * 0.35f;
            bool blink = Director.I && Director.I.PhoneRinging && (Time.time % 0.8f) < 0.45f;
            phoneLamp.sharedMaterial = Mats.Unlit(blink ? new Color(1f, 0.12f, 0.06f) : new Color(0.12f, 0.02f, 0.02f));
            if (lightsOn && !otherSide)
            {
                lampLight.intensity = Random.value < 0.015f + dread * 0.05f ? lampBase * 0.25f : lampBase;
                flick -= Time.deltaTime;
                if (dread > 0.08f && flick <= 0f)
                {
                    flick = Random.Range(Mathf.Lerp(16f, 4f, dread), Mathf.Lerp(28f, 10f, dread));
                    StartCoroutine(Flicker(0.3f));
                }
            }
        }

        public void SetLights(bool on)
        {
            lightsOn = on;
            cabinLight.enabled = on;
            deskLight.enabled = on;
            lampLight.enabled = on;
            bulb.sharedMaterial = on ? bulbOn : bulbOff;
        }

        public IEnumerator Flicker(float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                bool o = Random.value > 0.5f;
                cabinLight.enabled = o;
                deskLight.enabled = o;
                bulb.sharedMaterial = o ? bulbOn : bulbOff;
                t += Time.deltaTime;
                yield return null;
            }
            cabinLight.enabled = lightsOn;
            deskLight.enabled = lightsOn;
            bulb.sharedMaterial = lightsOn ? bulbOn : bulbOff;
        }

        public void SetClock(float minutes)
        {
            if (clockSpin) minutes = 1440f - Time.time * 240f;
            float m = minutes % 1440f;
            hourHand.localRotation = Quaternion.Euler(0f, 0f, (m / 60f % 12f) * 30f);
            minuteHand.localRotation = Quaternion.Euler(0f, 0f, (m % 60f) * 6f);
        }

        /// Points are locked while the signal is clear (interlocking) or while a train is running over them.
        public bool TrackOccupied()
        {
            foreach (var t in Train.All)
                if (t.FrontX() >= PointsX - 25f && t.TailX() <= PointsX + 6f) return true;
            return false;
        }

        void TogglePoints()
        {
            if (signal.clear)
            {
                Hud.I.Say("", "The points lever is locked while the signal is clear. Restore the signal to danger first.", 3.5f);
                PlayAt(Synth.Knock(), pointsLever.transform.position, 0.4f);
                return;
            }
            if (TrackOccupied())
            {
                Hud.I.Say("", "Locked — a train is running over the points.", 2.5f);
                PlayAt(Synth.Knock(), pointsLever.transform.position, 0.4f);
                return;
            }
            pointsLever.reversed = !pointsLever.reversed;
            PlayAt(Synth.Clank(), pointsLever.transform.position, 0.9f);
        }

        void ToggleSignal()
        {
            signalLever.reversed = !signalLever.reversed;
            signal.Set(signalLever.reversed);
            PlayAt(Synth.Clank(), signalLever.transform.position, 0.9f);
        }

        public void ForcePoints(bool reversed)
        {
            pointsLever.reversed = reversed;
            PlayAt(Synth.Clank(), pointsLever.transform.position, 1f);
        }

        public void PlayAt(AudioClip clip, Vector3 pos, float vol) => AudioSource.PlayClipAtPoint(clip, pos, vol);
        public void Sting() => sfx.PlayOneShot(Synth.Sting(), 0.7f);
        public static AudioSource Sfx;
    }
}
