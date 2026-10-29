using System;
using System.Reflection;
using RocketBoost.UI.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RocketBoost.UI.Editor
{
    /// <summary>
    /// Shared construction helpers for UI prefabs: rect placement, themed text, buttons,
    /// panels, sliders, toggles and dropdowns. Screens are composed from these functions so
    /// every widget is pixel-identical and the standalone prefabs are generated from the
    /// same code.
    /// </summary>
    public static class UIBuildUtil
    {
        public const string PrefabDir = "Assets/_Project/UI/Prefabs";

        public static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool sliced = false, bool raycast = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = false;
            return image;
        }

        public static RawImage AddRawImage(RectTransform rect, Texture texture, Color color)
        {
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            return image;
        }

        public static TextMeshProUGUI AddText(
            RectTransform rect,
            string content,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            float tracking = 0f,
            float lineSpacing = 0f)
        {
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                try { text.font = font; }
                catch (Exception exception) { Debug.LogError("[UIBuilder] Font assign failed for " + font.name + " material=" + (font.material != null) + " atlas=" + (font.atlasTexture != null) + ": " + exception.ToString()); throw; }
            }
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.raycastTarget = false;
            text.characterSpacing = tracking;
            text.lineSpacing = lineSpacing;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
            return text;
        }

        public static void Set(object target, string field, object value)
        {
            if (target == null)
            {
                return;
            }

            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (info != null)
                {
                    info.SetValue(target, value);
                    EditorUtility.SetDirty(target as UnityEngine.Object);
                    return;
                }

                type = type.BaseType;
            }

            Debug.LogError($"[UIBuilder] Field '{field}' not found on {target.GetType().Name}.");
        }

        // ------------------------------------------------------------------ widgets

        public static GameObject BuildButton(
            Transform parent,
            string name,
            string labelText,
            UITheme theme,
            UIButtonVariant variant,
            Sprite icon,
            Vector2 position,
            Vector2 size,
            float fontSize = 0f)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);

            Image background = AddImage(root, theme.buttonNormalSprite, theme.surfaceRaised, sliced: true, raycast: true);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            ThemedButton themed = root.gameObject.AddComponent<ThemedButton>();

            RectTransform ringRect = NewRect("FocusRing", root);
            Stretch(ringRect, -6f, -6f, -6f, -6f);
            Image ring = AddImage(ringRect, theme.buttonSelectedSprite, UITheme.Alpha(theme.primary, 0f), sliced: true);
            ring.raycastTarget = false;

            RectTransform borderRect = NewRect("Border", root);
            Stretch(borderRect, 0f, 0f, 0f, 0f);
            Image border = AddImage(borderRect, theme.buttonBorderSprite, theme.borderDim, sliced: true);
            border.raycastTarget = false;

            Image iconImage = null;
            bool iconOnly = icon != null && string.IsNullOrEmpty(labelText);
            if (icon != null)
            {
                RectTransform iconRect = NewRect("Icon", root);
                if (iconOnly)
                {
                    Place(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
                }
                else
                {
                    Place(iconRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(28f, 28f));
                }

                iconImage = AddImage(iconRect, icon, theme.primary);
            }

            RectTransform labelRect = NewRect("Label", root);
            Stretch(labelRect, icon != null ? 68f : 24f, 24f, 0f, 0f);
            TextMeshProUGUI label = AddText(
                labelRect,
                labelText,
                theme.bodyFont,
                fontSize > 0f ? fontSize : theme.labelSize,
                theme.textPrimary,
                icon != null && !iconOnly ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center,
                2f);

            Set(themed, "button", button);
            Set(themed, "background", background);
            Set(themed, "iconImage", iconImage);
            Set(themed, "label", label);
            Set(themed, "focusRing", ring);
            Set(themed, "border", border);
            Set(themed, "variant", variant);

            themed.ApplyTheme(theme);
            return root.gameObject;
        }

        public static GameObject BuildPanel(Transform parent, string name, UITheme theme, Vector2 position, Vector2 size)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            AddImage(root, theme.panelSprite, Color.white, sliced: true, raycast: true);
            return root.gameObject;
        }

        public static GameObject BuildSlider(Transform parent, string name, UITheme theme, Vector2 position, float width, float height = 32f)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, new Vector2(width, height));

            RectTransform trackRect = NewRect("Track", root);
            Stretch(trackRect, 0f, 0f, (height - 10f) * 0.5f, (height - 10f) * 0.5f);
            AddImage(trackRect, theme.gaugeTrackSprite, theme.borderDim, sliced: true);

            RectTransform fillArea = NewRect("Fill Area", root);
            Stretch(fillArea, 4f, 4f, (height - 10f) * 0.5f, (height - 10f) * 0.5f);

            RectTransform fillRect = NewRect("Fill", fillArea);
            Stretch(fillRect, 0f, 0f, 0f, 0f);
            Image fillImage = AddImage(fillRect, theme.gaugeFillSprite, theme.primary, sliced: true);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;

            RectTransform handleArea = NewRect("Handle Slide Area", root);
            Stretch(handleArea, 12f, 12f, 0f, 0f);

            RectTransform handleRect = NewRect("Handle", handleArea);
            Place(handleRect, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            Image handleImage = AddImage(handleRect, theme.sliderHandleSprite, Color.white, raycast: true);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.7f;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return root.gameObject;
        }

        public static GameObject BuildToggle(Transform parent, string name, string labelText, UITheme theme, Vector2 position)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, new Vector2(360f, 48f));

            RectTransform boxRect = NewRect("Box", root);
            Place(boxRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            Image box = AddImage(boxRect, theme.buttonNormalSprite, theme.surfaceRaised, sliced: true, raycast: true);

            RectTransform checkRect = NewRect("Check", boxRect);
            Stretch(checkRect, 9f, 9f, 9f, 9f);
            Image check = AddImage(checkRect, theme.iconCheck, theme.primary);

            RectTransform labelRect = NewRect("Label", root);
            Stretch(labelRect, 56f, 0f, 0f, 0f);
            AddText(labelRect, labelText, theme.bodyFont, theme.labelSize, theme.textPrimary, TextAlignmentOptions.MidlineLeft, 2f);

            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = true;
            toggle.transition = Selectable.Transition.None;
            toggle.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            return root.gameObject;
        }

        public static GameObject BuildDropdown(Transform parent, string name, UITheme theme, Vector2 position, float width)
        {
            RectTransform root = NewRect(name, parent);
            Place(root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, new Vector2(width, 54f));

            Image background = AddImage(root, theme.buttonNormalSprite, theme.surfaceRaised, sliced: true, raycast: true);

            RectTransform captionRect = NewRect("Label", root);
            Stretch(captionRect, 20f, 52f, 0f, 0f);
            TextMeshProUGUI caption = AddText(captionRect, "QUALITY", theme.bodyFont, theme.labelSize, theme.textPrimary, TextAlignmentOptions.MidlineLeft, 1f);

            RectTransform arrowRect = NewRect("Arrow", root);
            Place(arrowRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(20f, 20f));
            Image arrow = AddImage(arrowRect, theme.iconChevronRight, theme.primary);
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, 90f);

            RectTransform templateRect = NewRect("Template", root);
            Place(templateRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -58f), new Vector2(width, 248f));
            Image templateImage = AddImage(templateRect, theme.panelDeepSprite, Color.white, sliced: true, raycast: true);

            RectTransform viewportRect = NewRect("Viewport", templateRect);
            Stretch(viewportRect, 6f, 6f, 6f, 6f);
            viewportRect.gameObject.AddComponent<RectMask2D>();

            RectTransform contentRect = NewRect("Content", viewportRect);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 46f);

            RectTransform itemRect = NewRect("Item", contentRect);
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0f, 46f);

            Image itemBackground = AddImage(itemRect, theme.buttonNormalSprite, theme.surfaceRaised, sliced: true, raycast: true);

            RectTransform itemCheckRect = NewRect("Item Checkmark", itemRect);
            Place(itemCheckRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(18f, 18f));
            Image itemCheck = AddImage(itemCheckRect, theme.iconCheck, theme.primary);

            RectTransform itemLabelRect = NewRect("Item Label", itemRect);
            Stretch(itemLabelRect, 42f, 18f, 0f, 0f);
            TextMeshProUGUI itemLabel = AddText(itemLabelRect, "OPTION", theme.bodyFont, theme.labelSize, theme.textPrimary, TextAlignmentOptions.MidlineLeft, 1f);

            Toggle itemToggle = itemRect.gameObject.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBackground;
            itemToggle.graphic = itemCheck;

            ScrollRect scroll = templateRect.gameObject.AddComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            templateRect.gameObject.SetActive(false);

            TMP_Dropdown dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = background;
            dropdown.template = templateRect;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            dropdown.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            dropdown.transition = Selectable.Transition.None;

            return root.gameObject;
        }

        public static GameObject SavePrefab(GameObject root, string fileName)
        {
            System.IO.Directory.CreateDirectory(PrefabDir);
            string path = $"{PrefabDir}/{fileName}.prefab";
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
