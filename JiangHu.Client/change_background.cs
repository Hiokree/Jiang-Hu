using EFT;
using EFT.UI;
using EFT.UI.Matchmaker;
using EFT.UI.Screens;
using HarmonyLib;
using Newtonsoft.Json;
using System;
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
        private static ChangeBackground _instance;
        private Harmony _harmony;
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
        private bool _videoSoundEnabled = true;
        private float _videoVolume = 0.5f;
        private MusicPlayer _musicPlayer;


        public void Init()
        {
            _instance = this;
            LoadBackgroundConfig();
            ScanBackgroundFiles();

            _harmony = new Harmony("com.jianghu.splash");

            var awakeMethod = AccessTools.Method(typeof(SplashScreenPanel), "Awake");
            if (awakeMethod != null)
            {
                _harmony.Patch(awakeMethod,
                    postfix: new HarmonyMethod(typeof(ChangeBackground), nameof(OnSplashScreenPanelAwake_Postfix)));
            }

            var method1 = AccessTools.Method(typeof(SplashScreenPanel), "method_1",
                new[] { typeof(CanvasGroup), typeof(Action) });
            if (method1 != null)
            {
                _harmony.Patch(method1,
                    prefix: new HarmonyMethod(typeof(ChangeBackground), nameof(OnSplashScreenPanelMethod1_Prefix)));
            }

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
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json); // Change to object
                    if (configDict != null)
                    {
                        if (configDict.ContainsKey("Enable_Change_Background") && configDict["Enable_Change_Background"] is bool)
                            _backgroundEnabled = (bool)configDict["Enable_Change_Background"];

                        if (configDict.ContainsKey("Enable_Video_Sound") && configDict["Enable_Video_Sound"] is bool)
                            _videoSoundEnabled = (bool)configDict["Enable_Video_Sound"];

                        if (configDict.ContainsKey("Video_Volume"))
                        {
                            if (configDict["Video_Volume"] is long)
                                _videoVolume = (long)configDict["Video_Volume"] / 100f;
                            else if (configDict["Video_Volume"] is double)
                                _videoVolume = (float)(double)configDict["Video_Volume"] / 100f;
                        }

                        if (configDict.ContainsKey("Enable_Video_Sound") && configDict["Enable_Video_Sound"] is bool)
                            _videoSoundEnabled = (bool)configDict["Enable_Video_Sound"];

                        // Sync music volume based on video sound setting on load
                        if (_musicPlayer != null)
                        {
                            if (_videoSoundEnabled)
                            {
                                _musicPlayer.SetVolume(0f);
                            }
                            else
                            {
                                _musicPlayer.SetVolume(0.5f);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
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

        private static void OnSplashScreenPanelAwake_Postfix(SplashScreenPanel __instance)
        {
            if (_instance == null || !_instance._backgroundEnabled) return;
            _instance.ReplaceSplashScreenImages(__instance);
        }

        private static bool OnSplashScreenPanelMethod1_Prefix(SplashScreenPanel __instance, CanvasGroup canvasGroup, Action callback)
        {
            if (_instance == null || !_instance._backgroundEnabled) return true;

            var imageCanvasGroupField = AccessTools.Field(typeof(SplashScreenPanel), "_imageCanvasGroup");
            if (imageCanvasGroupField == null) return true;

            var imageCanvasGroup = imageCanvasGroupField.GetValue(__instance) as CanvasGroup;
            if (imageCanvasGroup == null || canvasGroup != imageCanvasGroup) return true;

            _instance.ReplaceSplashScreenImages(__instance);
            return true;
        }

        private void ReplaceSplashScreenImages(SplashScreenPanel splashPanel)
        {
            try
            {
                var imagesField = AccessTools.Field(typeof(SplashScreenPanel), "_images");
                if (imagesField == null) return;

                var splashImages = imagesField.GetValue(splashPanel) as Image[];
                if (splashImages == null || splashImages.Length == 0) return;

                foreach (var image in splashImages)
                {
                    if (image == null) continue;

                    var splashObject = image.gameObject;
                    if (splashObject.GetComponent<RawImage>() != null) continue;

                    var originalRect = image.rectTransform;
                    DestroyImmediate(image);

                    var rawImage = splashObject.AddComponent<RawImage>();
                    rawImage.rectTransform.anchorMin = originalRect.anchorMin;
                    rawImage.rectTransform.anchorMax = originalRect.anchorMax;
                    rawImage.rectTransform.offsetMin = originalRect.offsetMin;
                    rawImage.rectTransform.offsetMax = originalRect.offsetMax;

                    if (!string.IsNullOrEmpty(_selectedBackgroundName))
                    {
                        ApplyCurrentBackgroundToRawImage(rawImage, splashObject);
                    }
                }
            }
            catch (Exception) { }
        }

        private void ApplyCurrentBackgroundToRawImage(RawImage rawImage, GameObject targetObject = null)
        {
            if (string.IsNullOrEmpty(_selectedBackgroundName)) return;

            string backgroundPath = Path.Combine(Directory.GetCurrentDirectory(),
                "BepInEx", "plugins", "JiangHu.Client", "background");

            if (!Directory.Exists(backgroundPath)) return;

            var files = Directory.GetFiles(backgroundPath, $"{_selectedBackgroundName}.*")
                .Where(f => {
                    string ext = Path.GetExtension(f).ToLower();
                    return ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                           ext == ".mp4" || ext == ".webm" || ext == ".avi";
                }).ToArray();

            if (files.Length == 0) return;

            string filePath = files[0];
            string extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".mp4" || extension == ".webm" || extension == ".avi")
            {
                var videoPlayer = targetObject != null ?
                    targetObject.AddComponent<VideoPlayer>() :
                    rawImage.gameObject.AddComponent<VideoPlayer>();

                videoPlayer.url = "file://" + filePath;
                videoPlayer.isLooping = true;
                videoPlayer.playOnAwake = true;
                videoPlayer.renderMode = VideoRenderMode.MaterialOverride;

                if (rawImage.GetComponent<Renderer>() != null)
                {
                    videoPlayer.targetMaterialRenderer = rawImage.GetComponent<Renderer>();
                    videoPlayer.targetMaterialProperty = "_MainTex";
                    var renderTexture = new RenderTexture(Screen.width, Screen.height, 24); 
                    videoPlayer.targetTexture = renderTexture; 
                    rawImage.texture = renderTexture; 
                }
                else
                {
                    var renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
                    videoPlayer.targetTexture = renderTexture;
                    rawImage.texture = renderTexture;
                }

                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                videoPlayer.controlledAudioTrackCount = 1;
                videoPlayer.EnableAudioTrack(0, _videoSoundEnabled);
                videoPlayer.SetDirectAudioVolume(0, _videoSoundEnabled ? _videoVolume : 0f);
            }
            else
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    texture.LoadImage(bytes);
                    texture.Apply(true);
                    texture.filterMode = FilterMode.Trilinear;
                    texture.anisoLevel = 9;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    rawImage.texture = texture;
                    rawImage.uvRect = new Rect(0, 0, 1, 1);
                }
                catch (Exception) { }
            }
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
            var currentScreen = CurrentScreenSingletonClass.Instance?.RootScreenType;
            return currentScreen == EEftScreenType.BattleUI; 
        }

        private bool IsInWeaponModding()
        {
            var commonUI = CommonUI.Instance;
            if (commonUI == null) return false;

            bool isWeaponModding = commonUI.WeaponModdingScreen != null &&
                                  commonUI.WeaponModdingScreen.isActiveAndEnabled;

            bool isEditBuild = commonUI.EditBuildScreen != null &&
                              commonUI.EditBuildScreen.isActiveAndEnabled;

            return isWeaponModding || isEditBuild;
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
            catch (Exception)
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
            _videoPlayer.EnableAudioTrack(0, _videoSoundEnabled);
            _videoPlayer.SetDirectAudioVolume(0, _videoSoundEnabled ? _videoVolume : 0f);

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

                var activeSplashScreens = FindObjectsOfType<SplashScreenPanel>();
                foreach (var splashScreen in activeSplashScreens)
                {
                    if (splashScreen != null && splashScreen.gameObject.activeInHierarchy)
                    {
                        OnSplashScreenPanelAwake_Postfix(splashScreen);
                    }
                }
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

        public void SetVideoSoundEnabled(bool enabled)
        {
            _videoSoundEnabled = enabled;
            if (_videoPlayer != null)
            {
                _videoPlayer.EnableAudioTrack(0, enabled);
                _videoPlayer.SetDirectAudioVolume(0, enabled ? _videoVolume : 0f);
            }

            if (_musicPlayer != null)
            {
                if (enabled)
                {
                    _musicPlayer.SetVolume(0f);
                }
                else
                {
                    _musicPlayer.SetVolume(0.5f);
                }
            }
        }

        public void SetVideoVolume(float volume)
        {
            _videoVolume = volume;
            if (_videoPlayer != null && _videoSoundEnabled)
            {
                _videoPlayer.SetDirectAudioVolume(0, volume);
            }
        }

        public bool GetVideoSoundEnabled()
        {
            return _videoSoundEnabled;
        }

        public float GetVideoVolume()
        {
            return _videoVolume;
        }

        public void SetMusicPlayer(MusicPlayer musicPlayer)
        {
            _musicPlayer = musicPlayer;
        }


        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _harmony?.UnpatchSelf();

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