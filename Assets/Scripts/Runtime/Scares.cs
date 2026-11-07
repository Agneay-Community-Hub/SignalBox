using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// Horror set-pieces: the man on the line, the body, the face at the window, the runner,
    /// footsteps on the roof, whispers, the door, and the crowd on the other side.
    public class Scares : MonoBehaviour
    {
        public static Scares I;
        public const float JumperX = -6f;

        World w;
        public GameObject jumper, body, peeper, peepHands, runner, crowdRoot, handprints;
        public bool bodyExamined, peeperActive, hangerDone;
        public GameObject hanger, hangerHead, ropeStub, behind, behindHead;
        AudioSource ropeAudio;
        public Actor velan, velanBody, anand, drowned, runnerA, velanGhost;
        readonly List<Actor> crowdA = new List<Actor>();
        GameObject creature;
        readonly List<Actor> walkers = new List<Actor>();
        public bool doorFailed;
        bool peepNoted, anandNoted;
        int peepsSeen;
        bool behindArmed;
        HudState lastHud;
        readonly List<Transform> crowd = new List<Transform>();
        readonly List<Vector3> crowdStart = new List<Vector3>();
        AudioSource heart;
        float creep;
        Color fogC, ambC, bgC, cabC, lampC, deskC;
        float fogD, moonI;

        static readonly string[] Pleas =
        {
            "RAGHAVAN?|Kalvadi, open up. It's Raghavan. I walked all the way from the Junction.",
            "CHILD|Uncle... it's so cold out here. Please let me in.",
            "AT THE DOOR|Signalman. Open the door. We only want to get warm.",
            "A VOICE YOU KNOW|It's me. Open the door. It's only me."
        };

        static readonly string[] WhisperLines =
        {
            "...behind you...",
            "...signalman...",
            "...don't turn around...",
            "...we can see you through the glass...",
            "...it's so cold on the line...",
            "...you were here before..."
        };

        public void Build(World world)
        {
            I = this;
            w = world;
            var root = transform;
            var skin = Mats.Unlit(new Color(0.42f, 0.42f, 0.38f));
            var dead = Mats.Unlit(new Color(0.3f, 0.32f, 0.3f));
            var black = Mats.Lit(new Color(0.006f, 0.006f, 0.006f));
            var eyeM = Mats.Unlit(Color.black);
            var glow = Mats.Unlit(new Color(0.85f, 0.82f, 0.7f));
            var uniform = Mats.Lit(new Color(0.5f, 0.44f, 0.3f));
            var blood = Mats.Lit(new Color(0.24f, 0.01f, 0.01f), 0.25f);

            // The man standing on the running line, facing the cabin (-Z).
            jumper = new GameObject("Jumper");
            jumper.transform.SetParent(root, false);
            jumper.transform.localPosition = new Vector3(JumperX, 0.35f, 0f);
            Geo.Prim(PrimitiveType.Capsule, jumper.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 0.8f, 0.35f), Mats.Unlit(new Color(0.2f, 0.18f, 0.13f)), Quaternion.identity);
            Geo.Prim(PrimitiveType.Sphere, jumper.transform, new Vector3(0f, 1.95f, 0f), new Vector3(0.24f, 0.3f, 0.26f), skin, Quaternion.identity);
            Geo.Box(jumper.transform, new Vector3(-0.06f, 1.98f, -0.12f), new Vector3(0.05f, 0.03f, 0.02f), eyeM, false);
            Geo.Box(jumper.transform, new Vector3(0.06f, 1.98f, -0.12f), new Vector3(0.05f, 0.03f, 0.02f), eyeM, false);
            jumper.SetActive(false);

            // What is left of him, beside the line in front of the cabin.
            body = new GameObject("Body");
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(7.5f, 0.32f, -2.8f);
            Geo.Box(body.transform, new Vector3(0.2f, 0.01f, 0.1f), new Vector3(2.0f, 0.02f, 1.1f), blood, false);
            var torso = Geo.Prim(PrimitiveType.Capsule, body.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.42f, 0.8f, 0.32f), uniform, Quaternion.Euler(0f, 0f, 90f), true);
            Geo.Prim(PrimitiveType.Sphere, body.transform, new Vector3(-0.98f, 0.18f, 0.05f), new Vector3(0.24f, 0.22f, 0.28f), dead, Quaternion.identity);
            Geo.Box(body.transform, new Vector3(0.1f, 0.12f, 0.42f), new Vector3(0.75f, 0.1f, 0.1f), uniform, false, 35f);
            Geo.Box(body.transform, new Vector3(0.45f, 0.1f, 0.6f), new Vector3(0.14f, 0.06f, 0.1f), dead, false);
            Interactable.Add(torso, () => "[E] Look at him", ExamineBody).available = () => !bodyExamined;
            body.SetActive(false);

            // The face that rises at the window.
            peeper = new GameObject("Peeper");
            peeper.transform.SetParent(root, false);
            Geo.Prim(PrimitiveType.Sphere, peeper.transform, Vector3.zero, new Vector3(0.3f, 0.38f, 0.28f), skin, Quaternion.identity);
            Geo.Prim(PrimitiveType.Sphere, peeper.transform, new Vector3(0f, 0.09f, 0.05f), new Vector3(0.33f, 0.32f, 0.3f), black, Quaternion.identity);
            Geo.Box(peeper.transform, new Vector3(-0.065f, 0.02f, -0.13f), new Vector3(0.06f, 0.04f, 0.02f), eyeM, false);
            Geo.Box(peeper.transform, new Vector3(0.065f, 0.02f, -0.13f), new Vector3(0.06f, 0.04f, 0.02f), eyeM, false);
            Geo.Box(peeper.transform, new Vector3(0f, -0.11f, -0.13f), new Vector3(0.05f, 0.11f, 0.02f), eyeM, false);
            peeper.SetActive(false);
            peepHands = new GameObject("PeepHands");
            peepHands.transform.SetParent(root, false);
            Geo.Box(peepHands.transform, new Vector3(-0.22f, 1.27f, -5.2f), new Vector3(0.1f, 0.05f, 0.17f), skin, false);
            Geo.Box(peepHands.transform, new Vector3(0.22f, 1.27f, -5.2f), new Vector3(0.1f, 0.05f, 0.17f), skin, false);
            peepHands.SetActive(false);

            // Something that runs past the window.
            runner = new GameObject("Runner");
            runner.transform.SetParent(root, false);
            Geo.Prim(PrimitiveType.Capsule, runner.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.45f, 1.05f, 0.4f), black, Quaternion.identity);
            Geo.Prim(PrimitiveType.Sphere, runner.transform, new Vector3(0.1f, 2.25f, 0f), new Vector3(0.28f, 0.32f, 0.28f), black, Quaternion.identity);
            runner.SetActive(false);

            // Handprints on the outside of the glass.
            handprints = new GameObject("Handprints");
            handprints.transform.SetParent(root, false);
            var print = Mats.Unlit(new Color(0.27f, 0.26f, 0.24f));
            var rnd = new System.Random(1411);
            for (int i = 0; i < 9; i++)
            {
                float x = -2.5f + (float)rnd.NextDouble() * 3.4f, y = 1.35f + (float)rnd.NextDouble() * 0.95f;
                Geo.Box(handprints.transform, new Vector3(x, y, -5.33f), new Vector3(0.1f, 0.13f, 0.01f), print, false, 0f);
                for (int f = 0; f < 4; f++)
                    Geo.Box(handprints.transform, new Vector3(x - 0.045f + f * 0.03f, y + 0.1f, -5.33f), new Vector3(0.018f, 0.07f, 0.01f), print, false);
            }
            handprints.SetActive(false);

            // The crowd on the other side.
            crowdRoot = new GameObject("TheOtherSide");
            crowdRoot.transform.SetParent(root, false);
            var cr = new System.Random(310);
            for (int i = 0; i < 34; i++)
            {
                float a = (float)cr.NextDouble() * Mathf.PI * 2f, r = 9f + (float)cr.NextDouble() * 18f;
                var p = new Vector3(Mathf.Cos(a) * r, 0.35f, -8f + Mathf.Sin(a) * r);
                var f = new GameObject("Them").transform;
                f.SetParent(crowdRoot.transform, false);
                f.localPosition = p;
                Geo.Prim(PrimitiveType.Capsule, f, new Vector3(0f, 0.95f, 0f), new Vector3(0.45f, 0.85f, 0.35f), black, Quaternion.identity);
                Geo.Prim(PrimitiveType.Sphere, f, new Vector3(0f, 1.95f, 0f), new Vector3(0.26f, 0.32f, 0.28f), black, Quaternion.identity);
                Geo.Box(f, new Vector3(-0.06f, 1.98f, 0.13f), new Vector3(0.04f, 0.025f, 0.02f), glow, false);
                Geo.Box(f, new Vector3(0.06f, 1.98f, 0.13f), new Vector3(0.04f, 0.025f, 0.02f), glow, false);
                crowd.Add(f);
                crowdStart.Add(p);
            }
            crowdRoot.SetActive(false);

            // The hanging man: tied by the ankles to the cabin roof beam, hanging head-down.
            var shirt = Mats.Lit(new Color(0.46f, 0.41f, 0.29f));
            var trousers = Mats.Lit(new Color(0.18f, 0.16f, 0.13f));
            var ropeM = Mats.Lit(new Color(0.42f, 0.36f, 0.24f));
            var corpse = Mats.Lit(new Color(0.52f, 0.54f, 0.48f));
            hanger = new GameObject("Hanger");
            hanger.transform.SetParent(root, false);
            hanger.transform.localPosition = new Vector3(-0.9f, 3.33f, -7.7f);
            hanger.transform.localRotation = Quaternion.Euler(0f, 52f, 0f);
            var ht = hanger.transform;
            Geo.Box(ht, new Vector3(0f, -0.14f, 0f), new Vector3(0.035f, 0.28f, 0.035f), ropeM, false);
            Geo.Box(ht, new Vector3(0f, -0.3f, 0f), new Vector3(0.22f, 0.08f, 0.14f), ropeM, false);
            Geo.Box(ht, new Vector3(-0.07f, -0.66f, 0f), new Vector3(0.13f, 0.74f, 0.15f), trousers, false);
            Geo.Box(ht, new Vector3(0.08f, -0.66f, 0.01f), new Vector3(0.13f, 0.74f, 0.15f), trousers, false, 4f);
            Geo.Prim(PrimitiveType.Capsule, ht, new Vector3(0f, -1.38f, 0f), new Vector3(0.44f, 0.42f, 0.3f), shirt, Quaternion.identity);
            Geo.Box(ht, new Vector3(-0.27f, -1.86f, 0.02f), new Vector3(0.09f, 0.78f, 0.09f), shirt, false, 0f);
            Geo.Box(ht, new Vector3(0.28f, -1.84f, -0.02f), new Vector3(0.09f, 0.76f, 0.09f), shirt, false, 0f);
            Geo.Box(ht, new Vector3(-0.27f, -2.3f, 0.02f), new Vector3(0.08f, 0.14f, 0.11f), corpse, false);
            Geo.Box(ht, new Vector3(0.28f, -2.27f, -0.02f), new Vector3(0.08f, 0.14f, 0.11f), corpse, false);
            var hh = new GameObject("Head").transform;
            hh.SetParent(ht, false);
            hh.localPosition = new Vector3(0f, -1.98f, 0f);
            hangerHead = hh.gameObject;
            Geo.Prim(PrimitiveType.Sphere, hh, Vector3.zero, new Vector3(0.27f, 0.31f, 0.28f), corpse, Quaternion.identity);
            Geo.Box(hh, new Vector3(-0.065f, 0.04f, 0.13f), new Vector3(0.07f, 0.05f, 0.03f), eyeM, false);
            Geo.Box(hh, new Vector3(0.065f, 0.04f, 0.13f), new Vector3(0.07f, 0.05f, 0.03f), eyeM, false);
            Geo.Box(hh, new Vector3(0f, -0.08f, 0.13f), new Vector3(0.09f, 0.08f, 0.03f), eyeM, false);
            Geo.Box(hh, new Vector3(0f, -0.2f, 0f), new Vector3(0.25f, 0.18f, 0.25f), black, false);
            Geo.Box(hh, new Vector3(0f, -0.08f, 0.135f), new Vector3(0.05f, 0.25f, 0.01f), blood, false);
            ropeAudio = Geo.Audio(ht, Vector3.zero, Synth.Rope(), 0.9f, false, true, 12f);
            ropeAudio.loop = true;
            hanger.SetActive(false);
            ropeStub = Geo.Box(root, new Vector3(-0.9f, 3.2f, -7.7f), new Vector3(0.035f, 0.26f, 0.035f), ropeM, false);
            ropeStub.SetActive(false);

            // The thing that stands behind you while you read.
            behind = new GameObject("Behind");
            behind.transform.SetParent(root, false);
            Geo.Prim(PrimitiveType.Capsule, behind.transform, new Vector3(0f, 1.05f, 0f), new Vector3(0.5f, 1.05f, 0.36f), black, Quaternion.identity);
            var bh = new GameObject("Head").transform;
            bh.SetParent(behind.transform, false);
            bh.localPosition = new Vector3(0f, 2.22f, 0.03f);
            behindHead = bh.gameObject;
            Geo.Prim(PrimitiveType.Sphere, bh, Vector3.zero, new Vector3(0.27f, 0.36f, 0.27f), Mats.Unlit(new Color(0.55f, 0.55f, 0.5f)), Quaternion.identity);
            Geo.Box(bh, new Vector3(-0.065f, 0.03f, 0.125f), new Vector3(0.07f, 0.06f, 0.03f), glow, false);
            Geo.Box(bh, new Vector3(0.065f, 0.03f, 0.125f), new Vector3(0.07f, 0.06f, 0.03f), glow, false);
            Geo.Box(bh, new Vector3(0f, -0.12f, 0.125f), new Vector3(0.08f, 0.15f, 0.03f), eyeM, false);
            Geo.Box(bh, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.2f, 0.3f), black, false);
            behind.SetActive(false);

            heart = Geo.Audio(root, Vector3.zero, Synth.Heart(), 0.9f, false, false);
            heart.loop = true;
            UpgradeModels();
        }

        static void Clear(Transform t, int keep = 0)
        {
            for (int i = t.childCount - 1; i >= keep; i--) Destroy(t.GetChild(i).gameObject);
        }

        /// Swap the primitive stand-ins for the animated characters, when the model pack is present.
        void UpgradeModels()
        {
            if (!Resources.Load<GameObject>("ThirdParty/QuaterniusZombies/Characters_Matt")) return;
            var khaki = new Color(0.78f, 0.7f, 0.5f);
            var drownedC = new Color(0.55f, 0.62f, 0.6f);

            // Velan, walking onto the line.
            Clear(jumper.transform);
            velan = Actor.Spawn("Characters_Matt", jumper.transform, Vector3.zero, 0f, 1.62f, new Color(0.95f, 0.92f, 0.88f), "Walk", 0.3f);

            // Velan's body by the line.
            var blood = body.transform.GetChild(0).gameObject;
            Clear(body.transform, 1);
            velanBody = Actor.Spawn("Characters_Matt", body.transform, new Vector3(0f, 0.18f, 0f), 80f, 1.62f, new Color(0.85f, 0.8f, 0.78f), "Idle", 0.15f);
            velanBody.Pose("Idle", 0.3f);
            velanBody.transform.localRotation = Quaternion.Euler(-90f, 80f, 0f);
            var hit = new GameObject("Hit"); hit.transform.SetParent(body.transform, false);
            var bc = hit.AddComponent<BoxCollider>(); bc.size = new Vector3(2f, 0.8f, 1.4f); bc.center = new Vector3(0f, 0.3f, 0f); bc.isTrigger = true;
            Interactable.Add(hit, () => "[E] Look at him", ExamineBody).available = () => !bodyExamined;

            // A drowned passenger at the window (peeper origin = its head).
            Clear(peeper.transform);
            Clear(peepHands.transform);
            drowned = Actor.Spawn("Zombie_Arm", peeper.transform, new Vector3(0f, -1.58f, 0f), 180f, 1.58f, drownedC, "Idle", 0.3f, true);

            // Something running past the window.
            Clear(runner.transform);
            runnerA = Actor.Spawn("Zombie_Basic", runner.transform, Vector3.zero, 90f, 1.6f, drownedC * 0.6f, "Run", 0.1f, true);

            // The 212, standing at the river.
            string[] kinds = { "Zombie_Basic", "Zombie_Chubby", "Zombie_Arm", "Zombie_Basic", "Characters_Lis", "Characters_Shaun" };
            for (int i = 0; i < crowd.Count; i++)
            {
                Clear(crowd[i]);
                var a = Actor.Spawn(kinds[i % kinds.Length], crowd[i], Vector3.zero, 0f, Random.Range(1.5f, 1.7f), drownedC * 0.45f, "Idle", 0.25f, true);
                if (a) crowdA.Add(a);
            }

            // Anand, hanging by his ankles from the roof beam. (Jump_Idle arms-up pose, flipped, so the arms dangle.)
            for (int i = hanger.transform.childCount - 1; i >= 2; i--) if (hanger.transform.GetChild(i).GetComponent<AudioSource>() == null) Destroy(hanger.transform.GetChild(i).gameObject);
            anand = Actor.Spawn("Characters_Sam", hanger.transform, Vector3.zero, 0f, 1.6f, new Color(0.8f, 0.78f, 0.75f), "Idle", 0.12f);
            if (anand)
            {
                anand.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                anand.transform.localPosition = new Vector3(0f, -0.32f, 0f);
                anand.Pose("Idle", 0f);
                hangerHead = anand.head ? anand.head.gameObject : hangerHead;
            }

            // Velan's ghost, the one who writes in the register.
            Clear(behind.transform);
            velanGhost = Actor.Spawn("Characters_Matt", behind.transform, Vector3.zero, 0f, 1.66f, new Color(0.62f, 0.66f, 0.62f), "Idle", 0.55f, true);
            if (velanGhost && velanGhost.head) behindHead = velanGhost.head.gameObject;

            // The Walkers: the drowned, coming up the line from the river.
            string[] wk = { "Zombie_Basic", "Zombie_Chubby", "Zombie_Arm", "Zombie_Basic", "Zombie_Arm", "Zombie_Chubby", "Zombie_Basic", "Zombie_Arm", "Zombie_Basic" };
            var wroot = new GameObject("Walkers").transform; wroot.SetParent(transform, false);
            for (int i = 0; i < wk.Length; i++)
            {
                var a = Actor.Spawn(wk[i], wroot, Vector3.zero, 90f, Random.Range(1.5f, 1.68f), drownedC * 0.55f, "Walk", 0.3f, true);
                if (a) { a.gameObject.SetActive(false); walkers.Add(a); }
            }

            // The thing that stands in the river on the other side.
            var cpf = Resources.Load<GameObject>("ThirdParty/PSXHorrorEntity/Creature");
            if (cpf)
            {
                creature = Instantiate(cpf, transform, false);
                creature.transform.localPosition = new Vector3(36f, 0f, 24f);
                creature.transform.localRotation = Quaternion.Euler(0f, 225f, 0f);
                creature.transform.localScale = Vector3.one * 0.85f;
                var ctex = Resources.Load<Texture2D>("ThirdParty/PSXHorrorEntity/Creature_BaseColor");
                foreach (var r in creature.GetComponentsInChildren<Renderer>())
                {
                    var ms = r.materials;
                    for (int i = 0; i < ms.Length; i++)
                    {
                        if (ctex) { if (ms[i].HasProperty("_BaseMap")) ms[i].SetTexture("_BaseMap", ctex); ms[i].mainTexture = ctex; }
                        Mats.SetColor(ms[i], new Color(0.6f, 0.45f, 0.45f));
                    }
                    r.materials = ms;
                }
                var ca = creature.GetComponent<Animation>();
                if (ca && ca.clip) { ca.wrapMode = WrapMode.Loop; ca.Play(); }
                creature.SetActive(false);
            }
        }

        /// Velan steps out of the dark onto the running line, moments before the train reaches him.
        public IEnumerator StepOut()
        {
            jumper.transform.localPosition = new Vector3(JumperX, 0.35f, -2.8f);
            jumper.transform.localRotation = Quaternion.identity;
            jumper.SetActive(true);
            if (velan) velan.Play("Walk", 1.1f);
            float z = -2.8f;
            while (z < 0f && jumper.activeSelf)
            {
                z = Mathf.MoveTowards(z, 0f, 1.7f * Time.deltaTime);
                jumper.transform.localPosition = new Vector3(JumperX, 0.35f, z);
                yield return null;
            }
            if (!jumper.activeSelf) yield break;
            if (velan) velan.Play("Idle", 1f, true, 0f, 0.2f);
            float yaw = 0f;
            while (yaw < 180f && jumper.activeSelf)
            {
                yaw = Mathf.MoveTowards(yaw, 180f, 360f * Time.deltaTime);
                jumper.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                yield return null;
            }
        }

        public static bool PlayerInside()
        {
            var p = Player.I;
            if (!p) return false;
            var q = p.transform.position;
            return q.x > -3.1f && q.x < 3.1f && q.z < -5.4f && q.z > -10.6f;
        }

        public static bool Looking(Vector3 pos, float dot, float maxDist = 60f)
        {
            var p = Player.I;
            if (!p || !p.cam) return false;
            var c = p.cam.transform;
            var to = pos - c.position;
            return to.magnitude < maxDist && Vector3.Dot(c.forward, to.normalized) > dot;
        }

        static bool Over => Director.I && Director.I.Ending;

        // ---------- The man on the line ----------

        public void HitJumper(Train t)
        {
            if (!jumper.activeSelf) return;
            jumper.SetActive(false);
            w.sfx.PlayOneShot(Synth.Thud(), 1f);
            w.Sting();
            t.Horn();
            Hud.I.Flash(new Color(0.55f, 0f, 0f, 0.85f), 0.9f);
            if (Player.I) Player.I.AddShake(1.2f);
            Hud.I.Say("", "The train went straight through him.", 3.5f);
            body.SetActive(true);
        }

        void ExamineBody()
        {
            if (bodyExamined) return;
            bodyExamined = true;
            StartCoroutine(BodyScare());
        }

        IEnumerator BodyScare()
        {
            Hud.I.Say("", "An old khaki signalman's uniform. The brass badge reads: P. VELAN — KALVADI HALT.", 4f);
            yield return new WaitForSeconds(3.6f);
            Hud.I.Say("", "Velan. The signalman from 1981. You reach down to turn him over—", 2.6f);
            yield return new WaitForSeconds(2.4f);
            w.SetLights(false);
            if (Player.I && Player.I.torch) Player.I.torch.enabled = false;
            body.SetActive(false);
            JumpScare.I.Play(0, 1.1f);
            w.sfx.PlayOneShot(Synth.Scream(), 0.6f);
            yield return new WaitForSeconds(1.6f);
            Whisper("...I never touched the lever...");
            yield return new WaitForSeconds(1.8f);
            w.SetLights(true);
            Hud.I.Say("", "He is gone. Not even blood on the stones. Every year, this night, he walks onto the line again.", 5f);
        }

        public IEnumerator VanishBodyUnseen()
        {
            float t = 0f;
            while (body.activeSelf && t < 20f)
            {
                if (!Looking(body.transform.position, 0.3f, 120f)) break;
                t += Time.deltaTime;
                yield return null;
            }
            body.SetActive(false);
        }

        // ---------- The face at the window ----------

        public IEnumerator Peep(float timeout = 30f)
        {
            if (peeperActive || !PlayerInside() || Over) yield break;
            peeperActive = true;
            float x = new[] { -2.0f, -0.75f, 0.5f }[Random.Range(0, 3)];
            float y = 0.8f, top = 1.76f, t = 0f, seen = 0f;
            peeper.transform.localPosition = new Vector3(x, y, -5.02f);
            peepHands.transform.localPosition = new Vector3(x, 0f, 0f);
            peeper.SetActive(true);
            while (t < timeout && !Over)
            {
                float dt = Time.deltaTime;
                t += dt;
                bool look = Looking(peeper.transform.position, 0.94f, 9f);
                if (!look && y < top) y = Mathf.MoveTowards(y, top, 0.3f * dt);
                peeper.transform.localPosition = new Vector3(x, y, -5.02f);
                peepHands.SetActive(y > 1.3f);
                if (look && y > 1.3f)
                {
                    seen += dt;
                    if (seen > 0.35f) break;
                }
                yield return null;
            }
            if (seen > 0.35f && ++peepsSeen % 3 == 0)
            {
                peepHands.SetActive(false);
                peeper.SetActive(false);
                JumpScare.I.Play(1, 0.8f);
                w.PlayAt(Synth.Slam(), peeper.transform.position, 1f);
                peeperActive = false;
                yield break;
            }
            if (seen > 0.35f && !peepNoted)
            {
                peepNoted = true;
                Hud.I.Say("", "(Grey skin. Wet hair. River water running down the glass.)", 4f);
            }
            if (seen > 0.35f)
            {
                w.Sting();
                Hud.I.Flash(new Color(1f, 1f, 1f, 0.25f), 0.15f);
                StartCoroutine(w.Flicker(0.4f));
                peepHands.SetActive(false);
                while (y > 0.6f) { y -= 9f * Time.deltaTime; peeper.transform.localPosition = new Vector3(x, y, -5.02f); yield return null; }
            }
            else
            {
                peepHands.SetActive(false);
                while (y > 0.6f) { y -= 0.6f * Time.deltaTime; peeper.transform.localPosition = new Vector3(x, y, -5.02f); yield return null; }
            }
            peeper.SetActive(false);
            peeperActive = false;
        }

        // ---------- Small scares ----------

        public IEnumerator RunPast()
        {
            if (!PlayerInside() || Over) yield break;
            runner.SetActive(true);
            float x = -7f, step = 0f;
            while (x < 7f)
            {
                x += 8f * Time.deltaTime;
                runner.transform.localPosition = new Vector3(x, 0.35f, -4.25f);
                step -= Time.deltaTime;
                if (step <= 0f) { step = 0.22f; w.PlayAt(Synth.Step(), runner.transform.position, 1f); }
                yield return null;
            }
            runner.SetActive(false);
            Hud.I.Say("", "(Running footsteps outside the window.)", 2.5f);
        }

        public IEnumerator RoofSteps()
        {
            Hud.I.Say("", "(Footsteps. On the roof.)", 3f);
            for (int i = 0; i < 7 && !Over; i++)
            {
                w.PlayAt(Synth.Knock(), new Vector3(-2.6f + i * 0.85f, 3.6f, -8.3f + Mathf.Sin(i) * 0.6f), 0.55f);
                yield return new WaitForSeconds(0.55f + Random.Range(0f, 0.2f));
            }
            yield return new WaitForSeconds(1.5f);
            w.PlayAt(Synth.Knock(), new Vector3(2.5f, 3.6f, -6f), 0.9f);
        }

        public void Whisper(string text = null)
        {
            var p = Player.I;
            if (!p || !p.cam) return;
            var c = p.cam.transform;
            text = text ?? WhisperLines[Random.Range(0, WhisperLines.Length)];
            var pos = c.position - c.forward * 0.6f + c.right * 0.25f;
            var vc = Voice.Get(Voice.Whisper, text);
            if (vc) AudioSource.PlayClipAtPoint(vc, pos, 1f); else w.PlayAt(Synth.Whisper(), pos, 1f);
            Hud.I.Say("???", text, vc ? vc.length + 0.4f : 2.8f);
        }

        public void Scream()
        {
            w.sfx.PlayOneShot(Synth.Scream(), 0.3f);
            Hud.I.Say("", "(A scream, far out in the trees.)", 3f);
        }

        public bool DoorOpens()
        {
            if (w.door.open) return false;
            w.door.speed = 30f;
            w.door.open = true;
            w.PlayAt(Synth.Creak(), w.door.transform.position, 0.9f);
            Hud.I.Say("", "(The door creaks open. Nobody is there.)", 3f);
            return true;
        }

        public void ShowHands() => handprints.SetActive(true);

        /// The radio bulletin's warning, made real: for `secs` the drowned walk past and beg at the door.
        /// Opening the door is death (restart from the checkpoint).
        public IEnumerator Walkers(float secs)
        {
            doorFailed = false;
            if (w.door.open)
            {
                w.door.speed = 900f; w.door.open = false;
                w.sfx.PlayOneShot(Synth.Slam(), 0.8f);
                yield return new WaitForSeconds(0.3f);
                w.door.speed = 260f;
            }
            // Seven walk along the line toward the Junction; two come to your door.
            var door = new Vector3(2.1f, 0.05f, -4.05f);
            var starts = new List<Vector3>();
            for (int i = 0; i < walkers.Count; i++)
            {
                var a = walkers[i];
                var p = i < 2 ? new Vector3(-12f - i * 3f, 0.35f, -1.2f) : new Vector3(-16f - (i - 2) * 4.5f, 0.35f, Random.Range(-1.4f, 1.4f));
                starts.Add(p);
                a.transform.localPosition = p;
                a.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                a.gameObject.SetActive(true);
                a.anim.enabled = true;
                a.Play("Walk", Random.Range(0.55f, 0.75f), true, Random.value);
            }
            var pleas = Pleas;
            float t = 0f, knockT = 4f;
            int plea = 0;
            bool peeped = false;
            while (t < secs && !Over)
            {
                float dt = Time.deltaTime;
                t += dt;
                int left = Mathf.CeilToInt(secs - t);
                Director.I.objective = "DO NOT OPEN THE DOOR.   Stay quiet.   0:" + left.ToString("00");
                for (int i = 0; i < walkers.Count; i++)
                {
                    var a = walkers[i];
                    var lp = a.transform.localPosition;
                    if (i < 2)
                    {
                        var target = door + new Vector3(i * 0.7f - 0.35f, 0f, i * 0.4f);
                        var flat = new Vector3(target.x - lp.x, 0f, target.z - lp.z);
                        if (flat.magnitude > 0.15f)
                        {
                            a.transform.localPosition = Vector3.MoveTowards(lp, new Vector3(target.x, lp.y, target.z), 0.9f * dt);
                            a.transform.localRotation = Quaternion.LookRotation(flat);
                            if (lp.z < -2.5f && lp.y > 0.1f) a.transform.localPosition = new Vector3(a.transform.localPosition.x, Mathf.MoveTowards(lp.y, 0.05f, dt), a.transform.localPosition.z);
                        }
                        else { a.Play("Idle", 0.6f, true, 0f, 0.3f); a.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); }
                    }
                    else a.transform.localPosition = lp + Vector3.right * 0.8f * dt;
                }
                knockT -= dt;
                if (knockT <= 0f && t > 12f)
                {
                    knockT = Random.Range(4.5f, 7f);
                    StartCoroutine(KnockBurst());
                }
                if (plea < pleas.Length && t > 16f + plea * 11f)
                {
                    var bits = pleas[plea++].Split('|');
                    Director.I.Speak(bits[0], bits[1], 3.5f);
                    w.door.rattle = 1.2f;
                }
                if (!peeped && t > 30f && PlayerInside()) { peeped = true; StartCoroutine(Peep(14f)); }
                if (w.door.open) { doorFailed = true; break; }
                yield return null;
            }
            if (doorFailed && !Over)
            {
                foreach (var a in walkers) if (a.transform.localPosition.z < -3f) a.Play("Run", 1.4f);
                w.sfx.PlayOneShot(Synth.Boom(), 1f);
                yield return new WaitForSeconds(0.35f);
                JumpScare.I.Play(0, 1.3f);
                w.sfx.PlayOneShot(Synth.Scream(), 0.8f);
                yield return new WaitForSeconds(1.1f);
                Director.I.Die("YOU OPENED THE DOOR", "It was not Raghavan. It was never Raghavan.\n\nThe river does not give back what it takes.");
                yield break;
            }
            // They turn back to the river.
            Hud.I.Say("", "The knocking stops. Wet footsteps, walking away toward the river.", 4f);
            foreach (var a in walkers) { a.Play("Walk", 0.7f); a.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); }
            float f = 0f;
            while (f < 5f) { f += Time.deltaTime; foreach (var a in walkers) a.transform.localPosition += Vector3.left * 1.1f * Time.deltaTime; yield return null; }
            foreach (var a in walkers) a.gameObject.SetActive(false);
        }

        IEnumerator KnockBurst()
        {
            w.door.rattle = 0.9f;
            int n = Random.Range(2, 5);
            for (int i = 0; i < n; i++) { w.knock.Play(); yield return new WaitForSeconds(Random.Range(0.35f, 0.6f)); }
        }

        public IEnumerator DoorSlam()
        {
            if (Over) yield break;
            if (!w.door.open)
            {
                w.door.speed = 45f;
                w.door.open = true;
                w.PlayAt(Synth.Creak(), w.door.transform.position, 1f);
                yield return new WaitForSeconds(2.4f);
            }
            w.door.speed = 1400f;
            w.door.open = false;
            w.sfx.PlayOneShot(Synth.Slam(), 1f);
            w.PlayAt(Synth.Slam(), w.door.transform.position, 1f);
            if (Player.I) Player.I.AddShake(0.6f);
            StartCoroutine(w.Flicker(0.5f));
            Hud.I.Say("", "(The door slams shut.)", 2.5f);
            yield return new WaitForSeconds(0.3f);
            w.door.speed = 260f;
        }

        /// The cabin goes dark while you are outside. Something is hanging from the roof when you come back.
        public IEnumerator HangerScene()
        {
            if (hangerDone || Over) yield break;
            hangerDone = true;
            w.SetLights(false);
            w.PlayAt(Synth.Boom(), new Vector3(0f, 3f, -8f), 0.5f);
            Hud.I.Say("", PlayerInside() ? "(The bulb dies. Something creaks above you.)" : "(Behind you, the cabin lights go out.)", 3.5f);
            hanger.SetActive(true);
            ropeAudio.Play();
            float look = 0f, inside = 0f, t = 0f;
            var cam = Player.I.cam.transform;
            while (!Over && t < 150f)
            {
                t += Time.deltaTime;
                float sw = Mathf.Sin(Time.time * 0.9f);
                hanger.transform.localRotation = Quaternion.Euler(sw * 4f, 52f + sw * 18f, Mathf.Sin(Time.time * 0.6f) * 3f);
                var hp = hangerHead.transform.position;
                float d = Vector3.Distance(cam.position, hp);
                bool lookAt = Looking(hp, 0.9f, 5f);
                if (PlayerInside()) inside += Time.deltaTime;
                if (lookAt && PlayerInside()) look += Time.deltaTime; else look = Mathf.Max(0f, look - Time.deltaTime * 0.5f);
                if (!anandNoted && look > 0.2f)
                {
                    anandNoted = true;
                    Hud.I.Say("", "A signalman, hanging by his ankles. The name stitched on his shirt: S. ANAND.", 4f);
                }
                if (d < 1.6f || look > 1.4f || inside > 45f) break;
                yield return null;
            }
            if (Over) yield break;
            // It comes at you.
            ropeAudio.Stop();
            w.sfx.PlayOneShot(Synth.Thud(), 1f);
            var startPos = hanger.transform.position;
            float k = 0f;
            while (k < 1f)
            {
                k += Time.deltaTime / 0.18f;
                var target = cam.position + cam.forward * 0.35f;
                var off = target - hangerHead.transform.position;
                hanger.transform.position += off * Mathf.Clamp01(k);
                yield return null;
            }
            JumpScare.I.Play(0, 1.0f);
            w.sfx.PlayOneShot(Synth.Scream(), 0.7f);
            yield return new WaitForSeconds(0.25f);
            hanger.SetActive(false);
            hanger.transform.position = startPos;
            ropeStub.SetActive(true);
            yield return new WaitForSeconds(1.8f);
            w.SetLights(true);
            Hud.I.Say("", "Nothing is hanging from the roof. Only cut rope, still swinging. Anand never left Kalvadi.", 5f);
        }

        /// The next time you read the register or the notice, something is standing behind you when you close it.
        public void ArmBehind() { behindArmed = true; }

        IEnumerator BehindScene()
        {
            var cam = Player.I.cam.transform;
            var f = cam.forward; f.y = 0f; f.Normalize();
            var p = Player.I.transform.position - f * 0.95f;
            p.y = 0.35f;
            behind.transform.position = p;
            behind.transform.rotation = Quaternion.LookRotation(f);
            behind.SetActive(true);
            w.PlayAt(Synth.Breath(), behindHead.transform.position, 0.8f);
            Hud.I.Say("", "(Breathing. Right behind you.)", 3f);
            float t = 0f;
            while (!Over && t < 25f)
            {
                t += Time.deltaTime;
                if (Looking(behindHead.transform.position, 0.55f, 4f)) break;
                yield return null;
            }
            if (!Over && t < 25f)
            {
                JumpScare.I.Play(1, 0.9f);
                yield return new WaitForSeconds(0.2f);
            }
            else Whisper("...too slow...");
            behind.SetActive(false);
            Director.I.AddRegisterLine("I NEVER TOUCHED THE LEVER.  I NEVER TOUCHED THE LEVER.      (the same hand as 1981)");
            yield return new WaitForSeconds(1.2f);
            Hud.I.Say("", "Someone has written in the register while your back was turned. The handwriting matches 1981.", 5f);
        }

        public void RandomScare()
        {
            if (Over) return;
            bool inside = PlayerInside();
            int r = Random.Range(0, inside ? 8 : 5);
            if (!inside && r == 4) r = 7;
            switch (r)
            {
                case 0: Whisper(); break;
                case 1: Scream(); break;
                case 2: StartCoroutine(w.Flicker(1.2f)); break;
                case 3: if (Director.I) StartCoroutine(Director.I.GhostRing()); break;
                case 4: StartCoroutine(Peep()); break;
                case 5: StartCoroutine(RunPast()); break;
                case 6: StartCoroutine(RoofSteps()); break;
                case 7: StartCoroutine(DoorSlam()); break;
            }
        }

        // ---------- The other side ----------

        void SetOtherSide(bool on)
        {
            if (on == w.otherSide) return;
            var cam = Player.I ? Player.I.cam : null;
            if (on)
            {
                fogC = RenderSettings.fogColor; fogD = RenderSettings.fogDensity; ambC = RenderSettings.ambientLight;
                bgC = cam ? cam.backgroundColor : fogC;
                cabC = w.cabinLight.color; lampC = w.lampLight.color; deskC = w.deskLight.color;
                moonI = w.moon ? w.moon.intensity : 0f;

                var red = new Color(0.17f, 0.01f, 0.01f);
                RenderSettings.fogColor = red;
                RenderSettings.fogDensity = 0.045f;
                RenderSettings.ambientLight = new Color(0.16f, 0.02f, 0.02f);
                if (cam) cam.backgroundColor = red;
                w.cabinLight.color = new Color(1f, 0.08f, 0.04f);
                w.lampLight.color = new Color(1f, 0.05f, 0.03f);
                w.deskLight.color = new Color(1f, 0.1f, 0.05f);
                if (w.moon) w.moon.intensity = 0f;
                for (int i = 0; i < crowd.Count; i++) crowd[i].localPosition = crowdStart[i];
                crowdRoot.SetActive(true);
                if (creature) creature.SetActive(true);
                heart.Play();
            }
            else
            {
                RenderSettings.fogColor = fogC; RenderSettings.fogDensity = fogD; RenderSettings.ambientLight = ambC;
                if (cam) cam.backgroundColor = bgC;
                w.cabinLight.color = cabC; w.lampLight.color = lampC; w.deskLight.color = deskC;
                if (w.moon) w.moon.intensity = moonI;
                crowdRoot.SetActive(false);
                if (creature) creature.SetActive(false);
                heart.Stop();
            }
            w.otherSide = on;
            w.clockSpin = on;
        }

        /// Slips the whole world into the red other side. A glimpse lasts a heartbeat; the full version is the 02:35 set-piece.
        public IEnumerator OtherSide(float secs, bool glimpse)
        {
            if (Over || w.otherSide) yield break;
            w.SetLights(false);
            w.Sting();
            if (!glimpse)
            {
                Hud.I.Say("", "The lights die.", 2f);
                yield return new WaitForSeconds(1.8f);
            }
            SetOtherSide(true);
            w.SetLights(true);
            creep = glimpse ? 0f : 0.55f;
            foreach (var a in crowdA) if (a) { if (creep > 0f) a.Play("Walk", 0.55f, true, Random.value); else a.Play("Idle", 1f, true, Random.value); }
            Hud.I.Flash(new Color(0.6f, 0f, 0f, 0.8f), glimpse ? 0.3f : 0.7f);
            if (!glimpse) Hud.I.Say("", "14 November 1981. The night of the crash. The 212 are still standing by the river.", 5f);
            float t = 0f;
            bool saidMore = false;
            while (t < secs && !Over)
            {
                t += Time.deltaTime;
                if (!glimpse && !saidMore && t > secs * 0.45f)
                {
                    saidMore = true;
                    Director.I.Speak("PASSENGERS", "Signalman. We have been standing here since 1981.", 4f);
                    if (Director.I) StartCoroutine(Director.I.GhostRing());
                }
                if (!glimpse && Random.value < 0.006f) StartCoroutine(w.Flicker(0.25f));
                yield return null;
            }
            if (!glimpse && !Over)
            {
                JumpScare.I.Play(2, 0.85f);
                yield return new WaitForSeconds(0.8f);
            }
            w.SetLights(false);
            SetOtherSide(false);
            yield return new WaitForSeconds(glimpse ? 0.25f : 1.2f);
            w.SetLights(true);
        }

        void Update()
        {
            if (Hud.I)
            {
                var st = Hud.I.State;
                if (behindArmed && lastHud == HudState.Reading && st == HudState.Playing && PlayerInside() && !Over)
                {
                    behindArmed = false;
                    StartCoroutine(BehindScene());
                }
                lastHud = st;
            }
            if (!w || !w.otherSide || !Player.I) return;
            var pp = Player.I.transform.position;
            var centre = new Vector3(0f, 0f, -8f);
            foreach (var f in crowd)
            {
                var to = pp - f.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) f.rotation = Quaternion.LookRotation(to);
                var flat = new Vector3(f.position.x, 0f, f.position.z);
                if (creep > 0f && (flat - centre).magnitude > 6.2f)
                {
                    var tgt = new Vector3(centre.x, f.position.y, centre.z);
                    f.position = Vector3.MoveTowards(f.position, tgt, creep * Time.deltaTime);
                }
            }
        }
    }
}
