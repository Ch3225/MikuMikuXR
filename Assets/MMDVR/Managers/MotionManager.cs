using System.Collections.Generic;
using UnityEngine;
using LibMMD.Motion;
using LibMMD.Unity3D;

namespace MMDVR.Managers
{
    public class MotionManager : MonoBehaviour
    {
        public static MotionManager Instance { get; private set; }

        [Header("场景中所有动作（Motions）")]
        public List<GameObject> motions = new List<GameObject>();
        public Transform motionsRoot; // 挂载所有动作的父节点

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        public void AddMotion(GameObject motion)
        {
            if (!motions.Contains(motion)) motions.Add(motion);
            if (motionsRoot != null && motion.transform.parent != motionsRoot)
                motion.transform.SetParent(motionsRoot, false);
        }
        public void RemoveMotion(GameObject motion)
        {
            if (motions.Contains(motion)) motions.Remove(motion);
        }
        public GameObject GetMotion(int index)
        {
            if (index < 0 || index >= motions.Count) return null;
            return motions[index];
        }

        // 加载VMD动作文件，返回路径（可扩展为MmdMotion对象）
        public string LoadMotion(string path)
        {
            // 这里只做简单路径管理，实际可扩展为加载MmdMotion对象
            if (!motions.Exists(go => go.name == System.IO.Path.GetFileNameWithoutExtension(path)))
            {
                var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
                go.SetActive(false); // 仅做管理，不实例化到场景
                if (motionsRoot != null)
                    go.transform.SetParent(motionsRoot, false);
                AddMotion(go);
                MMDVR.Managers.EventManager.OnMotionListChanged?.Invoke();
            }
            return path;
        }

        // 将动作分配给指定模型
        public void AssignMotionToActor(GameObject actor, string motionPath)
        {
            if (actor == null || string.IsNullOrEmpty(motionPath)) return;
            var mmd = actor.GetComponent<MmdGameObject>();
            if (mmd != null)
            {
                mmd.LoadMotion(motionPath);
                // 维护模型-动作映射
                var motionGo = motions.Find(go => go.name == System.IO.Path.GetFileNameWithoutExtension(motionPath));
                if (motionGo != null)
                {
                    SceneStatesManager.Instance?.AddModelMotion(actor, motionGo);
                    // 分配动作后刷新动作下拉列表
                    MMDVR.Managers.EventManager.OnMotionListChanged?.Invoke();
                }
            }
        }

        // 组合调用：加载动作并分配给模型
        public void LoadAndAssignMotion(string motionPath, GameObject actor)
        {
            LoadMotion(motionPath);
            AssignMotionToActor(actor, motionPath);
        }
    }
}