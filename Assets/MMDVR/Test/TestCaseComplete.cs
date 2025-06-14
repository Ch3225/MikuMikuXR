using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Managers;

namespace MMDVR.Test
{
    /// <summary>
    /// 完整的测试用例 - 包含模型加载、动作分配和所有功能
    /// 这个测试用例演示了完整的MMDVR工作流程
    /// </summary>
    public class TestCaseComplete : MonoBehaviour
    {
        [Header("测试资源路径配置")]
        [SerializeField] private bool useTestPaths = true;
        
        // TMP目录在Assets同级 - 可在Inspector中修改这些路径
        [SerializeField] private string lenModel = "TMP/MMDTest/Models/TDA Len/TDA Len.pmx";
        [SerializeField] private string rinModel = "TMP/MMDTest/Models/TDA Rin/TDA Rin.pmx";
        [SerializeField] private string cameraVmd = "TMP/MMDTest/Motions/Dive to Blue/Dive to Blue Camera2 Low.vmd";
        [SerializeField] private string motion1 = "TMP/MMDTest/Motions/Dive to Blue/DivetoBlue_dance_iMarine_R40_カノン_ボーンフレームと表情フレーム.vmd";
        [SerializeField] private string motion2 = "TMP/MMDTest/Motions/Dive to Blue/DivetoBlue_dance_Umiko_R40_アリア_ボーンフレームと表情フレーム.vmd";
        [SerializeField] private string music = "TMP/MMDTest/Motions/Dive to Blue/内田彩 - Dive to Blue 调整.wav";

        private string projectRoot;
        
