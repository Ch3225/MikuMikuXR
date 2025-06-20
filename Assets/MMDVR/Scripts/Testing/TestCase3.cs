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
        private string projectRoot;

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            // 加载模型
            EventManager.OnModelLoadRequest?.Invoke(Path.Combine(projectRoot, model));
            StartCoroutine(LoadAfterModel());
        }        IEnumerator LoadAfterModel()
        {
            // 等待SceneStatesManager加载完毕
            while (SceneStatesManager.Instance == null)
                yield return null;

            // 加载演员（模型实例）
            Debug.Log("TestCase3: 开始加载演员和动作...");
            
            string modelPath = Path.Combine(projectRoot, model);
            string motionPath = Path.Combine(projectRoot, motion);
            
            if (File.Exists(modelPath))
            {
                SceneStatesManager.Instance.AddActor(modelPath);
                Debug.Log($"模型已添加: {modelPath}");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning($"模型文件不存在: {modelPath}");
                // 使用测试方法创建占位符
                SceneStatesManager.Instance.AddActorForTesting("test_actor", "Test Model");
            }
            
            string motionId = null;
            if (File.Exists(motionPath))
            {
                motionId = SceneStatesManager.Instance.AddMotion(motionPath);
                Debug.Log($"动作已添加: {motionPath}");
            }
            else
            {
                Debug.LogWarning($"动作文件不存在: {motionPath}");
                motionId = "test_motion";
                SceneStatesManager.Instance.AddMotionForTesting(motionId, "Test Motion");
            }

            // 分配动作给演员
            if (motionId != null)
            {
                var actorList = SceneStatesManager.Instance.GetActorList();
                if (actorList.Count > 0)
                {
                    SceneStatesManager.Instance.AssignMotionToActor(motionId, actorList[actorList.Count - 1].id);
                    Debug.Log("动作已分配给演员");
                }
            }

            yield return new WaitForSeconds(1f);
            
            // 加载相机
            string cameraPath = Path.Combine(projectRoot, cameraVmd);
            if (File.Exists(cameraPath))
            {
                SceneStatesManager.Instance.AddVMDCamera(cameraPath);
                Debug.Log($"摄像机已添加: {cameraPath}");
            }
            else
            {
                Debug.LogWarning($"摄像机文件不存在: {cameraPath}");
            }
            
            // 加载音乐
            string musicPath = Path.Combine(projectRoot, music);
            if (File.Exists(musicPath))
            {
                SceneStatesManager.Instance.AddMusic(musicPath);
                Debug.Log($"音乐已添加: {musicPath}");
            }
            else
            {
                Debug.LogWarning($"音乐文件不存在: {musicPath}");
            }

            // 统一刷新所有下拉框
            var uiMgr = FindObjectOfType<DesktopUIManager>();
            if (uiMgr != null)
                uiMgr.RefreshAllDropdowns();
                
            Debug.Log("TestCase3: 加载完成");
        }
    }
}
