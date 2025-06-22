using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase4 : MonoBehaviour
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
            Debug.Log("=== TestCase4 资源加载开始 ===");
            
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null)
            {
                Debug.Log("等待UserActionManager初始化...");
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("UserActionManager已就绪");

            // 加载Len和Rin模型（不分配动作）
            string lenPath = System.IO.Path.Combine(projectRoot, lenModel);
            string rinPath = System.IO.Path.Combine(projectRoot, rinModel);
            
            string lenModelId = null;
            string rinModelId = null;
            
            // 加载Len模型
            if (System.IO.File.Exists(lenPath))
            {
                Debug.Log("开始加载Len模型...");
                bool completed = false;
                UserActionManager.Instance.LoadAndShowModel(lenPath, (modelId) =>
                {
                    lenModelId = modelId;
                    completed = true;
                });
                yield return new WaitUntil(() => completed);
                Debug.Log($"✅ Len模型加载完成: {lenModelId}");
            }
            else
            {
                Debug.LogWarning($"Len模型文件不存在: {lenPath}");
            }

            yield return new WaitForSeconds(0.5f);

            // 加载Rin模型
            if (System.IO.File.Exists(rinPath))
            {
                Debug.Log("开始加载Rin模型...");
                bool completed = false;
                UserActionManager.Instance.LoadAndShowModel(rinPath, (modelId) =>
                {
                    rinModelId = modelId;
                    completed = true;
                });
                yield return new WaitUntil(() => completed);
                Debug.Log($"✅ Rin模型加载完成: {rinModelId}");
            }
            else
            {
                Debug.LogWarning($"Rin模型文件不存在: {rinPath}");
            }

            yield return new WaitForSeconds(0.5f);

            // 加载动作（但不分配给演员）
            Debug.Log("开始加载动作...");
            string motion1Path = System.IO.Path.Combine(projectRoot, motion1);
            string motion2Path = System.IO.Path.Combine(projectRoot, motion2);
            
            if (System.IO.File.Exists(motion1Path))
            {
                bool motionLoaded = false;
                UserActionManager.Instance.LoadMotion(motion1Path, (motionId) => 
                {
                    motionLoaded = true;
                });
                yield return new WaitUntil(() => motionLoaded);
                Debug.Log($"✅ 动作1加载完成: {motion1Path}");
            }
            else
            {
                Debug.LogWarning($"动作1文件不存在: {motion1Path}");
            }

            if (System.IO.File.Exists(motion2Path))
            {
                bool motionLoaded = false;
                UserActionManager.Instance.LoadMotion(motion2Path, (motionId) => 
                {
                    motionLoaded = true;
                });
                yield return new WaitUntil(() => motionLoaded);
                Debug.Log($"✅ 动作2加载完成: {motion2Path}");
            }
            else
            {
                Debug.LogWarning($"动作2文件不存在: {motion2Path}");
            }

            yield return new WaitForSeconds(0.5f);

            Debug.Log("TestCase4: 动作已加载但未分配给任何演员。");

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

            Debug.Log("=== TestCase4 资源加载完成 ===");
        }
    }
}