        [Header("测试控制")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float delayBetweenSteps = 2f;
        
        [Header("运行时状态")]
        [SerializeField] private bool isTestRunning = false;
        [SerializeField] private string currentStep = "";

        void Start()
        {
            if (autoStart)
            {
                StartTest();
            }
        }

        [ContextMenu("开始完整测试")]
        public void StartTest()
        {
            if (isTestRunning)
            {
                Debug.LogWarning("测试已在运行中...");
                return;
            }
            
            projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            Debug.Log($"=== 开始完整MMDVR测试 ===");
            Debug.Log($"项目根目录: {projectRoot}");
            
            StartCoroutine(RunCompleteTest());
        }

        IEnumerator RunCompleteTest()
        {
            isTestRunning = true;
            
            // 等待SceneStatesManager初始化
            currentStep = "等待SceneStatesManager初始化";
            Debug.Log($"步骤: {currentStep}");
            while (SceneStatesManager.Instance == null)
                yield return null;

            yield return new WaitForSeconds(1f);

            // 步骤1: 加载模型（创建Actor）
            currentStep = "加载模型";
            Debug.Log($"步骤: {currentStep}");
            yield return StartCoroutine(LoadModels());
            
            yield return new WaitForSeconds(delayBetweenSteps);

            // 步骤2: 加载动作
            currentStep = "加载动作";
            Debug.Log($"步骤: {currentStep}");
            yield return StartCoroutine(LoadMotions());
            
            yield return new WaitForSeconds(delayBetweenSteps);

            // 步骤3: 分配动作给模型
            currentStep = "分配动作";
            Debug.Log($"步骤: {currentStep}");
            yield return StartCoroutine(AssignMotions());
            
            yield return new WaitForSeconds(delayBetweenSteps);

            // 步骤4: 加载摄像机
            currentStep = "加载摄像机";
            Debug.Log($"步骤: {currentStep}");
            yield return StartCoroutine(LoadCamera());
            
            yield return new WaitForSeconds(delayBetweenSteps);

            // 步骤5: 加载音乐
            currentStep = "加载音乐";
            Debug.Log($"步骤: {currentStep}");
            yield return StartCoroutine(LoadMusic());
            
            yield return new WaitForSeconds(delayBetweenSteps);

            // 步骤6: 测试播放控制
            currentStep = "测试播放控制";
            Debug.Log($"步骤: {currentStep}");
            yield return StartCoroutine(TestPlayback());

            currentStep = "测试完成";
            Debug.Log($"=== 完整MMDVR测试完成 ===");
            isTestRunning = false;
        }

        IEnumerator LoadModels()
        {
            Debug.Log("--- 开始加载模型 ---");
            
            string lenPath = Path.Combine(projectRoot, lenModel);
            string rinPath = Path.Combine(projectRoot, rinModel);
            
            // 检查文件是否存在
            if (File.Exists(lenPath))
            {
                Debug.Log($"加载Len模型: {lenPath}");
                SceneStatesManager.Instance.AddActor(lenPath);
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning($"Len模型文件不存在: {lenPath}");
                // 使用测试方法创建占位符
                SceneStatesManager.Instance.AddActorForTesting("len_test", "TDA Len (Test)");
            }

            if (File.Exists(rinPath))
            {
                Debug.Log($"加载Rin模型: {rinPath}");
                SceneStatesManager.Instance.AddActor(rinPath);
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning($"Rin模型文件不存在: {rinPath}");
                // 使用测试方法创建占位符
                SceneStatesManager.Instance.AddActorForTesting("rin_test", "TDA Rin (Test)");
            }

            // 检查加载结果
            var actorList = SceneStatesManager.Instance.GetActorList();
            Debug.Log($"当前Actor数量: {actorList.Count}");
            foreach (var actor in actorList)
            {
                Debug.Log($"  - Actor: {actor.displayName} (ID: {actor.id})");
            }

            Debug.Log("--- 模型加载完成 ---");
        }

        IEnumerator LoadMotions()
        {
            Debug.Log("--- 开始加载动作 ---");
            
            string motion1Path = Path.Combine(projectRoot, motion1);
            string motion2Path = Path.Combine(projectRoot, motion2);

            string motionId1 = null;
            string motionId2 = null;

            // 加载第一个动作
            if (File.Exists(motion1Path))
            {
                Debug.Log($"加载动作1: {motion1Path}");
                motionId1 = SceneStatesManager.Instance.AddMotion(motion1Path);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.LogWarning($"动作1文件不存在: {motion1Path}");
                // 使用测试方法创建占位符
                motionId1 = "motion1_test";
                SceneStatesManager.Instance.AddMotionForTesting(motionId1, "Dive to Blue (Len)");
            }

            // 加载第二个动作
            if (File.Exists(motion2Path))
            {
                Debug.Log($"加载动作2: {motion2Path}");
                motionId2 = SceneStatesManager.Instance.AddMotion(motion2Path);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.LogWarning($"动作2文件不存在: {motion2Path}");
                // 使用测试方法创建占位符
                motionId2 = "motion2_test";
                SceneStatesManager.Instance.AddMotionForTesting(motionId2, "Dive to Blue (Rin)");
            }

            // 检查加载结果
            var motionList = SceneStatesManager.Instance.GetMotionList();
            Debug.Log($"当前Motion数量: {motionList.Count}");
            foreach (var motion in motionList)
            {
                Debug.Log($"  - Motion: {motion.displayName} (ID: {motion.id})");
            }

            Debug.Log("--- 动作加载完成 ---");
        }

        IEnumerator AssignMotions()
        {
            Debug.Log("--- 开始分配动作 ---");
            
            var actorList = SceneStatesManager.Instance.GetActorList();
            var motionList = SceneStatesManager.Instance.GetMotionList();

            if (actorList.Count >= 2 && motionList.Count >= 2)
            {
                // 将第一个动作分配给第一个Actor (Len)
                string actor1Id = actorList[0].id;
                string motion1Id = motionList[0].id;
                
                Debug.Log($"分配动作 {motionList[0].displayName} 给演员 {actorList[0].displayName}");
                SceneStatesManager.Instance.AssignMotionToActor(motion1Id, actor1Id);
                
                // 建立关联关系
                if (actorList[0].filePath != null && !actorList[0].filePath.Contains("TestData"))
                {
                    // 如果是真实模型，创建模型-动作关联
                    var modelList = SceneStatesManager.Instance.GetModelList();
                    if (modelList.Count > 0)
                    {
                        SceneStatesManager.Instance.AssociateModelWithMotion(modelList[0].id, motion1Id);
                    }
                }
                
                yield return new WaitForSeconds(1f);

                // 将第二个动作分配给第二个Actor (Rin)
                string actor2Id = actorList[1].id;
                string motion2Id = motionList[1].id;
                
                Debug.Log($"分配动作 {motionList[1].displayName} 给演员 {actorList[1].displayName}");
                SceneStatesManager.Instance.AssignMotionToActor(motion2Id, actor2Id);
                
                // 建立关联关系
                if (actorList[1].filePath != null && !actorList[1].filePath.Contains("TestData"))
                {
                    var modelList = SceneStatesManager.Instance.GetModelList();
                    if (modelList.Count > 1)
                    {
                        SceneStatesManager.Instance.AssociateModelWithMotion(modelList[1].id, motion2Id);
                    }
                }
                
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Debug.LogWarning($"Actor或Motion数量不足进行分配。Actor: {actorList.Count}, Motion: {motionList.Count}");
            }

            Debug.Log("--- 动作分配完成 ---");
        }

        IEnumerator LoadCamera()
        {
            Debug.Log("--- 开始加载摄像机 ---");
            
            string cameraPath = Path.Combine(projectRoot, cameraVmd);
            
            if (File.Exists(cameraPath))
            {
                Debug.Log($"加载VMD摄像机: {cameraPath}");
                SceneStatesManager.Instance.AddVMDCamera(cameraPath);
            }
            else
            {
                Debug.LogWarning($"摄像机文件不存在: {cameraPath}");
            }

            yield return new WaitForSeconds(0.5f);

            // 检查摄像机列表
            var cameraList = SceneStatesManager.Instance.GetCameraList();
            Debug.Log($"当前Camera数量: {cameraList.Count}");
            foreach (var camera in cameraList)
            {
                Debug.Log($"  - Camera: {camera.displayName} (ID: {camera.id})");
            }

            Debug.Log("--- 摄像机加载完成 ---");
        }

        IEnumerator LoadMusic()
        {
            Debug.Log("--- 开始加载音乐 ---");
            
            string musicPath = Path.Combine(projectRoot, music);
            
            if (File.Exists(musicPath))
            {
                Debug.Log($"加载音乐: {musicPath}");
                SceneStatesManager.Instance.AddMusic(musicPath);
            }
            else
            {
                Debug.LogWarning($"音乐文件不存在: {musicPath}");
            }

            yield return new WaitForSeconds(1f);

            // 检查音乐列表
            var musicList = SceneStatesManager.Instance.GetMusicList();
            Debug.Log($"当前Music数量: {musicList.Count}");
            foreach (var music in musicList)
            {
                Debug.Log($"  - Music: {music.title} (ID: {music.id})");
            }

            Debug.Log("--- 音乐加载完成 ---");
        }

        IEnumerator TestPlayback()
        {
            Debug.Log("--- 开始测试播放控制 ---");
            
            // 激活第一个摄像机（如果有的话）
            var cameraList = SceneStatesManager.Instance.GetCameraList();
            if (cameraList.Count > 1) // 第0个是Free Camera
            {
                Debug.Log($"激活摄像机: {cameraList[1].displayName}");
                SceneStatesManager.Instance.SetActiveCamera(cameraList[1].id);
                yield return new WaitForSeconds(1f);
            }

            // 激活音乐
            var musicList = SceneStatesManager.Instance.GetMusicList();
            if (musicList.Count > 0)
            {
                Debug.Log($"激活音乐: {musicList[0].title}");
                SceneStatesManager.Instance.SetActiveMusic(musicList[0].id);
                yield return new WaitForSeconds(1f);
            }

            // 开始播放
            Debug.Log("开始播放...");
            SceneStatesManager.Instance.Play();
            yield return new WaitForSeconds(3f);

            // 暂停播放
            Debug.Log("暂停播放...");
            SceneStatesManager.Instance.Pause();
            yield return new WaitForSeconds(1f);

            // 跳转到特定时间
            Debug.Log("跳转到5秒位置...");
            SceneStatesManager.Instance.SeekTo(5f);
            yield return new WaitForSeconds(1f);

            // 再次播放
            Debug.Log("继续播放...");
            SceneStatesManager.Instance.Play();
            yield return new WaitForSeconds(2f);

            // 最终暂停
            SceneStatesManager.Instance.Pause();

            Debug.Log("--- 播放控制测试完成 ---");
        }

        [ContextMenu("停止测试")]
        public void StopTest()
        {
            StopAllCoroutines();
            isTestRunning = false;
            currentStep = "测试已停止";
            Debug.Log("测试已手动停止");
        }

        [ContextMenu("打印当前状态")]
        public void PrintCurrentState()
        {
            if (SceneStatesManager.Instance == null)
            {
                Debug.Log("SceneStatesManager 未初始化");
                return;
            }

            Debug.Log("=== 当前MMDVR状态 ===");
            
            var actorList = SceneStatesManager.Instance.GetActorList();
            Debug.Log($"Actor数量: {actorList.Count}");
            
            var modelList = SceneStatesManager.Instance.GetModelList();
            Debug.Log($"Model数量: {modelList.Count}");
            
            var motionList = SceneStatesManager.Instance.GetMotionList();
            Debug.Log($"Motion数量: {motionList.Count}");
            
            var cameraList = SceneStatesManager.Instance.GetCameraList();
            Debug.Log($"Camera数量: {cameraList.Count}");
            
            var musicList = SceneStatesManager.Instance.GetMusicList();
            Debug.Log($"Music数量: {musicList.Count}");
            
            Debug.Log($"当前播放状态: {(SceneStatesManager.Instance.isPlaying ? "播放中" : "暂停")}");
            Debug.Log($"播放时间: {SceneStatesManager.Instance.playTime:F2}秒");
        }

        // 在Inspector中显示当前测试状态
        void OnGUI()
        {
            if (!isTestRunning) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"测试状态: {(isTestRunning ? "运行中" : "已停止")}", new GUIStyle() { fontSize = 16, normal = new GUIStyleState() { textColor = Color.white } });
            GUILayout.Label($"当前步骤: {currentStep}", new GUIStyle() { fontSize = 14, normal = new GUIStyleState() { textColor = Color.yellow } });
            
            if (GUILayout.Button("停止测试"))
            {
                StopTest();
            }
            GUILayout.EndArea();
        }
    }
}
