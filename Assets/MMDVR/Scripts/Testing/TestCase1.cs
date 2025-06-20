using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
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
        }        IEnumerator LoadAfterModel()
        {            // 等待SceneStatesManager初始化（替代ActorManager）
            while (SceneStatesManager.Instance == null)
                yield return null;

            // 加载演员（模型实例）
            Debug.Log("开始加载演员...");
            
            string lenPath = System.IO.Path.Combine(projectRoot, lenModel);
            string rinPath = System.IO.Path.Combine(projectRoot, rinModel);
            
            if (System.IO.File.Exists(lenPath))
            {
                SceneStatesManager.Instance.AddActor(lenPath);
                Debug.Log($"Len演员已添加: {lenPath}");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning($"Len模型文件不存在: {lenPath}");
                // 使用测试方法创建占位符
                SceneStatesManager.Instance.AddActorForTesting("len_test", "TDA Len (Test)");
            }
            
            if (System.IO.File.Exists(rinPath))
            {
                SceneStatesManager.Instance.AddActor(rinPath);
                Debug.Log($"Rin演员已添加: {rinPath}");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning($"Rin模型文件不存在: {rinPath}");
                // 使用测试方法创建占位符
                SceneStatesManager.Instance.AddActorForTesting("rin_test", "TDA Rin (Test)");
            }

            // 加载动作
            Debug.Log("开始加载动作...");
            
            string motion1Path = System.IO.Path.Combine(projectRoot, motion1);
            string motion2Path = System.IO.Path.Combine(projectRoot, motion2);
            
            string motionId1 = null;
            string motionId2 = null;
            
            if (System.IO.File.Exists(motion1Path))
            {
                motionId1 = SceneStatesManager.Instance.AddMotion(motion1Path);
                Debug.Log($"动作1已添加: {motion1Path}");
            }
            else
            {
                Debug.LogWarning($"动作1文件不存在: {motion1Path}");
                motionId1 = "motion1_test";
                SceneStatesManager.Instance.AddMotionForTesting(motionId1, "Dive to Blue (Len)");
            }
            
            if (System.IO.File.Exists(motion2Path))
            {
                motionId2 = SceneStatesManager.Instance.AddMotion(motion2Path);
                Debug.Log($"动作2已添加: {motion2Path}");
            }
            else
            {
                Debug.LogWarning($"动作2文件不存在: {motion2Path}");
                motionId2 = "motion2_test";
                SceneStatesManager.Instance.AddMotionForTesting(motionId2, "Dive to Blue (Rin)");
            }

            yield return new WaitForSeconds(1f);

            // 分配动作给演员
            Debug.Log("开始分配动作...");
            var actorList = SceneStatesManager.Instance.GetActorList();
            
            if (actorList.Count >= 2 && motionId1 != null && motionId2 != null)
            {
                // 将第一个动作分配给第一个演员
                SceneStatesManager.Instance.AssignMotionToActor(motionId1, actorList[0].id);
                Debug.Log($"动作1已分配给演员: {actorList[0].displayName}");
                
                yield return new WaitForSeconds(0.5f);
                
                // 将第二个动作分配给第二个演员
                SceneStatesManager.Instance.AssignMotionToActor(motionId2, actorList[1].id);
                Debug.Log($"动作2已分配给演员: {actorList[1].displayName}");
            }
            else
            {
                Debug.LogWarning($"演员或动作数量不足。Actor: {actorList.Count}, Motion1: {motionId1 != null}, Motion2: {motionId2 != null}");
            }

            yield return new WaitForSeconds(1f);

            // 加载相机VMD
            Debug.Log("开始加载摄像机...");
            string cameraPath = System.IO.Path.Combine(projectRoot, cameraVmd);
            if (System.IO.File.Exists(cameraPath))
            {
                SceneStatesManager.Instance.AddVMDCamera(cameraPath);
                Debug.Log($"摄像机已添加: {cameraPath}");
            }
            else
            {
                Debug.LogWarning($"摄像机文件不存在: {cameraPath}");
            }

            // 加载音乐
            Debug.Log("开始加载音乐...");
            string musicPath = System.IO.Path.Combine(projectRoot, music);
            if (System.IO.File.Exists(musicPath))
            {
                SceneStatesManager.Instance.AddMusic(musicPath);
                Debug.Log($"音乐已添加: {musicPath}");
            }
            else
            {
                Debug.LogWarning($"音乐文件不存在: {musicPath}");
            }

            Debug.Log("=== TestCase1 加载完成 ===");
        }
    }
}
