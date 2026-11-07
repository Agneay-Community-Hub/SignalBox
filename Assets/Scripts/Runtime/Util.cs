using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace SignalBox
{
    /// Runtime material factory. Base materials live in Resources so URP shaders survive build stripping.
    public static class Mats
    {
        static Material _lit, _unlit;
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static Material Fallback()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var m = g.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(g);
            return m;
        }

        static Material BaseLit()
        {
            if (_lit) return _lit;
            _lit = Resources.Load<Material>("SB_Lit");
            if (!_lit)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit");
                _lit = s ? new Material(s) : Fallback();
            }
            return _lit;
        }

        static Material BaseUnlit()
        {
            if (_unlit) return _unlit;
            _unlit = Resources.Load<Material>("SB_Unlit");
            if (!_unlit)
            {
                var s = Shader.Find("Universal Render Pipeline/Unlit");
                _unlit = s ? new Material(s) : BaseLit();
            }
            return _unlit;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static Material Lit(Color c, float smooth = 0.15f)
        {
            string k = "L" + c + smooth;
            if (Cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(BaseLit());
            SetColor(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            Cache[k] = m;
            return m;
        }

        public static Material Unlit(Color c)
        {
            string k = "U" + c;
            if (Cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(BaseUnlit());
            SetColor(m, c);
            Cache[k] = m;
            return m;
        }
    }

    /// Primitive-based geometry helpers.
    public static class Geo
    {
        public static GameObject Box(Transform parent, Vector3 pos, Vector3 size, Material mat, bool collide = true, float yaw = 0f, string name = "Box")
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            g.transform.localScale = size;
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (!collide) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, Quaternion rot, bool collide = false)
        {
            var g = GameObject.CreatePrimitive(type);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = scale;
            var r = g.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (!collide) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }

        /// A box stretched between two world points (used for curved branch-line rails).
        public static GameObject Beam(Transform parent, Vector3 a, Vector3 b, float w, float h, Material mat)
        {
            var d = b - a;
            var g = Box(parent, (a + b) * 0.5f, new Vector3(w, h, d.magnitude), mat, false);
            g.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            return g;
        }

        public static Light Point(Transform parent, Vector3 pos, Color c, float intensity, float range)
        {
            var g = new GameObject("Light");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        public static AudioSource Audio(Transform parent, Vector3 pos, AudioClip clip, float vol, bool loop, bool spatial, float maxDist = 40f)
        {
            var g = new GameObject("Audio");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var a = g.AddComponent<AudioSource>();
            a.clip = clip;
            a.volume = vol;
            a.loop = loop;
            a.playOnAwake = false;
            a.spatialBlend = spatial ? 1f : 0f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.minDistance = 1f;
            a.maxDistance = maxDist;
            a.dopplerLevel = 0f;
            if (loop && clip) a.Play();
            return a;
        }
    }

    /// Thin wrapper over the Input System (project is set to Input System only).
    public static class Inp
    {
        public static Vector2 Move()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        public static Vector2 Look()
        {
            var m = Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue();
        }

        public static bool Interact()
        {
            var k = Keyboard.current; var m = Mouse.current;
            return (k != null && k.eKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame);
        }

        /// Positive = scroll down. Mouse wheel plus W/S or arrow keys (held).
        public static float Scroll()
        {
            float v = 0f;
            var m = Mouse.current; if (m != null) v -= m.scroll.ReadValue().y * 0.5f;
            var k = Keyboard.current;
            if (k != null) v += ((k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f) - (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f)) * 520f * Time.unscaledDeltaTime;
            return v;
        }

        public static bool Click() { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        public static bool Sprint() { var k = Keyboard.current; return k != null && k.leftShiftKey.isPressed; }
        public static bool Torch() { var k = Keyboard.current; return k != null && k.fKey.wasPressedThisFrame; }
        public static bool Escape() { var k = Keyboard.current; return k != null && (k.escapeKey.wasPressedThisFrame || k.tabKey.wasPressedThisFrame); }
        public static bool Confirm() { var k = Keyboard.current; return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame); }
    }
}
