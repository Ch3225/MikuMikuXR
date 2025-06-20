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
            StartCoroutine(LoadResources());
        }        IEnumerator LoadResources()
        {
            // 等待SceneStatesManager加载完毕
            while (SceneStatesManager.Instance == null)
                yield return null;

            // 按照LibMMD示例的方式加载模型和动作
            yield return StartCoroutine(LoadModelAndMotion());
            
            // 加载相机 - 更新为SceneStatesManager
            SceneStatesManager.Instance?.AddVMDCamera(Path.Combine(projectRoot, cameraDir, "Camera1.vmd"));
            SceneStatesManager.Instance?.AddVMDCamera(Path.Combine(projectRoot, cameraDir, "Camera2.vmd"));
            SceneStatesManager.Instance?.AddVMDCamera(Path.Combine(projectRoot, cameraDir, "Camera3 by do-mode.vmd"));
            SceneStatesManager.Instance?.AddVMDCamera(Path.Combine(projectRoot, cameraDir, "Camera4.vmd"));
            // Also load from the subdirectory if it contains 'camera'
            string subDirName = "Deep Blue Town へおいでよ 2人用 位置 カメラ";
            string fullSubDirPath = Path.Combine(projectRoot, cameraDir, subDirName);
            if (Directory.Exists(fullSubDirPath))
            {
                string[] subDirVmdFiles = Directory.GetFiles(fullSubDirPath, "*.vmd");                foreach (string vmdFile in subDirVmdFiles)
                {
                    // Assuming all VMDs in a directory named with "カメラ" are camera files
                    SceneStatesManager.Instance?.AddVMDCamera(vmdFile);
                }
            }

            // 加载音乐
            SceneStatesManager.Instance?.AddMusic(Path.Combine(projectRoot, music));

            // 统一刷新所有下拉框
            var uiMgr = FindObjectOfType<DesktopUIManager>();
            if (uiMgr != null)
                uiMgr.RefreshAllDropdowns();
        }        IEnumerator LoadModelAndMotion()
        {
            // 使用SceneStatesManager来正确管理模型和动作资源
            try
            {
                string modelPath = Path.Combine(projectRoot, model);
                string motionPath = Path.Combine(projectRoot, motion);

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

                Debug.Log($"开始通过SceneStatesManager加载模型: {modelPath}");
                
                // 通过SceneStatesManager添加演员（模型）
                SceneStatesManager.Instance.AddActor(modelPath);
                
                Debug.Log($"开始通过SceneStatesManager加载动作: {motionPath}");
                
                // 通过SceneStatesManager添加动作
                string motionId = SceneStatesManager.Instance.AddMotion(motionPath);
                
                // 获取刚添加的演员（应该是列表中最后一个）
                var actorList = SceneStatesManager.Instance.GetActorList();
                if (actorList.Count > 0)
                {
                    string actorId = actorList[actorList.Count - 1].id;
                    
                    // 将动作分配给演员
                    SceneStatesManager.Instance.AssignMotionToActor(motionId, actorId);
                    
                    Debug.Log("模型和动作已通过SceneStatesManager正确加载和分配");
                }
                else
                {
                    Debug.LogError("没有找到演员来分配动作");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"通过SceneStatesManager加载模型和动作时出错: {e.Message}");
            }
        }
    }
}
