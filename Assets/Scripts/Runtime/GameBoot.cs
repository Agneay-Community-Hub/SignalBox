using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SignalBox
{
    /// Entry point. Builds the whole game at runtime, so the scene only needs this one object
    /// (and it's auto-created if missing).
    public class GameBoot : MonoBehaviour
    {
        bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Ensure();
        }

        static void OnLoaded(Scene s, LoadSceneMode m) => Ensure();

        static void Ensure()
        {
            if (FindFirstObjectByType<GameBoot>() == null) new GameObject("SignalBox (auto)").AddComponent<GameBoot>();
        }

        void Start()
        {
            if (built) return;
            built = true;
            Build();
        }

        void Build()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.026f;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.06f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.085f);
            RenderSettings.skybox = null;

            var world = World.Build(transform);

            var pgo = new GameObject("Player");
            pgo.transform.SetPositionAndRotation(world.playerStart, Quaternion.Euler(0f, world.playerYaw, 0f));
            var player = pgo.AddComponent<Player>();

            var eye = new GameObject("Eye");
            eye.transform.SetParent(pgo.transform, false);
            eye.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = eye.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 260f;
            cam.fieldOfView = 68f;
            eye.AddComponent<AudioListener>();
            player.Init(cam);

            // The world camera renders into a low-res texture; this one just clears the screen under the HUD.
            var sc = new GameObject("ScreenCam").AddComponent<Camera>();
            sc.transform.SetParent(transform, false);
            sc.cullingMask = 0;
            sc.clearFlags = CameraClearFlags.SolidColor;
            sc.backgroundColor = Color.black;
            sc.depth = -10f;

            gameObject.AddComponent<Hud>().Init(cam);
            gameObject.AddComponent<JumpScare>();
            gameObject.AddComponent<Director>().Init(world, player);
        }
    }
}
