using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    public static class EditorSceneLoader
    {
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
    }
}