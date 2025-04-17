using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    public static class SceneSOExtensions
    {
        public static string GetScenePath(this SceneSO scene)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found.");
                return null;
            }

            if (string.IsNullOrEmpty(scene.sceneReference.AssetGUID)) return null;

            var entry = settings.FindAssetEntry(scene.sceneReference.AssetGUID);
            // Not added to addressables, yet?
            if (entry == null)
            {
                // Try to fall back to the scene reference itself
                return AssetDatabase.GUIDToAssetPath(scene.sceneReference.AssetGUID);
            }

            return AssetDatabase.GUIDToAssetPath(entry.guid);
        }

        public static bool HasValidSceneReference(this SceneSO scene)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found.");
                return false;
            }

            if (string.IsNullOrEmpty(scene.sceneReference.AssetGUID)) return false;

            var entry = settings.FindAssetEntry(scene.sceneReference.AssetGUID);
            // Not added to addressables, yet?
            if (entry == null)
            {
                var hasValidSceneReference = AssetDatabase.GUIDToAssetPath(scene.sceneReference.AssetGUID) != null;
                if (!hasValidSceneReference) Debug.LogWarning($"AddressableAssetEntry for \"{scene.sceneName}\" not found.");
                return hasValidSceneReference;
            }

            return AssetDatabase.GUIDToAssetPath(entry.guid) != null;
        }
    }
}