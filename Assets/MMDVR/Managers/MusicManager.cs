using System.Collections.Generic;
using System.Linq; // Added for LINQ operations like FirstOrDefault
using UnityEngine;
using System.IO; // Added for Path operations

namespace MMDVR.Managers
{
    // Internal class to store detailed information about each music track
    public class MusicTrackInfo // Made public so MusicListController can potentially use it if needed, or adapt from it.
    {
        public string ID { get; set; }
        public string FilePath { get; set; }
        public string DisplayName { get; set; }
        public AudioSource AudioSource { get; set; }
        public GameObject MusicGameObject { get; set; }
        public float Length { get; set; } // Store length after loading
    }

    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        // Changed from List<AudioSource> musics to List<MusicTrackInfo> musicTracks
        private List<MusicTrackInfo> musicTracks = new List<MusicTrackInfo>();
        public Transform musicsRoot; //挂载所有音乐的父节点
        public int currentIndex = -1; // Index in the musicTracks list

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            // Ensure musicsRoot is assigned or found
            if (musicsRoot == null)
            {
                var musicsGo = GameObject.Find("Musics");
                if (musicsGo != null)
                {
                    musicsRoot = musicsGo.transform;
                }
                else
                {
                    // Create Musics node if it doesn't exist
                    var newMusicsGo = new GameObject("Musics");
                    musicsRoot = newMusicsGo.transform;
                    // Optionally parent it to this manager's GameObject or keep it at root
                    // newMusicsGo.transform.SetParent(this.transform); 
                }
            }
        }

        // Returns the ID of the loaded/existing music track
        public string LoadMusic(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("MusicManager: LoadMusic called with empty path.");
                return null;
            }

            // Check if music from this path already exists
            MusicTrackInfo existingTrack = musicTracks.FirstOrDefault(t => t.FilePath == path);
            if (existingTrack != null)
            {
                Debug.LogWarning($"MusicManager: Music from path '{path}' already loaded with ID '{existingTrack.ID}'.");
                return existingTrack.ID;
            }

            if (musicsRoot == null)
            {
                Debug.LogError("MusicManager: musicsRoot is not set. Cannot load music.");
                return null;
            }
            
            string trackId = System.Guid.NewGuid().ToString();
            string displayName = Path.GetFileNameWithoutExtension(path);

            GameObject go = new GameObject(displayName);
            go.transform.SetParent(musicsRoot, false);
            AudioSource audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var newTrack = new MusicTrackInfo
            {
                ID = trackId,
                FilePath = path,
                DisplayName = displayName,
                AudioSource = audioSource,
                MusicGameObject = go,
                Length = 0 // Will be updated after loading
            };

            musicTracks.Add(newTrack);
            StartCoroutine(LoadAudioClip(path, newTrack));
            
            Debug.Log($"MusicManager: Started loading '{displayName}' with ID '{trackId}'.");
            return trackId;
        }

        private System.Collections.IEnumerator LoadAudioClip(string path, MusicTrackInfo trackInfo)
        {
            var url = "file:///" + path.Replace("\\\\", "/");
            using (var www = new WWW(url)) // WWW is obsolete, consider UnityWebRequestMultimedia.GetAudioClip for newer Unity versions
            {
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    trackInfo.AudioSource.clip = www.GetAudioClip(false, false); // false, false for 2D, non-streaming
                    if (trackInfo.AudioSource.clip != null)
                    {
                        trackInfo.Length = trackInfo.AudioSource.clip.length;
                        Debug.Log($"MusicManager: Successfully loaded audio for '{trackInfo.DisplayName}'. Length: {trackInfo.Length}s");
                    }
                    else
                    {
                        Debug.LogError($"MusicManager: Failed to get AudioClip from WWW for '{trackInfo.DisplayName}'.");
                    }
                }
                else
                {
                    Debug.LogError($"MusicManager: WWW Error loading audio '{trackInfo.DisplayName}': {www.error}");
                    // Consider removing the track if loading fails catastrophically
                    // musicTracks.Remove(trackInfo);
                    // Destroy(trackInfo.MusicGameObject);
                }
            }
        }

        public List<MusicTrackInfo> GetLoadedMusicTrackInfos()
        {
            return new List<MusicTrackInfo>(musicTracks); // Return a copy
        }

        public MusicTrackInfo GetMusicTrackInfoById(string id)
        {
            return musicTracks.FirstOrDefault(t => t.ID == id);
        }
        
        public void PlayMusicById(string id)
        {
            int trackIndex = musicTracks.FindIndex(t => t.ID == id);
            if (trackIndex != -1)
            {
                PlayMusicByIndex(trackIndex);
            }
            else
            {
                Debug.LogWarning($"MusicManager: Track with ID '{id}' not found. Cannot play.");
            }
        }

        public void PlayMusicByIndex(int index)
        {
            if (index < 0 || index >= musicTracks.Count)
            {
                Debug.LogWarning($"MusicManager: PlayMusicByIndex called with invalid index {index}. Total tracks: {musicTracks.Count}");
                return;
            }

            // Stop currently playing music (if any and different)
            if (currentIndex != -1 && currentIndex < musicTracks.Count && currentIndex != index)
            {
                musicTracks[currentIndex].AudioSource.Pause(); // Using Pause, could be Stop
            }
            
            if (musicTracks[index].AudioSource.clip == null)
            {
                Debug.LogWarning($"MusicManager: AudioClip for '{musicTracks[index].DisplayName}' is not loaded yet. Cannot play.");
                // Optionally, try to reload or wait
                return;
            }

            musicTracks[index].AudioSource.Play();
            currentIndex = index;
            Debug.Log($"MusicManager: Playing '{musicTracks[index].DisplayName}' (Index: {currentIndex}).");
        }

        public void Pause()
        {
            if (currentIndex >= 0 && currentIndex < musicTracks.Count && musicTracks[currentIndex].AudioSource.isPlaying)
            {
                musicTracks[currentIndex].AudioSource.Pause();
                Debug.Log($"MusicManager: Paused '{musicTracks[currentIndex].DisplayName}'.");
            }
        }
        
        public void StopAllMusic()
        {
            foreach (var track in musicTracks)
            {
                if (track.AudioSource != null)
                {
                    track.AudioSource.Stop();
                }
            }
            currentIndex = -1; // Reset current index as nothing is playing
            Debug.Log("MusicManager: All music stopped.");
        }

        public void UninstallMusic(string id)
        {
            MusicTrackInfo trackToRemove = musicTracks.FirstOrDefault(t => t.ID == id);
            if (trackToRemove != null)
            {
                int trackIndex = musicTracks.IndexOf(trackToRemove);
                if (trackToRemove.AudioSource != null)
                {
                    trackToRemove.AudioSource.Stop();
                }
                if (trackToRemove.MusicGameObject != null)
                {
                    Destroy(trackToRemove.MusicGameObject);
                }
                musicTracks.Remove(trackToRemove);
                Debug.Log($"MusicManager: Uninstalled music '{trackToRemove.DisplayName}' (ID: {id}).");

                if (currentIndex == trackIndex)
                {
                    currentIndex = -1; // Reset if the uninstalled track was the current one
                }
                else if (currentIndex > trackIndex)
                {
                    currentIndex--; // Adjust current index if it was after the removed track
                }
            }
            else
            {
                Debug.LogWarning($"MusicManager: Could not find music with ID '{id}' to uninstall.");
            }
        }

        public void SetTime(float time)
        {
            if (currentIndex >= 0 && currentIndex < musicTracks.Count && musicTracks[currentIndex].AudioSource.clip != null)
            {
                musicTracks[currentIndex].AudioSource.time = Mathf.Clamp(time, 0, musicTracks[currentIndex].Length);
            }
        }

        public void SetVolume(float volume)
        {
            // Set volume for all tracks, or just the current one?
            // Current implementation sets for all, which might be desired for a global volume control.
            foreach (var track in musicTracks)
            {
                if (track.AudioSource != null)
                {
                    track.AudioSource.volume = Mathf.Clamp01(volume);
                }
            }
        }

        public float GetCurrentTime()
        {
            if (currentIndex >= 0 && currentIndex < musicTracks.Count && musicTracks[currentIndex].AudioSource.clip != null)
            {
                return musicTracks[currentIndex].AudioSource.time;
            }
            return 0f;
        }

        public float GetCurrentLength()
        {
            if (currentIndex >= 0 && currentIndex < musicTracks.Count && musicTracks[currentIndex].AudioSource.clip != null)
            {
                return musicTracks[currentIndex].Length; // Use stored length
            }
            return 0f;
        }
        
        public MusicTrackInfo GetCurrentTrackInfo()
        {
            if (currentIndex >= 0 && currentIndex < musicTracks.Count)
            {
                return musicTracks[currentIndex];
            }
            return null;
        }
    }
}