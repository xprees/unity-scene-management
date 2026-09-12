using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using Xprees.Core;
using Xprees.SceneManagement.Extensions;

namespace Xprees.SceneManagement.ScriptableObjects
{
    [ResetOnPlayMode(PlayModeResetTiming.Both)]
    [StatefulLifetime(StateLifetime.Persistent)] // SceneSO is a persistent ScriptableObject that holds scene data and runtime state.
    [CreateAssetMenu(menuName = "SceneData/New Scene data", fileName = "Scene")]
    public class SceneSO : DescriptionBaseSO, IRuntimeStateOwner
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

        public void ClearTransientState() => ResetRuntimeState();

        public void ResetRuntimeState()
        {
            lock (this)
            {
                IsBeingProcessed = false;
                IsLoaded = false;
                sceneInstance = null;
#if UNITY_EDITOR
                sceneReference?.ResetOperationHandle();
#endif
            }
        }
    }
}