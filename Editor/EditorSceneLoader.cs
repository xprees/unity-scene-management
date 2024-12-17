using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    public static class EditorSceneLoader
    {
        private const string initSceneDefaultPath = "Assets/Scenes/Initialization.unity";
        private readonly static string initScenePath;

        public static bool IsLoadedInitScene => SceneManager.GetSceneByPath(initScenePath).isLoaded;

        static EditorSceneLoader()
        {
            initScenePath = FindInitScenePath();
        }

        public static void OpenScene(SceneSO scene)
        {
            lock (scene) scene.IsBeingProcessed = true;

            EditorSceneManager.OpenScene(scene.GetScenePath(), OpenSceneMode.Additive);

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
                if (!initialScene.IsValid()) initialScene = SceneManager.GetSceneByPath("Assets/Scenes/Initialization.unity");
                if (initialScene.IsValid()) EditorSceneManager.OpenScene(initialScene.path, OpenSceneMode.Single);
            }

            EditorSceneManager.CloseScene(sceneByPath, true);

            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = false;
        }

        public static void LoadInitializationScene(OpenSceneMode mode = OpenSceneMode.Single)
        {
            EditorSceneManager.OpenScene(initScenePath, mode);
            var initSceneManagerPath = SceneManager.GetSceneByPath(initScenePath);
            SceneManager.SetActiveScene(initSceneManagerPath);

            var fstScene = SceneManager.GetSceneAt(0);
            EditorSceneManager.MoveSceneBefore(initSceneManagerPath, fstScene);
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