using UnityEngine;
using System.Collections;
using System.IO;
using MMDVR.Scripts.Managers;

namespace MMDVR.Scripts.Testing
{
    public class TestCase2 : MonoBehaviour
    {
        private string projectRoot;
        private string model = "TMP/MMDTest/Models/Sour miku/Sour Miku1.pmx";
        private string motion = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/DeepBlueTown_he_Oideyo_dance.vmd";
        private string cameraDir = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ";
        private string music = "TMP/MMDTest/Motions/アイマリンプロジェクト-内田彩&内田真礼&佐倉綾音 - Deep Blue Town へおいでよ/Deep Blue Town へおいでよ.wav";

        void Start()
        {
            projectRoot = Directory.GetParent(Application.dataPath).FullName;
            StartCoroutine(RunTest());
        }

        IEnumerator RunTest()
        {
            // 等待UserActionManager初始化
            while (UserActionManager.Instance == null) yield return null;

            // 加载模型
            string modelId = null;
            bool modelLoaded = false;
            UserActionManager.Instance.LoadAndShowModel(Path.Combine(projectRoot, model), id => { modelId = id; modelLoaded = true; });
            yield return new WaitUntil(() => modelLoaded);

            // 加载动作
            string motionId = null;
            bool motionLoaded = false;
            UserActionManager.Instance.LoadMotion(Path.Combine(projectRoot, motion), id => { motionId = id; motionLoaded = true; });
            yield return new WaitUntil(() => motionLoaded);

            // 关联动作到模型
            bool linkDone = false;
            UserActionManager.Instance.AssignMotionToModel(modelId, motionId, () => { linkDone = true; });
            yield return new WaitUntil(() => linkDone);

            // 加载相机（主目录下Camera1~4和Camera3 by do-mode.vmd）
            string[] cameraFiles = new string[]
            {
                "Camera1.vmd", "Camera2.vmd", "Camera3 by do-mode.vmd", "Camera4.vmd"
            };
            foreach (var camFile in cameraFiles)
            {
                string camPath = Path.Combine(projectRoot, cameraDir, camFile);
                if (File.Exists(camPath))
                {
                    bool camLoaded = false;
                    UserActionManager.Instance.LoadVMDCamera(camPath, id => { camLoaded = true; });
                    yield return new WaitUntil(() => camLoaded);
                }
            }
            // 加载子目录下所有.vmd（如有）
            string subDirName = "Deep Blue Town へおいでよ 2人用 位置 カメラ";
            string fullSubDirPath = Path.Combine(projectRoot, cameraDir, subDirName);
            if (Directory.Exists(fullSubDirPath))
            {
                var subDirVmds = Directory.GetFiles(fullSubDirPath, "*.vmd");
                foreach (var vmdFile in subDirVmds)
                {
                    bool camLoaded = false;
                    UserActionManager.Instance.LoadVMDCamera(vmdFile, id => { camLoaded = true; });
                    yield return new WaitUntil(() => camLoaded);
                }
            }

            // 加载音乐
            bool musicLoaded = false;
            UserActionManager.Instance.LoadMusic(Path.Combine(projectRoot, music), id => { musicLoaded = true; });
            yield return new WaitUntil(() => musicLoaded);

            Debug.Log("=== TestCase2 资源批量加载与关联测试完成 ===");
        }
    }
}
