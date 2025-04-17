using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;
using Xprees.SceneManagement.Editor.Initialization;
using Xprees.SceneManagement.ScriptableObjects;

namespace Xprees.SceneManagement.Editor
{
    [InitializeOnLoad]
    public static class SceneDropdownEditor
    {
        private readonly static EditorSceneTracker sceneTracker;

        static SceneDropdownEditor()
        {
            sceneTracker = new EditorSceneTracker();
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        #region GUI

        private static void OnToolbarGUI()
        {
            GUILayout.Space(5);

            // Create the dropdown button
            if (GUILayout.Button(GetTitle(), EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Refresh"), false, sceneTracker.RefreshList);
                menu.AddSeparator("");

                var isLoadedInitScene = EditorSceneLoader.IsLoadedInitScene;
                menu.AddItem(new GUIContent("Load Init Scene"), isLoadedInitScene,
                    async () => await EditorSceneLoader.ToggleLoadOrUnloadInitScene(OpenSceneMode.Additive));

                var editorInitializationActive = EditorInitializationLoader.Active;
                menu.AddItem(new GUIContent("Editor PlayMode Init (Normally on)"), editorInitializationActive,
                    () => EditorInitializationLoader.Active = !editorInitializationActive);

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Unload all scenes (except init)"), false, UnloadAllScenesExceptInit);
                menu.AddSeparator("");

                menu.AddDisabledItem(new GUIContent("Scenes"));
                menu.AddSeparator("");

                var previewSceneType = SceneType.PersistentManagers;
                foreach (var scene in sceneTracker.Scenes.OrderByDescending(scene => scene.sceneType))
                {
                    var isSceneOpenOrPresent = sceneTracker.IsSceneOpen(scene);
                    if (isSceneOpenOrPresent == null) continue;

                    if (scene.sceneType != previewSceneType)
                    {
                        previewSceneType = scene.sceneType;
                        menu.AddSeparator("");
                    }

                    var hasValidSceneReference = scene.HasValidSceneReference();
                    if (!hasValidSceneReference)
                    {
                        menu.AddDisabledItem(new GUIContent($"{scene.sceneName} ({scene.sceneType}) - Invalid Scene!"));
                        continue;
                    }

                    menu.AddItem(new GUIContent($"{scene.sceneName} ({scene.sceneType})"), isSceneOpenOrPresent!.Value, MenuFunction);
                    continue;

                    void MenuFunction() => ProcessScene(scene);
                }

                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();
        }

        private async static void UnloadAllScenesExceptInit()
        {
            if (!EditorSceneLoader.IsLoadedInitScene)
            {
                await EditorSceneLoader.LoadInitScene(OpenSceneMode.Single);
                return;
            }

            var scenes = sceneTracker.Scenes.Where(scene => scene.sceneType != SceneType.Initialization).ToList();
            foreach (var scene in scenes.Where(scene => sceneTracker.IsSceneOpen(scene) ?? false))
            {
                EditorSceneLoader.CloseScene(scene);
            }
        }

        private static GUIContent GetTitle() =>
            new("Scenes", EditorGUIUtility.IconContent("BuildSettings.Editor").image);

        #endregion

        private static void ProcessScene(SceneSO scene)
        {
            if (sceneTracker.IsSceneOpen(scene) ?? false)
            {
                EditorSceneLoader.CloseScene(scene);
                return;
            }

            EditorSceneLoader.OpenScene(scene);
        }
    }
}