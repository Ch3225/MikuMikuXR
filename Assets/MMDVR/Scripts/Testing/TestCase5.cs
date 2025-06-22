using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase5 : MonoBehaviour
    {
        // 资源路径
        private string model = "TMP/MMDTest/Models/Sour miku/Sour Miku1.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        private string projectRoot;
        
        // 记录加载的资源ID
        private string loadedModelId;

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(LoadAndTest());
        }

        IEnumerator LoadAndTest()
        {
            // 等待UserActionManager加载完毕
            while (UserActionManager.Instance == null)
                yield return null;

            // 1. 通过UserActionManager加载模型和动作，并建立关联
            yield return StartCoroutine(LoadModelAndMotionViaUserAction());

            // 等待几秒钟，确保动作已经开始播放
            Debug.Log("[TestCase5] 等待5秒，观察初始动作...");
            yield return new WaitForSeconds(5f);

            // 2. 重置模型到T-Pose
            Debug.Log("[TestCase5] 准备执行ResetToTPose...");
            yield return StartCoroutine(ResetModelToTPose());
            
            // 等待几秒钟，观察T-Pose效果
            Debug.Log("[TestCase5] 等待5秒，观察T-Pose效果...");
            yield return new WaitForSeconds(5f);

            // 3. 重新加载原始动作
            Debug.Log("[TestCase5] 准备重新加载原始动作...");
            yield return StartCoroutine(ReloadOriginalMotion());

            Debug.Log("[TestCase5] 测试完成.");
        }        IEnumerator LoadModelAndMotionViaUserAction()
        {
            string modelPath = Path.Combine(projectRoot, model);
            string motionPath = Path.Combine(projectRoot, motion);

            if (!File.Exists(modelPath) || !File.Exists(motionPath))
            {
                Debug.LogError($"[TestCase5] 模型或动作文件不存在. Model: {modelPath}, Motion: {motionPath}");
                yield break;
            }

            Debug.Log("[TestCase5] 通过UserActionManager加载模型和动作...");
            
            // 使用UserActionManager加载模型和动作（不自动关联）
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
            
            if (!string.IsNullOrEmpty(modelId) && !string.IsNullOrEmpty(motionId))
            {
                loadedModelId = modelId;
                Debug.Log($"[TestCase5] 模型(ID:{modelId})和动作(ID:{motionId})已加载.");
                
                // 显式关联Motion到Model
                Debug.Log("[TestCase5] 开始关联动作到模型...");
                bool associationCompleted = false;
                UserActionManager.Instance.AssignMotionToModel(modelId, motionId, () =>
                {
                    associationCompleted = true;
                });
                yield return new WaitUntil(() => associationCompleted);
                Debug.Log($"[TestCase5] 动作关联完成: {modelId} <-> {motionId}");
            }
            else
            {
                Debug.LogError("[TestCase5] 模型或动作加载失败.");
            }
        }IEnumerator ResetModelToTPose()
        {
            if (string.IsNullOrEmpty(loadedModelId))
            {
                Debug.LogError("[TestCase5] 没有已加载的模型ID.");
                yield break;
            }

            Debug.Log($"[TestCase5] 通过ResourceManager查询模型组件并重置到T-Pose...");
            
            // 通过ResourceManager查询模型组件（这不是用户行为，是程序内部查询）
            var modelComponent = ResourceManager.Instance.GetModel(loadedModelId);
            if (modelComponent != null)
            {
                var mmdGameObject = modelComponent.gameObject.GetComponent<LibMMD.Unity3D.MmdGameObject>();
                if (mmdGameObject != null)
                {
                    Debug.Log($"[TestCase5] 找到模型 '{loadedModelId}' 的MmdGameObject，正在调用ResetToTPose()...");
                    mmdGameObject.ResetToTPose();
                    Debug.Log("[TestCase5] ResetToTPose() 调用完成.");
                }
                else
                {
                    Debug.LogError("[TestCase5] 找不到MmdGameObject组件.");
                }
            }
            else
            {
                Debug.LogError("[TestCase5] 找不到模型组件.");
            }
            
            yield return new WaitForEndOfFrame();
        }        IEnumerator ReloadOriginalMotion()
        {
            if (string.IsNullOrEmpty(loadedModelId))
            {
                Debug.LogError("[TestCase5] 没有已加载的模型ID.");
                yield break;
            }

            Debug.Log($"[TestCase5] 重新开始播放动作...");
            
            // 这是用户行为：重新开始播放
            UserActionManager.Instance.StartPlayback(() => 
            {
                Debug.Log("[TestCase5] 重新开始播放完成.");
            });
            
            yield return new WaitForEndOfFrame();
        }
    }
}
