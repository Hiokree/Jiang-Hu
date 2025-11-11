using BepInEx.Configuration;
using EFT.UI;
using EFT.UI.Screens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace JiangHu
{
    public class ChangeBackground : MonoBehaviour
    {
        private Texture2D _backgroundTexture;
        private GameObject _backgroundCanvas;
        private RawImage _bgImage;
        private List<string> _availableBackgrounds = new List<string>();
        private string _currentBackground;
        private bool _backgroundEnabled = true;
        private string _selectedBackgroundName;
        private ConfigEntry<bool> _backgroundEnabledConfig;

        public void Init()
        {
            ScanBackgroundFiles();

            _selectedBackgroundName = LoadSavedBackgroundName();
            if (string.IsNullOrEmpty(_selectedBackgroundName) && _availableBackgrounds.Count > 0)
            {
                _selectedBackgroundName = _availableBackgrounds[0];
            }

            _backgroundEnabled = _backgroundEnabledConfig?.Value ?? true;

            if (!LoadBackgroundTexture(_selectedBackgroundName)) return;
            Invoke(nameof(CreateBackgroundSystem), 2f);
        }

        private void CreateBackgroundSystem()
        {
            if (IsInHideout()) return;

            _backgroundCanvas = new GameObject("JiangHu_BackgroundCanvas");
            Canvas canvas = _backgroundCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1000;

            CanvasScaler scaler = _backgroundCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Screen.width, Screen.height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            GameObject bgObject = new GameObject("BackgroundImage");
            bgObject.transform.SetParent(_backgroundCanvas.transform, false);
            RectTransform rect = bgObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _bgImage = bgObject.AddComponent<RawImage>();
            _bgImage.texture = _backgroundTexture;
            _bgImage.color = Color.white;
            _bgImage.raycastTarget = false;

            CanvasGroup canvasGroup = bgObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            _backgroundCanvas.layer = 2;
            bgObject.layer = 2;
            DontDestroyOnLoad(_backgroundCanvas);
        }

        public void SetBackgroundEnabled(bool enabled)
        {
            if (_backgroundEnabledConfig != null)
                _backgroundEnabledConfig.Value = enabled;

            _backgroundEnabled = enabled;
            if (_backgroundCanvas != null)
            {
                _backgroundCanvas.SetActive(enabled && ShouldBackgroundBeActive());
            }
        }

        private bool ShouldBackgroundBeActive()
        {
            return !IsInHideout() && !IsInRaid() && !IsInWeaponModding();
        }

        public bool GetBackgroundEnabled()
        {
            return _backgroundEnabled;
        }
        private void Update()
        {
            if (_backgroundCanvas == null) return;

            if (!_backgroundEnabled)
            {
                if (_backgroundCanvas.activeSelf)
                    _backgroundCanvas.SetActive(false);
                return;
            }

            bool shouldBeActive = ShouldBackgroundBeActive();
            if (_backgroundCanvas.activeSelf != shouldBeActive)
            {
                _backgroundCanvas.SetActive(shouldBeActive);
            }
        }

        private bool IsInHideout()
        {
            return CurrentScreenSingletonClass.Instance?.RootScreenType == EEftScreenType.Hideout;
        }

        private bool IsInRaid()
        {
            return CurrentScreenSingletonClass.Instance?.RootScreenType == EEftScreenType.FinalCountdown ||
                   GClass2340.InRaid;
        }

        private bool IsInWeaponModding()
        {
            var commonUI = CommonUI.Instance;
            if (commonUI == null) return false;

            return (commonUI.EditBuildScreen != null && commonUI.EditBuildScreen.isActiveAndEnabled);
        }

        private void ScanBackgroundFiles()
        {
            string backgroundPath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "JiangHu.Client", "background");
            if (!Directory.Exists(backgroundPath)) return;

            var imageFiles = Directory.GetFiles(backgroundPath, "*.jpg")
                                     .Concat(Directory.GetFiles(backgroundPath, "*.png"))
                                     .Concat(Directory.GetFiles(backgroundPath, "*.jpeg"))
                                     .ToArray();

            foreach (var file in imageFiles)
            {
                _availableBackgrounds.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private bool LoadBackgroundTexture(string backgroundName)
        {
            string backgroundPath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "JiangHu.Client", "background");
            if (!Directory.Exists(backgroundPath)) return false;

            var imageFiles = Directory.GetFiles(backgroundPath, $"{backgroundName}.*")
                                     .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg"))
                                     .ToArray();

            if (imageFiles.Length == 0) return false;

            try
            {
                byte[] bytes = File.ReadAllBytes(imageFiles[0]);
                Texture2D newTexture = new Texture2D(2, 2);

                if (UnityEngine.ImageConversion.LoadImage(newTexture, bytes))
                {
                    if (_backgroundTexture != null)
                        Destroy(_backgroundTexture);

                    _backgroundTexture = newTexture;
                    _currentBackground = backgroundName;

                    if (_bgImage != null)
                        _bgImage.texture = _backgroundTexture;

                    return true;
                }
            }
            catch (System.Exception)
            {
                return false;
            }
            return false;
        }

        public List<string> GetAvailableBackgrounds()
        {
            return _availableBackgrounds;
        }

        public void SetBackground(string backgroundName)
        {
            if (LoadBackgroundTexture(backgroundName))
            {
                _selectedBackgroundName = backgroundName;
                SaveBackgroundName(backgroundName);
            }
        }

        private void SaveBackgroundName(string backgroundName)
        {
            PlayerPrefs.SetString("JiangHu_SelectedBackground", backgroundName);
            PlayerPrefs.Save();
        }

        private string LoadSavedBackgroundName()
        {
            return PlayerPrefs.GetString("JiangHu_SelectedBackground", "");
        }

        public string GetCurrentBackground()
        {
            return _currentBackground;
        }
        public void SetConfig(ConfigEntry<bool> config)
        {
            _backgroundEnabledConfig = config;
        }

        void OnDestroy()
        {
            if (_backgroundTexture != null) Destroy(_backgroundTexture);
            if (_backgroundCanvas != null) Destroy(_backgroundCanvas);
        }
    }
}