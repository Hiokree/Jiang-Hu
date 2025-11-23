using BepInEx.Configuration;
using EFT.UI;
using EFT.UI.Screens;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
        private VideoPlayer _videoPlayer;
        private RenderTexture _currentRenderTexture;
        private bool _isVideoPrepared = false;

        public void Init()
        {
            LoadBackgroundConfig();
            ScanBackgroundFiles();

            if (_backgroundEnabled)
            {
                _selectedBackgroundName = LoadSavedBackgroundName();
                if (string.IsNullOrEmpty(_selectedBackgroundName) && _availableBackgrounds.Count > 0)
                {
                    _selectedBackgroundName = _availableBackgrounds[0];
                }

                LoadBackgroundTexture(_selectedBackgroundName);
                Invoke(nameof(CreateBackgroundSystem), 2f);
            }
            else
            {
                enabled = false;
            }
        }

        private void LoadBackgroundConfig()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                    if (configDict != null && configDict.ContainsKey("Enable_Change_Background"))
                    {
                        _backgroundEnabled = configDict["Enable_Change_Background"];
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading background config: {ex.Message}");
            }
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

            if (_backgroundTexture != null)
            {
                _bgImage.texture = _backgroundTexture;
            }
            else
            {
                Texture2D blackTexture = new Texture2D(1, 1);
                blackTexture.SetPixel(0, 0, Color.black);
                blackTexture.Apply();
                _bgImage.texture = blackTexture;
            }
            _bgImage.uvRect = new Rect(0, 0, 1, 1);
            _bgImage.color = Color.white;
            _bgImage.raycastTarget = false;

            string backgroundPath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "JiangHu.Client", "background");
            var mp4Files = Directory.GetFiles(backgroundPath, $"{_selectedBackgroundName}.mp4");
            bool isMp4 = mp4Files.Length > 0;

            if (_backgroundTexture == null && !string.IsNullOrEmpty(_selectedBackgroundName) && isMp4)
            {
                string filePath = Path.Combine(backgroundPath, _selectedBackgroundName + ".mp4");
                LoadVideoBackground(filePath, _selectedBackgroundName);
            }

            CanvasGroup canvasGroup = bgObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            _backgroundCanvas.layer = 2;
            bgObject.layer = 2;
            DontDestroyOnLoad(_backgroundCanvas);
        }

        public void SetBackgroundEnabled(bool enabled)
        {
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

            if (_bgImage != null && _currentRenderTexture != null && _bgImage.texture != _currentRenderTexture)
            {
                if (_bgImage.texture.width == 1 && _bgImage.texture.height == 1)
                {
                    Destroy(_bgImage.texture);
                }
                _bgImage.texture = _currentRenderTexture;
                _bgImage.uvRect = new Rect(0, 0, 1, 1);
            }

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
                                     .Concat(Directory.GetFiles(backgroundPath, "*.mp4"))
                                     .Concat(Directory.GetFiles(backgroundPath, "*.webm"))
                                     .Concat(Directory.GetFiles(backgroundPath, "*.avi"))
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
                                     .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".jpeg") || f.EndsWith(".mp4") || f.EndsWith(".webm") || f.EndsWith(".avi"))
                                     .ToArray();

            if (imageFiles.Length == 0) return false;

            string filePath = imageFiles[0];
            string extension = Path.GetExtension(filePath).ToLower();

            if (extension != ".mp4")
            {
                if (_videoPlayer != null)
                {
                    _videoPlayer.Stop();
                    Destroy(_videoPlayer);
                    _videoPlayer = null;
                }
                if (_currentRenderTexture != null)
                {
                    Destroy(_currentRenderTexture);
                    _currentRenderTexture = null;
                }
            }

            if (extension == ".mp4")
            {
                return LoadVideoBackground(filePath, backgroundName);
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(imageFiles[0]);
                Texture2D newTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                newTexture.LoadImage(bytes);
                newTexture.Apply(true);
                newTexture.filterMode = FilterMode.Trilinear;
                newTexture.anisoLevel = 9;
                newTexture.wrapMode = TextureWrapMode.Clamp;

                if (_backgroundTexture != null)
                    Destroy(_backgroundTexture);

                _backgroundTexture = newTexture;
                _currentBackground = backgroundName;

                if (_bgImage != null)
                {
                    _bgImage.texture = _backgroundTexture;
                    _bgImage.uvRect = new Rect(0, 0, 1, 1);
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private bool LoadVideoBackground(string filePath, string backgroundName)
        {
            if (_bgImage == null)
            {
                _currentBackground = backgroundName;
                return true;
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                Destroy(_videoPlayer);
                _videoPlayer = null;
            }

            _currentRenderTexture = new RenderTexture(Screen.width, Screen.height, 24)
            {
                filterMode = FilterMode.Trilinear,
                anisoLevel = 9
            };

            _videoPlayer = _bgImage.gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.url = "file://" + filePath;
            _videoPlayer.isLooping = true;
            _videoPlayer.playOnAwake = true;
            _videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.EnableAudioTrack(0, false);
            _videoPlayer.SetDirectAudioMute(0, true);

            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            _isVideoPrepared = false;
            _videoPlayer.prepareCompleted += (source) =>
            {
                if (_isVideoPrepared) return;
                _isVideoPrepared = true;

                _currentRenderTexture = new RenderTexture(Screen.width, Screen.height, 24)
                {
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 9
                };

                _currentRenderTexture.Create();
                _videoPlayer.targetTexture = _currentRenderTexture;

                if (_bgImage != null)
                {
                    _bgImage.texture = _currentRenderTexture;
                    _bgImage.uvRect = new Rect(0, 0, 1, 1);
                }

                _currentBackground = backgroundName;
                _videoPlayer.Play();
            };

            _videoPlayer.Prepare();
            return true;
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

        void OnDestroy()
        {
            if (_backgroundTexture != null) Destroy(_backgroundTexture);
            if (_backgroundCanvas != null) Destroy(_backgroundCanvas);
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                Destroy(_videoPlayer);
            }
            if (_currentRenderTexture != null) Destroy(_currentRenderTexture);
        }
    }
}