using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    /// <summary>
    /// TestCase3: 相机切换测试 - 只通过UserActionManager
    /// </summary>
    public class TestCase3 : MonoBehaviour
    {
        private string projectRoot;
        private string model = "TMP/MMDTest/Models/mmd___halloween_miku___dl_by_mlekoduszek-dccfunl/ミクハロウィーン.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        private string cameraVmd = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Camera3 by do-mode.vmd";
        private string music = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Deep Blue Town へおいでよ.wav";

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(RunTest());
        }

        IEnumerator RunTest()
        {
            Debug.Log("=== TestCase3: 相机切换测试 ===");
            
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null) yield return null;

            // 加载模型
            string modelId = null;
            bool modelLoaded = false;
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, model), id => { modelId = id; modelLoaded = true; });
            yield return new WaitUntil(() => modelLoaded);
            Debug.Log("✅ 模型加载完成");
            
            // 加载动作
            string motionId = null;
            bool motionLoaded = false;
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, motion), id => { motionId = id; motionLoaded = true; });
            yield return new WaitUntil(() => motionLoaded);
            Debug.Log("✅ 动作加载完成");
            
            // 不做动作与模型的关联

            // 加载相机
            bool camLoaded = false;
            UserActionManager.Instance.LoadVMDCamera(Path.Combine(projectRoot, cameraVmd), id => { camLoaded = true; });
            yield return new WaitUntil(() => camLoaded);
            Debug.Log("✅ VMD相机加载完成");
            
            // 加载音乐
            bool musicLoaded = false;
            UserActionManager.Instance.LoadMusic(Path.Combine(projectRoot, music), id => { musicLoaded = true; });
            yield return new WaitUntil(() => musicLoaded);
            Debug.Log("✅ 音乐加载完成");

            Debug.Log("=== TestCase3 资源加载测试完成 ===");
        }
    }
}
