using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pachacuti.EditorTools
{
    /// <summary>
    /// Keeps the project in the state the slice expects, and adds the PACHACUTI menu.
    /// Runs on every domain reload; only fixes what is missing and says what it did.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string BootScene = "Assets/Pachacuti/Scenes/Boot.unity";
        public const string PipelineAsset = "Assets/Pachacuti/Settings/PachacutiURP.asset";

        static ProjectSetup()
        {
            EditorApplication.delayCall += Validate;
        }

        [MenuItem("PACHACUTI/Validate Project Setup", priority = 100)]
        public static void Validate()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                var urp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelineAsset);
                if (urp != null)
                {
                    GraphicsSettings.defaultRenderPipeline = urp;
                    Debug.Log("[PACHACUTI] Assigned " + PipelineAsset + " as the default render pipeline.");
                }
                else
                {
                    Debug.LogError("[PACHACUTI] No render pipeline asset. Create one with Assets > Create > Rendering > URP Asset " +
                                   "(with Universal Renderer) and assign it in Project Settings > Graphics.");
                }
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == BootScene) && AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScene) != null)
            {
                scenes.Insert(0, new EditorBuildSettingsScene(BootScene, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[PACHACUTI] Added the Boot scene to the build.");
            }

            foreach (var name in new[] { "Player", "Characters", "Climbable", "Sky" })
                if (LayerMask.NameToLayer(name) < 0)
                    Debug.LogError($"[PACHACUTI] Layer '{name}' is missing (ProjectSettings/TagManager.asset). Climbing and stealth sight lines depend on it.");
        }

        [MenuItem("PACHACUTI/Play Slice %#p", priority = 1)]
        public static void PlaySlice()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(BootScene);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("PACHACUTI/Open Boot Scene", priority = 2)]
        public static void OpenBoot()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(BootScene);
        }
    }
}
