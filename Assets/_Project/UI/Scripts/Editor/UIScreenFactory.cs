using System.Collections.Generic;
using RocketBoost.UI.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI.Editor
{
    /// <summary>
    /// Builds every screen prefab and the persistent UIRoot prefab. Layout is authored against
    /// the 1920x1080 reference and anchored so it survives 16:10, 21:9 and portrait.
    /// </summary>
    public static class UIScreenFactory
    {
        private const float Margin = 72f;
        private static LevelCard cachedLevelCard;

        public static List<UIScreen> BuildScreens(UITheme theme, LevelCatalog catalog, string outputDir)
        {
            List<UIScreen> screens = new List<UIScreen>();

            BuildLevelCardPrefab(theme);

            Debug.Log("[UIOverhaul] Building MainMenuScreen...");
            screens.Add(BuildMainMenu(theme, outputDir));
            Debug.Log("[UIOverhaul] Building LevelSelectScreen...");
            screens.Add(BuildLevelSelect(theme, outputDir));
            Debug.Log("[UIOverhaul] Building HudScreen...");
            screens.Add(BuildHud(theme, outputDir));
            Debug.Log("[UIOverhaul] Building PauseScreen...");
            screens.Add(BuildPause(theme, outputDir));
            Debug.Log("[UIOverhaul] Building LevelCompleteScreen...");
            screens.Add(BuildLevelComplete(theme, outputDir));
            Debug.Log("[UIOverhaul] Building GameOverScreen...");
            screens.Add(BuildGameOver(theme, outputDir));
            Debug.Log("[UIOverhaul] Building SettingsScreen...");
            screens.Add(BuildSettings(theme, outputDir));
            Debug.Log("[UIOverhaul] Building LoadingScreen...");
            screens.Add(BuildLoading(theme, outputDir));

            return screens;
        }

        // ------------------------------------------------------------------ main menu

        private static UIScreen BuildMainMenu(UITheme theme, string outputDir)
        {
            MainMenuScreen screen = Create<MainMenuScreen>("MainMenuScreen", UIScreenType.MainMenu, theme, backdrop: true, gameplay: false);
            RectTransform content = ContentOf(screen);

            RectTransform column = NewRect("MenuColumn", content);
            Place(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(1180f, 940f));
            ResponsiveRect responsive = column.gameObject.AddComponent<ResponsiveRect>();
            SetResponsive(responsive,
                new ResponsiveRect.Layout
                {
                    anchorMin = new Vector2(0.5f, 0.5f), anchorMax = new Vector2(0.5f, 0.5f), pivot = new Vector2(0.5f, 0.5f),
                    anchoredPosition = new Vector2(0f, 26f), sizeDelta = new Vector2(1180f, 940f)
                },
                new ResponsiveRect.Layout
                {
                    anchorMin = new Vector2(0.5f, 0.5f), anchorMax = new Vector2(0.5f, 0.5f), pivot = new Vector2(0.5f, 0.5f),
                    anchoredPosition = new Vector2(0f, 90f), sizeDelta = new Vector2(1000f, 1020f)
                });

            // ---- logo lockup, centred across the top like a title card
            RectTransform logo = NewRect("LogoGroup", column);
            Place(logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1180f, 372f));
            logo.gameObject.AddComponent<UIFloat>();
            UIBuildUtil.Set(screen, "logoGroup", logo);

            RectTransform logoGlow = NewRect("LogoGlow", logo);
            Place(logoGlow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(900f, 520f));
            UIBuildUtil.AddImage(logoGlow, theme.glowSprite, UITheme.Alpha(theme.primary, 0.13f));
            UIPulse pulse = logoGlow.gameObject.AddComponent<UIPulse>();
            UIBuildUtil.Set(pulse, "minAlpha", 0.06f);
            UIBuildUtil.Set(pulse, "maxAlpha", 0.16f);
            UIBuildUtil.Set(pulse, "speed", 1.1f);

            TextMeshProUGUI titleA = CenteredLabel(logo, "TitleRocket", "ROCKET", theme.displayFont, 112f, theme.textPrimary, 18f,
                new Vector2(1180f, 128f), 6f);
            TextMeshProUGUI titleB = CenteredLabel(logo, "TitleBoost", "BOOST", theme.displayFont, 112f, theme.primary, 138f,
                new Vector2(1180f, 128f), 12f);

            RectTransform accent = NewRect("AccentRule", logo);
            Place(accent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -278f), new Vector2(280f, 4f));
            UIBuildUtil.AddImage(accent, theme.selectionStreakSprite, theme.primary, sliced: true);

            TextMeshProUGUI subtitle = CenteredLabel(logo, "Subtitle", "THREE SECTORS.  ONE PERFECT LANDING.", theme.bodyFont, 21f,
                theme.textSecondary, 296f, new Vector2(1180f, 32f), 5f);

            // ---- the list itself
            TextMeshProUGUI listEyebrow = CenteredLabel(column, "MenuEyebrow", "-  MENU  -", theme.bodyFont,
                theme.menuEyebrowSize, theme.primary, 400f, new Vector2(600f, 26f), 9f);

            RectTransform menu = NewRect("MenuGroup", column);
            Place(menu, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -438f),
                new Vector2(theme.menuListWidth, theme.menuListSpacing * 4f));
            CanvasGroup menuGroup = menu.gameObject.AddComponent<CanvasGroup>();
            UIBuildUtil.Set(screen, "menuGroup", menuGroup);

            ThemedButton play = ListRow(menu, theme, "PlayButton", "PLAY", "Launch the first sector of the campaign.", 0, 4);
            ThemedButton levelSelect = ListRow(menu, theme, "LevelSelectButton", "LEVEL SELECT", "Jump straight to any unlocked sector.", 1, 4);
            ThemedButton settings = ListRow(menu, theme, "SettingsButton", "SETTINGS", "Tune audio, display and the control guide.", 2, 4);
            ThemedButton quit = ListRow(menu, theme, "QuitButton", "QUIT", "Close Rocket Boost and return to desktop.", 3, 4);

            TextMeshProUGUI hint = CenteredLabel(column, "HintLabel", "Launch the first sector of the campaign.",
                theme.bodyFont, theme.menuHintSize, theme.textSecondary, 764f, new Vector2(900f, 30f), 2f);
            MenuHint caption = screen.gameObject.AddComponent<MenuHint>();
            UIBuildUtil.Set(caption, "label", hint);
            UIBuildUtil.Set(caption, "fadeDuration", theme.durationFast);

            PromptStrip(content, theme, ("A", "CONFIRM"), ("B", "BACK"));

            TextMeshProUGUI version = Label(content, "VersionLabel", "BUILD 1.0", theme.bodyFont, 13f, theme.textMuted,
                TextAlignmentOptions.BottomLeft, new Vector2(48f, 96f), new Vector2(600f, 22f), 4f);
            // Pinned to the screen corner instead of the column so narrow aspects (portrait)
            // never drop it on top of the stacked menu buttons.
            version.rectTransform.anchorMin = Vector2.zero;
            version.rectTransform.anchorMax = Vector2.zero;
            version.rectTransform.pivot = Vector2.zero;

            UIBuildUtil.Set(screen, "playButton", play);
            UIBuildUtil.Set(screen, "levelSelectButton", levelSelect);
            UIBuildUtil.Set(screen, "settingsButton", settings);
            UIBuildUtil.Set(screen, "quitButton", quit);
            UIBuildUtil.Set(screen, "versionLabel", version);
            UIBuildUtil.Set(screen, "subtitleLabel", subtitle);
            UIBuildUtil.Set(screen, "defaultSelection", play.Button);

            _ = listEyebrow;
            _ = titleA;
            _ = titleB;

            return SaveScreen(screen, "MainMenuScreen");
        }

        // ------------------------------------------------------------------ level select

        private static UIScreen BuildLevelSelect(UITheme theme, string outputDir)
        {
            LevelSelectScreen screen = Create<LevelSelectScreen>("LevelSelectScreen", UIScreenType.LevelSelect, theme, backdrop: true, gameplay: false);
            RectTransform content = ContentOf(screen);

            ThemedButton back = BackButton(content, theme, new Vector2(Margin, -Margin));

            Label(content, "Title", "SELECT SECTOR", theme.displayFont, 54f, theme.textPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(Margin + 92f, -Margin - 8f), new Vector2(900f, 64f), 4f);

            Label(content, "Subtitle", "CHOOSE YOUR FLIGHT PATH  ·  BEAT THE SECTOR RECORD", theme.bodyFont, 18f, theme.textSecondary,
                TextAlignmentOptions.TopLeft, new Vector2(Margin + 94f, -Margin - 78f), new Vector2(900f, 28f), 4f);

            TextMeshProUGUI progress = Label(content, "Progress", "CLEARED 0 / 3", theme.bodyFont, 18f, theme.primary,
                TextAlignmentOptions.TopRight, new Vector2(-Margin - 4f, -Margin - 14f), new Vector2(420f, 28f), 4f);
            progress.rectTransform.anchorMin = new Vector2(1f, 1f);
            progress.rectTransform.anchorMax = new Vector2(1f, 1f);
            progress.rectTransform.pivot = new Vector2(1f, 1f);

            TextMeshProUGUI summary = Label(content, "Summary", "COMPLETE A SECTOR TO UNLOCK THE NEXT", theme.bodyFont, 15f, theme.textMuted,
                TextAlignmentOptions.TopRight, new Vector2(-Margin - 4f, -Margin - 46f), new Vector2(560f, 24f), 3f);
            summary.rectTransform.anchorMin = new Vector2(1f, 1f);
            summary.rectTransform.anchorMax = new Vector2(1f, 1f);
            summary.rectTransform.pivot = new Vector2(1f, 1f);

            RectTransform grid = NewRect("CardGrid", content);
            grid.anchorMin = new Vector2(0f, 0f);
            grid.anchorMax = new Vector2(1f, 1f);
            grid.pivot = new Vector2(0.5f, 0.5f);
            grid.offsetMin = new Vector2(96f, 116f);
            grid.offsetMax = new Vector2(-96f, -206f);

            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(468f, 336f);
            layout.spacing = new Vector2(30f, 28f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(0, 0, 8, 0);

            ResponsiveGrid responsive = grid.gameObject.AddComponent<ResponsiveGrid>();
            UIBuildUtil.Set(responsive, "grid", layout);
            UIBuildUtil.Set(responsive, "landscapeCell", new Vector2(468f, 336f));
            UIBuildUtil.Set(responsive, "portraitCell", new Vector2(880f, 268f));
            UIBuildUtil.Set(responsive, "landscapeColumns", 3);
            UIBuildUtil.Set(responsive, "portraitColumns", 1);

            LevelCard cardPrefab = cachedLevelCard != null ? cachedLevelCard : LoadLevelCard();
            PromptStrip(content, theme, ("A", "LAUNCH"), ("B", "BACK"));
            UIBuildUtil.Set(screen, "backButton", back);
            UIBuildUtil.Set(screen, "cardContainer", grid);
            UIBuildUtil.Set(screen, "cardPrefab", cardPrefab);
            UIBuildUtil.Set(screen, "progressLabel", progress);
            UIBuildUtil.Set(screen, "summaryLabel", summary);
            UIBuildUtil.Set(screen, "defaultSelection", back.Button);

            return SaveScreen(screen, "LevelSelectScreen");
        }

        public static LevelCard BuildLevelCardPrefab(UITheme theme)
        {
            RectTransform root = NewRect("LevelCard", null);
            root.sizeDelta = new Vector2(468f, 336f);
            Image background = UIBuildUtil.AddImage(root, theme.buttonNormalSprite, theme.surfaceRaised, sliced: true, raycast: true);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            ThemedButton themed = root.gameObject.AddComponent<ThemedButton>();
            UIBuildUtil.Set(themed, "button", button);
            UIBuildUtil.Set(themed, "background", background);
            UIBuildUtil.Set(themed, "variant", UIButtonVariant.Default);
            themed.ApplyTheme(theme);

            RectTransform accent = NewRect("AccentBar", root);
            accent.anchorMin = new Vector2(0f, 1f);
            accent.anchorMax = new Vector2(1f, 1f);
            accent.pivot = new Vector2(0.5f, 1f);
            accent.anchoredPosition = new Vector2(0f, -18f);
            accent.sizeDelta = new Vector2(-48f, 3f);
            Image accentImage = UIBuildUtil.AddImage(accent, theme.glowSprite, theme.primary);

            TextMeshProUGUI sector = Label(root, "SectorLabel", "SECTOR 01", theme.bodyFont, 15f, theme.primary,
                TextAlignmentOptions.TopLeft, new Vector2(30f, -36f), new Vector2(280f, 22f), 7f);

            TextMeshProUGUI title = Label(root, "NameLabel", "FLY OVER", theme.displayFont, 34f, theme.textPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(30f, -66f), new Vector2(408f, 46f), 2f);

            TextMeshProUGUI tagline = Label(root, "TaglineLabel", "Thread the canyon and stick the landing.", theme.bodyFont, 16f, theme.textSecondary,
                TextAlignmentOptions.TopLeft, new Vector2(30f, -120f), new Vector2(408f, 52f), 1f);
            tagline.textWrappingMode = TextWrappingModes.Normal;

            RectTransform divider = NewRect("Divider", root);
            divider.anchorMin = new Vector2(0f, 0f);
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(0.5f, 0f);
            divider.anchoredPosition = new Vector2(0f, 118f);
            divider.sizeDelta = new Vector2(-60f, 1f);
            UIBuildUtil.AddImage(divider, null, theme.borderDim);

            RectTransform starRow = NewRect("Stars", root);
            starRow.anchorMin = new Vector2(0f, 0f);
            starRow.anchorMax = new Vector2(0f, 0f);
            starRow.pivot = new Vector2(0f, 0f);
            starRow.anchoredPosition = new Vector2(30f, 74f);
            starRow.sizeDelta = new Vector2(150f, 34f);

            Image[] stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform starRect = NewRect($"Star{i + 1}", starRow);
                starRect.anchorMin = new Vector2(0f, 0.5f);
                starRect.anchorMax = new Vector2(0f, 0.5f);
                starRect.pivot = new Vector2(0f, 0.5f);
                starRect.anchoredPosition = new Vector2(i * 44f, 0f);
                starRect.sizeDelta = new Vector2(34f, 34f);
                stars[i] = UIBuildUtil.AddImage(starRect, theme.starOutlineSprite, theme.starEmpty);
            }

            StarRating rating = starRow.gameObject.AddComponent<StarRating>();
            UIBuildUtil.Set(rating, "stars", stars);
            UIBuildUtil.Set(rating, "emptyScale", 0.7f);

            TextMeshProUGUI best = Label(root, "BestTime", "NO RECORD", theme.bodyFont, 16f, theme.textMuted,
                TextAlignmentOptions.BottomLeft, new Vector2(30f, 28f), new Vector2(300f, 26f), 2f);
            best.rectTransform.anchorMin = Vector2.zero;
            best.rectTransform.anchorMax = Vector2.zero;
            best.rectTransform.pivot = Vector2.zero;

            RectTransform lockOverlay = NewRect("LockOverlay", root);
            UIBuildUtil.Stretch(lockOverlay, 1f, 1f, 1f, 1f);
            Image scrim = UIBuildUtil.AddImage(lockOverlay, theme.panelSprite, UITheme.Alpha(theme.backgroundDeep, 0.72f), sliced: true, raycast: true);
            _ = scrim;

            RectTransform lockIcon = NewRect("LockIcon", lockOverlay);
            Place(lockIcon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(44f, 44f));
            UIBuildUtil.AddImage(lockIcon, theme.iconLock, theme.textMuted);

            Label(lockOverlay, "LockLabel", "LOCKED", theme.displayFont, 22f, theme.textSecondary,
                TextAlignmentOptions.Center, new Vector2(0f, -34f), new Vector2(300f, 30f), 8f, true);

            RectTransform badge = NewRect("RecordBadge", root);
            Place(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(146f, 30f));
            UIBuildUtil.AddImage(badge, theme.pillSprite, theme.starGold, sliced: true);
            Label(badge, "BadgeLabel", "NEW RECORD", theme.bodyFont, 11f, theme.backgroundDeep,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(146f, 30f), 3f, true);
            badge.gameObject.SetActive(false);

            LevelCard card = root.gameObject.AddComponent<LevelCard>();
            UIBuildUtil.Set(card, "button", themed);
            UIBuildUtil.Set(card, "sectorLabel", sector);
            UIBuildUtil.Set(card, "nameLabel", title);
            UIBuildUtil.Set(card, "taglineLabel", tagline);
            UIBuildUtil.Set(card, "bestTimeLabel", best);
            UIBuildUtil.Set(card, "stars", rating);
            UIBuildUtil.Set(card, "lockOverlay", lockOverlay.gameObject);
            UIBuildUtil.Set(card, "newRecordBadge", badge.gameObject);
            UIBuildUtil.Set(card, "accentBar", accentImage);

            GameObject cardAsset = UIBuildUtil.SavePrefab(root.gameObject, "LevelCard");
            cachedLevelCard = cardAsset != null ? cardAsset.GetComponent<LevelCard>() : null;
            return cachedLevelCard;
        }

        // ------------------------------------------------------------------ HUD

        private static UIScreen BuildHud(UITheme theme, string outputDir)
        {
            HudScreen screen = Create<HudScreen>("HudScreen", UIScreenType.Hud, theme, backdrop: false, gameplay: true);
            RectTransform content = ContentOf(screen);

            RectTransform topBar = NewRect("TopBar", content);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.anchoredPosition = new Vector2(0f, -28f);
            topBar.sizeDelta = new Vector2(-2f * Margin, 96f);

            RectTransform chip = NewRect("SectorChip", topBar);
            Place(chip, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 14f), new Vector2(470f, 54f));
            UIBuildUtil.AddImage(chip, theme.pillSprite, UITheme.Alpha(theme.surface, 0.85f), sliced: true);
            TextMeshProUGUI sector = Label(chip, "SectorLabel", "[ LIVE ]  SECTOR 01", theme.bodyFont, 17f, theme.textPrimary,
                TextAlignmentOptions.MidlineLeft, new Vector2(22f, 0f), new Vector2(430f, 54f), 4f, true);
            sector.rectTransform.anchorMin = new Vector2(0f, 0f);
            sector.rectTransform.anchorMax = new Vector2(1f, 1f);
            UIBuildUtil.Stretch(sector.rectTransform, 22f, 18f, 0f, 0f);

            RectTransform timerGroup = NewRect("TimerGroup", topBar);
            Place(timerGroup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(320f, 96f));

            TextMeshProUGUI timerCaption = Label(timerGroup, "TimerCaption", "MISSION TIME", theme.bodyFont, 12f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -4f), new Vector2(320f, 18f), 7f);
            timerCaption.rectTransform.anchorMin = new Vector2(0f, 1f);
            timerCaption.rectTransform.anchorMax = new Vector2(1f, 1f);
            timerCaption.rectTransform.pivot = new Vector2(0.5f, 1f);
            // Stretching the horizontal anchors makes sizeDelta.x an offset on top of the
            // parent width, so it has to be cleared or the label renders twice as wide.
            timerCaption.rectTransform.sizeDelta = new Vector2(0f, 18f);

            TextMeshProUGUI timer = Label(timerGroup, "Timer", "00:00.00", theme.displayFont, 46f, theme.textPrimary,
                TextAlignmentOptions.Center, new Vector2(0f, -24f), new Vector2(320f, 60f), 3f);
            timer.rectTransform.anchorMin = new Vector2(0f, 1f);
            timer.rectTransform.anchorMax = new Vector2(1f, 1f);
            timer.rectTransform.pivot = new Vector2(0.5f, 1f);
            timer.rectTransform.sizeDelta = new Vector2(0f, 60f);

            ThemedButton pause = Button(topBar, "PauseButton", string.Empty, theme, UIButtonVariant.Ghost, theme.iconPause,
                Vector2.zero, new Vector2(60f, 60f));
            RectTransform pauseRect = (RectTransform)pause.transform;
            pauseRect.anchorMin = new Vector2(1f, 0.5f);
            pauseRect.anchorMax = new Vector2(1f, 0.5f);
            pauseRect.pivot = new Vector2(1f, 0.5f);
            pauseRect.anchoredPosition = new Vector2(0f, 14f);

            TextMeshProUGUI attempts = Label(topBar, "Attempts", "ATTEMPT #1", theme.bodyFont, 13f, theme.textMuted,
                TextAlignmentOptions.Right, new Vector2(-4f, -28f), new Vector2(260f, 20f), 4f);
            attempts.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            attempts.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            attempts.rectTransform.pivot = new Vector2(1f, 0.5f);

            RectTransform bottomBar = NewRect("BottomBar", content);
            bottomBar.anchorMin = new Vector2(0f, 0f);
            bottomBar.anchorMax = new Vector2(1f, 0f);
            bottomBar.pivot = new Vector2(0.5f, 0f);
            bottomBar.anchoredPosition = new Vector2(0f, 30f);
            bottomBar.sizeDelta = new Vector2(-2f * Margin, 132f);

            RectTransform boostGroup = NewRect("BoostGroup", bottomBar);
            Place(boostGroup, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(360f, 108f));
            Label(boostGroup, "BoostLabel", "BOOST", theme.bodyFont, 14f, theme.textMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(180f, 20f), 7f);
            TextMeshProUGUI boostValue = Label(boostGroup, "BoostValue", "0%", theme.bodyFont, 18f, theme.primary,
                TextAlignmentOptions.TopRight, new Vector2(0f, 0f), new Vector2(360f, 24f), 2f);
            boostValue.rectTransform.anchorMin = new Vector2(0f, 1f);
            boostValue.rectTransform.anchorMax = new Vector2(1f, 1f);
            boostValue.rectTransform.pivot = new Vector2(0.5f, 1f);
            boostValue.rectTransform.sizeDelta = new Vector2(0f, 24f);

            GaugeBar boostBar = Gauge(boostGroup, theme, new Vector2(0f, -34f), new Vector2(360f, 18f), boostValue);

            RectTransform speedGroup = NewRect("SpeedGroup", bottomBar);
            Place(speedGroup, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(404f, 2f), new Vector2(300f, 108f));
            Label(speedGroup, "SpeedLabel", "SPEED", theme.bodyFont, 14f, theme.textMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(160f, 20f), 7f);

            TextMeshProUGUI speedValue = Label(speedGroup, "SpeedValue", "000", theme.displayFont, 44f, theme.textPrimary,
                TextAlignmentOptions.BottomLeft, new Vector2(0f, -30f), new Vector2(160f, 54f), 2f);
            Label(speedGroup, "SpeedUnit", "M/S", theme.bodyFont, 14f, theme.textMuted,
                TextAlignmentOptions.BottomLeft, new Vector2(158f, -28f), new Vector2(80f, 24f), 4f);

            GaugeBar speedBar = Gauge(speedGroup, theme, new Vector2(0f, -86f), new Vector2(260f, 6f), null);
            UIBuildUtil.Set(speedBar, "showPercent", false);

            RectTransform hint = NewRect("HintPill", bottomBar);
            hint.anchorMin = new Vector2(1f, 0f);
            hint.anchorMax = new Vector2(1f, 0f);
            hint.pivot = new Vector2(1f, 0f);
            hint.anchoredPosition = new Vector2(0f, 24f);
            hint.sizeDelta = new Vector2(600f, 52f);
            UIBuildUtil.AddImage(hint, theme.pillSprite, UITheme.Alpha(theme.surface, 0.7f), sliced: true);
            RectTransform hintIcon = NewRect("Icon", hint);
            Place(hintIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(24f, 24f));
            UIBuildUtil.AddImage(hintIcon, theme.iconKeyboard, theme.textMuted);
            Label(hint, "HintText", "SPACE THRUST    A / D ROTATE    ESC PAUSE", theme.bodyFont, 13f, theme.textSecondary,
                TextAlignmentOptions.MidlineLeft, new Vector2(56f, 0f), new Vector2(530f, 52f), 3f, true);
            CanvasGroup hintGroup = hint.gameObject.AddComponent<CanvasGroup>();

            UIBuildUtil.Set(screen, "sectorLabel", sector);
            UIBuildUtil.Set(screen, "timerLabel", timer);
            UIBuildUtil.Set(screen, "attemptLabel", attempts);
            UIBuildUtil.Set(screen, "speedLabel", speedValue);
            UIBuildUtil.Set(screen, "boostBar", boostBar);
            UIBuildUtil.Set(screen, "speedBar", speedBar);
            UIBuildUtil.Set(screen, "pauseButton", pause);
            UIBuildUtil.Set(screen, "topBar", topBar);
            UIBuildUtil.Set(screen, "bottomBar", bottomBar);
            UIBuildUtil.Set(screen, "hintGroup", hintGroup);

            return SaveScreen(screen, "HudScreen");
        }

        // ------------------------------------------------------------------ pause

        private static UIScreen BuildPause(UITheme theme, string outputDir)
        {
            PauseScreen screen = Create<PauseScreen>("PauseScreen", UIScreenType.Pause, theme, backdrop: false, gameplay: false);
            RectTransform content = ContentOf(screen);
            Scrim(content, 0.72f);

            // Boxless: the scrim carries the dimming, so the panel is only a transform to pop.
            RectTransform panel = NewRect("Panel", content);
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 780f));

            RectTransform accent = NewRect("Accent", panel);
            Place(accent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 322f), new Vector2(320f, 8f));
            UIBuildUtil.AddImage(accent, theme.selectionStreakSprite, theme.primary, sliced: true);

            Label(panel, "Title", "PAUSED", theme.displayFont, 62f, theme.textPrimary,
                TextAlignmentOptions.Center, new Vector2(0f, 254f), new Vector2(700f, 76f), 14f, true);

            TextMeshProUGUI sector = Label(panel, "Sector", "SECTOR 01", theme.bodyFont, 17f, theme.primary,
                TextAlignmentOptions.Center, new Vector2(0f, 196f), new Vector2(700f, 26f), 6f, true);

            TextMeshProUGUI time = Label(panel, "Time", "MISSION TIME  00:00.00", theme.bodyFont, 16f, theme.textSecondary,
                TextAlignmentOptions.Center, new Vector2(0f, 164f), new Vector2(700f, 26f), 3f, true);

            RectTransform list = NewRect("MenuGroup", panel);
            Place(list, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -46f),
                new Vector2(theme.menuListWidth, theme.menuListSpacing * 4f));

            ThemedButton resume = ListRow(list, theme, "ResumeButton", "RESUME", "Return to the flight.", 0, 4);
            ThemedButton restart = ListRow(list, theme, "RestartButton", "RESTART", "Fly the sector again from the start.", 1, 4);
            ThemedButton settings = ListRow(list, theme, "SettingsButton", "SETTINGS", "Tune audio, display and the control guide.", 2, 4);
            ThemedButton mainMenu = ListRow(list, theme, "MainMenuButton", "MAIN MENU", "Abandon the run and return to the menu.", 3, 4);

            TextMeshProUGUI hint = Label(panel, "HintLabel", "Return to the flight.", theme.bodyFont, theme.menuHintSize,
                theme.textSecondary, TextAlignmentOptions.Center, new Vector2(0f, -272f), new Vector2(860f, 28f), 2f, true);
            MenuHint caption = screen.gameObject.AddComponent<MenuHint>();
            UIBuildUtil.Set(caption, "label", hint);
            UIBuildUtil.Set(caption, "fadeDuration", theme.durationFast);

            PromptStrip(content, theme, ("A", "CONFIRM"), ("B", "RESUME"));

            UIBuildUtil.Set(screen, "resumeButton", resume);
            UIBuildUtil.Set(screen, "restartButton", restart);
            UIBuildUtil.Set(screen, "settingsButton", settings);
            UIBuildUtil.Set(screen, "mainMenuButton", mainMenu);
            UIBuildUtil.Set(screen, "sectorLabel", sector);
            UIBuildUtil.Set(screen, "timeLabel", time);
            UIBuildUtil.Set(screen, "panel", panel);
            UIBuildUtil.Set(screen, "defaultSelection", resume.Button);

            return SaveScreen(screen, "PauseScreen");
        }

        // ------------------------------------------------------------------ level complete

        private static UIScreen BuildLevelComplete(UITheme theme, string outputDir)
        {
            LevelCompleteScreen screen = Create<LevelCompleteScreen>("LevelCompleteScreen", UIScreenType.LevelComplete, theme, backdrop: false, gameplay: false);
            RectTransform content = ContentOf(screen);
            Scrim(content, 0.82f);

            RectTransform confetti = NewRect("Confetti", content);
            UIBuildUtil.Stretch(confetti, 0f, 0f, 0f, 0f);
            UIConfetti confettiEffect = confetti.gameObject.AddComponent<UIConfetti>();
            UIBuildUtil.Set(confettiEffect, "area", confetti);
            UIBuildUtil.Set(confettiEffect, "shapes", new[] { theme.starSmallSprite, theme.softDotSprite, theme.starSprite });

            RectTransform panel = NewRect("Panel", content);
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 820f));
            UIBuildUtil.AddImage(panel, theme.panelSprite, Color.white, sliced: true, raycast: true);

            UIBuildUtil.Set(screen, "confetti", confettiEffect);

            Label(panel, "Eyebrow", "MISSION DEBRIEF", theme.bodyFont, 14f, theme.primary,
                TextAlignmentOptions.Center, new Vector2(0f, 330f), new Vector2(800f, 22f), 9f, true);

            TextMeshProUGUI title = Label(panel, "Title", "SECTOR CLEARED", theme.displayFont, 52f, theme.textPrimary,
                TextAlignmentOptions.Center, new Vector2(0f, 272f), new Vector2(900f, 62f), 4f, true);

            RectTransform starRow = NewRect("Stars", panel);
            Place(starRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 186f), new Vector2(360f, 96f));
            Image[] stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform starRect = NewRect($"Star{i + 1}", starRow);
                Place(starRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 116f, 0f), new Vector2(96f, 96f));
                stars[i] = UIBuildUtil.AddImage(starRect, theme.starSprite, theme.starGold);
            }

            StarRating rating = starRow.gameObject.AddComponent<StarRating>();
            UIBuildUtil.Set(rating, "stars", stars);
            UIBuildUtil.Set(rating, "popDelay", 0.16f);

            TextMeshProUGUI rank = Label(panel, "Rank", "RANK A // MISSION ACCOMPLISHED", theme.bodyFont, 20f, theme.primary,
                TextAlignmentOptions.Center, new Vector2(0f, 112f), new Vector2(880f, 30f), 4f, true);

            Label(panel, "TimeCaption", "FLIGHT TIME", theme.bodyFont, 13f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, 62f), new Vector2(400f, 20f), 8f, true);

            RectTransform timeRect = NewRect("TimeValue", panel);
            Place(timeRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(520f, 78f));
            TextMeshProUGUI timeText = UIBuildUtil.AddText(timeRect, "00:00.00", theme.displayFont, 62f, theme.textPrimary, TextAlignmentOptions.Center, 3f);
            CountUpText countUp = timeRect.gameObject.AddComponent<CountUpText>();
            UIBuildUtil.Set(countUp, "target", timeText);

            TextMeshProUGUI best = Label(panel, "Best", "SECTOR RECORD  --:--.--", theme.bodyFont, 18f, theme.textSecondary,
                TextAlignmentOptions.Center, new Vector2(0f, -62f), new Vector2(800f, 26f), 3f, true);

            TextMeshProUGUI attempts = Label(panel, "Attempts", "ATTEMPTS  1", theme.bodyFont, 16f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -94f), new Vector2(800f, 24f), 4f, true);

            RectTransform badge = NewRect("NewRecordBadge", panel);
            Place(badge, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -42f), new Vector2(190f, 40f));
            UIBuildUtil.AddImage(badge, theme.pillSprite, theme.starGold, sliced: true);
            Label(badge, "BadgeLabel", "NEW RECORD", theme.bodyFont, 13f, theme.backgroundDeep,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(190f, 40f), 4f, true);
            badge.gameObject.SetActive(false);

            RectTransform list = NewRect("MenuGroup", panel);
            Place(list, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -232f),
                new Vector2(theme.menuListWidth, theme.menuListSpacing * 3f));

            ThemedButton next = ListRow(list, theme, "NextButton", "NEXT SECTOR", "Continue to the next sector.", 0, 3);
            ThemedButton retry = ListRow(list, theme, "RetryButton", "RETRY", "Fly this sector again for a better rank.", 1, 3);
            ThemedButton menu = ListRow(list, theme, "MenuButton", "MAIN MENU", "Leave the debrief and return to the menu.", 2, 3);

            TextMeshProUGUI hint = Label(panel, "NextHint", "Continue to the next sector when ready.", theme.bodyFont, 15f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -374f), new Vector2(820f, 26f), 2f, true);

            PromptStrip(content, theme, ("A", "CONFIRM"));

            UIBuildUtil.Set(screen, "titleLabel", title);
            UIBuildUtil.Set(screen, "sectorLabel", null);
            UIBuildUtil.Set(screen, "rankLabel", rank);
            UIBuildUtil.Set(screen, "bestLabel", best);
            UIBuildUtil.Set(screen, "attemptsLabel", attempts);
            UIBuildUtil.Set(screen, "nextHintLabel", hint);
            UIBuildUtil.Set(screen, "timeText", countUp);
            UIBuildUtil.Set(screen, "stars", rating);
            UIBuildUtil.Set(screen, "newRecordBadge", badge.gameObject);
            UIBuildUtil.Set(screen, "nextButton", next);
            UIBuildUtil.Set(screen, "retryButton", retry);
            UIBuildUtil.Set(screen, "menuButton", menu);
            UIBuildUtil.Set(screen, "panel", panel);
            UIBuildUtil.Set(screen, "defaultSelection", next.Button);

            return SaveScreen(screen, "LevelCompleteScreen");
        }

        // ------------------------------------------------------------------ game over

        private static UIScreen BuildGameOver(UITheme theme, string outputDir)
        {
            GameOverScreen screen = Create<GameOverScreen>("GameOverScreen", UIScreenType.GameOver, theme, backdrop: false, gameplay: false);
            RectTransform content = ContentOf(screen);
            Scrim(content, 0.76f);

            RectTransform vignetteRect = NewRect("Vignette", content);
            UIBuildUtil.Stretch(vignetteRect, 0f, 0f, 0f, 0f);
            Image vignetteImage = UIBuildUtil.AddImage(vignetteRect, theme.vignetteSprite, UITheme.Alpha(theme.danger, 0.18f));
            VignetteFlash flash = vignetteRect.gameObject.AddComponent<VignetteFlash>();
            UIBuildUtil.Set(flash, "image", vignetteImage);
            UIBuildUtil.Set(flash, "baseColor", UITheme.Alpha(theme.danger, 0.18f));
            UIBuildUtil.Set(flash, "flashAlpha", 0.85f);

            RectTransform glow = NewRect("AccentGlow", content);
            Place(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1000f, 1000f));
            Image glowImage = UIBuildUtil.AddImage(glow, theme.glowSprite, UITheme.Alpha(theme.danger, 0.16f));
            glow.gameObject.AddComponent<UIPulse>();

            RectTransform panel = NewRect("Panel", content);
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 600f));
            UIBuildUtil.AddImage(panel, theme.panelSprite, Color.white, sliced: true, raycast: true);

            TextMeshProUGUI title = Label(panel, "Title", "SIGNAL LOST", theme.displayFont, 66f, theme.danger,
                TextAlignmentOptions.Center, new Vector2(0f, 190f), new Vector2(760f, 84f), 4f, true);

            TextMeshProUGUI reason = Label(panel, "Reason", "HULL INTEGRITY LOST", theme.bodyFont, 20f, theme.textSecondary,
                TextAlignmentOptions.Center, new Vector2(0f, 120f), new Vector2(760f, 30f), 6f, true);

            RectTransform divider = NewRect("Divider", panel);
            Place(divider, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 86f), new Vector2(560f, 1f));
            UIBuildUtil.AddImage(divider, null, theme.borderDim);

            TextMeshProUGUI time = Label(panel, "Time", "FLIGHT TIME  00:00.00", theme.bodyFont, 18f, theme.textSecondary,
                TextAlignmentOptions.Center, new Vector2(0f, 56f), new Vector2(760f, 26f), 4f, true);

            TextMeshProUGUI attempts = Label(panel, "Attempts", "ATTEMPT  1", theme.bodyFont, 15f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, 28f), new Vector2(760f, 24f), 5f, true);

            RectTransform list = NewRect("MenuGroup", panel);
            Place(list, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -104f),
                new Vector2(theme.menuListWidth, theme.menuListSpacing * 2f));

            ThemedButton retry = ListRow(list, theme, "RetryButton", "RETRY", "Launch the sector again immediately.", 0, 2);
            ThemedButton menu = ListRow(list, theme, "MenuButton", "MAIN MENU", "Return to the menu and pick another sector.", 1, 2);

            TextMeshProUGUI hint = Label(panel, "RetryHint", "PRESS ESC OR RETRY", theme.bodyFont, 12f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -216f), new Vector2(700f, 22f), 6f, true);

            PromptStrip(content, theme, ("A", "CONFIRM"), ("B", "RETRY"));

            UIBuildUtil.Set(screen, "titleLabel", title);
            UIBuildUtil.Set(screen, "reasonLabel", reason);
            UIBuildUtil.Set(screen, "attemptLabel", attempts);
            UIBuildUtil.Set(screen, "timeLabel", time);
            UIBuildUtil.Set(screen, "retryHintLabel", hint);
            UIBuildUtil.Set(screen, "retryButton", retry);
            UIBuildUtil.Set(screen, "menuButton", menu);
            UIBuildUtil.Set(screen, "panel", panel);
            UIBuildUtil.Set(screen, "vignette", flash);
            UIBuildUtil.Set(screen, "accentGlow", glowImage);
            UIBuildUtil.Set(screen, "defaultSelection", retry.Button);

            return SaveScreen(screen, "GameOverScreen");
        }

        // ------------------------------------------------------------------ settings

        private static UIScreen BuildSettings(UITheme theme, string outputDir)
        {
            SettingsScreen screen = Create<SettingsScreen>("SettingsScreen", UIScreenType.Settings, theme, backdrop: false, gameplay: false);
            RectTransform content = ContentOf(screen);
            Scrim(content, 0.94f);

            RectTransform panel = NewRect("Panel", content);
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 820f));
            UIBuildUtil.AddImage(panel, theme.panelSprite, Color.white, sliced: true, raycast: true);

            Label(panel, "Title", "SETTINGS", theme.displayFont, 44f, theme.textPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(64f, -46f), new Vector2(500f, 56f), 6f);

            Label(panel, "Subtitle", "SYSTEM PREFERENCES  ·  SAVED LOCALLY", theme.bodyFont, 14f, theme.textMuted,
                TextAlignmentOptions.TopLeft, new Vector2(66f, -100f), new Vector2(600f, 22f), 5f);

            ThemedButton back = Button(panel, "BackButton", "BACK", theme, UIButtonVariant.Ghost, theme.iconChevronLeft,
                Vector2.zero, new Vector2(150f, 46f));
            RectTransform backRect = (RectTransform)back.transform;
            backRect.anchorMin = new Vector2(1f, 1f);
            backRect.anchorMax = new Vector2(1f, 1f);
            backRect.pivot = new Vector2(1f, 1f);
            backRect.anchoredPosition = new Vector2(-52f, -46f);
            backRect.sizeDelta = new Vector2(150f, 46f);

            Label(panel, "AudioHeader", "AUDIO", theme.bodyFont, 15f, theme.primary,
                TextAlignmentOptions.TopLeft, new Vector2(64f, -160f), new Vector2(300f, 22f), 7f);

            Slider master = SliderRow(panel, theme, "MASTER", "MasterSlider", -206f, out TextMeshProUGUI masterValue);
            Slider music = SliderRow(panel, theme, "MUSIC", "MusicSlider", -276f, out TextMeshProUGUI musicValue);
            Slider sfx = SliderRow(panel, theme, "SFX", "SfxSlider", -346f, out TextMeshProUGUI sfxValue);

            Label(panel, "DisplayHeader", "DISPLAY", theme.bodyFont, 15f, theme.primary,
                TextAlignmentOptions.TopLeft, new Vector2(600f, -160f), new Vector2(300f, 22f), 7f);

            Label(panel, "QualityLabel", "GRAPHICS QUALITY", theme.bodyFont, 13f, theme.textMuted,
                TextAlignmentOptions.TopLeft, new Vector2(600f, -194f), new Vector2(420f, 20f), 5f);

            GameObject dropdownObject = UIBuildUtil.BuildDropdown(panel, "QualityDropdown", theme, new Vector2(600f, -226f), 440f);
            TMP_Dropdown dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
            RectTransform dropdownRect = (RectTransform)dropdownObject.transform;
            dropdownRect.anchorMin = new Vector2(0f, 1f);
            dropdownRect.anchorMax = new Vector2(0f, 1f);
            dropdownRect.pivot = new Vector2(0f, 1f);
            dropdownRect.anchoredPosition = new Vector2(600f, -226f);

            Label(panel, "FullscreenLabel", "DISPLAY MODE", theme.bodyFont, 13f, theme.textMuted,
                TextAlignmentOptions.TopLeft, new Vector2(600f, -300f), new Vector2(300f, 20f), 5f);

            GameObject toggleObject = UIBuildUtil.BuildToggle(panel, "FullscreenToggle", "FULLSCREEN", theme, Vector2.zero);
            RectTransform toggleRect = (RectTransform)toggleObject.transform;
            toggleRect.anchorMin = new Vector2(0f, 1f);
            toggleRect.anchorMax = new Vector2(0f, 1f);
            toggleRect.pivot = new Vector2(0f, 1f);
            toggleRect.anchoredPosition = new Vector2(600f, -328f);
            Toggle toggle = toggleObject.GetComponent<Toggle>();

            TextMeshProUGUI fullscreenValue = Label(panel, "FullscreenValue", "ON", theme.bodyFont, 15f, theme.primary,
                TextAlignmentOptions.TopRight, new Vector2(-80f, -330f), new Vector2(120f, 24f), 4f);
            fullscreenValue.rectTransform.anchorMin = new Vector2(1f, 1f);
            fullscreenValue.rectTransform.anchorMax = new Vector2(1f, 1f);
            fullscreenValue.rectTransform.pivot = new Vector2(1f, 1f);

            Label(panel, "ControlsHeader", "CONTROLS", theme.bodyFont, 15f, theme.primary,
                TextAlignmentOptions.TopLeft, new Vector2(64f, -430f), new Vector2(300f, 22f), 7f);

            RectTransform controls = NewRect("ControlsPanel", panel);
            Place(controls, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -466f), new Vector2(992f, 250f));
            UIBuildUtil.AddImage(controls, theme.panelDeepSprite, UITheme.Alpha(theme.backgroundDeep, 0.65f), sliced: true);

            // One grid: action, keyboard, gamepad. Every row shares the same rhythm, so the caps
            // line up in two clean columns instead of floating at hand-placed offsets.
            const float ActionColumn = 24f;
            const float KeyboardColumn = 250f;
            const float GamepadColumn = 616f;
            const float ColumnWidth = 342f;
            const float TableWidth = 944f;
            float firstRow = 44f;
            float rowStep = theme.keyCapRowHeight;

            Label(controls, "ActionHeader", "ACTION", theme.bodyFont, 11f, theme.textMuted,
                TextAlignmentOptions.MidlineLeft, new Vector2(ActionColumn, -8f), new Vector2(180f, 22f), 6f);
            Label(controls, "KeyboardHeader", "KEYBOARD", theme.bodyFont, 11f, theme.textMuted,
                TextAlignmentOptions.MidlineLeft, new Vector2(KeyboardColumn, -8f), new Vector2(ColumnWidth, 22f), 6f);
            Label(controls, "GamepadHeader", "GAMEPAD", theme.bodyFont, 11f, theme.textMuted,
                TextAlignmentOptions.MidlineLeft, new Vector2(GamepadColumn, -8f), new Vector2(ColumnWidth, 22f), 6f);

            TableRule(controls, theme, "HeaderRule", 34f, TableWidth, theme.borderDim);

            KeyRow(controls, theme, "THRUST", ActionColumn, firstRow);
            KeyCaps(controls, theme, KeyboardColumn, firstRow, "SPACE");
            KeyCaps(controls, theme, GamepadColumn, firstRow, "RT");

            KeyRow(controls, theme, "ROTATE", ActionColumn, firstRow + rowStep);
            KeyCaps(controls, theme, KeyboardColumn, firstRow + rowStep, "A", "D");
            KeyCaps(controls, theme, GamepadColumn, firstRow + rowStep, "LEFT STICK");

            KeyRow(controls, theme, "PAUSE", ActionColumn, firstRow + rowStep * 2f);
            KeyCaps(controls, theme, KeyboardColumn, firstRow + rowStep * 2f, "ESC");
            KeyCaps(controls, theme, GamepadColumn, firstRow + rowStep * 2f, "START");

            KeyRow(controls, theme, "NAVIGATE", ActionColumn, firstRow + rowStep * 3f);
            KeyCaps(controls, theme, KeyboardColumn, firstRow + rowStep * 3f, "ARROWS");
            KeyCaps(controls, theme, GamepadColumn, firstRow + rowStep * 3f, "D-PAD");

            for (int i = 1; i < 4; i++)
            {
                TableRule(controls, theme, $"RowRule{i}",
                    firstRow + rowStep * i - (rowStep - theme.keyCapHeight) * 0.5f, TableWidth, UITheme.Alpha(theme.borderDim, 0.5f));
            }

            PromptStrip(content, theme, ("A", "ADJUST"), ("B", "CLOSE"));

            Label(panel, "Footer", "SETTINGS SAVE AUTOMATICALLY WHEN YOU CLOSE THIS PANEL", theme.bodyFont, 11f, theme.textMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -770f), new Vector2(1000f, 22f), 5f, true);

            UIBuildUtil.Set(screen, "masterSlider", master);
            UIBuildUtil.Set(screen, "musicSlider", music);
            UIBuildUtil.Set(screen, "sfxSlider", sfx);
            UIBuildUtil.Set(screen, "masterValue", masterValue);
            UIBuildUtil.Set(screen, "musicValue", musicValue);
            UIBuildUtil.Set(screen, "sfxValue", sfxValue);
            UIBuildUtil.Set(screen, "qualityDropdown", dropdown);
            UIBuildUtil.Set(screen, "fullscreenToggle", toggle);
            UIBuildUtil.Set(screen, "fullscreenValue", fullscreenValue);
            UIBuildUtil.Set(screen, "backButton", back);
            UIBuildUtil.Set(screen, "panel", panel);
            UIBuildUtil.Set(screen, "qualityHint", null);
            UIBuildUtil.Set(screen, "defaultSelection", back.Button);

            return SaveScreen(screen, "SettingsScreen");
        }

        // ------------------------------------------------------------------ loading

        private static UIScreen BuildLoading(UITheme theme, string outputDir)
        {
            LoadingScreen screen = Create<LoadingScreen>("LoadingScreen", UIScreenType.Loading, theme, backdrop: false, gameplay: false);
            RectTransform content = ContentOf(screen);

            RectTransform background = NewRect("Background", content);
            UIBuildUtil.Stretch(background, 0f, 0f, 0f, 0f);
            UIBuildUtil.AddImage(background, theme.backgroundGradientSprite, Color.white, raycast: true);

            RectTransform glow = NewRect("Glow", content);
            Place(glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1200f, 1200f));
            UIBuildUtil.AddImage(glow, theme.glowSprite, UITheme.Alpha(theme.primary, 0.08f));

            RectTransform emblem = NewRect("RocketEmblem", content);
            Place(emblem, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 205f), new Vector2(260f, 340f));

            RectTransform halo = NewRect("Halo", emblem);
            Place(halo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(370f, 370f));
            UIBuildUtil.AddImage(halo, theme.glowSprite, UITheme.Alpha(theme.primary, 0.16f));
            UIPulse haloPulse = halo.gameObject.AddComponent<UIPulse>();
            UIBuildUtil.Set(haloPulse, "minAlpha", 0.08f);
            UIBuildUtil.Set(haloPulse, "maxAlpha", 0.18f);
            UIBuildUtil.Set(haloPulse, "speed", 1f);

            // The flame's widest point sits just below the nozzle bell, so the plume reads as
            // attached rather than floating; the rocket is drawn last and overlaps its tip.
            RectTransform exhaust = NewRect("ExhaustGlow", emblem);
            Place(exhaust, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(170f, 170f));
            UIBuildUtil.AddImage(exhaust, theme.glowSprite, UITheme.Alpha(theme.secondary, 0.22f));
            UIPulse exhaustPulse = exhaust.gameObject.AddComponent<UIPulse>();
            UIBuildUtil.Set(exhaustPulse, "minAlpha", 0.12f);
            UIBuildUtil.Set(exhaustPulse, "maxAlpha", 0.26f);
            UIBuildUtil.Set(exhaustPulse, "speed", 5f);

            RectTransform flame = NewRect("Flame", emblem);
            Place(flame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(78f, 117f));
            Image flameImage = UIBuildUtil.AddImage(flame, theme.flameLoadingSprite, UITheme.Alpha(theme.secondary, 0.95f));
            flameImage.preserveAspect = true;
            UIPulse flamePulse = flame.gameObject.AddComponent<UIPulse>();
            UIBuildUtil.Set(flamePulse, "minAlpha", 0.55f);
            UIBuildUtil.Set(flamePulse, "maxAlpha", 0.95f);
            UIBuildUtil.Set(flamePulse, "speed", 7f);

            RectTransform flameCore = NewRect("Core", flame);
            Place(flameCore, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(40f, 60f));
            Color hot = Color.Lerp(theme.secondary, Color.white, 0.62f);
            Image flameCoreImage = UIBuildUtil.AddImage(flameCore, theme.flameLoadingSprite, UITheme.Alpha(hot, 0.95f));
            flameCoreImage.preserveAspect = true;
            UIPulse corePulse = flameCore.gameObject.AddComponent<UIPulse>();
            UIBuildUtil.Set(corePulse, "minAlpha", 0.6f);
            UIBuildUtil.Set(corePulse, "maxAlpha", 1f);
            UIBuildUtil.Set(corePulse, "speed", 9f);

            RectTransform rocket = NewRect("Rocket", emblem);
            Place(rocket, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(170f, 255f));
            Image rocketImage = UIBuildUtil.AddImage(rocket, theme.rocketLoadingSprite, theme.primary);
            rocketImage.preserveAspect = true;

            UIFloat bobbing = emblem.gameObject.AddComponent<UIFloat>();
            UIBuildUtil.Set(bobbing, "amplitude", 6f);
            UIBuildUtil.Set(bobbing, "speed", 0.8f);

            Label(content, "Eyebrow", "ROCKET  BOOST", theme.bodyFont, 14f, theme.primary,
                TextAlignmentOptions.Center, new Vector2(0f, 40f), new Vector2(600f, 22f), 10f, true);

            TextMeshProUGUI status = Label(content, "Status", "PREPARING FLIGHT SYSTEMS", theme.displayFont, 26f, theme.textPrimary,
                TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(900f, 36f), 4f, true);

            RectTransform percentRect = NewRect("Percent", content);
            Place(percentRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -66f), new Vector2(300f, 80f));
            TextMeshProUGUI percent = UIBuildUtil.AddText(percentRect, "0%", theme.displayFont, 64f, theme.primary, TextAlignmentOptions.Center, 2f);

            RectTransform barRoot = NewRect("ProgressBar", content);
            Place(barRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(760f, 10f));
            UIBuildUtil.AddImage(barRoot, theme.gaugeTrackSprite, UITheme.Alpha(theme.borderDim, 0.8f), sliced: true);

            RectTransform fillArea = NewRect("FillArea", barRoot);
            UIBuildUtil.Stretch(fillArea, 2f, 2f, 2f, 2f);
            RectTransform fill = NewRect("Fill", fillArea);
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(0f, 0f);
            UIBuildUtil.AddImage(fill, theme.gaugeFillSprite, theme.primary);

            UIProgressBar progress = barRoot.gameObject.AddComponent<UIProgressBar>();
            UIBuildUtil.Set(progress, "fill", fill);
            UIBuildUtil.Set(progress, "width", 756f);

            TextMeshProUGUI tip = Label(content, "Tip", "Hold SPACE to burn. Tap A and D to steer.", theme.bodyFont, 17f, theme.textSecondary,
                TextAlignmentOptions.Center, new Vector2(0f, -212f), new Vector2(1100f, 28f), 2f, true);

            UIBuildUtil.Set(screen, "progressBar", progress);
            UIBuildUtil.Set(screen, "percentLabel", percent);
            UIBuildUtil.Set(screen, "statusLabel", status);
            UIBuildUtil.Set(screen, "tipLabel", tip);
            UIBuildUtil.Set(screen, "tips", new[]
            {
                "Hold SPACE to burn. Tap A and D to steer.",
                "Small corrections beat big oversteers.",
                "The pad rewards a soft landing, not a fast one.",
                "Escape pauses; the timer stops with it.",
                "Beat your sector record to earn all three stars."
            });
            UIBuildUtil.Set(screen, "tipInterval", 2.6f);

            return SaveScreen(screen, "LoadingScreen");
        }

        // ------------------------------------------------------------------ shared parts

        public static GameObject BuildRoot(UITheme theme, LevelCatalog catalog, List<UIScreen> screens, Texture2D[] starTextures, string outputDir)
        {
            GameObject root = new GameObject("UIRoot");
            UIRoot rootComponent = root.AddComponent<UIRoot>();
            UIManager manager = root.AddComponent<UIManager>();
            UIAudio audio = root.AddComponent<UIAudio>();

            RectTransform canvasRect = NewRect("Canvas", root.transform);
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);

            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;

            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform backdrop = NewRect("Backdrop", canvasRect);
            UIBuildUtil.Stretch(backdrop, 0f, 0f, 0f, 0f);
            CanvasGroup backdropGroup = backdrop.gameObject.AddComponent<CanvasGroup>();
            backdropGroup.blocksRaycasts = false;
            backdropGroup.interactable = false;

            RectTransform gradient = NewRect("Gradient", backdrop);
            UIBuildUtil.Stretch(gradient, 0f, 0f, 0f, 0f);
            UIBuildUtil.AddImage(gradient, theme.backgroundGradientSprite, Color.white);

            RectTransform nebulaA = NewRect("NebulaCyan", backdrop);
            Place(nebulaA, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(420f, 260f), new Vector2(1500f, 1500f));
            UIBuildUtil.AddImage(nebulaA, theme.nebulaSprite, UITheme.Alpha(theme.primary, 0.13f));
            nebulaA.gameObject.AddComponent<UIPulse>();

            RectTransform nebulaB = NewRect("NebulaEmber", backdrop);
            Place(nebulaB, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-420f, -240f), new Vector2(1400f, 1400f));
            UIBuildUtil.AddImage(nebulaB, theme.nebulaSprite, UITheme.Alpha(theme.secondary, 0.1f));

            RectTransform starfield = NewRect("Starfield", backdrop);
            UIBuildUtil.Stretch(starfield, 0f, 0f, 0f, 0f);
            StarfieldBackground field = starfield.gameObject.AddComponent<StarfieldBackground>();

            StarfieldBackground.Layer[] layers = new StarfieldBackground.Layer[3];
            float[] speeds = { 0.0035f, 0.008f, 0.016f };
            float[] alphas = { 0.55f, 0.8f, 1f };
            for (int i = 0; i < 3; i++)
            {
                RectTransform layerRect = NewRect($"Layer{i + 1}", starfield);
                UIBuildUtil.Stretch(layerRect, 0f, 0f, 0f, 0f);
                Texture2D texture = starTextures != null && i < starTextures.Length ? starTextures[i] : null;
                RawImage image = UIBuildUtil.AddRawImage(layerRect, texture, new Color(1f, 1f, 1f, alphas[i]));
                image.uvRect = new Rect(0f, 0f, 1.35f, 1.35f);
                layers[i] = new StarfieldBackground.Layer
                {
                    image = image,
                    speed = speeds[i],
                    direction = new Vector2(1f, 0.12f),
                    sway = 0.004f + i * 0.002f,
                    swaySpeed = 0.12f + i * 0.05f
                };
            }

            UIBuildUtil.Set(field, "layers", layers);

            RectTransform scanlines = NewRect("Scanlines", backdrop);
            UIBuildUtil.Stretch(scanlines, 0f, 0f, 0f, 0f);
            Image scanlineImage = UIBuildUtil.AddImage(scanlines, theme.scanlineSprite, UITheme.Alpha(Color.white, 0.045f));
            scanlineImage.type = Image.Type.Tiled;
            scanlineImage.pixelsPerUnitMultiplier = 1f;

            RectTransform vignette = NewRect("Vignette", backdrop);
            UIBuildUtil.Stretch(vignette, 0f, 0f, 0f, 0f);
            UIBuildUtil.AddImage(vignette, theme.vignetteSprite, UITheme.Alpha(Color.black, 0.55f));

            RectTransform screenRoot = NewRect("Screens", canvasRect);
            UIBuildUtil.Stretch(screenRoot, 0f, 0f, 0f, 0f);

            RectTransform transition = NewRect("TransitionOverlay", canvasRect);
            UIBuildUtil.Stretch(transition, 0f, 0f, 0f, 0f);
            UIBuildUtil.AddImage(transition, null, UITheme.Alpha(theme.backgroundDeep, 1f), raycast: true);
            CanvasGroup transitionGroup = transition.gameObject.AddComponent<CanvasGroup>();
            transitionGroup.alpha = 1f;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            eventSystemObject.transform.SetParent(root.transform, false);
            eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            // Authored disabled: scene copies stay silent until the surviving UIRoot claims it.
            eventSystemObject.GetComponent<UnityEngine.EventSystems.EventSystem>().enabled = false;

            UIScreen[] screenArray = new UIScreen[screens.Count];
            for (int i = 0; i < screens.Count; i++)
            {
                screenArray[i] = screens[i];
            }

            UIBuildUtil.Set(manager, "theme", theme);
            UIBuildUtil.Set(manager, "levelCatalog", catalog);
            UIBuildUtil.Set(manager, "screenPrefabs", screenArray);
            UIBuildUtil.Set(manager, "screenRoot", screenRoot);
            UIBuildUtil.Set(manager, "backdropGroup", backdropGroup);
            UIBuildUtil.Set(manager, "transitionGroup", transitionGroup);
            UIBuildUtil.Set(manager, "audioService", audio);
            UIBuildUtil.Set(manager, "minimumLoadingTime", 1.3f);
            UIBuildUtil.Set(manager, "transitionDuration", 0.32f);

            UIBuildUtil.Set(rootComponent, "manager", manager);
            UIBuildUtil.Set(rootComponent, "audioService", audio);

            GameObject asset = UIBuildUtil.SavePrefab(root, "UIRoot");
            return asset;
        }

        // ------------------------------------------------------------------ helpers

        private static T Create<T>(string name, UIScreenType type, UITheme theme, bool backdrop, bool gameplay) where T : UIScreen
        {
            RectTransform root = NewRect(name, null);
            UIBuildUtil.Stretch(root, 0f, 0f, 0f, 0f);
            root.gameObject.AddComponent<CanvasGroup>();
            T screen = root.gameObject.AddComponent<T>();

            UIBuildUtil.Set(screen, "screenType", type);
            UIBuildUtil.Set(screen, "showBackdrop", backdrop);
            UIBuildUtil.Set(screen, "gameplayScreen", gameplay);
            UIBuildUtil.Set(screen, "entranceDuration", 0.26f);
            UIBuildUtil.Set(screen, "exitDuration", 0.16f);
            UIBuildUtil.Set(screen, "entranceOffset", 24f);

            RectTransform content = NewRect("Content", root);
            UIBuildUtil.Stretch(content, 0f, 0f, 0f, 0f);
            return screen;
        }

        private static RectTransform ContentOf(UIScreen screen) => (RectTransform)screen.transform.GetChild(0);

        private static RectTransform NewRect(string name, Transform parent) => UIBuildUtil.NewRect(name, parent);

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size) =>
            UIBuildUtil.Place(rect, anchor, pivot, position, size);

        private static TextMeshProUGUI Label(
            Transform parent,
            string name,
            string content,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 rectSize,
            float tracking = 0f,
            bool centered = false)
        {
            RectTransform rect = NewRect(name, parent);
            Place(rect, centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f),
                centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f), position, rectSize);
            return UIBuildUtil.AddText(rect, content, font, size, color, alignment, tracking);
        }

        private static ThemedButton Button(
            Transform parent,
            string name,
            string label,
            UITheme theme,
            UIButtonVariant variant,
            Sprite icon,
            Vector2 position,
            Vector2 size,
            float fontSize = 0f)
        {
            GameObject go = UIBuildUtil.BuildButton(parent, name, label, theme, variant, icon, Vector2.zero, size, fontSize);
            RectTransform rect = (RectTransform)go.transform;
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            return go.GetComponent<ThemedButton>();
        }

        private static ThemedButton BackButton(Transform parent, UITheme theme, Vector2 position)
        {
            GameObject go = UIBuildUtil.BuildButton(parent, "BackButton", string.Empty, theme, UIButtonVariant.Ghost,
                theme.iconChevronLeft, Vector2.zero, new Vector2(56f, 56f));
            RectTransform rect = (RectTransform)go.transform;
            Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(position.x, position.y), new Vector2(56f, 56f));
            return go.GetComponent<ThemedButton>();
        }

        /// <summary>Label hung from the horizontal centre of its parent, <paramref name="y"/> pixels below its top.</summary>
        private static TextMeshProUGUI CenteredLabel(
            Transform parent,
            string name,
            string content,
            TMP_FontAsset font,
            float size,
            Color color,
            float y,
            Vector2 rectSize,
            float tracking = 0f)
        {
            RectTransform rect = NewRect(name, parent);
            Place(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -y), rectSize);
            return UIBuildUtil.AddText(rect, content, font, size, color, TextAlignmentOptions.Center, tracking);
        }

        /// <summary>One row of a boxless centred menu list, stacked on the theme's list spacing.</summary>
        private static ThemedButton ListRow(
            Transform parent,
            UITheme theme,
            string name,
            string text,
            string hint,
            int index,
            int rowCount)
        {
            float y = rowCount * theme.menuListSpacing * 0.5f - theme.menuListSpacing * (index + 0.5f);
            return UIBuildUtil.BuildListButton(parent, name, text, theme, hint, new Vector2(0f, y))
                .GetComponent<ThemedButton>();
        }

        private static void PromptStrip(RectTransform content, UITheme theme, params PromptEntry[] entries)
        {
            UIBuildUtil.BuildPromptStrip(content, theme, 0f, entries);
        }

        private static void Scrim(Transform parent, float alpha)
        {
            RectTransform scrim = NewRect("Scrim", parent);
            UIBuildUtil.Stretch(scrim, 0f, 0f, 0f, 0f);
            UIBuildUtil.AddImage(scrim, null, new Color(0.004f, 0.008f, 0.02f, alpha), raycast: true);
        }

        private static GaugeBar Gauge(Transform parent, UITheme theme, Vector2 position, Vector2 size, TextMeshProUGUI valueLabel)
        {
            RectTransform root = NewRect("Gauge", parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = position;
            root.sizeDelta = size;

            Image track = UIBuildUtil.AddImage(root, theme.gaugeTrackSprite, theme.borderDim, sliced: true);

            RectTransform fillRect = NewRect("Fill", root);
            UIBuildUtil.Stretch(fillRect, 0f, 0f, 0f, 0f);
            Image fill = UIBuildUtil.AddImage(fillRect, theme.gaugeFillSprite, theme.primary, sliced: true);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;

            GaugeBar gauge = root.gameObject.AddComponent<GaugeBar>();
            UIBuildUtil.Set(gauge, "fill", fill);
            UIBuildUtil.Set(gauge, "track", track);
            UIBuildUtil.Set(gauge, "valueLabel", valueLabel);
            gauge.ApplyTheme(theme);
            return gauge;
        }

        private static Slider SliderRow(Transform panel, UITheme theme, string label, string name, float y, out TextMeshProUGUI valueLabel)
        {
            Label(panel, $"{name}Label", label, theme.bodyFont, 13f, theme.textMuted,
                TextAlignmentOptions.MidlineLeft, new Vector2(64f, y), new Vector2(120f, 26f), 5f);

            // The track stops short of the value column by more than the handle's half-width, so the
            // knob can never sit on top of the percentage. The row is 44px tall to give the whole
            // bar a comfortable click target while the visible track stays thin.
            GameObject sliderObject = UIBuildUtil.BuildSlider(panel, name, theme, Vector2.zero, 274f, 44f);
            RectTransform sliderRect = (RectTransform)sliderObject.transform;
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(0f, 1f);
            sliderRect.pivot = new Vector2(0f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(196f, y);

            valueLabel = Label(panel, $"{name}Value", "80%", theme.bodyFont, 15f, theme.primary,
                TextAlignmentOptions.MidlineRight, new Vector2(556f, y), new Vector2(60f, 26f), 1f);
            valueLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            valueLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            valueLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
            valueLabel.rectTransform.anchoredPosition = new Vector2(556f, y);
            valueLabel.rectTransform.sizeDelta = new Vector2(60f, 26f);

            return sliderObject.GetComponent<Slider>();
        }

        private static void KeyRow(Transform parent, UITheme theme, string action, float x, float y)
        {
            Label(parent, $"{action}Label", action, theme.bodyFont, 14f, theme.textSecondary,
                TextAlignmentOptions.MidlineLeft, new Vector2(x, -y), new Vector2(200f, theme.keyCapHeight), 5f);
        }

        /// <summary>Left-aligned key caps for one table cell; each chip is measured from its own label.</summary>
        private static void KeyCaps(Transform parent, UITheme theme, float x, float y, params string[] keys)
        {
            float cursor = x;
            foreach (string key in keys)
            {
                float width = Mathf.Max(theme.keyCapMinWidth, theme.keyCapPadding + key.Length * theme.keyCapCharWidth);
                KeyCap(parent, theme, key, new Vector2(cursor, -y), width);
                cursor += width + theme.keyCapGap;
            }
        }

        private static void TableRule(Transform parent, UITheme theme, string name, float y, float width, Color color)
        {
            RectTransform rule = NewRect(name, parent);
            Place(rule, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -y), new Vector2(width, 1f));
            UIBuildUtil.AddImage(rule, null, color);
        }

        private static void KeyCap(Transform parent, UITheme theme, string text, Vector2 position, float width)
        {
            RectTransform cap = NewRect($"Key_{text}", parent);
            cap.anchorMin = new Vector2(0f, 1f);
            cap.anchorMax = new Vector2(0f, 1f);
            cap.pivot = new Vector2(0f, 1f);
            cap.anchoredPosition = position;
            cap.sizeDelta = new Vector2(width, theme.keyCapHeight);
            UIBuildUtil.AddImage(cap, theme.pillSprite, UITheme.Alpha(theme.surfaceRaised, 0.9f), sliced: true);

            RectTransform label = NewRect("Label", cap);
            UIBuildUtil.Stretch(label, 6f, 6f, 0f, 0f);
            UIBuildUtil.AddText(label, text, theme.bodyFont, 13f, theme.textPrimary, TextAlignmentOptions.Center, 2f);
        }

        private static T SaveScreen<T>(T screen, string fileName) where T : UIScreen
        {
            GameObject asset = UIBuildUtil.SavePrefab(screen.gameObject, fileName);
            return asset != null ? asset.GetComponent<T>() : null;
        }

        private static void Save(UIScreen screen, string outputDir, string fileName)
        {
            UIBuildUtil.SavePrefab(screen.gameObject, fileName);
        }

        private static UIScreen LoadScreen(string fileName, string outputDir)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{UIBuildUtil.PrefabDir}/{fileName}.prefab");
            return asset != null ? asset.GetComponent<UIScreen>() : null;
        }

        private static LevelCard LoadLevelCard() =>
            AssetDatabase.LoadAssetAtPath<LevelCard>($"{UIBuildUtil.PrefabDir}/LevelCard.prefab");

        private static void SetResponsive(ResponsiveRect component, ResponsiveRect.Layout landscape, ResponsiveRect.Layout portrait)
        {
            UIBuildUtil.Set(component, "landscape", landscape);
            UIBuildUtil.Set(component, "portrait", portrait);
            component.Apply(force: true);
        }
    }
}
