using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase1 : MonoBehaviour
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
            projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(RunTest());
        }

        IEnumerator RunTest()
        {
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null) yield return null;

            // 加载模型
            string lenId = null, rinId = null;
            bool lenLoaded = false, rinLoaded = false;
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, lenModel), id => { lenId = id; lenLoaded = true; });
            yield return new WaitUntil(() => lenLoaded);
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, rinModel), id => { rinId = id; rinLoaded = true; });
            yield return new WaitUntil(() => rinLoaded);

            // 加载动作
            string motion1Id = null, motion2Id = null;
            bool motion1Loaded = false, motion2Loaded = false;
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, motion1), id => { motion1Id = id; motion1Loaded = true; });
            yield return new WaitUntil(() => motion1Loaded);
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, motion2), id => { motion2Id = id; motion2Loaded = true; });
            yield return new WaitUntil(() => motion2Loaded);

            // 加载相机
            bool camLoaded = false;
            UserActionManager.Instance.LoadVMDCamera(Path.Combine(projectRoot, cameraVmd), id => { camLoaded = true; });
            yield return new WaitUntil(() => camLoaded);

            // 加载音乐
            bool musicLoaded = false;
            UserActionManager.Instance.LoadMusic(Path.Combine(projectRoot, music), id => { musicLoaded = true; });
            yield return new WaitUntil(() => musicLoaded);

            // 关联动作到模型
            bool link1 = false, link2 = false;
            UserActionManager.Instance.AssignMotionToModel(lenId, motion1Id, () => { link1 = true; });
            yield return new WaitUntil(() => link1);
            UserActionManager.Instance.AssignMotionToModel(rinId, motion2Id, () => { link2 = true; });
            yield return new WaitUntil(() => link2);

            Debug.Log("=== TestCase1 加载完成 ===");
        }
    }
}
