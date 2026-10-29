using System;
using System.Collections.Generic;
using System.IO;
using RocketBoost.UI.Data;
using TMPro;
using UnityEditor;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace RocketBoost.UI.Editor
{
    /// <summary>
    /// One-click rebuild of the whole UI: theme, generated art, fonts, audio mixer,
    /// prefabs, screen prefabs and scene integration. Idempotent - run it again after any
    /// theme or layout change.
    /// </summary>
    public static class UIOverhaulBuilder
    {
        public const string ThemeDir = "Assets/_Project/UI/Theme";
        public const string ThemePath = ThemeDir + "/UITheme.asset";
        public const string CatalogPath = ThemeDir + "/LevelCatalog.asset";
        public const string FontDir = "Assets/_Project/UI/Fonts";
        public const string AudioDir = "Assets/_Project/Audio";
        public const string MixerPath = AudioDir + "/Mixer/GameAudioMixer.mixer";
        public const string RootPrefabPath = UIBuildUtil.PrefabDir + "/UIRoot.prefab";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Scene 1.unity",
            "Assets/Scenes/Scene 2.unity",
            "Assets/Scenes/Scene 3.unity"
        };

        private static readonly List<string> sceneProblems = new List<string>();

        [MenuItem("Rocket Boost/UI/Rebuild UI System", priority = 0)]
        public static void BuildAll()
        {
            Debug.Log("[UIOverhaul] Build started.");

            EnsureFolders();
            AssetDatabase.Refresh();

            UITheme theme = CreateTheme();
            UISpriteFactory.GenerateAll(theme);
            AssignSprites(theme);

            theme.displayFont = CreateFontAsset($"{FontDir}/Orbitron-Bold.ttf", $"{FontDir}/TMP_Orbitron_Display.asset", "TMP_Orbitron_Display");
            theme.bodyFont = CreateFontAsset($"{FontDir}/Rajdhani-Medium.ttf", $"{FontDir}/TMP_Rajdhani_Body.asset", "TMP_Rajdhani_Body");

            ConfigureAudioImporters();
            AudioMixer mixer = null;
            try { mixer = CreateAudioMixer(); } catch (Exception exception) { Debug.LogWarning("[UIOverhaul] Mixer creation skipped: " + exception.Message); }
            AssignAudio(theme, mixer);
            AssignSprites(theme);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            LevelCatalog catalog = CreateCatalog();
            List<UIScreen> screens = null;
            try { screens = UIScreenFactory.BuildScreens(theme, catalog, UIBuildUtil.PrefabDir); }
            catch (Exception screenException) { Debug.LogError("[UIOverhaul] Screen build failed: " + screenException); throw; }
            Texture2D[] starTextures =
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{UISpriteFactory.SpriteDir}/stars_far.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{UISpriteFactory.SpriteDir}/stars_mid.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{UISpriteFactory.SpriteDir}/stars_near.png")
            };

            UIScreenFactory.BuildRoot(theme, catalog, screens, starTextures, UIBuildUtil.PrefabDir);
            AssetDatabase.SaveAssets();

            IntegrateScenes();
            ApplyPlayerSettings();
            Validate();

            AssetDatabase.Refresh();
            Debug.Log("[UIOverhaul] Build finished.");
        }

        [MenuItem("Rocket Boost/UI/Verify UI Content", priority = 1)]
        public static void Validate()
        {
            int problems = 0;

            UITheme theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogError("[UIOverhaul] Theme asset missing.");
                problems++;
            }
            else
            {
                problems += Check(theme.displayFont != null, "display font");
                problems += Check(theme.bodyFont != null, "body font");
                problems += Check(theme.panelSprite != null, "panel sprite");
                problems += Check(theme.buttonNormalSprite != null, "button sprite");
                problems += Check(theme.buttonBorderSprite != null, "button border sprite");
                problems += Check(theme.iconPlay != null, "play icon");
                problems += Check(theme.starSprite != null, "star sprite");
                problems += Check(theme.menuMusic != null, "menu music");
                problems += Check(theme.masterGroup != null, "master mixer group");
            }

            problems += Check(AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath) != null, "UIRoot prefab");
            problems += Check(AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath) != null, "level catalog");

            // Scene contents are checked while IntegrateScenes already has each scene open;
            // reloading them here would only add load/unload churn and Unity warnings.
            foreach (string problem in sceneProblems)
            {
                problems += Check(false, problem);
            }

            sceneProblems.Clear();

            Debug.Log(problems == 0
                ? "[UIOverhaul] Verification passed."
                : $"[UIOverhaul] Verification found {problems} problem(s).");
        }

        private static int Check(bool condition, string label)
        {
            if (!condition)
            {
                Debug.LogError($"[UIOverhaul] Missing or invalid: {label}.");
                return 1;
            }

            return 0;
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                ThemeDir,
                FontDir,
                UIBuildUtil.PrefabDir,
                UISpriteFactory.SpriteDir,
                AudioDir + "/Music",
                AudioDir + "/SFX",
                AudioDir + "/Mixer",
                "Assets/_Project/UI/Animations"
            };

            foreach (string folder in folders)
            {
                Directory.CreateDirectory(folder);
            }
        }

        private static UITheme CreateTheme()
        {
            AssetDatabase.DeleteAsset(ThemePath);
            UITheme theme = ScriptableObject.CreateInstance<UITheme>();
            theme.name = "UITheme";
            theme.displayTracking = 5f;
            theme.titleSize = 108f;
            theme.headingSize = 40f;
            theme.bodySize = 24f;
            theme.labelSize = 18f;
            theme.spaceXs = 4f;
            theme.spaceSm = 8f;
            theme.spaceMd = 16f;
            theme.spaceLg = 24f;
            theme.spaceXl = 40f;
            theme.spaceXxl = 64f;
            theme.screenMargin = 72f;
            theme.radiusSm = 10f;
            theme.radiusMd = 16f;
            theme.radiusLg = 24f;
            theme.durationFast = 0.12f;
            theme.durationNormal = 0.22f;
            theme.durationSlow = 0.42f;
            theme.durationScreenFade = 0.3f;
            theme.countUpDuration = 0.9f;
            theme.hoverScale = 1.035f;
            theme.pressScale = 0.965f;
            theme.stagger = 0.06f;

            AssetDatabase.CreateAsset(theme, ThemePath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        }

        private static void AssignSprites(UITheme theme)
        {
            string dir = UISpriteFactory.SpriteDir;
            Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{name}.png");

            theme.panelSprite = Load("panel_glass");
            theme.panelDeepSprite = Load("panel_deep");
            theme.cardSprite = Load("panel_card");
            theme.buttonNormalSprite = Load("button_normal");
            theme.buttonHoverSprite = Load("button_hover");
            theme.buttonPressedSprite = Load("button_pressed");
            theme.buttonSelectedSprite = Load("button_selected");
            theme.buttonBorderSprite = Load("button_border");
            theme.buttonDisabledSprite = Load("button_disabled");
            theme.pillSprite = Load("pill");
            theme.gaugeTrackSprite = Load("gauge_track");
            theme.gaugeFillSprite = Load("gauge_fill");
            theme.sliderHandleSprite = Load("slider_handle");
            theme.backgroundGradientSprite = Load("bg_gradient");

            theme.glowSprite = Load("glow");
            theme.softDotSprite = Load("dot_soft");
            theme.starSprite = Load("star");
            theme.starSmallSprite = Load("star_small");
            theme.starOutlineSprite = Load("star_outline");
            theme.vignetteSprite = Load("vignette");
            theme.scanlineSprite = Load("scanlines");
            theme.nebulaSprite = Load("nebula");
            theme.ringSprite = Load("ring");

            theme.iconPlay = Load("icon_play");
            theme.iconPause = Load("icon_pause");
            theme.iconRestart = Load("icon_restart");
            theme.iconSettings = Load("icon_settings");
            theme.iconHome = Load("icon_home");
            theme.iconChevronLeft = Load("icon_chevron_left");
            theme.iconChevronRight = Load("icon_chevron_right");
            theme.iconLock = Load("icon_lock");
            theme.iconCheck = Load("icon_check");
            theme.iconVolume = Load("icon_volume");
            theme.iconFullscreen = Load("icon_fullscreen");
            theme.iconKeyboard = Load("icon_keyboard");
            theme.iconGamepad = Load("icon_gamepad");
            theme.iconStar = Load("icon_star");

            EditorUtility.SetDirty(theme);
        }



        private static TMP_FontAsset CreateFontAsset(string ttfPath, string assetPath, string name)
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null)
            {
                Debug.LogError($"[UIOverhaul] Font source missing: {ttfPath}");
                return null;
            }

            AssetDatabase.DeleteAsset(assetPath);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            asset.name = name;
            AssetDatabase.CreateAsset(asset, assetPath);

            if (asset.atlasTextures != null)
            {
                foreach (Texture atlas in asset.atlasTextures)
                {
                    if (atlas == null)
                    {
                        continue;
                    }

                    atlas.name = name + " Atlas";
                    AssetDatabase.AddObjectToAsset(atlas, asset);
                }
            }

            if (asset.material != null)
            {
                asset.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIOverhaul] Font " + name + " material=" + (asset.material != null) + " atlas=" + (asset.atlasTexture != null) + " atlasCount=" + (asset.atlasTextures != null ? asset.atlasTextures.Length : -1));
            return asset;
        }

        private static void ConfigureAudioImporters()
        {
            ConfigureClip($"{AudioDir}/SFX/ui_hover.ogg", 0.7f, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip($"{AudioDir}/SFX/ui_click.ogg", 0.7f, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip($"{AudioDir}/SFX/ui_back.ogg", 0.7f, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip($"{AudioDir}/SFX/ui_confirm.ogg", 0.7f, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip($"{AudioDir}/SFX/ui_crash.ogg", 0.8f, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip($"{AudioDir}/SFX/ui_level_complete.ogg", 0.8f, AudioClipLoadType.DecompressOnLoad);
            ConfigureClip($"{AudioDir}/Music/MenuAmbience_Loop.wav", 0.65f, AudioClipLoadType.CompressedInMemory);
        }

        private static void ConfigureClip(string path, float quality, AudioClipLoadType loadType)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[UIOverhaul] Audio clip not found: {path}");
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = quality;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            settings.preloadAudioData = loadType == AudioClipLoadType.DecompressOnLoad;
            importer.loadInBackground = loadType != AudioClipLoadType.DecompressOnLoad;
            importer.SaveAndReimport();
        }

        private static AudioMixer CreateAudioMixer()
        {
            if (File.Exists(MixerPath))
            {
                AssetDatabase.DeleteAsset(MixerPath);
            }

            // UnityEditor.Audio types are internal, so the mixer is authored through reflection.
            System.Reflection.Assembly editorAssembly = typeof(EditorApplication).Assembly;
            Type controllerType = editorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
            Type parameterPathType = editorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");

            if (controllerType == null || parameterPathType == null)
            {
                Debug.LogError("[UIOverhaul] AudioMixer editor API unavailable in this Unity version.");
                return null;
            }

            MethodInfo create = controllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static);
            object controller = create != null ? create.Invoke(null, new object[] { MixerPath }) : null;
            if (controller == null)
            {
                Debug.LogError("[UIOverhaul] Could not create AudioMixer.");
                return null;
            }

            object master = controllerType.GetProperty("masterGroup")?.GetValue(controller);
            MethodInfo createGroup = controllerType.GetMethod("CreateNewGroup");
            object music = createGroup.Invoke(controller, new object[] { "Music", false });
            object sfx = createGroup.Invoke(controller, new object[] { "SFX", false });

            MethodInfo addChild = controllerType.GetMethod("AddChildToParent");
            SafeInvoke(addChild, controller, new[] { music, master }, "parent Music");
            SafeInvoke(addChild, controller, new[] { sfx, master }, "parent SFX");

            object snapshot = controllerType.GetProperty("TargetSnapshot")?.GetValue(controller);
            SetGroupVolume(controller, master, snapshot, 0f);
            SetGroupVolume(controller, music, snapshot, -4f);
            SetGroupVolume(controller, sfx, snapshot, -2f);

            MethodInfo addExposed = controllerType.GetMethod("AddExposedParameter");
            string masterGuid = ExposeVolume(controller, parameterPathType, addExposed, master);
            string musicGuid = ExposeVolume(controller, parameterPathType, addExposed, music);
            string sfxGuid = ExposeVolume(controller, parameterPathType, addExposed, sfx);

            Dictionary<string, string> names = new Dictionary<string, string>
            {
                { masterGuid, AudioParams.MasterVolume },
                { musicGuid, AudioParams.MusicVolume },
                { sfxGuid, AudioParams.SfxVolume }
            };

            RenameExposedParameters(controllerType, controller, names);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            int groupCount = mixer != null ? mixer.FindMatchingGroups("Master").Length : 0;
            Debug.Log("[UIOverhaul] Mixer created with " + groupCount + " master group(s).");
            return mixer;
        }

        private static void SafeInvoke(MethodInfo method, object target, object[] args, string label)
        {
            if (method == null)
            {
                Debug.LogWarning("[UIOverhaul] Mixer API unavailable: " + label);
                return;
            }

            try
            {
                method.Invoke(target, args);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[UIOverhaul] Mixer step failed (" + label + "): " + exception.Message);
            }
        }

        private static void SetGroupVolume(object controller, object group, object snapshot, float decibels)
        {
            try
            {
                group.GetType()
                    .GetMethod("SetValueForVolume")
                    ?.Invoke(group, new[] { controller, snapshot, (object)decibels });
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[UIOverhaul] Set volume failed: " + exception.Message);
            }
        }

        private static string ExposeVolume(object controller, Type parameterPathType, MethodInfo addExposed, object group)
        {
            object guid = group.GetType().GetMethod("GetGUIDForVolume")?.Invoke(group, null);
            object path = Activator.CreateInstance(parameterPathType, new object[] { group, guid });
            try { addExposed.Invoke(controller, new[] { path }); } catch (Exception exception) { Debug.LogWarning("[UIOverhaul] Expose parameter failed: " + exception.Message); }
            return guid?.ToString() ?? string.Empty;
        }

        private static void RenameExposedParameters(Type controllerType, object controller, Dictionary<string, string> names)
        {
            System.Reflection.PropertyInfo property = controllerType.GetProperty("exposedParameters");
            if (property == null || !(property.GetValue(controller) is Array exposed))
            {
                return;
            }

            Type elementType = exposed.GetType().GetElementType();
            FieldInfo guidField = elementType?.GetField("guid");
            FieldInfo nameField = elementType?.GetField("name");
            if (guidField == null || nameField == null)
            {
                return;
            }

            for (int i = 0; i < exposed.Length; i++)
            {
                object element = exposed.GetValue(i);
                string guid = guidField.GetValue(element)?.ToString();
                if (guid != null && names.TryGetValue(guid, out string name))
                {
                    nameField.SetValue(element, name);
                    exposed.SetValue(element, i);
                }
            }

            property.SetValue(controller, exposed);
            EditorUtility.SetDirty(controller as UnityEngine.Object);
        }

        private static void AssignAudio(UITheme theme, AudioMixer mixer)
        {
            theme.hoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/ui_hover.ogg");
            theme.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/ui_click.ogg");
            theme.backClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/ui_back.ogg");
            theme.confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/ui_confirm.ogg");
            theme.crashClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/ui_crash.ogg");
            theme.levelCompleteClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/SFX/ui_level_complete.ogg");
            theme.menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/Music/MenuAmbience_Loop.wav");

            if (mixer == null)
            {
                return;
            }

            AudioMixerGroup[] master = mixer.FindMatchingGroups("Master");
            AudioMixerGroup[] music = mixer.FindMatchingGroups("Music");
            AudioMixerGroup[] sfx = mixer.FindMatchingGroups("SFX");

            theme.masterGroup = master.Length > 0 ? master[0] : null;
            theme.musicGroup = music.Length > 0 ? music[0] : null;
            theme.sfxGroup = sfx.Length > 0 ? sfx[0] : null;
            EditorUtility.SetDirty(theme);
        }

        private static LevelCatalog CreateCatalog()
        {
            AssetDatabase.DeleteAsset(CatalogPath);
            LevelCatalog catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.name = "LevelCatalog";

            LevelCatalog.LevelDefinition[] levels =
            {
                new LevelCatalog.LevelDefinition
                {
                    buildIndex = 1,
                    sceneName = "Scene 1",
                    sectorCode = "SECTOR 01",
                    displayName = "FLY OVER",
                    tagline = "Thread the canyon and kiss the landing pad.",
                    threeStarTime = 24f,
                    twoStarTime = 40f
                },
                new LevelCatalog.LevelDefinition
                {
                    buildIndex = 2,
                    sceneName = "Scene 2",
                    sectorCode = "SECTOR 02",
                    displayName = "ORBITAL MAZE",
                    tagline = "Weave the debris field. No brakes, no excuses.",
                    threeStarTime = 48f,
                    twoStarTime = 75f
                },
                new LevelCatalog.LevelDefinition
                {
                    buildIndex = 3,
                    sceneName = "Scene 3",
                    sectorCode = "SECTOR 03",
                    displayName = "FINAL APPROACH",
                    tagline = "One corridor. One pad. No second chances.",
                    threeStarTime = 42f,
                    twoStarTime = 70f
                }
            };

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty array = serialized.FindProperty("levels");
            array.arraySize = levels.Length;

            for (int i = 0; i < levels.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("buildIndex").intValue = levels[i].buildIndex;
                element.FindPropertyRelative("sceneName").stringValue = levels[i].sceneName;
                element.FindPropertyRelative("sectorCode").stringValue = levels[i].sectorCode;
                element.FindPropertyRelative("displayName").stringValue = levels[i].displayName;
                element.FindPropertyRelative("tagline").stringValue = levels[i].tagline;
                element.FindPropertyRelative("threeStarTime").floatValue = levels[i].threeStarTime;
                element.FindPropertyRelative("twoStarTime").floatValue = levels[i].twoStarTime;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
        }

        private static void IntegrateScenes()
        {
            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (rootPrefab == null)
            {
                Debug.LogError("[UIOverhaul] UIRoot prefab missing; cannot integrate scenes.");
                return;
            }

            foreach (string scenePath in ScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"[UIOverhaul] Scene missing: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "MainMenu Canvas" || root.name == "InGame Canvas" ||
                        root.name == "EventSystem" || root.name == "UIRoot")
                    {
                        UnityEngine.Object.DestroyImmediate(root);
                    }
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab, scene);
                instance.transform.position = Vector3.zero;
                instance.name = "UIRoot";

                int uiRoots = 0;
                int canvases = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "UIRoot")
                    {
                        uiRoots++;
                    }

                    canvases += root.GetComponentsInChildren<Canvas>(true).Length;
                }

                if (uiRoots != 1)
                {
                    sceneProblems.Add($"{Path.GetFileName(scenePath)} has exactly one UIRoot (found {uiRoots})");
                }

                if (canvases > 1)
                {
                    sceneProblems.Add($"{Path.GetFileName(scenePath)} has one canvas (found {canvases})");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, scenePath);
                Debug.Log($"[UIOverhaul] Integrated UI into {Path.GetFileName(scenePath)}.");
            }

            EnsureBuildSettings();
        }

        private static void EnsureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in ScenePaths)
            {
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            
        }
    }
}
