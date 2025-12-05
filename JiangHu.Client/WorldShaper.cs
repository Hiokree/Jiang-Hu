using BepInEx.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace JiangHu
{
    public class WorldShaper : MonoBehaviour
    {
        private MusicPlayer _musicPlayer;
        private ChangeBackground _changeBackground;
        private ConfigEntry<KeyboardShortcut> _hotkey;
        private ConfigEntry<bool> _showGUI;

        private Rect windowRect = new Rect(20, 20, 360, 320);
        private bool _showBackgroundList = false;
        private bool _showMusicList = false;
        private Vector2 _musicScrollPosition;
        private Vector2 _backgroundScrollPosition;
        private bool showGUI = false;

        private bool _musicEnabled = true;
        private bool _backgroundEnabled = true;

        public void SetConfig(ConfigEntry<KeyboardShortcut> hotkey, ConfigEntry<bool> showGUIConfig, MusicPlayer musicPlayer, ChangeBackground changeBackground)
        {
            _hotkey = hotkey;
            _showGUI = showGUIConfig;
            _musicPlayer = musicPlayer;
            _changeBackground = changeBackground;
            LoadConfigFromJson();
        }

        void Update()
        {
            if (_hotkey.Value.IsDown())
            {
                showGUI = !showGUI;
                _showGUI.Value = showGUI;
            }
        }

        private void LoadConfigFromJson()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                    if (configDict != null)
                    {
                        if (configDict.ContainsKey("Enable_Music_Player"))
                            _musicEnabled = configDict["Enable_Music_Player"];
                        if (configDict.ContainsKey("Enable_Change_Background"))
                            _backgroundEnabled = configDict["Enable_Change_Background"];
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading WorldShaper config: {ex.Message}");
            }
        }

        private void SaveConfigToJson()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");

                Dictionary<string, bool> configDict = new Dictionary<string, bool>();
                if (File.Exists(configPath))
                {
                    string existingJson = File.ReadAllText(configPath);
                    configDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(existingJson) ?? new Dictionary<string, bool>();
                }

                configDict["Enable_Music_Player"] = _musicEnabled;
                configDict["Enable_Change_Background"] = _backgroundEnabled;

                string json = JsonConvert.SerializeObject(configDict, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error saving WorldShaper config: {ex.Message}");
            }
        }

        void OnGUI()
        {
            if (!_showGUI.Value) return;

            windowRect = GUI.Window(12345, windowRect, DrawWorldShaperWindow, "World Shaper    世界塑造器");

            if (_showBackgroundList)
            {
                float bgX = windowRect.x + windowRect.width + 10;
                float bgY = windowRect.y;

                if (bgX + 360 > Screen.width)
                {
                    bgX = windowRect.x - 360 - 10;
                }

                Rect bgListRect = new Rect(bgX, bgY, 360, 480);
                bgListRect = GUI.Window(12346, bgListRect, DrawBackgroundListWindow, "Backgrounds");
            }

            if (_showMusicList)
            {
                float musicX = windowRect.x - 360 - 10;

                Rect musicListRect = new Rect(musicX, windowRect.y, 360, 480);
                musicListRect = GUI.Window(12347, musicListRect, DrawMusicListWindow, "Music Files");
            }
        }

        void DrawWorldShaperWindow(int windowID)
        {
            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                showGUI = false;
                _showGUI.Value = false;
                return;
            }

            GUILayout.BeginVertical();
            GUILayout.Space(5);

            // Now Playing Box
            GUILayout.BeginVertical("box");
            GUILayout.Label("Now Playing:", GUIStyle.none);
            GUILayout.Space(5);

            var shuffledSongs = _musicPlayer.GetShuffledSongs();
            if (shuffledSongs.Length > 0 && _musicPlayer.GetCurrentSongIndex() < shuffledSongs.Length)
            {
                string currentSong = shuffledSongs[_musicPlayer.GetCurrentSongIndex()];
                string songName = Path.GetFileNameWithoutExtension(currentSong);
                GUILayout.Label(songName);
            }
            else
            {
                GUILayout.Label("No songs");
            }
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Controls Box
            GUILayout.BeginVertical("box");

            // Volume slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vol:", GUILayout.Width(30));
            float newVolume = GUILayout.HorizontalSlider(_musicPlayer.GetVolume(), 0f, 1f, GUILayout.Width(120));
            if (Mathf.Abs(newVolume - _musicPlayer.GetVolume()) > 0.01f)
                _musicPlayer.SetVolume(newVolume);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Music controls 
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("◀◀", GUILayout.Height(25), GUILayout.Width(50)))
                _musicPlayer.PlayPreviousSong(); 
            GUILayout.FlexibleSpace();

            string playPauseText = _musicPlayer.IsPlaying() ? "■" : "▶";
            if (GUILayout.Button(playPauseText, GUILayout.Height(25), GUILayout.Width(50)))
                _musicPlayer.TogglePlayPause();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▶▶", GUILayout.Height(25), GUILayout.Width(50)))
                _musicPlayer.PlayNextSong(); 
            GUILayout.FlexibleSpace();

            GUI.color = _musicPlayer.IsLoopingCurrentSong() ? Color.green : Color.white;
            if (GUILayout.Button("◐", GUILayout.Height(25), GUILayout.Width(50)))
                _musicPlayer.ToggleLoopCurrentSong();
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Music Box 
            GUILayout.BeginVertical("box");
            bool musicToggleState = GUILayout.Toggle(_musicEnabled, " Toggle Music Player On or Off    开关音乐播放器", GUIStyle.none);
            if (musicToggleState != _musicEnabled)
            {
                _musicEnabled = musicToggleState;
                _musicPlayer.SetMusicEnabled(musicToggleState);
                SaveConfigToJson();
            }
            GUILayout.Space(10);

            if (GUILayout.Button(" ◄ Select Music       选择音乐", GUILayout.Height(20)))
            {
                _showMusicList = !_showMusicList; 
            }
            GUILayout.EndVertical();
            GUILayout.Space(10);

            // Background Box 
            GUILayout.BeginVertical("box");
            if (_changeBackground != null)
            {
                bool backgroundToggleState = GUILayout.Toggle(_changeBackground.GetBackgroundEnabled(), " Toggle Background On or Off    开关背景", GUIStyle.none);
                if (backgroundToggleState != _changeBackground.GetBackgroundEnabled())
                {
                    _changeBackground.SetBackgroundEnabled(backgroundToggleState);
                    _backgroundEnabled = backgroundToggleState;
                    SaveConfigToJson();
                }
                GUILayout.Space(10);

                if (GUILayout.Button(" Select Background       选择背景 ► ", GUILayout.Height(20)))
                {
                    if (_changeBackground.GetAvailableBackgrounds().Count > 0)
                    {
                        _showBackgroundList = !_showBackgroundList;
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, windowRect.width, windowRect.height));
        }

        void DrawBackgroundListWindow(int windowID)
        {
            if (GUI.Button(new Rect(360 - 25, 5, 20, 20), "X"))
            {
                _showBackgroundList = false;
                return;
            }

            var backgrounds = _changeBackground.GetAvailableBackgrounds();

            _backgroundScrollPosition = GUILayout.BeginScrollView(_backgroundScrollPosition,
                GUILayout.Width(340), GUILayout.Height(450));

            foreach (var background in backgrounds)
            {
                if (GUILayout.Button(background, GUILayout.Height(30)))
                {
                    _changeBackground.SetBackground(background);
                }
            }

            GUILayout.EndScrollView();
        }

        void DrawMusicListWindow(int windowID)
        {
            if (GUI.Button(new Rect(360 - 25, 5, 20, 20), "X"))
            {
                _showMusicList = false;
                return;
            }

            var shuffledSongs = _musicPlayer.GetShuffledSongs();

            _musicScrollPosition = GUILayout.BeginScrollView(_musicScrollPosition,
                GUILayout.Width(340), GUILayout.Height(450));

            for (int i = 0; i < shuffledSongs.Length; i++)
            {
                string songName = Path.GetFileNameWithoutExtension(shuffledSongs[i]);
                if (GUILayout.Button(songName, GUILayout.Height(30)))
                {
                    _musicPlayer.SetCurrentSongIndex(i);
                }
            }

            GUILayout.EndScrollView();
        }
    }
}