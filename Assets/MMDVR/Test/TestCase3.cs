using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Managers;

namespace MMDVR.Test
{
    public class TestCase3 : MonoBehaviour
    {
        // 资源路径
        private string model = "TMP/MMDTest/Models/mmd___halloween_miku___dl_by_mlekoduszek-dccfunl/ミクハロウィーン.pmx";
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
        }        IEnumerator LoadAfterModel()
        {
            // 等待SceneStatesManager加载完毕
            while (SceneStatesManager.Instance == null)
                yield return null;// TODO: 实现演员和动作加载
            // SceneStatesManager.Instance?.AddActor(Path.Combine(projectRoot, model));
            // SceneStatesManager.Instance?.AddMotion(Path.Combine(projectRoot, motion));
            Debug.LogWarning("TestCase3: Actor and Motion loading not implemented in new architecture yet");
            
            // 加载相机
            SceneStatesManager.Instance?.AddVMDCamera(Path.Combine(projectRoot, cameraVmd));
            // 加载音乐
            SceneStatesManager.Instance?.AddMusic(Path.Combine(projectRoot, music));

            // 统一刷新所有下拉框
            var uiMgr = FindObjectOfType<DesktopUIManager>();
            if (uiMgr != null)
                uiMgr.RefreshAllDropdowns();
        }
    }
}
