using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Managers;
using LibMMD.Unity3D;

namespace MMDVR.Test
{
    public class TestCase5 : MonoBehaviour
    {
        // 资源路径
        private string model = "TMP/MMDTest/Models/Sour miku/Sour Miku1.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        private string projectRoot;

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(LoadAndTest());
        }

        IEnumerator LoadAndTest()
        {
            // 等待SceneStatesManager加载完毕
            while (SceneStatesManager.Instance == null)
                yield return null;

            // 1. 通过SceneStatesManager加载模型和动作，并建立关联
            yield return StartCoroutine(LoadModelAndMotionViaManager());

            // 等待几秒钟，确保动作已经开始播放
            Debug.Log("[TestCase5] 等待5秒，观察初始动作...");
            yield return new WaitForSeconds(5f);

            // 2. 获取MmdGameObject并调用ResetToTPose
            Debug.Log("[TestCase5] 准备执行ResetToTPose...");
            ResetFirstActorToTPose();
            
            // 等待几秒钟，观察T-Pose效果
            Debug.Log("[TestCase5] 等待5秒，观察T-Pose效果...");
            yield return new WaitForSeconds(5f);

            // 3. 重新加载原始动作
            Debug.Log("[TestCase5] 准备重新加载原始动作...");
            ReloadOriginalMotion();

            Debug.Log("[TestCase5] 测试完成.");
        }

        IEnumerator LoadModelAndMotionViaManager()
        {
            try
            {
                string modelPath = Path.Combine(projectRoot, model);
                string motionPath = Path.Combine(projectRoot, motion);

                if (!File.Exists(modelPath) || !File.Exists(motionPath))
                {
                    Debug.LogError($"[TestCase5] 模型或动作文件不存在. Model: {modelPath}, Motion: {motionPath}");
                    yield break;
                }                Debug.Log("[TestCase5] 通过SceneStatesManager加载模型和动作...");
                
                SceneStatesManager.Instance.AddActor(modelPath);
                string motionId = SceneStatesManager.Instance.AddMotion(motionPath);
                
                // 获取最后添加的演员ID
                var actorList = SceneStatesManager.Instance.GetActorList();
                if (actorList.Count > 0)
                {
                    string actorId = actorList[actorList.Count - 1].id;
                    SceneStatesManager.Instance.AssignMotionToActor(motionId, actorId);
                    Debug.Log($"[TestCase5] 模型(ID:{actorId})和动作(ID:{motionId})已加载并分配.");
                }
                else
                {
                    Debug.LogError("[TestCase5] 添加演员后未找到演员列表.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TestCase5] 加载模型和动作时出错: {e.Message}");
            }
        }

        void ResetFirstActorToTPose()
        {
            var actorList = SceneStatesManager.Instance.GetActorList();
            if (actorList.Count > 0)
            {
                string actorId = actorList[0].id;
                var actorObj = SceneStatesManager.Instance.GetActorObjectById(actorId);
                if (actorObj != null)
                {
                    var mmdGameObject = actorObj.GetComponent<MmdGameObject>();
                    if (mmdGameObject != null)
                    {                        Debug.Log($"[TestCase5] 找到Actor '{actorId}' 的MmdGameObject，正在调用ResetToTPose()...");
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
                    Debug.LogError("[TestCase5] 找不到Actor对象.");
                }
            }
            else
            {
                Debug.LogError("[TestCase5] 场景中没有Actor.");
            }
        }

        void ReloadOriginalMotion()
        {
            var actorList = SceneStatesManager.Instance.GetActorList();
            var motionList = SceneStatesManager.Instance.GetMotionDataList();

            if (actorList.Count > 0 && motionList.Count > 0)
            {
                string actorId = actorList[0].id;
                string motionPath = motionList[0].FilePath; // 使用第一个已加载的动作

                var actorObj = SceneStatesManager.Instance.GetActorObjectById(actorId);
                if (actorObj != null)
                {
                    var mmdGameObject = actorObj.GetComponent<MmdGameObject>();
                    if (mmdGameObject != null)
                    {
                        Debug.Log($"[TestCase5] 找到Actor '{actorId}' 的MmdGameObject，正在重新加载动作: {motionPath}");
                        mmdGameObject.LoadMotion(motionPath);
                        mmdGameObject.Playing = true; // 确保开始播放
                        Debug.Log("[TestCase5] 重新加载动作完成.");
                    }
                }
            }
            else
            {
                Debug.LogError("[TestCase5] 场景中没有Actor或Motion.");
            }
        }
    }
}
