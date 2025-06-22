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
        private string music = "TMP/MMDTest/Motions/Dive to Blue/内田彩 - Dive to Blue 调整.wav";        void Start()
        {
            projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            // 只通过UserActionManager进行操作
            StartCoroutine(LoadResources());
        }        IEnumerator LoadResources()
        {
            Debug.Log("=== TestCase1 资源加载开始 ===");
            
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null)
            {
                Debug.Log("等待UserActionManager初始化...");
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("UserActionManager已就绪");

            // 加载模型和动作
            string lenPath = System.IO.Path.Combine(projectRoot, lenModel);
            string rinPath = System.IO.Path.Combine(projectRoot, rinModel);
            string motion1Path = System.IO.Path.Combine(projectRoot, motion1);
            string motion2Path = System.IO.Path.Combine(projectRoot, motion2);
            
            string lenModelId = null;
            string rinModelId = null;
            string motionId1 = null;
            string motionId2 = null;
              // 加载Len模型和动作
            if (System.IO.File.Exists(lenPath) && System.IO.File.Exists(motion1Path))
            {
                Debug.Log("开始加载Len模型和动作...");
                bool completed = false;
                UserActionManager.Instance.LoadModelAndMotion(lenPath, motion1Path, (modelId, motionId) =>
                {
                    lenModelId = modelId;
                    motionId1 = motionId;
                    completed = true;
                });
                yield return new WaitUntil(() => completed);
                Debug.Log($"✅ Len模型和动作加载完成: {lenModelId}, {motionId1}");
                
                // 显式关联Motion到Model
                if (!string.IsNullOrEmpty(lenModelId) && !string.IsNullOrEmpty(motionId1))
                {
                    Debug.Log("🔗 关联Len的动作...");
                    bool associationCompleted = false;
                    UserActionManager.Instance.AssignMotionToModel(lenModelId, motionId1, () =>
                    {
                        associationCompleted = true;
                    });
                    yield return new WaitUntil(() => associationCompleted);
                    Debug.Log($"✅ Len动作关联完成: {lenModelId} <-> {motionId1}");
                }
            }
            else
            {
                Debug.LogWarning($"Len模型或动作文件不存在: {lenPath}, {motion1Path}");
            }

            yield return new WaitForSeconds(0.5f);            // 加载Rin模型和动作
            if (System.IO.File.Exists(rinPath) && System.IO.File.Exists(motion2Path))
            {
                Debug.Log("开始加载Rin模型和动作...");
                bool completed = false;
                UserActionManager.Instance.LoadModelAndMotion(rinPath, motion2Path, (modelId, motionId) =>
                {
                    rinModelId = modelId;
                    motionId2 = motionId;
                    completed = true;
                });
                yield return new WaitUntil(() => completed);
                Debug.Log($"✅ Rin模型和动作加载完成: {rinModelId}, {motionId2}");
                
                // 显式关联Motion到Model
                if (!string.IsNullOrEmpty(rinModelId) && !string.IsNullOrEmpty(motionId2))
                {
                    Debug.Log("🔗 关联Rin的动作...");
                    bool associationCompleted = false;
                    UserActionManager.Instance.AssignMotionToModel(rinModelId, motionId2, () =>
                    {
                        associationCompleted = true;
                    });
                    yield return new WaitUntil(() => associationCompleted);
                    Debug.Log($"✅ Rin动作关联完成: {rinModelId} <-> {motionId2}");
                }
            }
            else
            {
                Debug.LogWarning($"Rin模型或动作文件不存在: {rinPath}, {motion2Path}");
            }

            yield return new WaitForSeconds(0.5f);

            // 加载相机VMD
            Debug.Log("开始加载摄像机...");
            string cameraPath = System.IO.Path.Combine(projectRoot, cameraVmd);
            if (System.IO.File.Exists(cameraPath))
            {
                bool cameraLoaded = false;
                UserActionManager.Instance.LoadVMDCamera(cameraPath, (cameraId) => 
                {
                    cameraLoaded = true;
                });
                yield return new WaitUntil(() => cameraLoaded);
                Debug.Log($"✅ 摄像机加载完成: {cameraPath}");
            }
            else
            {
                Debug.LogWarning($"摄像机文件不存在: {cameraPath}");
            }

            yield return new WaitForSeconds(0.5f);

            // 加载音乐
            Debug.Log("开始加载音乐...");
            string musicPath = System.IO.Path.Combine(projectRoot, music);
            if (System.IO.File.Exists(musicPath))
            {
                bool musicLoaded = false;
                UserActionManager.Instance.LoadMusic(musicPath, (musicId) => 
                {
                    musicLoaded = true;
                });
                yield return new WaitUntil(() => musicLoaded);
                Debug.Log($"✅ 音乐加载完成: {musicPath}");
            }
            else
            {
                Debug.LogWarning($"音乐文件不存在: {musicPath}");
            }

            Debug.Log("=== TestCase1 资源加载完成 ===");
        }
    }
}
