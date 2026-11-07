using UnityEngine;

namespace SignalBox
{
    /// Anything the player can look at and press E on.
    public class Interactable : MonoBehaviour
    {
        public System.Func<string> prompt;
        public System.Action use;
        public System.Func<bool> available;

        public bool Available => available == null || available();

        public static Interactable Add(GameObject g, System.Func<string> prompt, System.Action use)
        {
            var i = g.AddComponent<Interactable>();
            i.prompt = prompt;
            i.use = use;
            return i;
        }
    }
}
