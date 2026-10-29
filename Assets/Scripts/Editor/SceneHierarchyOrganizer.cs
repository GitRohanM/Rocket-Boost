#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RocketBoost.Editor
{
    /// <summary>
    /// Analyzes and organizes the GameObject hierarchy across all project scenes.
    /// Standardizes all scenes into a clean, unified top-level hierarchy structure:
    ///   1. GameManager
    ///   2. Main Camera
    ///   3. CinemachineCamera (if present)
    ///   4. Player Rocket / Showcase Rocket
    ///   5. Lighting (contains Directional Light, Global Volume, Light Boxes, Spotlights)
    ///   6. Environment (contains terrain, level structures, obstacles, Launch & Landing Pads)
    ///   7. UIRoot
    ///
    /// Preserves all world transforms, physics colliders, tags, and references.
    /// </summary>
    public static class SceneHierarchyOrganizer
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Scene 1.unity",
            "Assets/Scenes/Scene 2.unity",
            "Assets/Scenes/Scene 3.unity"
        };

        [MenuItem("Rocket Boost/Organize Scene Hierarchies", priority = 2)]
        public static void OrganizeAll()
        {
            Debug.Log("[HierarchyOrganizer] Starting hierarchy cleanup across all scenes...");

            foreach (string scenePath in ScenePaths)
            {
                OrganizeScene(scenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HierarchyOrganizer] Hierarchy cleanup complete for all scenes!");
        }

        public static void OrganizeScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[HierarchyOrganizer] Failed to open scene: {scenePath}");
                return;
            }

            Debug.Log($"[HierarchyOrganizer] Organizing '{scene.name}'...");

            // 1. Ensure Lighting container exists at origin
            GameObject lightingRoot = GetOrCreateRootContainer(scene, "Lighting");

            // 2. Ensure Environment container exists at origin
            GameObject environmentRoot = GetOrCreateRootContainer(scene, "Environment");

            // 3. Move lighting-related root GameObjects under Lighting container
            string[] lightingNames =
            {
                "Directional Light",
                "Global Volume",
                "Lights",
                "Light Boxes",
                "Launchpad Upward Spotlight",
                "Rocket Rim Light"
            };

            foreach (string name in lightingNames)
            {
                GameObject go = FindRootGameObject(scene, name);
                if (go != null && go != lightingRoot)
                {
                    Undo.SetTransformParent(go.transform, lightingRoot.transform, "Organize Lighting");
                    go.transform.SetParent(lightingRoot.transform, true);
                }
            }

            // 4. Move environment-related root GameObjects under Environment container
            string[] environmentNames =
            {
                "Launch Pad",
                "Landing Pad",
                "Sector02_Canyon_Outpost",
                "Sector03_Space_Backdrop",
                "Sector03_Orbital_Superstructure"
            };

            foreach (string name in environmentNames)
            {
                GameObject go = FindRootGameObject(scene, name);
                if (go != null && go != environmentRoot)
                {
                    Undo.SetTransformParent(go.transform, environmentRoot.transform, "Organize Environment");
                    go.transform.SetParent(environmentRoot.transform, true);
                }
            }

            // 5. Standardize root ordering
            // Desired top-level order:
            // 0: GameManager
            // 1: Main Camera
            // 2: CinemachineCamera
            // 3: Player Rocket / Showcase Rocket
            // 4: Lighting
            // 5: Environment
            // 6: UIRoot
            string[] rootOrder =
            {
                "GameManager",
                "Main Camera",
                "CinemachineCamera",
                "Player Rocket",
                "Showcase Rocket",
                "Lighting",
                "Environment",
                "UIRoot"
            };

            int siblingIndex = 0;
            foreach (string rootName in rootOrder)
            {
                GameObject rootGo = FindRootGameObject(scene, rootName);
                if (rootGo != null)
                {
                    rootGo.transform.SetSiblingIndex(siblingIndex++);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HierarchyOrganizer] Successfully organized and saved '{scene.name}'!");
        }

        private static GameObject GetOrCreateRootContainer(Scene scene, string name)
        {
            GameObject existing = FindRootGameObject(scene, name);
            if (existing != null)
            {
                return existing;
            }

            GameObject container = new GameObject(name);
            container.transform.position = Vector3.zero;
            container.transform.rotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
            SceneManager.MoveGameObjectToScene(container, scene);
            Undo.RegisterCreatedObjectUndo(container, "Create " + name);
            return container;
        }

        private static GameObject FindRootGameObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }
            return null;
        }
    }
}
#endif
