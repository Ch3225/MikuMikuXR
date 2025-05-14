using System.Collections.Generic;
using UnityEngine;

namespace MMDVR.Managers
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("场景中所有音乐（Musics）")]
        public List<AudioSource> musics = new List<AudioSource>();
        public Transform musicsRoot; // 挂载所有音乐的父节点
        public int currentIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        public void LoadMusic(string path)
        {
            // 自动查找Musics节点作为musicsRoot
            if (musicsRoot == null)
            {
                var musicsGo = GameObject.Find("Musics");
                if (musicsGo != null)
                    musicsRoot = musicsGo.transform;
            }
            // 只保留Musics节点下唯一AudioSource
            AudioSource audio = null;
            if (musicsRoot != null)
            {
                for (int i = musicsRoot.childCount - 1; i >= 0; i--)
                {
                    GameObject.DestroyImmediate(musicsRoot.GetChild(i).gameObject);
                }
                var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
                go.transform.SetParent(musicsRoot, false);
                audio = go.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                musics.Clear();
                musics.Add(audio);
            }
            else
            {
                var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
                audio = go.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                musics.Clear();
                musics.Add(audio);
            }
            StartCoroutine(LoadAudioClip(path, audio));
        }

        private System.Collections.IEnumerator LoadAudioClip(string path, AudioSource audio)
        {
            var url = "file:///" + path.Replace("\\", "/");
            using (var www = new WWW(url))
            {
                yield return www;
                audio.clip = www.GetAudioClip();
            }
        }

        public void Play(int index)
        {
            if (index < 0 || index >= musics.Count) return;
            for (int i = 0; i < musics.Count; i++)
            {
                if (i == index) musics[i].Play();
                else musics[i].Pause();
            }
            currentIndex = index;
        }
        public void Pause()
        {
            if (currentIndex >= 0 && currentIndex < musics.Count)
                musics[currentIndex].Pause();
        }
        public void SetTime(float time)
        {
            if (currentIndex >= 0 && currentIndex < musics.Count && musics[currentIndex].clip != null)
                musics[currentIndex].time = time;
        }
        public void SetVolume(float volume)
        {
            foreach (var audio in musics)
                audio.volume = volume;
        }
        public float GetCurrentTime()
        {
            if (currentIndex >= 0 && currentIndex < musics.Count && musics[currentIndex].clip != null)
                return musics[currentIndex].time;
            return 0f;
        }
        public float GetCurrentLength()
        {
            if (currentIndex >= 0 && currentIndex < musics.Count && musics[currentIndex].clip != null)
                return musics[currentIndex].clip.length;
            return 0f;
        }
    }
}