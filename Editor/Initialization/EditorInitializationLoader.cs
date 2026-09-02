using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor.Initialization
{
    [InitializeOnLoad]
    public static class EditorInitializationLoader
    {
        private const string initActivePrefsKey = "EditorInitializationLoader.Active";
        private static bool active;

        public static bool Active
        {
            get => active;
            set
            {
                active = value;
                EditorPrefs.SetBool(initActivePrefsKey, value);
                UpdatePlayModeStartScene(value);
            }
        }

        /// Invoked after entering play mode and initializing the scenes for play mode
        public static event Action EditorPlayModeInitialized;

        /// Invoked after exiting play mode and cleaning up the scenes
        public static event Action EditorPlayModeTeardown;

        static EditorInitializationLoader()
        {
            active = EditorPrefs.GetBool(initActivePrefsKey, true);
            UpdatePlayModeStartScene(active);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void UpdatePlayModeStartScene(bool isEnabled)
        {
            if (!isEnabled)
            {
                if (EditorSceneManager.playModeStartScene) EditorSceneManager.playModeStartScene = null;
                return;
            }

            if (!CanAccessAssetDatabase()) return;

            var initScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorSceneLoader.InitScenePath);
            if (initScene && EditorSceneManager.playModeStartScene != initScene)
            {
                EditorSceneManager.playModeStartScene = initScene;
            }
        }

        private static void ResetAllSceneSOStates()
        {
            if (!CanAccessAssetDatabase()) return;

            var guids = AssetDatabase.FindAssets($"t:{nameof(SceneSO)}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sceneSO = AssetDatabase.LoadAssetAtPath<SceneSO>(path);
                if (sceneSO) sceneSO.ResetRuntimeState();
            }
        }

        private static bool CanAccessAssetDatabase() =>
            !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;

        private async static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredEditMode:
                    UpdatePlayModeStartScene(Active);
                    ResetAllSceneSOStates();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    UpdatePlayModeStartScene(Active);
                    ResetAllSceneSOStates();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    if (Active)
                    {
                        await OnEnterPlayMode();
                    }

                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    if (Active)
                    {
                        await OnExitPlayMode();
                    }

                    break;
                default:
                    Debug.LogError("Something went wrong with play mode state change");
                    break;
            }
        }

        private async static UniTask OnEnterPlayMode()
        {
            // Single unloads all other scenes
            if (!EditorSceneLoader.IsLoadedInitScene) await EditorSceneLoader.LoadInitScene(OpenSceneMode.Single);

            await UniTask.DelayFrame(2);
            EditorPlayModeInitialized?.Invoke();
        }

        private static UniTask OnExitPlayMode()
        {
            EditorPlayModeTeardown?.Invoke();
            return UniTask.CompletedTask;
        }
    }
}