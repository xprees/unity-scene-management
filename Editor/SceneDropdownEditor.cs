using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Xprees.SceneManagement.Editor.Initialization;
using Xprees.SceneManagement.ScriptableObjects;
#if UNITY_6000_3_OR_NEWER
using System.Collections.Generic;
using UnityEditor.Toolbars;

#else
using UnityToolbarExtender;
#endif

namespace Xprees.SceneManagement.Editor
{
    public static class SceneDropdownEditor
    {
        private readonly static EditorSceneTracker sceneTracker = new();

#if UNITY_6000_3_OR_NEWER
        static SceneDropdownEditor()
        {
            SceneManager.activeSceneChanged += (_, _) => RefreshSceneList();
            EditorApplication.projectChanged += RefreshSceneList;
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) => RefreshSceneList();
        }

        private const string toolbarSceneDropdownPath = "Scenes/Scene Selector";

        [MainToolbarElement(toolbarSceneDropdownPath, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static IEnumerable<MainToolbarElement> RenderSceneTools()
        {
            // Scene Dropdown
            var content = new MainToolbarContent(
                GetTitle().text,
                EditorGUIUtility.IconContent("BuildSettings.Editor").image as Texture2D,
                "Manage scenes");

            var sceneSelectionDropdown = new MainToolbarDropdown(content, ShowSceneDropDownMenu);

            // Editor Auto Play Button
            var autoPlayIcon = "PauseButton";
            var label = "Off";
            if (EditorInitializationLoader.Active)
            {
                autoPlayIcon = "PlayButton";
                label = "Auto";
            }

            var autoPlayBtnContent = new MainToolbarContent(label,
                EditorGUIUtility.IconContent(autoPlayIcon).image as Texture2D,
                $"Turn {(EditorInitializationLoader.Active ? "off" : "on")} Editor Auto Play Mode Init (Normally should be on Auto)");

            var autoPlayButton = new MainToolbarButton(autoPlayBtnContent, () =>
            {
                EditorInitializationLoader.Active = !EditorInitializationLoader.Active;
                RefreshSceneList();
                MainToolbar.Refresh(toolbarSceneDropdownPath);
            });

            return new MainToolbarElement[] { autoPlayButton, sceneSelectionDropdown };
        }

        private static void ShowSceneDropDownMenu(Rect dropdownRect)
        {
            PrepareSceneListMenu().DropDown(dropdownRect);
            MainToolbar.Refresh(toolbarSceneDropdownPath);
        }
#else
        [InitializeOnLoadMethod]
        public static void RenderToolbarTools()
        {
            ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
        }

        private static void OnToolbarGUI()
        {
            GUILayout.Space(5);

            // Create the dropdown button
            if (GUILayout.Button(GetTitle(), EditorStyles.toolbarDropDown))
            {
                var menu = PrepareSceneListMenu();
                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();
        }
#endif


        /// Prepares the scene list menu for the dropdown
        private static GenericMenu PrepareSceneListMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Refresh"), false, RefreshSceneList);
            menu.AddSeparator("");

            var isLoadedInitScene = EditorSceneLoader.IsLoadedInitScene;
            menu.AddItem(new GUIContent("Load Init Scene"), isLoadedInitScene,
                async () => await EditorSceneLoader.ToggleLoadOrUnloadInitScene(OpenSceneMode.Additive));

#if !UNITY_6000_3_OR_NEWER
            // This is moved to the separate button in the new toolbar system
            var editorInitializationActive = EditorInitializationLoader.Active;
            menu.AddItem(new GUIContent("Editor PlayMode Init (Normally on)"), editorInitializationActive,
                () => EditorInitializationLoader.Active = !editorInitializationActive);
#endif

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

            return menu;
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

        private static void ProcessScene(SceneSO scene)
        {
            if (sceneTracker.IsSceneOpen(scene) ?? false)
            {
                EditorSceneLoader.CloseScene(scene);
                return;
            }

            EditorSceneLoader.OpenScene(scene);
        }

        private static void RefreshSceneList()
        {
            sceneTracker.RefreshList();
#if UNITY_6000_3_OR_NEWER
            MainToolbar.Refresh(toolbarSceneDropdownPath); // Refresh the toolbar to reflect any changes
#endif
        }
    }
}