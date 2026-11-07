using UnityEngine;

namespace SignalBox
{
    /// First-person signalman: walk, look, torch, interact.
    public class Player : MonoBehaviour
    {
        public static Player I;
        public Camera cam;
        public Light torch;
        public Interactable focus;
        public bool frozen;

        CharacterController cc;
        AudioSource feet;
        float yaw, pitch, vy, stepAcc, bobT, shake;

        void Awake()
        {
            I = this;
            // Ignore Raycast layer: the look ray must never hit the player's own capsule.
            gameObject.layer = 2;
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.75f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.875f, 0f);
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;
        }

        public void Init(Camera c)
        {
            cam = c;
            yaw = transform.eulerAngles.y;
            var tg = new GameObject("Torch");
            tg.transform.SetParent(c.transform, false);
            torch = tg.AddComponent<Light>();
            torch.type = LightType.Spot;
            torch.range = 20f;
            torch.spotAngle = 55f;
            torch.intensity = 3.5f;
            torch.color = new Color(1f, 0.92f, 0.75f);
            torch.shadows = LightShadows.None;
            torch.enabled = false;
            feet = Geo.Audio(transform, Vector3.zero, Synth.Step(), 0.25f, false, false);
        }

        public void AddShake(float v) { shake = Mathf.Max(shake, v); }
        public float Yaw { get => yaw; set => yaw = value; }
        public float Pitch { get => pitch; set => pitch = Mathf.Clamp(value, -85f, 85f); }
        public void Teleport(Vector3 p) { cc.enabled = false; transform.position = p; cc.enabled = true; vy = 0f; }

        void Update()
        {
            if (!cam) return;
            bool active = Hud.I && Hud.I.State == HudState.Playing && (Cursor.lockState == CursorLockMode.Locked || Hud.TestMode) && !frozen;

            if (active)
            {
                var l = Inp.Look() * 0.08f * Settings.Sens;
                yaw += l.x;
                pitch = Mathf.Clamp(pitch - l.y * (Settings.InvertY ? -1f : 1f), -85f, 85f);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector2 mv = active ? Inp.Move() : Vector2.zero;
            float spd = Inp.Sprint() ? 4.2f : 2.4f;
            Vector3 wish = transform.forward * mv.y + transform.right * mv.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy -= 15f * Time.deltaTime;
            if (Time.deltaTime > 0f) cc.Move((wish * spd + Vector3.up * vy) * Time.deltaTime);

            float hs = new Vector2(cc.velocity.x, cc.velocity.z).magnitude;
            if (hs > 0.2f && cc.isGrounded)
            {
                bobT += hs * Time.deltaTime * 2.4f;
                stepAcc += hs * Time.deltaTime;
                if (stepAcc > 0.75f)
                {
                    stepAcc = 0f;
                    feet.pitch = Random.Range(0.85f, 1.15f);
                    feet.Play();
                }
            }
            float bob = Mathf.Sin(bobT * Mathf.PI) * 0.035f * Mathf.Clamp01(hs / 2.4f);
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.5f);
            cam.transform.localPosition = new Vector3(0f, 1.6f + bob, 0f) + Random.insideUnitSphere * shake * 0.04f;
            cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            focus = null;
            if (active && Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 2.6f, ~(1 << 2), QueryTriggerInteraction.Collide))
            {
                var it = hit.collider.GetComponentInParent<Interactable>();
                if (it && it.Available) focus = it;
            }
            if (active && focus && Hud.I.ReadyForInput && Inp.Interact()) focus.use?.Invoke();
            if (active && Inp.Torch()) torch.enabled = !torch.enabled;
        }
    }
}
