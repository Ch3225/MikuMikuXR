using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    /// <summary>
    /// TestCase4: 多角色加载测试 - 只通过UserActionManager
    /// </summary>
    public class TestCase4 : MonoBehaviour
    {
        private string projectRoot;
        private string lenModel = "TMP/MMDTest/Models/TDA Len/TDA Len.pmx";
        private string rinModel = "TMP/MMDTest/Models/TDA Rin/TDA Rin.pmx";
        private string cameraVmd = "TMP/MMDTest/Motions/Dive to Blue/Dive to Blue Camera2 Low.vmd";
        private string motion1 = "TMP/MMDTest/Motions/Dive to Blue/DivetoBlue_dance_iMarine_R40_カノン_ボーンフレームと表情フレーム.vmd";
        private string motion2 = "TMP/MMDTest/Motions/Dive to Blue/DivetoBlue_dance_Umiko_R40_アリア_ボーンフレームと表情フレーム.vmd";
        private string music = "TMP/MMDTest/Motions/Dive to Blue/内田彩 - Dive to Blue 调整.wav";

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(RunTest());
        }

        IEnumerator RunTest()
        {
            Debug.Log("=== TestCase4: 多角色加载测试 ===");
            
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null) yield return null;

            // 加载Len模型
            string lenId = null;
            bool lenLoaded = false;
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, lenModel), id => { lenId = id; lenLoaded = true; });
            yield return new WaitUntil(() => lenLoaded);
            Debug.Log("✅ Len角色加载完成");
            
            // 加载Rin模型
            string rinId = null;
            bool rinLoaded = false;
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, rinModel), id => { rinId = id; rinLoaded = true; });
            yield return new WaitUntil(() => rinLoaded);
            Debug.Log("✅ Rin角色加载完成");
            
            // 加载动作1
            string motion1Id = null;
            bool motion1Loaded = false;
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, motion1), id => { motion1Id = id; motion1Loaded = true; });
            yield return new WaitUntil(() => motion1Loaded);
            Debug.Log("✅ 动作1加载完成");
            
            // 加载动作2
            string motion2Id = null;
            bool motion2Loaded = false;
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, motion2), id => { motion2Id = id; motion2Loaded = true; });
            yield return new WaitUntil(() => motion2Loaded);
            Debug.Log("✅ 动作2加载完成");
            
            // 不做动作与模型的关联

            // 加载相机
            bool camLoaded = false;
            UserActionManager.Instance.LoadVMDCamera(Path.Combine(projectRoot, cameraVmd), id => { camLoaded = true; });
            yield return new WaitUntil(() => camLoaded);
            Debug.Log("✅ 相机加载完成");
            
            // 加载音乐
            bool musicLoaded = false;
            UserActionManager.Instance.LoadMusic(Path.Combine(projectRoot, music), id => { musicLoaded = true; });
            yield return new WaitUntil(() => musicLoaded);
            Debug.Log("✅ 音乐加载完成");

            Debug.Log("=== TestCase4 完成 ===");
        }
    }
}
