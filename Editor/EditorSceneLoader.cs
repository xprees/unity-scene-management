using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    public static class EditorSceneLoader
    {
        private const string initSceneDefaultPath = "Assets/Scenes/Initialization.unity";
        private readonly static string initScenePath;

        public static bool IsLoadedInitScene => SceneManager.GetSceneByPath(initScenePath).isLoaded;

        private static bool IsInPlayMode => EditorApplication.isPlaying;

        static EditorSceneLoader()
        {
            initScenePath = FindInitScenePath();
        }

        public static void OpenScene(SceneSO scene)
        {
            lock (scene) scene.IsBeingProcessed = true;

            if (IsInPlayMode)
            {
                // TODO handle playmode
            }
            else
            {
                EditorSceneManager.OpenScene(scene.GetScenePath(), OpenSceneMode.Additive);
            }

            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = true;
        }

        public static void CloseScene(SceneSO scene)
        {
            lock (scene) scene.IsBeingProcessed = true;
            var sceneByPath = SceneManager.GetSceneByPath(scene.GetScenePath());
            if (sceneByPath.isDirty)
            {
                EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { sceneByPath });
            }

            var loadedSceneCount = SceneManager.sceneCount;
            // Try to open initialization scene if no other scene is loaded
            if (loadedSceneCount <= 1)
            {
                var initialScene = SceneManager.GetSceneByBuildIndex(0);
                if (!initialScene.IsValid()) initialScene = SceneManager.GetSceneByPath(initScenePath);
                // TODO handle PlayMode
                if (initialScene.IsValid()) EditorSceneManager.OpenScene(initialScene.path, OpenSceneMode.Single);
            }

            EditorSceneManager.CloseScene(sceneByPath, true);

            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = false;
        }

        public async static UniTask ToggleLoadOrUnloadInitScene(OpenSceneMode mode = OpenSceneMode.Single)
        {
            if (IsLoadedInitScene)
            {
                await UnloadInitScene();
                return;
            }

            await LoadInitScene(mode);
        }

        public static async UniTask UnloadInitScene()
        {
            var initSceneManagerPath = SceneManager.GetSceneByPath(initScenePath);
            if (IsInPlayMode)
            {
                await SceneManager.UnloadSceneAsync(initSceneManagerPath);
                return;
            }

            EditorSceneManager.CloseScene(initSceneManagerPath, true);
        }

        public async static UniTask LoadInitScene(OpenSceneMode mode)
        {
            if (IsInPlayMode)
            {
                var loadSceneMode = mode == OpenSceneMode.Single ? LoadSceneMode.Single : LoadSceneMode.Additive;
                await SceneManager.LoadSceneAsync(0, loadSceneMode); // Initialization scene is always at index 0
                return;
            }

            var scene = EditorSceneManager.OpenScene(initScenePath, mode);
            SceneManager.SetActiveScene(scene);

            // Move initialization scene to the top of the hierarchy
            var fstScene = SceneManager.GetSceneAt(0);
            EditorSceneManager.MoveSceneBefore(scene, fstScene);
        }

        private static string FindInitScenePath()
        {
            var findInitScenePath = AssetDatabase
                .FindAssets("t:Scene Initialization")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault();

            return findInitScenePath ?? initSceneDefaultPath;
        }
    }
}