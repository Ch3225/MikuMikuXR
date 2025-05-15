using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Managers;

namespace MMDVR.Test
{
    public class TestCase1 : MonoBehaviour
    {
        // TMP目录在Assets同级
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
            // 用项目根目录拼接TMP下的路径
            EventManager.OnModelLoadRequest?.Invoke(System.IO.Path.Combine(projectRoot, lenModel));
            EventManager.OnModelLoadRequest?.Invoke(System.IO.Path.Combine(projectRoot, rinModel));
            // 协程等待模型加载完毕后再加载动作、相机、音乐
            StartCoroutine(LoadAfterModel());
        }

        IEnumerator LoadAfterModel()
        {
            // 等待ActorManager中有两个actor（即两个模型都加载完毕）
            while (ActorManager.Instance == null || ActorManager.Instance.actors.Count < 2)
                yield return null;

            // 给两个模型分别加载动作
            var actor0 = ActorManager.Instance.actors[0];
            var actor1 = ActorManager.Instance.actors[1];
            MotionManager.Instance.LoadAndAssignMotion(System.IO.Path.Combine(projectRoot, motion1), actor0);
            MotionManager.Instance.LoadAndAssignMotion(System.IO.Path.Combine(projectRoot, motion2), actor1);

            // 加载相机VMD
            MMDCameraManager.Instance?.AddVmdCamera(System.IO.Path.Combine(projectRoot, cameraVmd));

            // 加载音乐
            MusicManager.Instance?.LoadMusic(System.IO.Path.Combine(projectRoot, music));
        }
    }
}
