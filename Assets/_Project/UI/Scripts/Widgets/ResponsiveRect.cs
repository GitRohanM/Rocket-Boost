using System;
using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>
    /// Re-anchors a RectTransform between authored landscape and portrait layouts so the same
    /// prefab works at 16:9, 16:10, 21:9 and portrait phones.
    /// </summary>
    public sealed class ResponsiveRect : MonoBehaviour
    {
        [Serializable]
        public struct Layout
        {
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
        }

        [SerializeField] private Layout landscape;
        [SerializeField] private Layout portrait;
        [SerializeField] private float portraitAspectThreshold = 0.95f;

        private RectTransform rect;
        private bool? lastPortrait;

        private void OnEnable()
        {
            Apply(force: true);
        }

        private void Update()
        {
            if (Screen.height <= 0 || Screen.width <= 0)
            {
                return;
            }

            bool portraitNow = (float)Screen.width / Screen.height < portraitAspectThreshold;
            if (lastPortrait != portraitNow)
            {
                Apply(force: false);
            }
        }

        public void Apply(bool force)
        {
            if (rect == null)
            {
                rect = (RectTransform)transform;
            }

            bool isPortrait = Screen.height > 0 && (float)Screen.width / Screen.height < portraitAspectThreshold;
            if (!force && lastPortrait == isPortrait)
            {
                return;
            }

            lastPortrait = isPortrait;
            Layout layout = isPortrait ? portrait : landscape;

            rect.anchorMin = layout.anchorMin;
            rect.anchorMax = layout.anchorMax;
            rect.pivot = layout.pivot;
            rect.anchoredPosition = layout.anchoredPosition;
            rect.sizeDelta = layout.sizeDelta;
        }
    }
}
