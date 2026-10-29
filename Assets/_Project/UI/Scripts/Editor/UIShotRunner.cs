using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RocketBoost.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RocketBoost.UI.Editor
{
    /// <summary>
    /// Drives the built UI through every screen in play mode and writes screenshots to
    /// Logs/UI-Shots for visual review. Editor-only convenience tool.
    /// </summary>
    public static class UIShotRunner
    {
        private const string MenuPath = "Rocket Boost/Capture Screenshots";
        private static readonly string OutputRoot = Path.GetFullPath("Logs/UI-Shots");

        private static float nextStepAt;
        private static float settleDeadline;
        private static int step;
        private static bool running;
        private static bool bootstrapping;
        private static float bootstrappedAt;
        private static float loadingDeadline;
        private static int captureWidth = 1920;
        private static int captureHeight = 1080;
        private static string captureFolder = string.Empty;
        private static readonly Queue<(int width, int height, string folder)> Pending = new Queue<(int, int, string)>();

        private static string OutputDir => string.IsNullOrEmpty(captureFolder)
            ? OutputRoot
            : Path.Combine(OutputRoot, captureFolder);

        [MenuItem("Rocket Boost/Capture Screenshots/All Resolutions (Recommended)", priority = 20)]
        public static void CaptureAllResolutions()
        {
            Pending.Clear();
            Pending.Enqueue((1920, 1080, string.Empty));
            Pending.Enqueue((1280, 720, "1280x720"));
            Pending.Enqueue((2560, 1080, "2560x1080"));
            Pending.Enqueue((1080, 1920, "portrait"));
            (int width, int height, string folder) = Pending.Dequeue();
            RequestCapture(width, height, folder);
        }

        [MenuItem("Rocket Boost/Capture Screenshots/1080p (1920x1080)", priority = 21)]
        public static void CaptureAll() => CaptureOne(1920, 1080, string.Empty);

        [MenuItem("Rocket Boost/Capture Screenshots/720p (1280x720)", priority = 22)]
        public static void Capture720p() => CaptureOne(1280, 720, "1280x720");

        [MenuItem("Rocket Boost/Capture Screenshots/Ultrawide (2560x1080)", priority = 23)]
        public static void CaptureUltraWide() => CaptureOne(2560, 1080, "2560x1080");

        [MenuItem("Rocket Boost/Capture Screenshots/Portrait (1080x1920)", priority = 24)]
        public static void CapturePortrait() => CaptureOne(1080, 1920, "portrait");

        private static void CaptureOne(int width, int height, string folder)
        {
            Pending.Clear();
            RequestCapture(width, height, folder);
        }

        public static void RequestCapture(int width, int height, string folder)
        {
            captureWidth = width;
            captureHeight = height;
            captureFolder = folder;
            EditorApplication.update -= Tick;
            EditorApplication.update -= Bootstrap;
            running = false;
            bootstrapping = true;
            bootstrappedAt = Time.realtimeSinceStartup;
            Debug.Log($"[UIShots] Capture requested at {width}x{height}.");
            EditorApplication.update += Bootstrap;
        }

        private static void Bootstrap()
        {
            if (!bootstrapping)
            {
                EditorApplication.update -= Bootstrap;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                bootstrappedAt = Time.realtimeSinceStartup;
                return;
            }

            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                }

                bootstrappedAt = Time.realtimeSinceStartup;
                return;
            }

            if (Time.realtimeSinceStartup - bootstrappedAt > 30f)
            {
                bootstrapping = false;
                EditorApplication.update -= Bootstrap;
                Debug.LogError("[UIShots] Bootstrap timed out; capture aborted.");
                return;
            }

            bootstrapping = false;
            EditorApplication.update -= Bootstrap;
            SetGameViewSize(captureWidth, captureHeight, $"RB {captureWidth}x{captureHeight}");
            Directory.CreateDirectory(OutputDir);
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            running = true;
            step = 0;
            nextStepAt = Time.realtimeSinceStartup + 2.5f;
            settleDeadline = nextStepAt + 4f;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
            Debug.Log($"[UIShots] Capture started at {OutputDir} ({Screen.width}x{Screen.height}).");
        }

        private static void Tick()
        {
            if (!running)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (Time.realtimeSinceStartup < nextStepAt)
            {
                return;
            }

            if (step == 0 && Time.timeSinceLevelLoad < 2.5f)
            {
                return;
            }

            UIManager manager = UIManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[UIShots] Step {step}: UIManager.Instance is null; retrying.");
                nextStepAt = Time.realtimeSinceStartup + 0.5f;
                return;
            }

            if (IsCaptureStep(step) && !UiSettled() && Time.realtimeSinceStartup < settleDeadline)
            {
                nextStepAt = Time.realtimeSinceStartup + 0.1f;
                return;
            }

            switch (step)
            {
                case 0:
                    Capture("01-main-menu");
                    Next(0f);
                    break;
                case 1:
                    manager.ShowPrimary(UIScreenType.LevelSelect);
                    Next(1.2f);
                    break;
                case 2:
                    Capture("02-level-select");
                    Next(0f);
                    break;
                case 3:
                    manager.ShowOverlay(UIScreenType.Settings);
                    Next(1.2f);
                    break;
                case 4:
                    Capture("03-settings");
                    Next(0f);
                    break;
                case 5:
                    manager.CloseOverlay();
                    manager.GoToLevel(1);
                    Next(4.5f);
                    break;
                case 6:
                    Capture("04-hud");
                    Next(0f);
                    break;
                case 7:
                    manager.OpenPause();
                    Next(1.0f);
                    break;
                case 8:
                    Capture("05-pause");
                    Next(0f);
                    break;
                case 9:
                    manager.ResumeGame();
                    PresentComplete(manager);
                    Next(1.8f);
                    break;
                case 10:
                    Capture("06-level-complete");
                    Next(0f);
                    break;
                case 11:
                    manager.CloseOverlay();
                    PresentGameOver(manager);
                    Next(1.6f);
                    break;
                case 12:
                    Capture("07-game-over");
                    Next(0f);
                    break;
                case 13:
                    manager.CloseOverlay();
                    manager.LoadNextLevel();
                    loadingDeadline = Time.realtimeSinceStartup + 4f;
                    Next(0f);
                    break;
                case 14:
                    // The curtain is opaque while it fades in and out; wait for it to lift so
                    // the loading UI is actually visible when the frame is captured.
                    float curtain = CurtainAlpha(manager);
                    if (curtain > 0.25f && Time.realtimeSinceStartup < loadingDeadline)
                    {
                        nextStepAt = Time.realtimeSinceStartup + 0.1f;
                        break;
                    }

                    Debug.Log($"[UIShots] Loading curtain alpha {curtain:0.00}.");
                    Next(0.2f);
                    break;
                case 15:
                    Capture("08-loading");
                    Next(0f);
                    break;
                default:
                    Finish();
                    break;
            }
        }

        private static void PresentComplete(UIManager manager)
        {
            LevelCompleteScreen screen = UnityEngine.Object.FindFirstObjectByType<LevelCompleteScreen>(FindObjectsInactive.Include);
            if (screen != null)
            {
                screen.Present(new GameManager.LevelResult(22.45f, 24.10f, 1, true));
            }

            manager.ShowOverlay(UIScreenType.LevelComplete);
        }

        private static void PresentGameOver(UIManager manager)
        {
            GameOverScreen screen = UnityEngine.Object.FindFirstObjectByType<GameOverScreen>(FindObjectsInactive.Include);
            if (screen != null)
            {
                screen.Present(3);
            }

            manager.ShowOverlay(UIScreenType.GameOver);
        }

        private static void Next(float delay)
        {
            step++;
            nextStepAt = Time.realtimeSinceStartup + delay;
            settleDeadline = nextStepAt + 4f;
        }

        private static bool IsCaptureStep(int index) =>
            index == 0 || index == 2 || index == 4 || index == 6 || index == 8 || index == 10 || index == 12 || index == 15;

        /// <summary>
        /// True once every visible screen has finished fading in. The step delays are wall-clock
        /// but entrances advance per rendered frame, so a stalled player loop can otherwise let a
        /// capture run before a screen has populated itself.
        /// </summary>
        private static bool UiSettled()
        {
            foreach (UIScreen screen in UnityEngine.Object.FindObjectsByType<UIScreen>(FindObjectsSortMode.None))
            {
                if (screen.IsVisible && screen.Group != null && screen.Group.alpha < 0.98f)
                {
                    return false;
                }
            }

            return true;
        }

        private static float CurtainAlpha(UIManager manager)
        {
            FieldInfo field = typeof(UIManager).GetField("transitionGroup", BindingFlags.Instance | BindingFlags.NonPublic);
            CanvasGroup group = field != null ? field.GetValue(manager) as CanvasGroup : null;
            return group != null ? group.alpha : 1f;
        }

        private static void Capture(string name)
        {
            string path = Path.Combine(OutputDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[UIShots] Captured " + path);
        }

        private static void Finish()
        {
            running = false;
            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            Debug.Log("[UIShots] Capture finished.");

            if (Pending.Count > 0)
            {
                (int width, int height, string folder) = Pending.Dequeue();
                RequestCapture(width, height, folder);
            }
        }

        /// <summary>Adds a fixed Game View size (1920x1080 etc.) and selects it.</summary>
        public static void SetGameViewSize(int width, int height, string label)
        {
            try
            {
                Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
                Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type sizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");

                const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
                object instance = sizesType.GetProperty("instance", any).GetValue(null);
                object group = sizesType.GetProperty("currentGroup", any).GetValue(instance);
                if (instance == null || group == null)
                {
                    Debug.LogWarning("[UIShots] Game View size groups unavailable; using the current size.");
                    return;
                }

                MethodInfo getTotalCount = group.GetType().GetMethod("GetTotalCount", any);
                MethodInfo getSize = group.GetType().GetMethod("GetGameViewSize", any);
                int count = (int)getTotalCount.Invoke(group, null);

                int index = -1;
                for (int i = 0; i < count; i++)
                {
                    object size = getSize.Invoke(group, new object[] { i });
                    PropertyInfo widthProp = size.GetType().GetProperty("width", any);
                    PropertyInfo heightProp = size.GetType().GetProperty("height", any);
                    if (widthProp == null || heightProp == null)
                    {
                        continue;
                    }

                    if ((int)widthProp.GetValue(size) == width && (int)heightProp.GetValue(size) == height)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    ConstructorInfo ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
                    object size = ctor.Invoke(new object[] { Enum.Parse(sizeTypeEnum, "FixedResolution"), width, height, label });
                    group.GetType().GetMethod("AddCustomSize", any).Invoke(group, new[] { size });
                    index = count;
                }

                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", any);
                selected.SetValue(gameView, index);
                gameView.Repaint();
                Debug.Log($"[UIShots] Game View size set to {width}x{height}.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[UIShots] Could not set Game View size: " + exception);
            }
        }
    }
}
