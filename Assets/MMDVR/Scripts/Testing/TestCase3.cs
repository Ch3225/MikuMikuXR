using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase3 : MonoBehaviour
    {
        // 资源路径
        private string model = "TMP/MMDTest/Models/mmd___halloween_miku___dl_by_mlekoduszek-dccfunl/ミクハロウィーン.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        private string cameraVmd = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Camera3 by do-mode.vmd";
        private string music = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Deep Blue Town へおいでよ.wav";
        private string projectRoot;        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            // 只通过UserActionManager进行操作
            StartCoroutine(LoadResources());
        }        IEnumerator LoadResources()
        {
            Debug.Log("=== TestCase3 资源加载开始 ===");
            
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null)
            {
                Debug.Log("等待UserActionManager初始化...");
                yield return new WaitForSeconds(0.5f);
            }
            Debug.Log("UserActionManager已就绪");

            string modelPath = Path.Combine(projectRoot, model);
            string motionPath = Path.Combine(projectRoot, motion);
              // 加载模型和动作
            if (File.Exists(modelPath) && File.Exists(motionPath))
            {
                Debug.Log("开始加载模型和动作...");
                bool completed = false;
                string modelId = null;
                string motionId = null;
                
                UserActionManager.Instance.LoadModelAndMotion(modelPath, motionPath, (mId, moId) =>
                {
                    modelId = mId;
                    motionId = moId;
                    completed = true;
                });
                
                yield return new WaitUntil(() => completed);
                Debug.Log($"✅ 模型和动作加载完成: {modelId}, {motionId}");
                
                // 显式地关联Motion到Model
                if (!string.IsNullOrEmpty(modelId) && !string.IsNullOrEmpty(motionId))
                {
                    Debug.Log("🔗 开始关联动作到模型...");
                    bool associationCompleted = false;
                    UserActionManager.Instance.AssignMotionToModel(modelId, motionId, () =>
                    {
                        associationCompleted = true;
                    });
                    yield return new WaitUntil(() => associationCompleted);
                    Debug.Log($"✅ 动作关联完成: {modelId} <-> {motionId}");
                }
            }
            else
            {
                Debug.LogWarning($"模型或动作文件不存在: {modelPath}, {motionPath}");
            }

            yield return new WaitForSeconds(0.5f);
            
            // 加载相机
            string cameraPath = Path.Combine(projectRoot, cameraVmd);
            if (File.Exists(cameraPath))
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
            string musicPath = Path.Combine(projectRoot, music);
            if (File.Exists(musicPath))
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

            Debug.Log("=== TestCase3 资源加载完成 ===");
        }
    }
}
