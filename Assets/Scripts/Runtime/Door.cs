using UnityEngine;

namespace SignalBox
{
    public class Door : MonoBehaviour
    {
        public bool open;
        public float speed = 260f;
        public float rattle;
        float a;

        void Update()
        {
            a = Mathf.MoveTowards(a, open ? -100f : 0f, speed * Time.deltaTime);
            rattle = Mathf.MoveTowards(rattle, 0f, Time.deltaTime);
            float j = rattle > 0f && !open ? Mathf.Sin(Time.time * 55f) * 2.2f * Mathf.Clamp01(rattle * 3f) : 0f;
            transform.localRotation = Quaternion.Euler(0f, a + j, 0f);
        }
    }
}
