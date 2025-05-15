using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Managers;

namespace MMDVR.Test
{
    public class TestCase2 : MonoBehaviour
    {
        // 资源路径
        private string model = "TMP/MMDTest/Models/Sour miku/Sour miku1.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        private string cameraVmd = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Camera3 by do-mode.vmd";
        private string music = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Deep Blue Town へおいでよ.wav";
        private string projectRoot;

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            // 加载模型
            EventManager.OnModelLoadRequest?.Invoke(Path.Combine(projectRoot, model));
            StartCoroutine(LoadAfterModel());
        }

        IEnumerator LoadAfterModel()
        {
            // 等待模型加载完毕
            while (ActorManager.Instance == null || ActorManager.Instance.actors.Count < 1)
                yield return null;

            var actor = ActorManager.Instance.actors[0];
            // 加载动作
            MotionManager.Instance.LoadAndAssignMotion(Path.Combine(projectRoot, motion), actor);
            // 加载相机
            MMDCameraManager.Instance?.AddVmdCamera(Path.Combine(projectRoot, cameraVmd));
            // 加载音乐
            MusicManager.Instance?.LoadMusic(Path.Combine(projectRoot, music));

            // 统一刷新所有下拉框
            var uiMgr = FindObjectOfType<DesktopUIManager>();
            if (uiMgr != null)
                uiMgr.RefreshAllDropdowns();
        }
    }
}
