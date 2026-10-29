using System;
using System.Collections.Generic;
using System.IO;
using RocketBoost.UI.Data;
using UnityEditor;
using UnityEngine;

namespace RocketBoost.UI.Editor
{
    /// <summary>
    /// Generates every sprite used by the UI (rounded 9-slice panels, button states, icons,
    /// stars, starfields, vignette) as crisp PNGs and imports them with correct borders.
    /// Keeping the art procedural means no external asset license and perfectly consistent radii.
    /// </summary>
    public static class UISpriteFactory
    {
        public const string SpriteDir = "Assets/_Project/UI/Sprites";

        private const int IconSize = 128;
        private const int PanelSize = 128;
        private const int RocketWidth = 256;
        private const int RocketHeight = 384;
        private const int FlameWidth = 128;
        private const int FlameHeight = 192;

        public static void GenerateAll(UITheme theme)
        {
            Directory.CreateDirectory(SpriteDir);

            WritePanel("panel_glass", theme, 28f, 34f, UITheme.Alpha(theme.surfaceRaised, 0.94f), UITheme.Alpha(theme.surface, 0.9f), theme.border, 1.8f);
            WritePanel("panel_card", theme, 22f, 28f, UITheme.Alpha(theme.surfaceRaised, 0.98f), UITheme.Alpha(theme.surface, 0.96f), theme.borderDim, 1.4f);
            WritePanel("panel_deep", theme, 20f, 26f, UITheme.Alpha(theme.backgroundDeep, 0.98f), UITheme.Alpha(theme.backgroundDeep, 0.9f), theme.borderDim, 1.2f);

            // Button art is a neutral mask: ThemedButton tints the fill per variant and
            // state, and draws its border as a separate graphic, so nothing is pre-tinted.
            WriteFill("button_normal");
            WriteFill("button_hover");
            WriteFill("button_pressed");
            WriteFill("button_disabled");
            WriteOutline("button_border", 1.6f);
            WriteOutline("button_selected", 2.6f);

            // Baked as a neutral mask: callers tint pills (gold badges, chips, keycaps),
            // so any color baked in here would multiply into every tint.
            WritePill("pill", theme, Color.white, UITheme.Alpha(Color.white, 0.5f));
            WriteGauge("gauge_track", theme, UITheme.Alpha(theme.backgroundDeep, 0.9f), theme.borderDim);
            WriteGauge("gauge_fill", theme, theme.primary, theme.primary);

            WriteGradient("bg_gradient", theme.backgroundDeep, theme.background);
            WriteGlow("glow", 256, 2.4f);
            WriteDot("dot_soft", 64);
            WriteVignette("vignette", 512, 0.92f);
            WriteScanlines("scanlines");
            WriteNebula("nebula", 256);
            WriteRing("ring", 256);
            WriteStreak("menu_streak", 256, 24);
            WriteShape("rocket_loading", RocketWidth, RocketHeight, RocketSdf);
            WriteShape("flame_loading", FlameWidth, FlameHeight, FlameSdf);
            WriteHandle("slider_handle", theme);

            WriteStar("star", 128, false);
            WriteStar("star_outline", 128, true);
            WriteStar("star_small", 64, false);

            WriteStars("stars_far", 1024, 520, 1f, 0.55f);
            WriteStars("stars_mid", 1024, 210, 1.8f, 0.8f);
            WriteStars("stars_near", 1024, 70, 2.8f, 1f, true);

            WriteIcons();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureAll(theme);
        }

        // ------------------------------------------------------------------ primitives

        private static float SdRoundBox(Vector2 p, Vector2 half, float radius)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * radius;
            return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude - radius;
        }

        private static float SdCircle(Vector2 p, float radius) => p.magnitude - radius;

        private static float SdSegment(Vector2 p, Vector2 a, Vector2 b, float thickness)
        {
            Vector2 pa = p - a;
            Vector2 ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude - thickness;
        }

