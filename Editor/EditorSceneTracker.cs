using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    public class EditorSceneTracker
    {
        private string[] scenePaths;
        private List<Tuple<SceneSO, bool>> scenesTracked;

        public IEnumerable<SceneSO> Scenes => scenesTracked?.Select(tuple => tuple.Item1);

        public EditorSceneTracker()
        {
            InitScenesList();
        }

        public bool IsSceneOpen(SceneSO scene) => IsSceneOpen(scene.GetScenePath());

        private bool IsSceneOpen(string scenePath)
        {
            if (scenePath == null) return false;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var currentScene = SceneManager.GetSceneAt(i);
                if (currentScene.path == scenePath) return true;
            }

            return false;
        }

        private void InitScenesList()
        {
            scenePaths = GetAllScenePaths().ToArray();
            var scenes = scenePaths.Select(AssetDatabase.LoadAssetAtPath<SceneSO>);
            scenesTracked = scenes.Select(scene => new Tuple<SceneSO, bool>(scene, IsSceneOpen(scene))).ToList();
        }

        private static IEnumerable<string> GetAllScenePaths() =>
            AssetDatabase.FindAssets($"t:{nameof(SceneSO)}")
                .Select(AssetDatabase.GUIDToAssetPath);
    }
}