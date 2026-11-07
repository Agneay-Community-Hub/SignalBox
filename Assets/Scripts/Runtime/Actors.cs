using System.Collections.Generic;
using UnityEngine;

namespace SignalBox
{
    /// Animated characters from Resources/ThirdParty (Quaternius CC0 pack, legacy animation import).
    /// Each spawn is scaled so its head sits at a believable height, re-tinted, and played by short clip name ("Walk", "Idle"...).
    public class Actor : MonoBehaviour
    {
        public Animation anim;
        public Transform head;
        string current;

        public static Actor Spawn(string model, Transform parent, Vector3 localPos, float yaw, float headHeight, Color tint, string clip = "Idle", float emission = 0f, bool ghost = false)
        {
            var pf = Resources.Load<GameObject>("ThirdParty/QuaterniusZombies/" + model);
            if (!pf) return null;
            var go = Instantiate(pf, parent, false);
            go.name = model;
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var a = go.AddComponent<Actor>();
            a.anim = go.GetComponent<Animation>();
            foreach (var t in go.GetComponentsInChildren<Transform>()) if (t.name == "Head") { a.head = t; break; }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (Props.Contains(r.name)) r.gameObject.SetActive(false);
            var gray = ghost ? Gray() : null;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
                var ms = r.materials;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (gray) { if (ms[i].HasProperty("_BaseMap")) ms[i].SetTexture("_BaseMap", gray); ms[i].mainTexture = gray; }
                    Mats.SetColor(ms[i], tint);
                    if (ms[i].HasProperty("_Smoothness")) ms[i].SetFloat("_Smoothness", 0.25f);
                    if (emission > 0f && ms[i].HasProperty("_EmissionColor"))
                    {
                        ms[i].EnableKeyword("_EMISSION");
                        ms[i].SetColor("_EmissionColor", tint * emission);
                        if (ms[i].HasProperty("_EmissionMap") && ms[i].mainTexture) ms[i].SetTexture("_EmissionMap", ms[i].mainTexture);
                        ms[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    }
                }
                r.materials = ms;
            }
            // Measure standing (Idle, first frame) so the scale never depends on the clip we start in.
            var rot = go.transform.rotation; go.transform.rotation = Quaternion.identity;
            a.Play("Idle", 1f, true, 0f);
            a.anim.Sample();
            float hy = a.head ? a.head.position.y - go.transform.position.y : 1f;
            float k = headHeight / Mathf.Max(0.1f, hy / Mathf.Max(0.0001f, go.transform.lossyScale.y));
            go.transform.localScale = Vector3.one * k;
            go.transform.rotation = rot;
            a.Play(clip, 1f, true, Random.value);
            return a;
        }

        static readonly HashSet<string> Props = new HashSet<string> { "Axe", "Guitar", "Knife", "Pistol", "Rifle", "Shotgun", "SMG", "Spear", "WoodenBat_Barbed", "WoodenBat_Saw", "Shoulder" };
        static Texture2D gray;

        /// The pack's colour atlas, faded to a contrasty monochrome so every character can be tinted like an old photograph.
        static Texture2D Gray()
        {
            if (gray) return gray;
            var src = Resources.Load<Texture2D>("ThirdParty/QuaterniusZombies/Zombie_Atlas");
            if (!src || !src.isReadable) return null;
            var px = src.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                float l = px[i].r * 0.3f + px[i].g * 0.59f + px[i].b * 0.11f;
                l = Mathf.Clamp01((l - 0.5f) * 1.35f + 0.55f);
                px[i] = new Color(l, l, l, 1f);
            }
            gray = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            gray.SetPixels(px);
            gray.Apply();
            return gray;
        }

        string Full(string clip)
        {
            if (!anim) return null;
            foreach (AnimationState s in anim) if (s.name == clip || s.name.EndsWith("|" + clip)) return s.name;
            return null;
        }

        /// at: normalised start time (0..1).
        public void Play(string clip, float speed = 1f, bool loop = true, float at = 0f, float fade = 0f)
        {
            var f = Full(clip);
            if (f == null) return;
            var st = anim[f];
            st.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            st.speed = speed;
            if (current != f) st.normalizedTime = at;
            if (fade > 0f && current != null) anim.CrossFade(f, fade); else anim.Play(f);
            current = f;
        }

        /// Freeze on one frame of a clip (e.g. the last frame of Death for a body).
        public void Pose(string clip, float normalized)
        {
            var f = Full(clip);
            if (f == null) return;
            anim.Stop();
            var st = anim[f];
            st.enabled = true; st.weight = 1f; st.normalizedTime = normalized;
            anim.Sample();
            st.enabled = false;
            anim.enabled = false;
            current = null;
        }
    }

    public static class Tex
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        /// A Poly Haven texture (CC0) as a lit material, tiled, with its normal map. Falls back to a flat colour.
        public static Material Mat(string id, Color tint, Vector2 tiling, float smooth = 0.1f)
        {
            string k = id + tint + tiling;
            if (Cache.TryGetValue(k, out var m) && m) return m;
            var diff = Resources.Load<Texture2D>("ThirdParty/PolyHaven/" + id + "_diff_1k");
            if (!diff) return Mats.Lit(tint, smooth);
            m = new Material(Mats.Lit(Color.white, smooth));
            Mats.SetColor(m, tint);
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", diff); m.SetTextureScale("_BaseMap", tiling); }
            m.mainTexture = diff; m.mainTextureScale = tiling;
            var nor = Resources.Load<Texture2D>("ThirdParty/PolyHaven/" + id + "_nor_gl_1k");
            if (nor && m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", nor); m.SetTextureScale("_BumpMap", tiling); m.EnableKeyword("_NORMALMAP"); }
            Cache[k] = m;
            return m;
        }
    }
}
