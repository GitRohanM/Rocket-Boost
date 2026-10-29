using UnityEngine;

namespace RocketBoost.UI
{
    /// <summary>Applies the shared theme to every theme-aware widget under a root.</summary>
    public static class UIThemeBinder
    {
        public static void Bind(GameObject root, Data.UITheme theme)
        {
            if (root == null || theme == null)
            {
                return;
            }

            foreach (ThemedButton button in root.GetComponentsInChildren<ThemedButton>(true))
            {
                button.ApplyTheme(theme);
            }

            foreach (GaugeBar gauge in root.GetComponentsInChildren<GaugeBar>(true))
            {
                gauge.ApplyTheme(theme);
            }
        }
    }
}
