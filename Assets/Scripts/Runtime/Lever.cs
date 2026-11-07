using UnityEngine;

namespace SignalBox
{
    /// A lever in the cabin frame. NORMAL leans toward the track; REVERSE is pulled back toward the signalman.
    public class Lever : MonoBehaviour
    {
        public bool reversed;
        public GameObject hit;
        float angle = 22f;

        void Update()
        {
            float target = reversed ? -22f : 22f;
            angle = Mathf.MoveTowards(angle, target, 150f * Time.deltaTime);
            transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
