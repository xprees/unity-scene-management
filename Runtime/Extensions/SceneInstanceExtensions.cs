using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Xprees.SceneManagement.Extensions
{
    public static class SceneInstanceExtensions
    {
        /// Sets the scene as active scene. If the scene is not valid, nothing happens.
        public static void SetAsActiveScene(this SceneInstance scene)
        {
            if (!scene.Scene.IsValid()) return;
            SceneManager.SetActiveScene(scene.Scene);
        }

    }
}