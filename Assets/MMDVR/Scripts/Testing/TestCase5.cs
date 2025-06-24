using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase5 : MonoBehaviour
    {
        private string projectRoot;
        private string testModel = "TMP/MMDTest/Models/TDA Len/TDA Len.pmx";
        private string testMotion = "TMP/MMDTest/Motions/Dive to Blue/DivetoBlue_dance_iMarine_R40.vmd";
        private string testMusic = "TMP/MMDTest/Motions/Dive to Blue/内田彩 - Dive to Blue 调整.wav";

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(RunTest());
        }

        IEnumerator RunTest()
        {
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null) yield return null;

            // 加载模型
            string modelId = null;
            bool modelLoaded = false;
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, testModel), id => { modelId = id; modelLoaded = true; });
            yield return new WaitUntil(() => modelLoaded);

            // 加载动作
            string motionId = null;
            bool motionLoaded = false;
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, testMotion), id => { motionId = id; motionLoaded = true; });
            yield return new WaitUntil(() => motionLoaded);

            // 不做动作与模型的关联

            // 加载音乐
            string musicId = null;
            bool musicLoaded = false;
            UserActionManager.Instance.LoadMusic(Path.Combine(projectRoot, testMusic), id => { musicId = id; musicLoaded = true; });
            yield return new WaitUntil(() => musicLoaded);

            // 播放流程测试
            UserActionManager.Instance.StartPlayback();
            Debug.Log("🎵 开始播放");
            yield return new WaitForSeconds(3f);
            UserActionManager.Instance.PausePlayback();
            Debug.Log("⏸️ 暂停播放");
            yield return new WaitForSeconds(1f);
            UserActionManager.Instance.StartPlayback();
            Debug.Log("▶️ 继续播放");

            Debug.Log("=== TestCase5 完整播放流程测试完成 ===");
        }
    }
}
