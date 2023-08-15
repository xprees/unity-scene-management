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
            EditorSceneManager.CloseScene(
                SceneManager.GetSceneByPath(scene.GetScenePath()),
                true
            );
            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = false;
        }
    }
}