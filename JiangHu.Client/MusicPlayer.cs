using BepInEx.Configuration;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static ScreenshotCreator.CameraObject;

namespace JiangHu
{
    public class MusicPlayer : MonoBehaviour
    {
        private AudioSource audioSource;
        private string[] shuffledSongs = new string[0];
        private Coroutine musicCoroutine;
        private int currentSongIndex = 0;
        private bool isPlaying = false;
        private float volume = 0.2f;
        private ChangeBackground _changeBackground;
        private bool _showBackgroundList = false;
        private Vector2 _scrollPosition;
        private bool _showMusicList = false;
        private Vector2 _musicScrollPosition;

        private ConfigEntry<KeyboardShortcut> _hotkey;
        private ConfigEntry<bool> _showGUI;
        private Rect windowRect = new Rect(20, 20, 280, 250);
        private bool showGUI = false;

        public void SetConfig(ConfigEntry<KeyboardShortcut> hotkey, ConfigEntry<bool> showGUIConfig, ChangeBackground changeBackground)
        {
            _hotkey = hotkey;
            _showGUI = showGUIConfig;
            _changeBackground = changeBackground;
        }

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = volume;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;

            StartCoroutine(StopBGMForever());
        }

        IEnumerator StopBGMForever()
        {
            float duration = 10f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                var allAudioSources = FindObjectsOfType<AudioSource>();
                foreach (var source in allAudioSources)
                {
                    if (source != audioSource && source.isPlaying && source.clip != null && source.clip.length > 15f)
                    {
                        source.Stop();
                    }
                }

                var allBehaviours = FindObjectsOfType<MonoBehaviour>();
                foreach (var behaviour in allBehaviours)
                {
                    string typeName = behaviour.GetType().Name.ToLower();
                    if ((typeName.Contains("music") || typeName.Contains("ambient") || typeName.Contains("bgm") || typeName.Contains("background"))
                        && behaviour.enabled)
                    {
                        behaviour.enabled = false;
                    }
                }

                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
        }

        void Start()
        {
            StartMusicInternal();
        }

        void Update()
        {
            if (_hotkey.Value.IsDown())
            {
                showGUI = !showGUI;
                _showGUI.Value = showGUI;
            }
        }

        private void StartMusicInternal()
        {
            ScanAndShuffleSongs();
            if (shuffledSongs.Length == 0) return;
            isPlaying = true;
            currentSongIndex = 0;
            PlayCurrentSong();
            musicCoroutine = StartCoroutine(MusicLoop());
        }

        private void ScanAndShuffleSongs()
        {
            string musicPath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "JiangHu.Client", "music");
            if (!Directory.Exists(musicPath)) Directory.CreateDirectory(musicPath);

