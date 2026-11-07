using UnityEngine;

namespace SignalBox
{
    public class SignalPost : MonoBehaviour
    {
        public bool clear;
        Renderer lamp;
        Light glow;

        public void Init(Renderer r, Light l)
        {
            lamp = r;
            glow = l;
            Set(false);
        }

        public void Set(bool isClear)
        {
            clear = isClear;
            lamp.sharedMaterial = Mats.Unlit(isClear ? new Color(0.2f, 1f, 0.55f) : new Color(1f, 0.08f, 0.05f));
            glow.color = isClear ? new Color(0.2f, 1f, 0.5f) : new Color(1f, 0.1f, 0.05f);
        }
    }
}
