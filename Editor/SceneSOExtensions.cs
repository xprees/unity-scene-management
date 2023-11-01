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

            var entry = settings.FindAssetEntry(scene.sceneReference.AssetGUID);
            if (entry != null) return AssetDatabase.GUIDToAssetPath(entry.guid);

            Debug.LogWarning($"Asset entry for \"{scene.name}\" not found.");
            return null;
        }
    }
}