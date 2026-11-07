using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    public enum TrainKind { Express, Goods, Passenger, Ghost, ThreeTen }

    /// A train of box-built cars following a Route. Stops at the home signal when it shows danger.
    public class Train : MonoBehaviour
    {
        public static readonly List<Train> All = new List<Train>();

        class Car { public Transform t; public float off, len; public Vector3 half; }
        readonly List<Car> cars = new List<Car>();
        readonly List<GameObject> doors = new List<GameObject>();

        public World w;
        public Route route;
        public TrainKind kind;
        public string id;
        public float s, speed, cruise, totalLen;
        public bool living, ghost, decided, toBranch, done, stopped, doorsOpen;
        public System.Action<Train> onDecided;

        AudioSource rumble, horn;
        bool approachHorn;
        float hornCd = 8f;
        Material doorClosed, doorOpen;

        const float Accel = 0.8f, Brake = 1.6f;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static Train Spawn(World w, TrainKind kind, string id, float cruiseOverride = 0f)
        {
            var go = new GameObject("Train " + id);
            go.transform.SetParent(w.transform, false);
            var t = go.AddComponent<Train>();
            t.w = w;
            t.kind = kind;
            t.id = id;
            t.route = w.mainRoute;
            t.Build();
            if (cruiseOverride > 0f) t.cruise = t.speed = cruiseOverride;
            t.Place();
            return t;
        }

        public float FrontX() => route.Pos(s).x;
        public float TailX() => route.Pos(s - totalLen).x;

        void Build()
        {
            Color loco, body, win;
            int count; float len, sil; bool windows = true;
            switch (kind)
            {
                case TrainKind.Express:
                    loco = new Color(0.5f, 0.1f, 0.07f); body = new Color(0.13f, 0.22f, 0.45f); win = new Color(1f, 0.85f, 0.55f);
                    count = 8; len = 16f; sil = 0.3f; cruise = 18f; living = true; s = 0f; break;
                case TrainKind.Goods:
                    loco = new Color(0.36f, 0.13f, 0.06f); body = new Color(0.27f, 0.14f, 0.08f); win = Color.black;
                    count = 12; len = 10f; sil = 0f; windows = false; cruise = 12f; living = true; s = 140f; break;
                case TrainKind.Passenger:
                    loco = new Color(0.14f, 0.2f, 0.38f); body = new Color(0.34f, 0.08f, 0.06f); win = new Color(1f, 0.8f, 0.45f);
                    count = 6; len = 16f; sil = 0.9f; cruise = 13f; living = true; s = 120f; break;
                case TrainKind.Ghost:
                    loco = new Color(0.035f, 0.035f, 0.04f); body = new Color(0.15f, 0.06f, 0.05f); win = new Color(0.45f, 0.75f, 0.62f);
                    count = 6; len = 16f; sil = 0.95f; cruise = 9f; ghost = true; s = 260f; break;
                default:
                    loco = new Color(0.035f, 0.035f, 0.04f); body = new Color(0.12f, 0.05f, 0.05f); win = new Color(0.55f, 0.8f, 0.7f);
                    count = 5; len = 16f; sil = 1f; cruise = 8f; ghost = true; s = 300f; break;
            }
            speed = cruise;
            doorClosed = Mats.Lit(body * 0.8f);
            doorOpen = Mats.Unlit(new Color(0.75f, 0.9f, 0.8f));

            float off = 0f;
            AddCar(true, 17f, loco, win, 0f, false, off);
            off += 17.8f;
            for (int i = 0; i < count; i++)
            {
                AddCar(false, len, body, win, sil, windows, off);
                off += len + 0.8f;
            }
            totalLen = off;

            rumble = Geo.Audio(cars[0].t, Vector3.zero, Synth.Rumble(), 0.6f, true, true, 200f);
            horn = Geo.Audio(cars[0].t, new Vector3(0f, 4f, 6f), Synth.Horn(), 1f, false, true, 420f);
            if (ghost) { horn.pitch = 0.55f; rumble.pitch = 0.6f; }
        }

        void AddCar(bool isLoco, float len, Color bodyC, Color winC, float sil, bool windows, float off)
        {
            var root = new GameObject(isLoco ? "Loco" : "Coach").transform;
            root.SetParent(transform, false);
            float h = isLoco ? 3.9f : 3.5f;
            Geo.Box(root, new Vector3(0f, 0.75f + h * 0.5f, 0f), new Vector3(3.1f, h, len), Mats.Lit(bodyC, 0.3f), true, 0f, "Body");
            Geo.Box(root, new Vector3(0f, 0.83f + h, 0f), new Vector3(2.8f, 0.16f, len - 0.4f), Mats.Lit(bodyC * 0.55f), false);
            var dark = Mats.Lit(new Color(0.04f, 0.04f, 0.04f));
            Geo.Box(root, new Vector3(0f, 0.45f, len * 0.5f - 2.6f), new Vector3(2.5f, 0.7f, 3f), dark, false);
            Geo.Box(root, new Vector3(0f, 0.45f, -len * 0.5f + 2.6f), new Vector3(2.5f, 0.7f, 3f), dark, false);

            if (isLoco)
            {
                Color lamp = ghost ? new Color(0.7f, 0.85f, 0.75f) : new Color(1f, 0.95f, 0.75f);
                Geo.Box(root, new Vector3(0f, 3.3f, len * 0.5f + 0.01f), new Vector3(2.4f, 0.9f, 0.05f), Mats.Lit(new Color(0.03f, 0.04f, 0.05f), 0.9f), false);
                Geo.Box(root, new Vector3(0f, 4.35f, len * 0.5f + 0.03f), new Vector3(0.5f, 0.3f, 0.06f), Mats.Unlit(lamp), false);
                Geo.Box(root, new Vector3(0f, 1.7f, 0f), new Vector3(3.12f, 0.28f, len - 1f), Mats.Lit(ghost ? new Color(0.2f, 0.2f, 0.18f) : new Color(0.85f, 0.78f, 0.55f)), false);
                var lg = new GameObject("Headlight");
                lg.transform.SetParent(root, false);
                lg.transform.localPosition = new Vector3(0f, 4.35f, len * 0.5f + 0.3f);
                var l = lg.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 70f;
                l.spotAngle = 40f;
                l.intensity = ghost ? 3f : 9f;
                l.color = lamp;
                l.shadows = LightShadows.None;
            }
            else if (windows)
            {
                var wm = Mats.Unlit(winC);
                var sm = Mats.Lit(new Color(0.01f, 0.01f, 0.01f));
                for (float z = -len * 0.5f + 2.3f; z <= len * 0.5f - 2.3f; z += 1.5f)
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Geo.Box(root, new Vector3(side * 1.56f, 2.75f, z), new Vector3(0.02f, 0.7f, 0.9f), wm, false);
                        if (Random.value < sil)
                        {
                            float dz = Random.Range(-0.2f, 0.2f);
                            Geo.Box(root, new Vector3(side * 1.59f, 2.55f, z + dz), new Vector3(0.02f, 0.5f, 0.34f), sm, false);
                            Geo.Box(root, new Vector3(side * 1.59f, 2.93f, z + dz), new Vector3(0.02f, 0.24f, 0.2f), sm, false);
                        }
                    }
                if (ghost)
                    for (int e = -1; e <= 1; e += 2)
                    {
                        // Local +X faces the cabin side of the line.
                        var d = Geo.Box(root, new Vector3(1.57f, 2.05f, e * (len * 0.5f - 1.1f)), new Vector3(0.03f, 2.2f, 0.95f), doorClosed, true, 0f, "Door");
                        Interactable.Add(d, () => "[E] Board the train", () => Director.I.Board()).available = () => doorsOpen;
                        doors.Add(d);
                    }
            }
            else
            {
                var rib = Mats.Lit(bodyC * 0.7f);
                for (float z = -len * 0.5f + 1f; z < len * 0.5f; z += 1.6f)
                    Geo.Box(root, new Vector3(0f, 0.75f + h * 0.5f, z), new Vector3(3.16f, h - 0.2f, 0.12f), rib, false);
            }
            cars.Add(new Car { t = root, off = off, len = len, half = new Vector3(1.6f, 2.5f, len * 0.5f) });
        }

        public void Horn() { if (horn) horn.Play(); }

        public void SetDoors(bool open)
        {
            doorsOpen = open;
            foreach (var d in doors)
                if (d) d.GetComponent<Renderer>().sharedMaterial = open ? doorOpen : doorClosed;
        }

        void Update()
        {
            if (done) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool hold = false;
            bool beforeSignal = s < World.SignalS - 0.5f;
            if (!w.SignalClear && beforeSignal)
            {
                float dist = World.SignalStopS - s;
                if (dist < 0.4f)
                {
                    speed = Mathf.MoveTowards(speed, 0f, 6f * dt);
                    hold = true;
                }
                else
                {
                    float req = speed * speed / (2f * dist);
                    if (req > Brake * 0.85f)
                    {
                        speed = Mathf.Max(0f, speed - Mathf.Min(Mathf.Max(req, Brake), 5f) * dt);
                        hold = true;
                    }
                    else if (speed < 3f)
                    {
                        speed = Mathf.Min(3f, speed + Accel * dt);
                        hold = true;
                    }
                }
            }
            if (!hold) speed = Mathf.MoveTowards(speed, cruise, (speed < cruise ? Accel : Brake) * dt);

            stopped = speed < 0.05f && !w.SignalClear && beforeSignal && World.SignalStopS - s < 1.5f;
            if (stopped)
            {
                hornCd -= dt;
                if (hornCd <= 0f) { hornCd = ghost ? 22f : 14f; Horn(); }
            }
            else hornCd = 8f;

            if (!approachHorn && FrontX() > -170f) { approachHorn = true; Horn(); }

            s += speed * dt;

            if (!decided && s >= World.PointsS - 1.5f)
            {
                decided = true;
                toBranch = w.PointsReversed;
                route = toBranch ? w.branchRoute : w.mainRoute;
                onDecided?.Invoke(this);
            }

            Place();

            rumble.volume = 0.25f + 0.75f * Mathf.Clamp01(speed / 15f);
            rumble.pitch = (ghost ? 0.55f : 0.8f) + speed / 40f;

            var p = Player.I;
            if (p)
            {
                Vector3 pp = p.transform.position + Vector3.up * 0.9f;
                float near = 999f;
                foreach (var c in cars)
                {
                    var lp = c.t.InverseTransformPoint(pp);
                    float dx = Mathf.Abs(lp.x) - c.half.x, dz = Mathf.Abs(lp.z) - c.half.z;
                    near = Mathf.Min(near, Mathf.Max(dx, dz, 0f));
                    if (speed > 0.8f && dx < 0.1f && dz < 0.1f && Director.I) Director.I.Struck();
                }
                p.AddShake(Mathf.Clamp01(1f - near / 12f) * Mathf.Clamp01(speed / 12f));
            }

            if (s - totalLen > World.PointsS + 140f) done = true;
        }

        void Place()
        {
            foreach (var c in cars)
            {
                float d = s - c.off - c.len * 0.5f;
                c.t.SetPositionAndRotation(route.Pos(d) + Vector3.up * 0.35f, Quaternion.LookRotation(route.Dir(d), Vector3.up));
            }
        }
    }
}
