#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SecurityCenter.MainMenu.Editor
{
    public static class MainMenuAssetGenerator
    {
        private const string BaseDir = "Assets/MainMenu";
        private const string TexDir = BaseDir + "/Textures";
        private const string MatDir = BaseDir + "/Materials";
        private const string FontDir = BaseDir + "/Fonts";
        private const string AudioDir = BaseDir + "/Audio";

        public static void GenerateAllAssets()
        {
            EnsureDir(BaseDir);
            EnsureDir(TexDir);
            EnsureDir(MatDir);
            EnsureDir(FontDir);
            EnsureDir(AudioDir);

            GenerateFonts();
            GenerateTextures();
            GenerateAudioClips();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ConfigureSpriteImporters();
            GenerateMaterial();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDir(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!Directory.Exists(full)) Directory.CreateDirectory(full);
        }

        // =====================================================================
        // 1. FONTS (Impact + Courier New Bold + Leelawadee UI Bold Thai Fallback)
        // =====================================================================
        private static void GenerateFonts()
        {
            CopyWinFontIfAvailable("impact.ttf", FontDir + "/MM_Impact.ttf");
            CopyWinFontIfAvailable("courbd.ttf", FontDir + "/MM_CourierBold.ttf");
            if (!CopyWinFontIfAvailable("LeelUIb.ttf", FontDir + "/MM_ThaiBold.ttf"))
            {
                CopyWinFontIfAvailable("tahomabd.ttf", FontDir + "/MM_ThaiBold.ttf");
            }
            AssetDatabase.Refresh();

            var thaiFont = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/MM_ThaiBold.ttf");
            var impactFont = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/MM_Impact.ttf");
            var courierFont = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/MM_CourierBold.ttf");

            TMP_FontAsset thaiTmp = CreateOrLoadDynamicTmp(thaiFont, FontDir + "/MM_ThaiUI_SDF.asset");
            TMP_FontAsset titleTmp = CreateOrLoadDynamicTmp(impactFont != null ? impactFont : thaiFont, FontDir + "/MM_TitleFont_SDF.asset");
            TMP_FontAsset typeTmp = CreateOrLoadDynamicTmp(courierFont != null ? courierFont : thaiFont, FontDir + "/MM_Typewriter_SDF.asset");

            if (thaiTmp != null)
            {
                if (titleTmp != null && titleTmp != thaiTmp)
                {
                    if (titleTmp.fallbackFontAssetTable == null) titleTmp.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    if (!titleTmp.fallbackFontAssetTable.Contains(thaiTmp))
                    {
                        titleTmp.fallbackFontAssetTable.Add(thaiTmp);
                        EditorUtility.SetDirty(titleTmp);
                    }
                }
                if (typeTmp != null && typeTmp != thaiTmp)
                {
                    if (typeTmp.fallbackFontAssetTable == null) typeTmp.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    if (!typeTmp.fallbackFontAssetTable.Contains(thaiTmp))
                    {
                        typeTmp.fallbackFontAssetTable.Add(thaiTmp);
                        EditorUtility.SetDirty(typeTmp);
                    }
                }
            }
        }

        private static bool CopyWinFontIfAvailable(string winFileName, string targetAssetPath)
        {
            string fullTarget = Path.Combine(Directory.GetCurrentDirectory(), targetAssetPath);
            if (File.Exists(fullTarget)) return true;

            string winFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string src = Path.Combine(winFonts, winFileName);
            if (File.Exists(src))
            {
                File.Copy(src, fullTarget, true);
                return true;
            }
            return false;
        }

        private static TMP_FontAsset CreateOrLoadDynamicTmp(Font sourceFont, string savePath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(savePath);
            if (existing != null) return existing;
            if (sourceFont == null) return TMP_Settings.defaultFontAsset;

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 44, 4, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);

            if (fontAsset == null) return TMP_Settings.defaultFontAsset;

            AssetDatabase.CreateAsset(fontAsset, savePath);
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    if (fontAsset.atlasTextures[i] != null)
                    {
                        fontAsset.atlasTextures[i].name = fontAsset.name + " Atlas " + i;
                        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);
                    }
                }
            }
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        // =====================================================================
        // 2. PROCEDURAL TEXTURES (Ref 2, Ref 3, Ref 5)
        // =====================================================================
        private static void GenerateTextures()
        {
            GenerateDeskBackgroundTexture(TexDir + "/BG_Desk.png");
            GenerateStaticNoiseTexture(TexDir + "/StaticNoiseTex.png");
            GenerateStampBrushTexture(TexDir + "/StampBrushBox.png");
            GenerateScanlineMaskTexture(TexDir + "/ScanlineMask.png");
            GeneratePlayIconTexture(TexDir + "/PlayTriangle.png");
            GenerateVignetteTexture(TexDir + "/Vignette.png");
            GenerateGhostEyeTexture(TexDir + "/GhostEye.png");
            GenerateShrineSilhouetteTexture(TexDir + "/ShrineSilhouette.png");
            GenerateRetroFolderGreenTexture(TexDir + "/RetroFolderGreen.png");
            GeneratePhoneCallButtonTexture(TexDir + "/PhoneBtnAccept.png", true);
            GeneratePhoneCallButtonTexture(TexDir + "/PhoneBtnDecline.png", false);
            GenerateHorrorAlmondEyeTexture(TexDir + "/HorrorAlmondEye.png");
            GenerateHorrorEyePupilTexture(TexDir + "/HorrorEyePupil.png");
            GenerateGlassCrackTexture(TexDir + "/GlassCrackOverlay.png");
        }

        private static void GenerateStaticNoiseTexture(string path)
        {
            int w = 512, h = 288;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            var rng = new System.Random(7777);
            for (int y = 0; y < h; y++)
            {
                float band = 0.85f + 0.15f * Mathf.Sin(y * 0.45f);
                for (int x = 0; x < w; x++)
                {
                    float n = (float)rng.NextDouble();
                    // High-contrast CRT TV static snow (Ref 3)
                    float v = Mathf.Clamp01((n * 0.85f + 0.10f) * band);
                    pixels[y * w + x] = new Color(v, v, v, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateDeskBackgroundTexture(string path)
        {
            int w = 960, h = 540;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            var rng = new System.Random(1337);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;

                    // Dark overhead desk lamp cone centered-right (like Ref 5)
                    float lampDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.64f, 0.52f));
                    float lampGlow = Mathf.Clamp01(1f - lampDist * 1.45f);
                    lampGlow = lampGlow * lampGlow * 0.26f;

                    // Desk surface in lower half
                    float deskSurface = ny < 0.48f ? 0.05f + (0.48f - ny) * 0.08f : 0.02f;

                    // CRT Monitor silhouette on center-left of desk (x: 0.36..0.58, y: 0.36..0.72)
                    float crtBox = 0f;
                    if (nx > 0.37f && nx < 0.57f && ny > 0.38f && ny < 0.72f)
                    {
                        crtBox = 0.09f;
                        if (nx > 0.39f && nx < 0.55f && ny > 0.42f && ny < 0.68f)
                            crtBox = 0.17f + 0.02f * Mathf.Sin(y * 1.6f); // CRT phosphor glow
                    }

                    // Scattered case files & documents on bottom-right (like Ref 5)
                    float paperVal = 0f;
                    // Paper 1 rotated
                    Vector2 p1 = Rotate2D(new Vector2(nx - 0.62f, ny - 0.24f), 0.24f);
                    if (Mathf.Abs(p1.x) < 0.11f && Mathf.Abs(p1.y) < 0.14f)
                    {
                        paperVal = 0.22f;
                        // Simulated text lines on paper
                        if (Mathf.Abs(p1.x) < 0.085f && Mathf.Sin(p1.y * 190f) > 0.45f) paperVal = 0.10f;
                    }
                    // Paper 2 rotated
                    Vector2 p2 = Rotate2D(new Vector2(nx - 0.75f, ny - 0.18f), -0.31f);
                    if (Mathf.Abs(p2.x) < 0.12f && Mathf.Abs(p2.y) < 0.13f)
                    {
                        paperVal = 0.26f;
                        if (Mathf.Abs(p2.x) < 0.09f && Mathf.Sin(p2.y * 170f) > 0.45f) paperVal = 0.12f;
                    }

                    // Framed portrait on far right (like Ref 5)
                    float frameVal = 0f;
                    Vector2 pf = Rotate2D(new Vector2(nx - 0.86f, ny - 0.50f), -0.06f);
                    if (Mathf.Abs(pf.x) < 0.085f && Mathf.Abs(pf.y) < 0.18f)
                    {
                        frameVal = 0.20f;
                        if (Mathf.Abs(pf.x) < 0.068f && Mathf.Abs(pf.y) < 0.15f)
                        {
                            float faceDist = Vector2.Distance(pf, new Vector2(0.01f, 0.02f));
                            frameVal = faceDist < 0.045f ? 0.28f : 0.11f;
                        }
                    }

                    // Shrine silhouette (Small_roof_lamp) on far right shelf
                    float shrineVal = 0f;
                    if (nx > 0.18f && nx < 0.26f && ny > 0.45f && ny < 0.68f)
                    {
                        float roofW = Mathf.Lerp(0.045f, 0.01f, (ny - 0.45f) / 0.23f);
                        if (Mathf.Abs(nx - 0.22f) < roofW) shrineVal = 0.08f;
                    }

                    float grain = ((float)rng.NextDouble() - 0.5f) * 0.025f;
                    float v = Mathf.Clamp01(0.03f + lampGlow + deskSurface + crtBox + paperVal + frameVal + shrineVal + grain);
                    // Slightly cool desaturated tint
                    pixels[y * w + x] = new Color(v * 0.96f, v * 0.98f, v * 1.0f, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static Vector2 Rotate2D(Vector2 v, float angleRad)
        {
            float c = Mathf.Cos(angleRad), s = Mathf.Sin(angleRad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private static void GenerateStampBrushTexture(string path)
        {
            // Authentic distressed white brush-stroke banner (Ref 5 "r e q u i e m")
            int w = 512, h = 72;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            var rng = new System.Random(4242);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                // Dry brush streaks running horizontally across the stroke
                float rowStreak = Mathf.PerlinNoise(0.5f, ny * 28f);
                float leftBrush = 0.03f + 0.08f * Mathf.PerlinNoise(1.7f, ny * 22f) + (rowStreak < 0.35f ? 0.05f : 0f);
                float rightBrush = 0.96f - 0.06f * Mathf.PerlinNoise(4.3f, ny * 19f) - (rowStreak > 0.72f ? 0.04f : 0f);

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;

                    // Organic uneven top & bottom edges with subtle paint splatter notches
                    float topBrush = 0.88f - 0.07f * Mathf.PerlinNoise(nx * 14f, 1.2f) - 0.03f * Mathf.PerlinNoise(nx * 45f, 2.4f);
                    float botBrush = 0.12f + 0.07f * Mathf.PerlinNoise(nx * 14f, 5.7f) + 0.03f * Mathf.PerlinNoise(nx * 45f, 8.1f);

                    bool inside = nx >= leftBrush && nx <= rightBrush && ny >= botBrush && ny <= topBrush;
                    if (!inside)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    // Subtle dry-brush bristles at very outer edges only & organic grunge paper grain
                    float edgeFadeL = Mathf.Clamp01((nx - leftBrush) / 0.025f);
                    float bristle = Mathf.PerlinNoise(nx * 18f, ny * 52f);
                    if (edgeFadeL < 0.7f && bristle < 0.35f * (1f - edgeFadeL))
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float grain = ((float)rng.NextDouble() - 0.5f) * 0.03f;
                    float v = Mathf.Clamp01(0.93f + grain);
                    pixels[y * w + x] = new Color(v, v * 0.99f, v * 0.97f, 0.97f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateScanlineMaskTexture(string path)
        {
            int w = 64, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float a = (y % 4 < 2) ? 0.30f : 0.0f;
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = new Color(0f, 0f, 0f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GeneratePlayIconTexture(string path)
        {
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            Vector2 a = new Vector2(28f, 18f);
            Vector2 b = new Vector2(28f, 110f);
            Vector2 c = new Vector2(108f, 64f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float sd = SignedDistTriangle(p, a, b, c) - 4f; // rounded by 4px
                    float alpha = Mathf.Clamp01(0.5f - sd * 0.65f);
                    pixels[y * w + x] = new Color(0.96f, 0.96f, 0.95f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static float SignedDistTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(Vector2.Min(
                new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        private static void GenerateVignetteTexture(string path)
        {
            int w = 256, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / (h - 1) * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / (w - 1) * 2f - 1f;
                    float dist = Mathf.Sqrt(nx * nx * 0.85f + ny * ny * 1.05f);
                    float a = Mathf.SmoothStep(0.32f, 1.08f, dist) * 0.88f;
                    pixels[y * w + x] = new Color(0.01f, 0.01f, 0.01f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateGhostEyeTexture(string path)
        {
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            Vector2 center = new Vector2(64f, 64f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), center);
                    if (r > 58f) { pixels[y * w + x] = Color.clear; continue; }
                    if (r < 14f) pixels[y * w + x] = new Color(0.02f, 0.02f, 0.02f, 0.95f);
                    else if (r < 28f) pixels[y * w + x] = new Color(0.95f, 0.12f, 0.12f, 0.95f);
                    else if (r < 34f) pixels[y * w + x] = new Color(0.05f, 0.05f, 0.05f, 0.95f);
                    else pixels[y * w + x] = new Color(0.92f, 0.90f, 0.90f, Mathf.Clamp01((58f - r) / 8f));
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateShrineSilhouetteTexture(string path)
        {
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = Mathf.Abs((float)x / w - 0.5f);
                    bool hit = false;
                    // Pedestal + Shrine House + Pointed Thai Roof (Small_roof_lamp)
                    if (ny >= 0.10f && ny < 0.35f && nx < 0.08f) hit = true;
                    else if (ny >= 0.35f && ny < 0.42f && nx < 0.24f) hit = true;
                    else if (ny >= 0.42f && ny < 0.65f && nx < 0.17f) hit = true;
                    else if (ny >= 0.65f && ny < 0.92f && nx < Mathf.Lerp(0.28f, 0.01f, (ny - 0.65f) / 0.27f)) hit = true;

                    pixels[y * w + x] = hit ? new Color(0.9f, 0.9f, 0.9f, 1f) : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateRetroFolderGreenTexture(string path)
        {
            int w = 160, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            Color darkBack = new Color(0.10f, 0.56f, 0.26f, 1f);
            Color brightFront = new Color(0.32f, 0.88f, 0.52f, 1f);
            Color borderDark = new Color(0.01f, 0.06f, 0.03f, 1f);

            for (int y = 0; y < h; y++)
            {
                bool scanDark = (y % 4 == 0);
                for (int x = 0; x < w; x++)
                {
                    // Back folder body: x in [18..126], y in [16..88]
                    // Back folder top tab: x in [18..64], y in [88..104]
                    bool inBack = (x >= 18 && x <= 126 && y >= 16 && y <= 88) ||
                                  (x >= 18 && x <= 64 && y >= 88 && y <= 104);

                    // Front skewed flap: y in [16..74], skewed right as y increases
                    float skewOffset = (y - 16f) * 0.24f;
                    float frontL = 22f + skewOffset;
                    float frontR = 134f + skewOffset;
                    bool inFront = (y >= 16 && y <= 74 && x >= frontL && x <= frontR);

                    if (!inBack && !inFront)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    Color c;
                    if (inFront)
                    {
                        bool isEdge = (y <= 18 || y >= 72 || x <= frontL + 2.5f || x >= frontR - 2.5f);
                        c = isEdge ? borderDark : brightFront;
                    }
                    else
                    {
                        bool isEdge = (x <= 20 || x >= 124 || y <= 18 || (y >= 86 && x > 64) || y >= 102 || (y > 88 && x >= 62));
                        c = isEdge ? borderDark : darkBack;
                    }

                    if (scanDark && c != borderDark)
                    {
                        c.r *= 0.72f;
                        c.g *= 0.72f;
                        c.b *= 0.72f;
                    }
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GeneratePhoneCallButtonTexture(string path, bool isAccept)
        {
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            Vector2 center = new Vector2(64f, 64f);
            Color baseCol = isAccept ? new Color(0.32f, 0.88f, 0.50f, 1f) : new Color(0.95f, 0.36f, 0.36f, 1f);

            for (int y = 0; y < h; y++)
            {
                float scan = (y % 4 == 0) ? 0.72f : 1.0f;
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float r = p.magnitude;
                    if (r > 58f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float edgeAlpha = Mathf.Clamp01((58f - r) / 2.5f);
                    Color c = new Color(baseCol.r * scan, baseCol.g * scan, baseCol.b * scan, edgeAlpha);

                    // Draw phone receiver icon in center
                    Vector2 rp = isAccept ? Rotate2D(p, -0.72f) : p;
                    // Arch of handset + 2 earpieces
                    float archDist = Vector2.Distance(rp, new Vector2(0f, -14f));
                    bool inHandsetArch = (archDist >= 18f && archDist <= 26f && rp.y > -8f && Mathf.Abs(rp.x) <= 22f);
                    bool inLeftCap = Vector2.Distance(rp, new Vector2(-17f, -6f)) <= 6.5f;
                    bool inRightCap = Vector2.Distance(rp, new Vector2(17f, -6f)) <= 6.5f;

                    if (inHandsetArch || inLeftCap || inRightCap)
                    {
                        c = new Color(0.04f, 0.06f, 0.05f, edgeAlpha);
                    }

                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateHorrorAlmondEyeTexture(string path)
        {
            // Almond eye socket + red rim + white sclera with scanlines (pupil is separate so it can rove)
            int w = 256, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (y / (float)(h - 1)) * 2f - 1f; // -1..1
                float scan = (y % 3 == 0) ? 0.68f : 1.0f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x / (float)(w - 1)) * 2f - 1f; // -1..1
                    float ax = Mathf.Abs(nx);
                    if (ax >= 0.96f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }
                    // Almond eyelid curve: height = cos(nx * pi/2) * 0.78
                    float lidHalfH = Mathf.Pow(Mathf.Cos(nx * Mathf.PI * 0.5f), 1.25f) * 0.82f;
                    float ay = Mathf.Abs(ny);
                    if (ay > lidHalfH)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    float rimDist = lidHalfH - ay;
                    Color c;
                    if (rimDist < 0.09f)
                    {
                        // Bright crimson outer eyelid rim
                        c = new Color(0.95f * scan, 0.08f * scan, 0.08f * scan, 1f);
                    }
                    else if (rimDist < 0.14f)
                    {
                        // Dark inner crease
                        c = new Color(0.08f, 0.02f, 0.02f, 1f);
                    }
                    else
                    {
                        // Sclera shading from white center to reddish corners
                        float cornerShade = Mathf.Clamp01(1f - ax * 0.65f);
                        float r = Mathf.Lerp(0.75f, 0.96f, cornerShade) * scan;
                        float g = Mathf.Lerp(0.45f, 0.88f, cornerShade) * scan;
                        float b = Mathf.Lerp(0.45f, 0.88f, cornerShade) * scan;
                        c = new Color(r, g, b, 1f);
                    }
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateHorrorEyePupilTexture(string path)
        {
            // Red concentric iris + black pupil + white glint (Screenshot 3)
            int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            Vector2 center = new Vector2(64f, 64f);
            for (int y = 0; y < h; y++)
            {
                float scan = (y % 3 == 0) ? 0.72f : 1.0f;
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float r = p.magnitude;
                    if (r > 58f)
                    {
                        pixels[y * w + x] = Color.clear;
                        continue;
                    }

                    // Specular white highlight top-left
                    if (Vector2.Distance(p, new Vector2(-14f, 12f)) < 8.5f)
                    {
                        pixels[y * w + x] = new Color(1f, 0.96f, 0.96f, 0.96f);
                        continue;
                    }

                    Color c;
                    if (r < 19f)
                    {
                        c = new Color(0.02f, 0.02f, 0.02f, 1f);
                    }
                    else if (r < 23f)
                    {
                        c = new Color(0.98f, 0.88f, 0.88f, 1f);
                    }
                    else if (r > 52f)
                    {
                        c = new Color(0.05f, 0.01f, 0.01f, 1f);
                    }
                    else
                    {
                        // Concentric rings + radial spokes
                        float angle = Mathf.Atan2(p.y, p.x);
                        bool ringLine = Mathf.Abs(r - 33f) < 1.6f || Mathf.Abs(r - 43f) < 1.6f;
                        bool spoke = Mathf.Abs(Mathf.Sin(angle * 8f)) > 0.95f;
                        if (ringLine || spoke)
                            c = new Color(0.12f, 0.02f, 0.02f, 1f);
                        else
                            c = new Color(0.88f * scan, 0.10f * scan, 0.10f * scan, 1f);
                    }
                    pixels[y * w + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void GenerateGlassCrackTexture(string path)
        {
            int w = 512, h = 320;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            Vector2 impact = new Vector2(340f, 210f);
            Vector2[] rays =
            {
                new Vector2(210f, 310f),
                new Vector2(495f, 305f),
                new Vector2(505f, 195f),
                new Vector2(490f, 25f),
                new Vector2(270f, 10f),
                new Vector2(20f, 35f),
                new Vector2(15f, 285f)
            };

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float minDist = 999f;
                    for (int i = 0; i < rays.Length; i++)
                    {
                        float d = DistToSegment(p, impact, rays[i]);
                        if (d < minDist) minDist = d;
                    }
                    float r = Vector2.Distance(p, impact);
                    float ring1 = Mathf.Abs(r - 28f);
                    float ring2 = Mathf.Abs(r - 58f);
                    minDist = Mathf.Min(minDist, Mathf.Min(ring1, ring2));

                    if (minDist < 1.6f)
                        pixels[y * w + x] = new Color(1f, 0.92f, 0.94f, 0.95f);
                    else if (minDist < 3.4f)
                        pixels[y * w + x] = new Color(0.94f, 0.25f, 0.25f, 0.65f);
                    else
                        pixels[y * w + x] = Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, Vector2.Dot(ab, ab)));
            return Vector2.Distance(p, a + ab * t);
        }

        private static void ConfigureSpriteImporters()
        {
            string[] spriteFiles =
            {
                TexDir + "/BG_Desk.png",
                TexDir + "/StampBrushBox.png",
                TexDir + "/ScanlineMask.png",
                TexDir + "/PlayTriangle.png",
                TexDir + "/Vignette.png",
                TexDir + "/GhostEye.png",
                TexDir + "/ShrineSilhouette.png",
                TexDir + "/RetroFolderGreen.png",
                TexDir + "/PhoneBtnAccept.png",
                TexDir + "/PhoneBtnDecline.png",
                TexDir + "/HorrorAlmondEye.png",
                TexDir + "/HorrorEyePupil.png",
                TexDir + "/GlassCrackOverlay.png"
            };

            foreach (var p in spriteFiles)
            {
                var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                if (imp != null && imp.textureType != TextureImporterType.Sprite)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
            }
        }

        private static void GenerateMaterial()
        {
            string matPath = MatDir + "/MM_CRTStatic.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var shader = Shader.Find("SecurityCenter/MM_CRTStatic");
            if (shader == null) return;

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }
            mat.SetFloat("_Intensity", 0.08f);
            mat.SetFloat("_ScanlineStrength", 0.22f);
            mat.SetFloat("_RollSpeed", 0.16f);
            mat.SetFloat("_NoiseScale", 420f);
            EditorUtility.SetDirty(mat);
        }

        // =====================================================================
        // 3. PROCEDURAL WAV AUDIO CLIPS (Section 6 of Spec)
        // =====================================================================
        private static void GenerateAudioClips()
        {
            var rng = new System.Random(9001);

            // 1. amb_crt_hum.wav (2.5s loop: 60Hz hum + gentle static)
            WriteWavIfMissing(AudioDir + "/amb_crt_hum.wav", 2.5f, (t, dur) =>
            {
                float hum = Mathf.Sin(t * 2f * Mathf.PI * 60f) * 0.16f + Mathf.Sin(t * 2f * Mathf.PI * 120f) * 0.08f;
                float hiss = ((float)rng.NextDouble() * 2f - 1f) * 0.05f;
                return hum + hiss;
            });

            // 2. music_menu.wav (6.0s eerie dark drone loop)
            WriteWavIfMissing(AudioDir + "/music_menu.wav", 6.0f, (t, dur) =>
            {
                float drone = Mathf.Sin(t * 2f * Mathf.PI * 55f) * 0.22f
                            + Mathf.Sin(t * 2f * Mathf.PI * 82.4f) * 0.12f
                            + Mathf.Sin(t * 2f * Mathf.PI * 116.5f) * 0.08f * Mathf.Sin(t * 0.5f * Mathf.PI);
                return drone;
            });

            // 3. crt_on.wav
            WriteWavIfMissing(AudioDir + "/crt_on.wav", 0.35f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                float freq = Mathf.Lerp(95f, 1800f, t / dur);
                float hiss = ((float)rng.NextDouble() * 2f - 1f) * 0.25f * env;
                return (Mathf.Sin(t * 2f * Mathf.PI * freq) * 0.35f + hiss) * env;
            });

            // 4. crt_off.wav
            WriteWavIfMissing(AudioDir + "/crt_off.wav", 0.32f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                float freq = Mathf.Lerp(2200f, 55f, t / dur);
                return Mathf.Sin(t * 2f * Mathf.PI * freq) * 0.45f * env;
            });

            // 5. ui_hover_tick.wav
            WriteWavIfMissing(AudioDir + "/ui_hover_tick.wav", 0.045f, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 2f);
                return Mathf.Sin(t * 2f * Mathf.PI * 1350f) * 0.35f * env;
            });

            // 6. ui_select.wav
            WriteWavIfMissing(AudioDir + "/ui_select.wav", 0.14f, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 1.6f);
                float thud = Mathf.Sin(t * 2f * Mathf.PI * 190f) * 0.45f;
                float click = ((float)rng.NextDouble() * 2f - 1f) * (t < 0.025f ? 0.4f : 0.05f);
                return (thud + click) * env;
            });

            // 7. ui_back.wav
            WriteWavIfMissing(AudioDir + "/ui_back.wav", 0.12f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                float freq = Mathf.Lerp(540f, 240f, t / dur);
                return Mathf.Sin(t * 2f * Mathf.PI * freq) * 0.35f * env;
            });

            // 8. ui_deny.wav
            WriteWavIfMissing(AudioDir + "/ui_deny.wav", 0.18f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                return Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 110f)) * 0.28f * env;
            });

            // 9. ui_value_tick.wav
            WriteWavIfMissing(AudioDir + "/ui_value_tick.wav", 0.038f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                return Mathf.Sin(t * 2f * Mathf.PI * 980f) * 0.32f * env;
            });

            // 10. typewriter_key.wav
            WriteWavIfMissing(AudioDir + "/typewriter_key.wav", 0.032f, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 2.2f);
                float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.38f;
                float ping = Mathf.Sin(t * 2f * Mathf.PI * 1650f) * 0.2f;
                return (noise + ping) * env;
            });

            // 11. typewriter_ding.wav
            WriteWavIfMissing(AudioDir + "/typewriter_ding.wav", 0.28f, (t, dur) =>
            {
                float env = Mathf.Pow(1f - (t / dur), 1.8f);
                return Mathf.Sin(t * 2f * Mathf.PI * 2480f) * 0.32f * env;
            });

            // 12. glitch_burst.wav
            WriteWavIfMissing(AudioDir + "/glitch_burst.wav", 0.18f, (t, dur) =>
            {
                float env = 1f - (t / dur);
                float n = ((float)rng.NextDouble() * 2f - 1f) * 0.42f;
                float buzz = Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 75f)) * 0.22f;
                return (n + buzz) * env;
            });

            // 13. phone_ring_far.wav
            WriteWavIfMissing(AudioDir + "/phone_ring_far.wav", 1.1f, (t, dur) =>
            {
                bool activeRing = (t < 0.4f) || (t > 0.58f && t < 0.98f);
                if (!activeRing) return 0f;
                float mod = Mathf.Sin(t * 2f * Mathf.PI * 20f) > 0f ? 1f : 0.4f;
                return (Mathf.Sin(t * 2f * Mathf.PI * 440f) + Mathf.Sin(t * 2f * Mathf.PI * 480f)) * 0.12f * mod;
            });
        }

        private static void WriteWavIfMissing(string assetPath, float durationSec, Func<float, float, float> sampleFunc)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (File.Exists(fullPath)) return;

            int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(durationSec * sampleRate));
            short[] pcm = new short[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float s = Mathf.Clamp(sampleFunc(t, durationSec), -1f, 1f);
                pcm[i] = (short)(s * 32760f);
            }

            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = sampleRate * 2;
                int dataSize = sampleCount * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + dataSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1); // PCM
                bw.Write((short)1); // Mono
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);
                for (int i = 0; i < sampleCount; i++)
                    bw.Write(pcm[i]);
            }
        }
    }
}
#endif
