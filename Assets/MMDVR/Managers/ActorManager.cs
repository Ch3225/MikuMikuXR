using System.Collections.Generic;
using UnityEngine;
using LibMMD.Unity3D;

namespace MMDVR.Managers
{
    public class ActorManager : MonoBehaviour
    {
        public static ActorManager Instance { get; private set; }

        [Header("场景中所有模型（Actors）")]
        public List<GameObject> actors = new List<GameObject>();
        public Transform actorsRoot; // 指向Actors节点

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            // 监听事件
            MMDVR.Managers.EventManager.OnModelLoadRequest += LoadModel;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                MMDVR.Managers.EventManager.OnModelLoadRequest -= LoadModel;
                CleanupAllModels();
            }
        }
        
        private void OnApplicationQuit()
        {
            CleanupAllModels();
        }
        
        private void CleanupAllModels()
        {
            // 在应用退出或组件销毁时，清理所有模型资源
            foreach (var actor in actors)
            {
                if (actor != null)
                {
                    var mmdComponent = actor.GetComponent<MmdGameObject>();
                    if (mmdComponent != null)
                    {
                        // 尝试提前调用Release逻辑
                        try
                        {
                            // 使用反射调用私有的Release方法
                            var releaseMethod = typeof(MmdGameObject).GetMethod("Release", 
                                System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                                
                            if (releaseMethod != null)
                            {
                                releaseMethod.Invoke(mmdComponent, null);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("Error releasing MmdGameObject: " + e.Message);
                        }
                    }
                }
            }        }

        // 加载MMD模型
        public void LoadModel(string path)
        {
            // 创建MmdGameObject
            var mmdGo = MmdGameObject.CreateGameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            var mmdComponent = mmdGo.GetComponent<MmdGameObject>();
            if (mmdComponent.LoadModel(path))
            {
                if (actorsRoot != null)
                    mmdGo.transform.SetParent(actorsRoot, false);
                AddActor(mmdGo);
                // 通知UI刷新下拉列表
                MMDVR.Managers.EventManager.OnActorListChanged?.Invoke();
            }
            else
            {
                UnityEngine.Debug.LogError($"模型加载失败: {path}");
                GameObject.Destroy(mmdGo);
            }
        }

        public void AddActor(GameObject actor)
        {
            if (!actors.Contains(actor)) actors.Add(actor);
        }
        public void RemoveActor(GameObject actor)
        {
            if (actors.Contains(actor)) actors.Remove(actor);
        }
        public GameObject GetActor(int index)
        {
            if (index < 0 || index >= actors.Count) return null;
            return actors[index];
        }
    }
}