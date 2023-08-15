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

            EditorSceneManager.CloseScene(sceneByPath, true);

            lock (scene) scene.IsBeingProcessed = false;
            scene.IsLoaded = false;
        }
    }
}