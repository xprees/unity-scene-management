using System.Reflection;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Xprees.SceneManagement.Extensions
{
    public static class AssetReferenceExtensions
    {
        private readonly static FieldInfo operationHandleField =
            typeof(AssetReference).GetField("m_Operation", BindingFlags.NonPublic | BindingFlags.Instance);

        /// Loads a scene using Addressables.LoadSceneAsync with this AssetReference as key.
        /// Directly calls Addressables.LoadSceneAsync instead of AssetReference.LoadSceneAsync,
        /// avoiding caching the operation handle on the AssetReference instance which causes
        /// errors when Domain Reload is disabled.
        public static AsyncOperationHandle<SceneInstance> LoadSceneAddressableAsync(
            this AssetReference assetRef,
            LoadSceneMode loadMode = LoadSceneMode.Additive,
            bool activateOnLoad = true,
            int priority = 100
        ) => Addressables.LoadSceneAsync(assetRef, loadMode, activateOnLoad, priority);

        /// Loads an asset using Addressables.LoadAssetAsync with this AssetReference as key.
        /// Directly calls Addressables.LoadAssetAsync instead of AssetReference.LoadAssetAsync,
        /// avoiding caching the operation handle on the AssetReference instance which causes
        /// errors when Domain Reload is disabled.
        public static AsyncOperationHandle<TObject> LoadAssetAddressableAsync<TObject>(
            this AssetReference assetRef
        ) => Addressables.LoadAssetAsync<TObject>(assetRef);

        /// Safely releases an AssetReference if its operation handle is valid,
        /// and ensures the handle is cleared.
        public static void SafeRelease(this AssetReference assetRef)
        {
            if (assetRef == null) return;
            if (assetRef.OperationHandle.IsValid())
            {
                assetRef.ReleaseAsset();
            }
#if UNITY_EDITOR
            assetRef.ResetOperationHandle();
#endif
        }

        /// Resets the internal cached OperationHandle on the AssetReference.
        /// Useful when Domain Reload is disabled in the Editor, as AssetReference
        /// instances on persistent ScriptableObjects retain stale handles across Play Mode runs.
        public static void ResetOperationHandle(this AssetReference assetRef)
        {
            if (assetRef == null) return;
            operationHandleField?.SetValue(assetRef, default(AsyncOperationHandle));
        }
    }
}