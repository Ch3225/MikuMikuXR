using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// 关联管理器 - 管理模型与动作的关联关系
    /// 职责: 存储、管理、触发 <模型ID, Set<动作ID>> 关联
    /// </summary>
    public class AssociationManager : MonoBehaviour
    {
        public static AssociationManager Instance { get; private set; }

        [Header("模型-动作关联")]
        [SerializeField] private Dictionary<string, HashSet<string>> modelMotionAssociations = new Dictionary<string, HashSet<string>>();

        // 关联变化事件
        public static event Action<string, string> OnModelMotionAssociated; // modelId, motionId
        public static event Action<string, string> OnModelMotionDisassociated; // modelId, motionId

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== 模型-动作关联管理 ====================

        /// <summary>
        /// 关联模型和动作
        /// </summary>
        public void AssociateModelWithMotion(string modelId, string motionId)
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(motionId))
                return;

            if (!modelMotionAssociations.ContainsKey(modelId))
            {
                modelMotionAssociations[modelId] = new HashSet<string>();
            }

            if (modelMotionAssociations[modelId].Add(motionId))
            {
                OnModelMotionAssociated?.Invoke(modelId, motionId);
                Debug.Log($"关联模型 {modelId} 与动作 {motionId}");
            }
        }

        /// <summary>
        /// 取消模型和动作的关联
        /// </summary>
        public void DisassociateModelFromMotion(string modelId, string motionId)
        {
            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(motionId))
                return;

            if (modelMotionAssociations.ContainsKey(modelId) && 
                modelMotionAssociations[modelId].Remove(motionId))
            {
                OnModelMotionDisassociated?.Invoke(modelId, motionId);
                Debug.Log($"取消关联模型 {modelId} 与动作 {motionId}");

                // 如果没有动作了，移除模型条目
                if (modelMotionAssociations[modelId].Count == 0)
                {
                    modelMotionAssociations.Remove(modelId);
                }
            }
        }

        /// <summary>
        /// 获取模型关联的所有动作ID
        /// </summary>
        public List<string> GetModelAssociatedMotions(string modelId)
        {
            if (string.IsNullOrEmpty(modelId) || !modelMotionAssociations.ContainsKey(modelId))
                return new List<string>();

            return modelMotionAssociations[modelId].ToList();
        }

        /// <summary>
        /// 清除模型的所有关联
        /// </summary>
        public void ClearModelAssociations(string modelId)
        {
            if (string.IsNullOrEmpty(modelId) || !modelMotionAssociations.ContainsKey(modelId))
                return;

            var motionIds = modelMotionAssociations[modelId].ToList();
            foreach (var motionId in motionIds)
            {
                OnModelMotionDisassociated?.Invoke(modelId, motionId);
            }

            modelMotionAssociations.Remove(modelId);
            Debug.Log($"清除模型 {modelId} 的所有关联");
        }

        /// <summary>
        /// 清除动作的所有关联
        /// </summary>
        public void ClearMotionAssociations(string motionId)
        {
            if (string.IsNullOrEmpty(motionId))
                return;

            var modelsToUpdate = new List<string>();
            foreach (var kvp in modelMotionAssociations)
            {
                if (kvp.Value.Contains(motionId))
                {
                    modelsToUpdate.Add(kvp.Key);
                }
            }

            foreach (var modelId in modelsToUpdate)
            {
                DisassociateModelFromMotion(modelId, motionId);
            }
        }

        /// <summary>
        /// 清除所有关联
        /// </summary>
        public void ClearAllAssociations()
        {
            modelMotionAssociations.Clear();
            Debug.Log("清除所有模型-动作关联");
        }        /// <summary>
        /// 检查模型是否与动作关联
        /// </summary>
        public bool IsModelAssociatedWithMotion(string modelId, string motionId)
        {
            return !string.IsNullOrEmpty(modelId) && 
                   !string.IsNullOrEmpty(motionId) &&
                   modelMotionAssociations.ContainsKey(modelId) &&
                   modelMotionAssociations[modelId].Contains(motionId);        }
    }
}
