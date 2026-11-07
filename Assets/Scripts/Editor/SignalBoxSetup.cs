using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SignalBox.EditorTools
{
    /// One-time project setup (scene, base materials, build settings) plus build menu items.
    [InitializeOnLoad]
    public static class SignalBoxSetup
    {
        public const string ScenePath = "Assets/Scenes/SignalBox.unity";

        static SignalBoxSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath) || !File.Exists("Assets/Resources/SB_Lit.mat")) Setup();
            };
        }

        [MenuItem("Signal Box/Setup Project")]
        public static void Setup()
        {
            EnsureMaterials();
            if (!File.Exists(ScenePath)) CreateScene();
            EnsureSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Signal Box] Project setup complete.");
        }

        static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            MakeMat("Assets/Resources/SB_Lit.mat", "Universal Render Pipeline/Lit");
            MakeMat("Assets/Resources/SB_Unlit.mat", "Universal Render Pipeline/Unlit");
        }

        static void MakeMat(string path, string shaderName)
        {
            if (File.Exists(path)) return;
            var s = Shader.Find(shaderName);
            if (!s) { Debug.LogWarning("[Signal Box] Shader not found: " + shaderName); return; }
            AssetDatabase.CreateAsset(new Material(s), path);
        }

        static void CreateScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("SignalBox").AddComponent<global::SignalBox.GameBoot>();
            // Baked into the scene so URP keeps the fog shader variants in builds.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.026f;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.06f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.085f);
            RenderSettings.skybox = null;
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void EnsureSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Signal Box";
            PlayerSettings.companyName = "Chetan Kumar G";
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
        }

        [MenuItem("Signal Box/Build WebGL (itch.io)")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        [MenuItem("Signal Box/Build Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/SignalBox.exe");

        static void Build(BuildTarget target, string path)
        {
            Setup();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log("[Signal Box] " + target + " build: " + s.result + ", " + (s.totalSize / 1048576f).ToString("0.0") + " MB, " + s.totalErrors + " errors -> " + path);
        }
    }
}
