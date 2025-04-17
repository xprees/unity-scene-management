using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Xprees.SceneManagement.Editor.Initialization
{
    [InitializeOnLoad]
    public static class EditorInitializationLoader
    {
        private const string initActivePrefsKey = "EditorInitializationLoader.Active";
        private static bool active = true;

        public static bool Active
        {
            get => active;
            set
            {
                active = value;
                EditorPrefs.SetBool(initActivePrefsKey, value);
            }
        }

        /// Invoked after entering play mode and initializing the scenes for play mode
        public static event Action EditorPlayModeInitialized;

        /// Invoked after exiting play mode and cleaning up the scenes
        public static event Action EditorPlayModeTeardown;

        static EditorInitializationLoader()
        {
            Active = EditorPrefs.GetBool(initActivePrefsKey, true);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }


        private async static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!Active) return;

            switch (change)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    await OnEnterPlayMode();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    await OnExitPlayMode();
                    break;
                default:
                    Debug.LogError("Something went wrong with play mode state change");
                    break;
            }
        }

        private async static UniTask OnEnterPlayMode()
        {
            await EditorSceneLoader.LoadInitScene(OpenSceneMode.Single); // Single unloads all other scenes
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