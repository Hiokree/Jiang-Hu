using BepInEx.Configuration;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

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
        private bool _musicEnabled = true;
        private bool isLoopingCurrentSong = false;

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
                        && !typeName.Contains("changebackground")
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
            LoadMusicConfig();
            if (_musicEnabled)
            {
                StartMusicInternal();
            }
            else
            {
                enabled = false;
            }
        }

        private void LoadMusicConfig()
        {
            try
            {
                string modPath = Path.GetDirectoryName(Application.dataPath);
                string configPath = Path.Combine(modPath, "SPT", "user", "mods", "JiangHu.Server", "config", "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                    if (configDict != null && configDict.ContainsKey("Enable_Music_Player"))
                    {
                        _musicEnabled = configDict["Enable_Music_Player"];
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error loading music config: {ex.Message}");
            }
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;

            if (!enabled)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                isPlaying = false;
            }
            else if (enabled)
            {
                isPlaying = true;
                if (shuffledSongs.Length > 0)
                {
                    if (audioSource != null && !audioSource.isPlaying)
                    {
                        PlayCurrentSong();
                    }
                }
                else
                {
                    ScanAndShuffleSongs();
                    if (shuffledSongs.Length > 0)
                    {
                        PlayCurrentSong();
                    }
                }

                if (musicCoroutine == null)
                {
                    musicCoroutine = StartCoroutine(MusicLoop());
                }
            }
        }

        public bool IsMusicEnabled()
        {
            return _musicEnabled;
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

            var audioFiles = Directory.GetFiles(musicPath, "*.*")
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

        public void PlayCurrentSong()
        {
            if (!_musicEnabled) return;
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

        public void PlayNextSong()
        {
            if (!_musicEnabled) return;
            if (shuffledSongs.Length == 0) return;

            if (isLoopingCurrentSong)
            {
                isLoopingCurrentSong = false;
            }

            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            currentSongIndex = (currentSongIndex + 1) % shuffledSongs.Length;
            PlayCurrentSong();
        }

        public void PlayPreviousSong()
        {
            if (!_musicEnabled) return;
            if (shuffledSongs.Length == 0) return;

            if (isLoopingCurrentSong)
            {
                isLoopingCurrentSong = false;
            }

            if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
            currentSongIndex = (currentSongIndex - 1 + shuffledSongs.Length) % shuffledSongs.Length;
            PlayCurrentSong();
        }

        public void TogglePlayPause()
        {
            if (!_musicEnabled) return; 

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

        public void ToggleLoopCurrentSong()
        {
            isLoopingCurrentSong = !isLoopingCurrentSong;

            if (isLoopingCurrentSong)
            {
                if (audioSource != null)
                {
                    audioSource.loop = false; 
                }
            }
        }

        // Add getter method
        public bool IsLoopingCurrentSong()
        {
            return isLoopingCurrentSong;
        }

        public void SetVolume(float newVolume)
        {
            volume = newVolume;
            if (audioSource != null) audioSource.volume = volume;
        }

        public void SetCurrentSongIndex(int index)
        {
            if (isLoopingCurrentSong)
            {
                isLoopingCurrentSong = false;
            }

            currentSongIndex = index;
            if (_musicEnabled && shuffledSongs.Length > 0)
            {
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
                        if (isLoopingCurrentSong)
                        {
                            PlayCurrentSong();
                        }
                        else
                        {
                            currentSongIndex = (currentSongIndex + 1) % shuffledSongs.Length;
                            PlayCurrentSong();
                        }
                    }
                }
            }
        }


        public float GetVolume()
        {
            return volume;
        }

        public string[] GetShuffledSongs()
        {
            return shuffledSongs;
        }

        public int GetCurrentSongIndex()
        {
            return currentSongIndex;
        }

        public bool IsPlaying()
        {
            return isPlaying && audioSource != null && audioSource.isPlaying;
        }

        public AudioSource GetAudioSource()
        {
            return audioSource;
        }
    }
}