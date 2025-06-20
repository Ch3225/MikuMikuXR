using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase2 : MonoBehaviour
    {        // 资源路径
        private string model = "TMP/MMDTest/Models/Sour miku/Sour Miku1.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        // private string cameraVmd = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Camera3 by do-mode.vmd"; // Removed
        private string cameraDir = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ";
        private string music = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Deep Blue Town へおいでよ.wav";
        private string projectRoot;        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            
            // 使用ResourceManager的全局协程功能启动加载流程
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.StartGlobalCoroutine(LoadResources());
            }
            else
            {
                // 如果ResourceManager还没有准备好，使用传统方式
                StartCoroutine(LoadResources());
            }
        }        IEnumerator LoadResources()
        {
            Debug.Log("=== TestCase2 资源加载开始 ===");
            
            // 等待SceneStatesManager加载完毕
            while (SceneStatesManager.Instance == null)
            {
                Debug.Log("等待SceneStatesManager初始化...");
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("SceneStatesManager已就绪");

            // 分步骤加载，每步之间有视觉反馈
            yield return ResourceManager.Instance.StartGlobalCoroutine(LoadModelAndMotionWithProgress());
            
            yield return ResourceManager.Instance.StartGlobalCoroutine(LoadCamerasWithProgress());
            
            yield return ResourceManager.Instance.StartGlobalCoroutine(LoadMusicWithProgress());

            // 最后刷新UI
            yield return new WaitForEndOfFrame();
            var uiMgr = FindObjectOfType<DesktopUIManager>();
            if (uiMgr != null)
            {
                Debug.Log("刷新所有UI下拉框");
                uiMgr.RefreshAllDropdowns();
            }
            
            Debug.Log("=== TestCase2 资源加载完成 ===");
        }
          /// <summary>
        /// 带进度的模型和动作加载
        /// </summary>
        IEnumerator LoadModelAndMotionWithProgress()
        {
            Debug.Log("📦 开始加载模型和动作...");
            
            string modelPath = Path.Combine(projectRoot, model);
            string motionPath = Path.Combine(projectRoot, motion);

            // 检查文件存在性
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"模型文件不存在: {modelPath}");
                yield break;
            }

            if (!File.Exists(motionPath))
            {
                Debug.LogError($"动作文件不存在: {motionPath}");
                yield break;
            }

            Debug.Log($"✓ 文件检查通过");
            yield return new WaitForSeconds(0.2f); // 让用户看到检查结果
            
            Debug.Log($"📋 开始加载模型: {Path.GetFileName(modelPath)}");
            
            // 通过SceneStatesManager添加演员（模型）
            bool modelLoadSuccess = false;
            string motionId = null;
            
            try
            {
                SceneStatesManager.Instance.AddActor(modelPath);
                modelLoadSuccess = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 加载模型时出错: {e.Message}");
                yield break;
            }
            
            yield return new WaitForSeconds(0.5f); // 等待模型加载
            
            if (modelLoadSuccess)
            {
                Debug.Log($"🎭 开始加载动作: {Path.GetFileName(motionPath)}");
                
                try
                {
                    // 通过SceneStatesManager添加动作
                    motionId = SceneStatesManager.Instance.AddMotion(motionPath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 加载动作时出错: {e.Message}");
                    yield break;
                }
                
                yield return new WaitForSeconds(0.3f); // 等待动作加载
                
                // 获取刚添加的演员并分配动作
                var actorList = SceneStatesManager.Instance.GetActorList();
                if (actorList.Count > 0)
                {                    string actorId = actorList[actorList.Count - 1].id;
                    
                    Debug.Log($"🎪 分配动作给演员: {actorId}");
                    try
                    {
                        SceneStatesManager.Instance.AssignMotionToActor(motionId, actorId);
                        Debug.Log("✅ 模型和动作加载完成");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"❌ 分配动作时出错: {e.Message}");
                    }
                    
                    yield return new WaitForSeconds(0.2f);
                }
                else
                {
                    Debug.LogError("❌ 没有找到演员来分配动作");
                }
            }
        }
        
        /// <summary>
        /// 带进度的摄像机加载
        /// </summary>
        IEnumerator LoadCamerasWithProgress()
        {
            Debug.Log("📹 开始加载摄像机...");
            
            // 主摄像机文件列表
            string[] cameraFiles = {
                "Camera1.vmd",
                "Camera2.vmd", 
                "Camera3 by do-mode.vmd",
                "Camera4.vmd"
            };
            
            int loadedCount = 0;
            foreach (string cameraFile in cameraFiles)
            {
                string cameraPath = Path.Combine(projectRoot, cameraDir, cameraFile);
                if (File.Exists(cameraPath))
                {
                    Debug.Log($"📷 加载摄像机 {++loadedCount}/{cameraFiles.Length}: {cameraFile}");
                    SceneStatesManager.Instance?.AddVMDCamera(cameraPath);
                    yield return new WaitForSeconds(0.1f); // 分帧加载
                }
            }
            
            // 加载子目录中的摄像机
            string subDirName = "Deep Blue Town へおいでよ 2人用 位置 カメラ";
            string fullSubDirPath = Path.Combine(projectRoot, cameraDir, subDirName);
            if (Directory.Exists(fullSubDirPath))
            {
                Debug.Log($"📁 扫描子目录摄像机: {subDirName}");
                string[] subDirVmdFiles = Directory.GetFiles(fullSubDirPath, "*.vmd");
                
                foreach (string vmdFile in subDirVmdFiles)
                {
                    Debug.Log($"📷 加载子目录摄像机: {Path.GetFileName(vmdFile)}");
                    SceneStatesManager.Instance?.AddVMDCamera(vmdFile);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            Debug.Log("✅ 所有摄像机加载完成");
        }
        
        /// <summary>
        /// 带进度的音乐加载
        /// </summary>
        IEnumerator LoadMusicWithProgress()
        {
            Debug.Log("🎵 开始加载音乐...");
            
            string musicPath = Path.Combine(projectRoot, music);
            if (File.Exists(musicPath))
            {
                Debug.Log($"🎼 加载音乐文件: {Path.GetFileName(musicPath)}");
                SceneStatesManager.Instance?.AddMusic(musicPath);
                yield return new WaitForSeconds(0.3f);
                Debug.Log("✅ 音乐加载完成");
            }
            else
            {
                Debug.LogWarning($"⚠️ 音乐文件不存在: {musicPath}");
            }
        }        IEnumerator LoadModelAndMotion()
        {
            // 这个方法已经被 LoadModelAndMotionWithProgress 替代
            // 保留用于兼容性，但实际不再使用
            Debug.LogWarning("LoadModelAndMotion已废弃，请使用LoadModelAndMotionWithProgress");
            yield break;
        }
    }
}
