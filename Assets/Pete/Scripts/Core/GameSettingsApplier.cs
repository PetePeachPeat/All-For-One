using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Pete.Interaction;

namespace Pete.Core
{
    /// <summary>
    /// อ่านค่าการตั้งค่าจากหน้า Settings (PlayerPrefs MM_*) และนำมาปรับใช้ในซีนเกมหลักจริงๆ
    /// </summary>
    public class GameSettingsApplier : MonoBehaviour
    {
        [Header("Visual Overlays in Game Scene")]
        [SerializeField] private Image brightnessOverlayImage;
        [SerializeField] private List<Image> crtScanlineImages = new List<Image>();

        private void Awake()
        {
            ApplyAllSettings();
        }

        private void Start()
        {
            ApplyAllSettings();
        }

        public void ApplyAllSettings()
        {
            // 1. Display Mode & Resolution
            int fsModeIdx = PlayerPrefs.GetInt("MM_FullscreenMode", 0);
            FullScreenMode fsMode = FullScreenMode.FullScreenWindow;
            if (fsModeIdx == 0) fsMode = FullScreenMode.ExclusiveFullScreen;
            else if (fsModeIdx == 1) fsMode = FullScreenMode.Windowed;
            else fsMode = FullScreenMode.FullScreenWindow;

            int resIdx = PlayerPrefs.GetInt("MM_ResIndex", 0);
            int targetW = 1920;
            int targetH = 1080;
            if (resIdx == 1) { targetW = 1600; targetH = 900; }
            else if (resIdx == 2) { targetW = 1280; targetH = 720; }

            Screen.fullScreenMode = fsMode;
            Screen.SetResolution(targetW, targetH, fsMode);

            // 2. V-Sync & FPS Limit
            int vsync = PlayerPrefs.GetInt("MM_VSync", 1);
            QualitySettings.vSyncCount = (vsync == 1) ? 1 : 0;

            int fpsIdx = PlayerPrefs.GetInt("MM_FpsLimit", 1);
            int targetFps = 60;
            if (fpsIdx == 0) targetFps = 30;
            else if (fpsIdx == 1) targetFps = 60;
            else if (fpsIdx == 2) targetFps = 120;
            else targetFps = -1;
            Application.targetFrameRate = targetFps;

            // 3. Master Volume
            float masterVol = Mathf.Clamp01(PlayerPrefs.GetInt("MM_VolMaster", 80) / 100f);
            AudioListener.volume = masterVol;

            // 4. Brightness Overlay (0..100, default 50)
            int bright = PlayerPrefs.GetInt("MM_Brightness", 50);
            if (brightnessOverlayImage != null)
            {
                if (bright <= 50)
                {
                    float darkAlpha = Mathf.Lerp(0.68f, 0f, bright / 50f);
                    brightnessOverlayImage.color = new Color(0f, 0f, 0f, darkAlpha);
                }
                else
                {
                    float liftAlpha = Mathf.Lerp(0f, 0.22f, (bright - 50f) / 50f);
                    brightnessOverlayImage.color = new Color(0.95f, 0.97f, 1f, liftAlpha);
                }
            }

            // 5. CRT Scanline Intensity on Monitor & Phone UI (0 = OFF, 1 = LOW, 2 = HIGH)
            int crtMode = PlayerPrefs.GetInt("MM_CRTFx", PlayerPrefs.GetInt("MM_CrtEffect", 2));
            float scanAlpha = crtMode == 0 ? 0f : (crtMode == 1 ? 0.14f : 0.32f);
            for (int i = 0; i < crtScanlineImages.Count; i++)
            {
                if (crtScanlineImages[i] != null)
                {
                    Color c = crtScanlineImages[i].color;
                    c.a = scanAlpha;
                    crtScanlineImages[i].color = c;
                }
            }

            // 6. Camera Controls (A/D Sensitivity, Smoothing, Right-Click Back)
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ApplySavedControlSettings();
            }
        }
    }
}
