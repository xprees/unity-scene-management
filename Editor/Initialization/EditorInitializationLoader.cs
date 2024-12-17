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
        /// Invoked after entering play mode and initializing the scenes for play mode
        public static event Action EditorPlayModeInitialized;

        /// Invoked after exiting play mode and cleaning up the scenes
        public static event Action EditorPlayModeTeardown;

        static EditorInitializationLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }


        private async static void OnPlayModeChanged(PlayModeStateChange change)
        {
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
            // TODO save currently opened scenes - necessary?
            await EditorSceneLoader.LoadInitScene(OpenSceneMode.Single); // Single unloads all other scenes
            await UniTask.DelayFrame(2);
            EditorPlayModeInitialized?.Invoke();
        }

        private static UniTask OnExitPlayMode()
        {
            Debug.Log("Exiting play mode");
            // TODO load previously opened scenes - might be unnecessary (done automatically)
            //  Cleanup if needed? 
            EditorPlayModeTeardown?.Invoke();
            return UniTask.CompletedTask;
        }
    }
}