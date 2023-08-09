using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using Xprees.Core;
using Xprees.EditorTools.Attributes.ReadOnlyAttribute;

namespace Xprees.SceneManagement.ScriptableObjects
{
    [CreateAssetMenu(menuName = "SceneData/New Scene data", fileName = "Scene")]
    public class SceneSO : DescriptionBaseSO
    {
        [Header("Reference")]
        public AssetReference sceneReference;

        [Header("Settings")]
        public string sceneName;

        public SceneType sceneType;

        [field: Header("Run-time state")]
        [field: ReadOnly]
        [field: SerializeField] public bool IsBeingProcessed { get; set; }

        [field: ReadOnly]
        [field: SerializeField] public bool IsLoaded { get; set; }

        public SceneInstance? sceneInstance;

        private void Awake() => ResetRuntimeState();

        private void ResetRuntimeState()
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