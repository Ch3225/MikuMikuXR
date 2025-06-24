using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMDVR.Scripts.Managers;
using LibMMD.Unity3D;

namespace MMDVR.Scripts.UIInteraction
{
    public class PlaybackController : MonoBehaviour
    {
        [Header("播放控制UI组件")]
        public Button playButton;
        public TextMeshProUGUI playButtonText;
        public Slider playSlider;
        public TextMeshProUGUI timerText;
        public Button muteButton;
        public Slider volumeSlider;
        public TextMeshProUGUI volumePercentText;

        private bool isSliderDragging = false;
        private bool wasPlayingBeforeDrag = false;

        void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayButtonClicked);
            if (playSlider != null)
            {
                playSlider.onValueChanged.AddListener(OnPlaySliderChanged);
                var eventTrigger = playSlider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                    eventTrigger = playSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
                entryDown.callback.AddListener((data) => {
                    wasPlayingBeforeDrag = PlaybackManager.Instance.isPlaying;
                    PlaybackManager.Instance.Pause();
                    isSliderDragging = true;
                    float value = playSlider.value;
                    // 拖拽开始时，所有MMD模型进入Playing+None物理模式
                    SetAllMmdGameObjectState(true, MmdGameObject.PhysicsModeEnum.None, value);
                    PlaybackManager.Instance.SeekTo(value);
                    UpdateTimerText(value);
                });
                eventTrigger.triggers.Add(entryDown);
                var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => {
                    isSliderDragging = false;
                    float value = playSlider.value;
                    PlaybackManager.Instance.SeekTo(value);
                    UpdateTimerText(value);
                    // 拖拽结束时，恢复所有MMD模型的播放/暂停和物理模式
                    if (wasPlayingBeforeDrag)
                        SetAllMmdGameObjectState(true, MmdGameObject.PhysicsModeEnum.Bullet, value);
                    else
                        SetAllMmdGameObjectState(false, MmdGameObject.PhysicsModeEnum.Bullet, value);
                    if (wasPlayingBeforeDrag) {
                        PlaybackManager.Instance.Play();
                    }
                });
                eventTrigger.triggers.Add(entryUp);
            }
            if (muteButton != null)
                muteButton.onClick.AddListener(OnMuteClicked);
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (volumeSlider != null)
                volumeSlider.value = 1f;
        }        void Update()
        {
            if (PlaybackManager.Instance != null)
            {
                float currentTime = PlaybackManager.Instance.playTime;
                float totalDuration = PlaybackManager.Instance.totalDuration;
                
                // 实时更新播放按钮文本
                if (playButtonText != null)
                {
                    playButtonText.text = PlaybackManager.Instance.isPlaying ? "Pause" : "Play";
                }
                
                if (playSlider != null && totalDuration > 0)
                {
                    playSlider.maxValue = totalDuration;
                    if (!isSliderDragging)
                    {
                        playSlider.value = currentTime;
                    }
                }
                
                if (timerText != null)
                {
                    timerText.text = $"{FormatTime(currentTime)}/{FormatTime(totalDuration)}";
                }
            }
            
            // 实时刷新音量百分比
            if (volumeSlider != null && volumePercentText != null)
            {
                int percent = Mathf.RoundToInt(volumeSlider.value * 100f);
                volumePercentText.text = "" + percent;
            }
        }private void OnPlayButtonClicked()
        {
            Debug.Log("PlaybackController: Play button clicked");
            
            var playbackMgr = PlaybackManager.Instance;
            if (playbackMgr == null) 
            {
                Debug.LogError("PlaybackController: PlaybackManager.Instance is null");
                return;
            }
            
            try
            {
                if (playbackMgr.isPlaying)
                    playbackMgr.Pause();
                else
                    playbackMgr.Play();
                    
                if (playButtonText != null)
                    playButtonText.text = playbackMgr.isPlaying ? "Pause" : "Play";
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"PlaybackController: Error in OnPlayButtonClicked: {ex.Message}");
                Debug.LogError(ex.StackTrace);
            }
        }private void OnPlaySliderChanged(float value)
        {            if (isSliderDragging || !PlaybackManager.Instance.isPlaying)
            {
                PlaybackManager.Instance.SeekTo(value);
                UpdateTimerText(value);
            }
        }        private void UpdateTimerText(float time)
        {
            float total = PlaybackManager.Instance?.GetMusicDuration() ?? 0f;
            if (timerText != null)
                timerText.text = $"{FormatTime(time)}/{FormatTime(total)}";
        }
        private string FormatTime(float t)
        {
            int min = Mathf.FloorToInt(t / 60f);
            int sec = Mathf.FloorToInt(t % 60f);
            return $"{min:00}:{sec:00}";
        }        private void OnMuteClicked()
        {
            // TODO: 实现静音功能，需要在SceneStatesManager中添加静音状态管理
            Debug.Log("静音功能需要在SceneStatesManager中实现");
        }        private void OnVolumeChanged(float value)        {
            PlaybackManager.Instance?.SetMusicVolume(value);
        }
        private void SetAllMmdGameObjectState(bool playing, MmdGameObject.PhysicsModeEnum physicsMode, float? seekTime = null)
        {
            var sceneDisplayManager = SceneDisplayManager.Instance;
            if (sceneDisplayManager == null || sceneDisplayManager.actorContainer == null) return;
            for (int i = 0; i < sceneDisplayManager.actorContainer.childCount; i++)
            {
                var actor = sceneDisplayManager.actorContainer.GetChild(i);
                var mmd = actor.GetComponent<MmdGameObject>();
                if (mmd != null)
                {
                    mmd.Playing = playing;
                    mmd.PhysicsMode = physicsMode;
                    if (seekTime.HasValue)
                        mmd.SetMotionPos(seekTime.Value);
                }
            }
        }
    }
}
