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
        {            // 等待SceneStatesManager初始化（替代ActorManager）
            while (SceneStatesManager.Instance == null)
                yield return null;

            // TODO: 添加演员和动作加载逻辑
            // SceneStatesManager.Instance?.AddActor(System.IO.Path.Combine(projectRoot, lenModel));
            // SceneStatesManager.Instance?.AddActor(System.IO.Path.Combine(projectRoot, rinModel));
            // SceneStatesManager.Instance?.AddMotion(System.IO.Path.Combine(projectRoot, motion1));
            // SceneStatesManager.Instance?.AddMotion(System.IO.Path.Combine(projectRoot, motion2));
            
            Debug.LogWarning("TestCase1: Actor and Motion loading not implemented in new architecture yet");// 加载相机VMD
            SceneStatesManager.Instance?.AddVMDCamera(System.IO.Path.Combine(projectRoot, cameraVmd));

            // 加载音乐
            SceneStatesManager.Instance?.AddMusic(System.IO.Path.Combine(projectRoot, music));
        }
    }
}
