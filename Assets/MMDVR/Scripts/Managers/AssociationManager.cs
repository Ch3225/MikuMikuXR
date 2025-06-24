using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MMDVR.Scripts.Managers
{
    /// <summary>
    /// 用于在Inspector中显示关联关系的数据结构
    /// </summary>
    [System.Serializable]
    public class ModelMotionAssociation
    {
        public string modelId;
        public List<string> motionIds = new List<string>();
        
        public ModelMotionAssociation(string model, IEnumerable<string> motions)
        {
            modelId = model;
            motionIds = new List<string>(motions);
        }
    }

    /// <summary>
    /// 关联管理器 - 管理模型与动作的关联关系
    /// 职责: 存储、管理、触发 <模型ID, Set<动作ID>> 关联
    /// </summary>
    public class AssociationManager : MonoBehaviour
    {
        public static AssociationManager Instance { get; private set; }

        [Header("模型-动作关联 (运行时数据)")]
        [SerializeField] private List<ModelMotionAssociation> inspectorAssociations = new List<ModelMotionAssociation>();
        
        // 实际存储的关联数据
        private Dictionary<string, HashSet<string>> modelMotionAssociations = new Dictionary<string, HashSet<string>>();

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

        // ==================== Inspector显示更新 ====================
        
        /// <summary>
        /// 更新Inspector中的关联显示
        /// </summary>
        private void UpdateInspectorDisplay()
        {
            inspectorAssociations.Clear();
            foreach (var kvp in modelMotionAssociations)
            {
                inspectorAssociations.Add(new ModelMotionAssociation(kvp.Key, kvp.Value));
            }
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
                MMDVR.Events.SceneDisplayEvents.TriggerModelMotionAssociationChanged(modelId, motionId, true);
                UpdateInspectorDisplay();
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
                MMDVR.Events.SceneDisplayEvents.TriggerModelMotionAssociationChanged(modelId, motionId, false);
                UpdateInspectorDisplay();
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
                MMDVR.Events.SceneDisplayEvents.TriggerModelMotionAssociationChanged(modelId, motionId, false);
            }
            modelMotionAssociations.Remove(modelId);
            UpdateInspectorDisplay();
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
        }        /// <summary>
        /// 清除所有关联
        /// </summary>
        public void ClearAllAssociations()
        {
            modelMotionAssociations.Clear();
            UpdateInspectorDisplay();
            Debug.Log("清除所有模型-动作关联");
        }

        /// <summary>
        /// 检查模型是否与动作关联
        /// </summary>
        public bool IsModelAssociatedWithMotion(string modelId, string motionId)
        {
            return !string.IsNullOrEmpty(modelId) && 
                   !string.IsNullOrEmpty(motionId) &&
                   modelMotionAssociations.ContainsKey(modelId) &&
                   modelMotionAssociations[modelId].Contains(motionId);
        }
    }
}