        private static float Coverage(float distance, float softness = 1.1f) =>
            Mathf.Clamp01(0.5f - distance / softness);

        private static void Blend(Color32[] pixels, int width, int x, int y, Color color, float coverage)
        {
            if (coverage <= 0f)
            {
                return;
            }

            int index = y * width + x;
            Color dst = pixels[index];
            float alpha = Mathf.Clamp01(color.a * coverage);
            float outAlpha = alpha + dst.a * (1f - alpha);
            if (outAlpha <= 0.0001f)
            {
                pixels[index] = new Color(0f, 0f, 0f, 0f);
                return;
            }

            Color rgb = (color * alpha + dst * dst.a * (1f - alpha)) / outAlpha;
            pixels[index] = new Color(rgb.r, rgb.g, rgb.b, outAlpha);
        }

        private static Texture2D NewTexture(int width, int height) =>
            new Texture2D(width, height, TextureFormat.RGBA32, false, false);

        private static void Save(Texture2D texture, string name)
        {
            byte[] png = texture.EncodeToPNG();
            string path = $"{SpriteDir}/{name}.png";
            File.WriteAllBytes(path, png);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        // ------------------------------------------------------------------ panels & buttons

        private static void WritePanel(string name, UITheme theme, float radius, float border, Color top, Color bottom, Color rim, float rimWidth)
        {
            int size = PanelSize;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 half = new Vector2(size * 0.5f, size * 0.5f);
            float r = radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                    float sd = SdRoundBox(p, half - Vector2.one * 0.5f, r);
                    float inside = Coverage(sd, 1.2f);

                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float t = Mathf.Clamp01((float)y / size);
                    Color fill = Color.Lerp(bottom, top, t);
                    fill.a *= inside;

                    // Vertical glass sheen near the top edge.
                    float sheen = Mathf.Clamp01((y - (size - 34f)) / 34f) * 0.06f;
                    fill = Color.Lerp(fill, Color.white, sheen);

                    Blend(pixels, size, x, y, fill, inside);

                    float edge = Mathf.Abs(sd + rimWidth * 0.5f);
                    float rimCoverage = Coverage(edge - rimWidth * 0.5f, 1.6f) * inside;
                    if (rimCoverage > 0f)
                    {
                        Blend(pixels, size, x, y, UITheme.Alpha(rim, rim.a), rimCoverage);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteFill(string name)
        {
            int size = 96;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - new Vector2(size * 0.5f, size * 0.5f);
                    float sd = SdRoundBox(p, new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f), 20f);
                    float inside = Coverage(sd, 1.1f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Blend(pixels, size, x, y, Color.white, inside);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteOutline(string name, float rimWidth)
        {
            int size = 96;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - new Vector2(size * 0.5f, size * 0.5f);
                    float sd = SdRoundBox(p, new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f), 20f);
                    float inside = Coverage(sd, 1.1f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    float edge = Mathf.Abs(sd + rimWidth * 0.5f);
                    Blend(pixels, size, x, y, Color.white, Coverage(edge - rimWidth * 0.5f, 1.2f) * inside);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WritePill(string name, UITheme theme, Color fillColor, Color rim)
        {
            int size = 64;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - new Vector2(size * 0.5f, size * 0.5f);
                    float sd = SdRoundBox(p, new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f), size * 0.5f - 1f);
                    float inside = Coverage(sd, 1.1f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Blend(pixels, size, x, y, fillColor, inside);
                    float edge = Mathf.Abs(sd + 0.7f);
                    Blend(pixels, size, x, y, rim, Coverage(edge - 0.7f, 1.4f) * inside);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteGauge(string name, UITheme theme, Color fillColor, Color rim)
        {
            int size = 32;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - new Vector2(size * 0.5f, size * 0.5f);
                    float sd = SdRoundBox(p, new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f), 7f);
                    float inside = Coverage(sd, 1f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Blend(pixels, size, x, y, fillColor, inside);
                    Blend(pixels, size, x, y, rim, Coverage(Mathf.Abs(sd) - 0.5f, 1.2f) * inside * 0.8f);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteGradient(string name, Color top, Color bottom)
        {
            int width = 8;
            int height = 256;
            Texture2D texture = NewTexture(width, height);
            Color32[] pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                Color color = Color.Lerp(bottom, top, (float)y / (height - 1));
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteGlow(string name, int size, float exponent)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), exponent);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteDot(string name, int size)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.55f, 1f, d));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteVignette(string name, int size, float strength)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.35f) / 0.75f)) * strength;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteScanlines(string name)
        {
            int size = 4;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                float a = y % 2 == 0 ? 0.5f : 0f;
                for (int x = 0; x < size; x++)
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteNebula(string name, int size)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) / size;
                    float noise = Fbm(p * 4.2f);
                    float d = (new Vector2(x + 0.5f, y + 0.5f) - center).magnitude / (size * 0.5f);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f) * Mathf.Lerp(0.35f, 1f, noise);
                    float tint = Mathf.Lerp(0.55f, 1f, noise);
                    pixels[y * size + x] = new Color(tint, tint, 1f, a * 0.75f);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteRing(string name, int size)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.42f;
            float half = size * 0.014f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Abs((new Vector2(x + 0.5f, y + 0.5f) - center).magnitude - radius) - half;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f)
                    {
                        continue;
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        /// <summary>
        /// Selection streak for the highlighted row of the text-list menus: a bright core that
        /// tapers to nothing at both ends, so it reads as a light sweep rather than a bar.
        /// </summary>
        private static void WriteStreak(string name, int width, int height)
        {
            Texture2D texture = NewTexture(width, height);
            Color32[] pixels = new Color32[width * height];
            float half = height * 0.5f;

            for (int y = 0; y < height; y++)
            {
                float v = 1f - Mathf.Abs(y + 0.5f - half) / half;
                float core = Mathf.Pow(Mathf.Clamp01(v), 5f);
                float halo = Mathf.Pow(Mathf.Clamp01(v), 1.6f) * 0.45f;

                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)(width - 1);
                    float taper = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.22f))
                        * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.22f));

                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((core + halo) * taper));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteHandle(string name, UITheme theme)
        {
            int size = 48;
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float sd = SdCircle(p, size * 0.5f - 2f);
                    float inside = Coverage(sd, 1.2f);
                    if (inside <= 0f)
                    {
                        continue;
                    }

                    Blend(pixels, size, x, y, theme.backgroundDeep, inside);
                    float ring = Coverage(Mathf.Abs(p.magnitude - (size * 0.5f - 4f)) - 1.6f, 1.2f);
                    Blend(pixels, size, x, y, theme.primary, ring);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static void WriteStar(string name, int size, bool outline)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2[] points = StarPoints(center, size * 0.46f, size * 0.2f, 5);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float sd = PolygonDistance(p, points);
                    float coverage;

                    if (outline)
                    {
                        coverage = Coverage(Mathf.Abs(sd) - size * 0.035f, 1.3f);
                    }
                    else
                    {
                        coverage = Coverage(sd, 1.3f);
                    }

                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, coverage);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static Vector2[] StarPoints(Vector2 center, float outer, float inner, int count)
        {
            Vector2[] points = new Vector2[count * 2];
            float step = Mathf.PI / count;

            for (int i = 0; i < points.Length; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                float angle = Mathf.PI * 0.5f + i * step;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return points;
        }

        private static void WriteStars(string name, int size, int count, float dotSize, float brightness, bool glow = false)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            System.Random random = new System.Random(name.GetHashCode());

            for (int i = 0; i < count; i++)
            {
                float x = (float)random.NextDouble() * size;
                float y = (float)random.NextDouble() * size;
                float radius = dotSize * (0.55f + (float)random.NextDouble() * 0.9f);
                float alpha = brightness * (0.35f + (float)random.NextDouble() * 0.65f);
                Color color = new Color(1f, 1f, 1f, alpha);

                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        float cx = x + ox * size;
                        float cy = y + oy * size;
                        float extent = glow ? radius * 4f : radius;

                        int minX = Mathf.Max(0, Mathf.FloorToInt(cx - extent));
                        int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(cx + extent));
                        int minY = Mathf.Max(0, Mathf.FloorToInt(cy - extent));
                        int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(cy + extent));

                        for (int py = minY; py <= maxY; py++)
                        {
                            for (int px = minX; px <= maxX; px++)
                            {
                                float d = Vector2.Distance(new Vector2(px + 0.5f, py + 0.5f), new Vector2(cx, cy));
                                float coverage = Mathf.Clamp01(1f - d / extent);
                                coverage = glow ? coverage * coverage * 0.5f : Mathf.Clamp01(coverage * 1.6f);
                                Blend(pixels, size, px, py, color, coverage);
                            }
                        }
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static float PolygonDistance(Vector2 p, Vector2[] polygon)
        {
            float d = Vector2.Dot(p - polygon[0], p - polygon[0]);
            float s = 1f;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i, i++)
            {
                Vector2 e = polygon[j] - polygon[i];
                Vector2 w = p - polygon[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));

                bool c1 = p.y >= polygon[i].y;
                bool c2 = p.y < polygon[j].y;
                bool c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                {
                    s = -s;
                }
            }

            return s * Mathf.Sqrt(d);
        }

        private static float Hash(float x, float y)
        {
            float v = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        private static float Fbm(Vector2 p)
        {
            float total = 0f;
            float amplitude = 0.6f;
            float frequency = 1f;

            for (int i = 0; i < 4; i++)
            {
                float x = p.x * frequency;
                float y = p.y * frequency;
                int xi = Mathf.FloorToInt(x);
                int yi = Mathf.FloorToInt(y);
                float xf = x - xi;
                float yf = y - yi;

                float a = Hash(xi, yi);
                float b = Hash(xi + 1, yi);
                float c = Hash(xi, yi + 1);
                float d = Hash(xi + 1, yi + 1);

                float u = xf * xf * (3f - 2f * xf);
                float v = yf * yf * (3f - 2f * yf);

                total += Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v) * amplitude;
                amplitude *= 0.5f;
                frequency *= 2.1f;
            }

            return Mathf.Clamp01(total);
        }

        // ------------------------------------------------------------------ icons

        private static void WriteIcons()
        {
            WriteIcon("icon_play", p => TriangleDistance(p, new Vector2(-0.32f, 0.55f), new Vector2(-0.32f, -0.55f), new Vector2(0.58f, 0f)));
            WriteIcon("icon_pause", p => Mathf.Min(
                SdRoundBox(p - new Vector2(-0.3f, 0f), new Vector2(0.16f, 0.55f), 0.08f),
                SdRoundBox(p - new Vector2(0.3f, 0f), new Vector2(0.16f, 0.55f), 0.08f)));
            WriteIcon("icon_restart", RestartSdf);
            WriteIcon("icon_settings", SettingsSdf);
            WriteIcon("icon_home", HomeSdf);
            WriteIcon("icon_chevron_left", p => Mathf.Min(
                SdSegment(p, new Vector2(0.15f, 0.55f), new Vector2(-0.25f, 0f), 0.1f),
                SdSegment(p, new Vector2(-0.25f, 0f), new Vector2(0.15f, -0.55f), 0.1f)));
            WriteIcon("icon_chevron_right", p => Mathf.Min(
                SdSegment(p, new Vector2(-0.15f, 0.55f), new Vector2(0.25f, 0f), 0.1f),
                SdSegment(p, new Vector2(0.25f, 0f), new Vector2(-0.15f, -0.55f), 0.1f)));
            WriteIcon("icon_lock", LockSdf);
            WriteIcon("icon_check", p => Mathf.Min(
                SdSegment(p, new Vector2(-0.45f, 0.02f), new Vector2(-0.12f, -0.32f), 0.1f),
                SdSegment(p, new Vector2(-0.12f, -0.32f), new Vector2(0.46f, 0.4f), 0.1f)));
            WriteIcon("icon_volume", VolumeSdf);
            WriteIcon("icon_fullscreen", FullscreenSdf);
            WriteIcon("icon_keyboard", KeyboardSdf);
            WriteIcon("icon_gamepad", GamepadSdf);
            WriteIcon("icon_star", p => StarDistance(p, 0.56f, 0.24f));
        }

        private static void WriteIcon(string name, Func<Vector2, float> sdf) =>
            WriteShape(name, IconSize, IconSize, sdf);

        private static void WriteShape(string name, int width, int height, Func<Vector2, float> sdf)
        {
            Texture2D texture = NewTexture(width, height);
            Color32[] pixels = new Color32[width * height];
            float half = height * 0.5f;
            float halfWidth = width * 0.5f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float coverage = 0f;

                    for (int sy = 0; sy < 2; sy++)
                    {
                        for (int sx = 0; sx < 2; sx++)
                        {
                            Vector2 p = new Vector2(
                                (x + 0.25f + sx * 0.5f - halfWidth) / half,
                                (y + 0.25f + sy * 0.5f - half) / half);

                            float pixelDistance = sdf(p) * half;
                            coverage += Mathf.Clamp01(0.5f - pixelDistance / 1.6f);
                        }
                    }

                    coverage *= 0.25f;
                    if (coverage <= 0.001f)
                    {
                        continue;
                    }

                    pixels[y * width + x] = new Color(1f, 1f, 1f, coverage);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            Save(texture, name);
        }

        private static float RocketSdf(Vector2 p)
        {
            float body = SdRoundBox(p - new Vector2(0f, -0.02f), new Vector2(0.18f, 0.43f), 0.07f);
            float nose = TriangleDistance(p, new Vector2(0f, 0.84f), new Vector2(-0.18f, 0.30f), new Vector2(0.18f, 0.30f));
            float nozzle = SdRoundBox(p - new Vector2(0f, -0.50f), new Vector2(0.11f, 0.06f), 0.02f);
            float leftFin = TriangleDistance(p, new Vector2(-0.17f, -0.16f), new Vector2(-0.40f, -0.52f), new Vector2(-0.17f, -0.52f));
            float rightFin = TriangleDistance(p, new Vector2(0.17f, -0.16f), new Vector2(0.40f, -0.52f), new Vector2(0.17f, -0.52f));

            float hull = Mathf.Min(Mathf.Min(body, nose), Mathf.Min(nozzle, Mathf.Min(leftFin, rightFin)));
            float windowRing = Mathf.Abs(SdCircle(p - new Vector2(0f, 0.14f), 0.10f)) - 0.018f;
            float windowHole = SdCircle(p - new Vector2(0f, 0.14f), 0.078f);
            return Mathf.Max(Mathf.Min(hull, windowRing), -windowHole);
        }

        private static float FlameSdf(Vector2 p)
        {
            float cone = TriangleDistance(p, new Vector2(0f, -0.88f), new Vector2(-0.42f, 0.16f), new Vector2(0.42f, 0.16f));
            float bulb = SdCircle(p - new Vector2(0f, 0.22f), 0.34f);
            return Mathf.Min(cone, bulb);
        }

        private static float TriangleDistance(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2[] points = { a, b, c };
            return PolygonDistance(p, points);
        }

        private static float StarDistance(Vector2 p, float outer, float inner)
        {
            Vector2[] points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return PolygonDistance(p, points);
        }

        private static float RestartSdf(Vector2 p)
        {
            float radius = 0.46f;
            float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            float arc = Mathf.Abs(p.magnitude - radius) - 0.1f;

            // Leave a wedge open on the right side for the arrow head.
            if (angle > -30f && angle < 38f)
            {
                return 1f;
            }

            float head = TriangleDistance(
                p,
                new Vector2(0.30f, 0.62f),
                new Vector2(0.80f, 0.30f),
                new Vector2(0.26f, 0.10f));

            return Mathf.Min(arc, head);
        }

        private static float SettingsSdf(Vector2 p)
        {
            float ring = Mathf.Abs(p.magnitude - 0.52f) - 0.12f;
            float teeth = 1f;

            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 centre = dir * 0.66f;
                Vector2 local = p - centre;
                float ca = Mathf.Cos(-a);
                float sa = Mathf.Sin(-a);
                Vector2 rotated = new Vector2(local.x * ca - local.y * sa, local.x * sa + local.y * ca);
                teeth = Mathf.Min(teeth, SdRoundBox(rotated, new Vector2(0.11f, 0.16f), 0.04f));
            }

            float body = Mathf.Min(ring, teeth);
            float hole = SdCircle(p, 0.18f);
            return Mathf.Max(body, -hole);
        }

        private static float HomeSdf(Vector2 p)
        {
            float roof = TriangleDistance(
                p,
                new Vector2(-0.62f, 0.02f),
                new Vector2(0f, 0.64f),
                new Vector2(0.62f, 0.02f));

            float body = SdRoundBox(p - new Vector2(0f, -0.26f), new Vector2(0.46f, 0.34f), 0.04f);
            float door = SdRoundBox(p - new Vector2(0f, -0.36f), new Vector2(0.12f, 0.2f), 0.03f);
            return Mathf.Max(Mathf.Min(roof, body), -door);
        }

        private static float LockSdf(Vector2 p)
        {
            float body = SdRoundBox(p - new Vector2(0f, -0.22f), new Vector2(0.46f, 0.36f), 0.08f);
            float ring = Mathf.Abs(p.magnitude - 0.28f) - 0.08f;
            float arc = p.y > 0.1f ? ring : 1f;
            float keyhole = SdCircle(p - new Vector2(0f, -0.2f), 0.08f);
            return Mathf.Max(Mathf.Min(body, arc), -keyhole);
        }

        private static float VolumeSdf(Vector2 p)
        {
            Vector2[] speaker =
            {
                new Vector2(-0.62f, 0.22f),
                new Vector2(-0.62f, -0.22f),
                new Vector2(-0.18f, -0.5f),
                new Vector2(-0.18f, 0.5f)
            };

            float shape = PolygonDistance(p, speaker);

            float arc1 = Mathf.Abs(Vector2.Distance(p, new Vector2(-0.08f, 0f)) - 0.42f) - 0.055f;
            float arc2 = Mathf.Abs(Vector2.Distance(p, new Vector2(-0.08f, 0f)) - 0.62f) - 0.055f;
            bool right = p.x > 0.12f;
            float arcs = right ? Mathf.Min(arc1, arc2) : 1f;

            return Mathf.Min(shape, arcs);
        }

        private static float FullscreenSdf(Vector2 p)
        {
            const float len = 0.28f;
            const float thick = 0.07f;
            float d = 1f;

            d = Mathf.Min(d, SdSegment(p, new Vector2(-0.6f, 0.6f), new Vector2(-0.6f + len, 0.6f), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(-0.6f, 0.6f), new Vector2(-0.6f, 0.6f - len), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.6f, 0.6f), new Vector2(0.6f - len, 0.6f), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.6f, 0.6f), new Vector2(0.6f, 0.6f - len), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(-0.6f, -0.6f), new Vector2(-0.6f + len, -0.6f), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(-0.6f, -0.6f), new Vector2(-0.6f, -0.6f + len), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.6f, -0.6f), new Vector2(0.6f - len, -0.6f), thick));
            d = Mathf.Min(d, SdSegment(p, new Vector2(0.6f, -0.6f), new Vector2(0.6f, -0.6f + len), thick));
            return d;
        }

        private static float KeyboardSdf(Vector2 p)
        {
            float frame = Mathf.Max(
                SdRoundBox(p, new Vector2(0.68f, 0.4f), 0.1f),
                -SdRoundBox(p, new Vector2(0.6f, 0.32f), 0.06f));

            float keys = 1f;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    Vector2 position = new Vector2(-0.36f + col * 0.24f, row == 0 ? 0.14f : -0.12f);
                    keys = Mathf.Min(keys, SdRoundBox(p - position, new Vector2(0.06f, 0.055f), 0.02f));
                }
            }

            keys = Mathf.Min(keys, SdRoundBox(p - new Vector2(0f, -0.26f), new Vector2(0.26f, 0.045f), 0.02f));
            return Mathf.Min(frame, keys);
        }

        private static float GamepadSdf(Vector2 p)
        {
            float body = SdRoundBox(p, new Vector2(0.62f, 0.34f), 0.26f);
            float grip = Mathf.Min(
                SdCircle(p - new Vector2(-0.5f, -0.18f), 0.22f),
                SdCircle(p - new Vector2(0.5f, -0.18f), 0.22f));
            float shape = Mathf.Min(body, grip);

            float dpad = Mathf.Min(
                SdRoundBox(p - new Vector2(-0.34f, 0.06f), new Vector2(0.13f, 0.04f), 0.02f),
                SdRoundBox(p - new Vector2(-0.34f, 0.06f), new Vector2(0.04f, 0.13f), 0.02f));

            float buttons = Mathf.Min(
                SdCircle(p - new Vector2(0.32f, 0.12f), 0.07f),
                SdCircle(p - new Vector2(0.44f, -0.02f), 0.07f));

            return Mathf.Min(shape, Mathf.Min(dpad, buttons));
        }

        // ------------------------------------------------------------------ import settings

        private static void ConfigureAll(UITheme theme)
        {
            string[] names = Directory.GetFiles(SpriteDir, "*.png");
            foreach (string file in names)
            {
                string path = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                bool isTiled = fileName == "scanlines" || fileName.StartsWith("stars_");
                bool isIcon = fileName.StartsWith("icon_") || fileName is "star" or "star_outline" or "star_small";

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = isTiled ? FilterMode.Bilinear : FilterMode.Bilinear;
                importer.wrapMode = isTiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePixelsPerUnit = 100f;

                if (!isIcon && !isTiled)
                {
                    settings.spriteBorder = BorderFor(fileName);
                }

                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static Vector4 BorderFor(string name)
        {
            switch (name)
            {
                case "panel_glass": return new Vector4(36f, 36f, 36f, 36f);
                case "panel_card": return new Vector4(30f, 30f, 30f, 30f);
                case "panel_deep": return new Vector4(28f, 28f, 28f, 28f);
                case "button_normal":
                case "button_hover":
                case "button_pressed":
                case "button_disabled": return new Vector4(28f, 28f, 28f, 28f);
                // Outlines use tighter borders: they must still 9-slice cleanly on
                // short buttons where the rounded corners take most of the height.
                case "button_selected":
                case "button_border": return new Vector4(20f, 20f, 20f, 20f);
                case "pill": return new Vector4(32f, 32f, 32f, 32f);
                // The tapered ends live inside the horizontal borders, so stretching the
                // 2px core row keeps the sweep's shape at any bar width.
                case "menu_streak": return new Vector4(58f, 11f, 58f, 11f);
                case "gauge_track":
                case "gauge_fill": return new Vector4(10f, 10f, 10f, 10f);
                default: return Vector4.zero;
            }
        }
    }
}
