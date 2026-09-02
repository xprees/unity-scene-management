using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using Xprees.Core;

namespace Xprees.SceneManagement.ScriptableObjects
{
    [StatefulLifetime(StateLifetime.Persistent)] // SceneSO is a persistent ScriptableObject that holds scene data and runtime state.
    [CreateAssetMenu(menuName = "SceneData/New Scene data", fileName = "Scene")]
    public class SceneSO : DescriptionBaseSO
    {
        [Header("Reference")]
        public AssetReference sceneReference;

        [Header("Settings")]
        public string sceneName;

        public SceneType sceneType;

        [field: Header("Run-time state")]
        [field: NonSerialized] public bool IsBeingProcessed { get; set; }

        [field: NonSerialized] public bool IsLoaded { get; set; }

        [NonSerialized] public SceneInstance? sceneInstance;

        private void OnEnable() => ResetRuntimeState();

        private void OnDisable() => ResetRuntimeState();

        public void ResetRuntimeState()
        {
            lock (this)
            {
                IsBeingProcessed = false;
                IsLoaded = false;
                sceneInstance = null;
            }
        }

        public override void ResetState() => ResetRuntimeState();
    }
}