            var audioFiles = Directory.GetFiles(musicPath, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    string ext = Path.GetExtension(f).ToLower();
                    return ext == ".mp3" || ext == ".ogg" || ext == ".wav";
                })
                .ToArray();

            if (audioFiles.Length > 0)
            {
                shuffledSongs = new string[audioFiles.Length];
                System.Array.Copy(audioFiles, shuffledSongs, audioFiles.Length);
                FisherYatesShuffle(shuffledSongs);
            }
        }

        private void FisherYatesShuffle(string[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                string temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        private void PlayCurrentSong()
        {
            if (shuffledSongs.Length == 0) return;
            string currentSong = shuffledSongs[currentSongIndex];
            StartCoroutine(LoadAndPlaySong(currentSong));
        }

        IEnumerator LoadAndPlaySong(string path)
        {
            string url = "file:///" + path.Replace("\\", "/");
            AudioType audioType = GetAudioType(path);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null && audioSource != null)
                    {
                        audioSource.clip = clip;
                        audioSource.Play();
                        isPlaying = true;
                    }
                }
            }
        }

        private AudioType GetAudioType(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext switch
            {
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".wav" => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
        }

        private void PlayNextSong()
        {
            if (shuffledSongs.Length == 0) return;
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            currentSongIndex = (currentSongIndex + 1) % shuffledSongs.Length;
            PlayCurrentSong();
        }

        private void PlayPreviousSong()
        {
            if (shuffledSongs.Length == 0) return;
            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            currentSongIndex = (currentSongIndex - 1 + shuffledSongs.Length) % shuffledSongs.Length;
            PlayCurrentSong();
        }

        private void TogglePlayPause()
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
                isPlaying = false;
            }
            else if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
                isPlaying = true;
            }
            else if (shuffledSongs.Length > 0)
            {
                isPlaying = true;
                PlayCurrentSong();
            }
        }

        IEnumerator MusicLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);

                if (isPlaying && shuffledSongs.Length > 0 && audioSource != null && !audioSource.isPlaying)
                {
                    yield return new WaitForSeconds(3f);

                    if (isPlaying && shuffledSongs.Length > 0)
                    {
                        currentSongIndex = (currentSongIndex + 1) % shuffledSongs.Length;
                        PlayCurrentSong();
                    }
                }
            }
        }

        public void SetVolume(float newVolume)
        {
            volume = newVolume;
            if (audioSource != null) audioSource.volume = volume;
        }

        void OnGUI()
        {
            if (!_showGUI.Value) return;
            windowRect = GUI.Window(12345, windowRect, DrawMusicPlayerWindow, "Jiang Hu World Shaper");

            if (_showBackgroundList)
            {
                Rect bgListRect = new Rect(windowRect.x + windowRect.width + 10, windowRect.y, 200, 300);
                bgListRect = GUI.Window(12346, bgListRect, DrawBackgroundListWindow, "Backgrounds");
            }

            if (_showMusicList)
            {
                Rect musicListRect = new Rect(windowRect.x - 250, windowRect.y, 240, 300);
                musicListRect = GUI.Window(12347, musicListRect, DrawMusicListWindow, "Music Files");
            }
        }

        void DrawMusicPlayerWindow(int windowID)
        {
            // Close button
            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                showGUI = false;
                _showGUI.Value = false;
                return;
            }

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 25, 20));

            GUILayout.BeginVertical();

            // Music Player Section
            GUILayout.Label("Now Playing:", GUIStyle.none);
            GUILayout.Space(5);
            if (shuffledSongs.Length > 0 && currentSongIndex < shuffledSongs.Length)
            {
                string currentSong = shuffledSongs[currentSongIndex];
                string songName = Path.GetFileNameWithoutExtension(currentSong);
                GUILayout.Label(songName);
            }
            else
            {
                GUILayout.Label("No songs");
            }

            GUILayout.Space(10);

            // Volume slider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vol:", GUILayout.Width(30));
            float newVolume = GUILayout.HorizontalSlider(volume, 0f, 1f, GUILayout.Width(120));
            if (Mathf.Abs(newVolume - volume) > 0.01f) SetVolume(newVolume);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Music controls
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("◀◀", GUILayout.Height(25), GUILayout.Width(50))) PlayPreviousSong();
            GUILayout.FlexibleSpace();

            string playPauseText = (audioSource != null && audioSource.isPlaying) ? "■" : "▶";
            if (GUILayout.Button(playPauseText, GUILayout.Height(25), GUILayout.Width(50))) TogglePlayPause();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("▶▶", GUILayout.Height(25), GUILayout.Width(50))) PlayNextSong();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Music Selection Section
            GUILayout.Space(10);
            if (shuffledSongs.Length > 0 && GUILayout.Button(" ◄ Select Music ", GUILayout.Height(20)))
            {
                _showMusicList = !_showMusicList;
            }

            // Background Section
            GUILayout.Space(10);
            if (_changeBackground != null)
            {
                bool newBackgroundEnabled = GUILayout.Toggle(_changeBackground.GetBackgroundEnabled(), " Enable Background");
                if (newBackgroundEnabled != _changeBackground.GetBackgroundEnabled())
                {
                    _changeBackground.SetBackgroundEnabled(newBackgroundEnabled);
                }

                var backgrounds = _changeBackground.GetAvailableBackgrounds();
                if (backgrounds.Count > 0 && GUILayout.Button(" Select Background ► ", GUILayout.Height(20)))
                {
                    _showBackgroundList = !_showBackgroundList;
                }
            }

            GUILayout.EndVertical();
        }
        void DrawBackgroundListWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 200, 25)); 

            var backgrounds = _changeBackground.GetAvailableBackgrounds();

            GUILayout.BeginArea(new Rect(10, 30, 180, 270)); 
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            foreach (var background in backgrounds)
            {
                if (GUILayout.Button(background, GUILayout.Height(30)))
                {
                    _changeBackground.SetBackground(background);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        void DrawMusicListWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 240, 25));

            GUILayout.BeginArea(new Rect(10, 30, 220, 270));
            _musicScrollPosition = GUILayout.BeginScrollView(_musicScrollPosition, GUILayout.Width(220), GUILayout.Height(270));

            foreach (var song in shuffledSongs)
            {
                string songName = Path.GetFileNameWithoutExtension(song);
                if (GUILayout.Button(songName, GUILayout.Height(30)))
                {
                    for (int i = 0; i < shuffledSongs.Length; i++)
                    {
                        if (shuffledSongs[i] == song)
                        {
                            currentSongIndex = i;
                            PlayCurrentSong();
                            break;
                        }
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}