using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MMDVR.Managers
{
    public class BottomPlaybackBarManager : MonoBehaviour
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
                    wasPlayingBeforeDrag = SceneStatesManager.Instance.isPlaying;
                    SceneStatesManager.Instance.Pause();
                    isSliderDragging = true;
                    float value = playSlider.value;
                    SceneStatesManager.Instance.SeekTo(value);
                    UpdateTimerText(value);
                });
                eventTrigger.triggers.Add(entryDown);
                var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
                entryUp.callback.AddListener((data) => {
                    isSliderDragging = false;
                    float value = playSlider.value;
                    SceneStatesManager.Instance.SeekTo(value);
                    UpdateTimerText(value);
                    if (wasPlayingBeforeDrag) {
                        SceneStatesManager.Instance.Play();
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
        }

        void Update()
        {
            var musicMgr = MusicManager.Instance;
            // if (musicMgr != null && musicMgr.musics.Count > 0 && musicMgr.currentIndex >= 0)
            var currentTrackInfo = musicMgr?.GetCurrentTrackInfo();
            if (currentTrackInfo != null && currentTrackInfo.AudioSource != null && currentTrackInfo.AudioSource.clip != null)            
            {
                float cur = musicMgr.GetCurrentTime();
                float total = musicMgr.GetCurrentLength();
                if (playSlider != null && total > 0)
                {
                    playSlider.maxValue = total;
                    playSlider.value = cur;
                }
                if (timerText != null)
                {
                    timerText.text = $"{FormatTime(cur)}/{FormatTime(total)}";
                }
            }
            if (!isSliderDragging && SceneStatesManager.Instance.isPlaying)
            {
                float cur = MusicManager.Instance?.GetCurrentTime() ?? 0f;
                playSlider.value = cur;
                UpdateTimerText(cur);
            }
            // 实时刷新音量百分比
            if (volumeSlider != null && volumePercentText != null)
            {
                int percent = Mathf.RoundToInt(volumeSlider.value * 100f);
                volumePercentText.text = "" + percent;
            }
        }

        private void OnPlayButtonClicked()
        {
            var stateMgr = SceneStatesManager.Instance;
            if (stateMgr == null) return;
            if (stateMgr.isPlaying)
                stateMgr.Pause();
            else
                stateMgr.Play();
            if (playButtonText != null)
                playButtonText.text = stateMgr.isPlaying ? "Pause" : "Play";
        }
        private void OnPlaySliderChanged(float value)
        {
            if (isSliderDragging || !SceneStatesManager.Instance.isPlaying)
            {
                SceneStatesManager.Instance.SeekTo(value);
                UpdateTimerText(value);
            }
        }
        private void UpdateTimerText(float time)
        {
            float total = MusicManager.Instance?.GetCurrentLength() ?? 0f;
            if (timerText != null)
                timerText.text = $"{FormatTime(time)}/{FormatTime(total)}";
        }
        private string FormatTime(float t)
        {
            int min = Mathf.FloorToInt(t / 60f);
            int sec = Mathf.FloorToInt(t % 60f);
            return $"{min:00}:{sec:00}";
        }
        private void OnMuteClicked()
        {
            var musicMgr = MusicManager.Instance;
            // if (musicMgr != null && musicMgr.musics.Count > 0)
            // {
            //     var audio = musicMgr.musics[0];
            var currentTrack = musicMgr?.GetCurrentTrackInfo();
            if (currentTrack != null && currentTrack.AudioSource != null)
            {
                var audio = currentTrack.AudioSource;
                audio.mute = !audio.mute;
                if (volumeSlider != null)
                {
                    if (audio.mute)
                        volumeSlider.value = 0f;
                    else
                        volumeSlider.value = audio.volume > 0 ? audio.volume : 1f;
                }
                if (muteButton != null && muteButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() != null)
                {
                    muteButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = audio.mute ? "Unmute" : "Mute";
                }
            }
        }
        private void OnVolumeChanged(float value)
        {
            MusicManager.Instance?.SetVolume(value);
        }
    }
}
