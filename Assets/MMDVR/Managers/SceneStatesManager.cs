using System.Collections.Generic;
using UnityEngine;

namespace MMDVR.Managers
{
    [System.Serializable]
    public class ModelMotionPair
    {
        [Tooltip("模型对象")] public GameObject model;
        [Tooltip("该模型对应的动作列表")] public List<GameObject> motions;
    }

    public class SceneStatesManager : MonoBehaviour
    {
        public static SceneStatesManager Instance { get; private set; }

        [Header("播放状态")]
        [Tooltip("是否正在播放")] public bool isPlaying;
        [Tooltip("当前播放进度（秒）")] [Range(0, 9999)] public float playTime;

        [Header("模型与动作映射")] 
        [Tooltip("模型与动作的一对多关系")] 
        public List<ModelMotionPair> modelMotionMap = new List<ModelMotionPair>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        // 模型-动作映射API
        public void AddModelMotion(GameObject model, GameObject motion)
        {
            var pair = modelMotionMap.Find(p => p.model == model);
            if (pair == null)
            {
                pair = new ModelMotionPair { model = model, motions = new List<GameObject>() };
                modelMotionMap.Add(pair);
            }
            if (!pair.motions.Contains(motion))
                pair.motions.Add(motion);
        }
        public List<GameObject> GetMotionsForModel(GameObject model)
        {
            var pair = modelMotionMap.Find(p => p.model == model);
            return pair != null ? pair.motions : null;
        }

        // 统一控制：播放
        public void Play()
        {
            isPlaying = true;
            // 控制所有模型、相机、音乐
            foreach (var actor in ActorManager.Instance.actors)
            {
                var mmd = actor.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmd != null) mmd.Playing = true;
            }
            MMDCameraManager.Instance?.Play();
            MusicManager.Instance?.Play(MusicManager.Instance.currentIndex >= 0 ? MusicManager.Instance.currentIndex : 0);
        }
        // 统一控制：暂停
        public void Pause()
        {
            isPlaying = false;
            foreach (var actor in ActorManager.Instance.actors)
            {
                var mmd = actor.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmd != null) mmd.Playing = false;
            }
            MMDCameraManager.Instance?.Pause();
            MusicManager.Instance?.Pause();
        }
        // 统一控制：定位到某一时间点
        public void SeekTo(float time)
        {
            playTime = time;
            // 音乐
            var audio = MusicManager.Instance?.musics.Count > 0 ? MusicManager.Instance.musics[0] : null;
            if (audio != null && audio.clip != null) audio.time = time;
            // 所有模型
            foreach (var actor in ActorManager.Instance.actors)
            {
                var mmd = actor.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmd != null)
                {
                    mmd.SetMotionPos(time);
                }
            }
            // 当前激活MMD相机
            MMDCameraManager.Instance?.SetTime(time);
        }
    }
}
