using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MMDVR.Scripts.Events;
using MMDVR.Events; // 添加对ResourceEvents的引用
using MMDVR.Scripts.Managers; // 添加对EventManager的引用
using MMDVR.Scripts.Model; // 添加对CameraData的引用
using MMDVR.Scripts.Components; // 添加对MusicComponent的引用

namespace MMDVR.Scripts.UIInteraction.ResourceManagement
{    /// <summary>
    /// 主控制面板管理器 - 管理整个UI面板的布局刷新
    /// 当任何列表发生变化时，自动刷新布局
    /// </summary>
    public class MainControlPanelManager : MonoBehaviour
    {
        public static MainControlPanelManager Instance { get; private set; }
        
        [Header("自动布局刷新设置")]
        [SerializeField] public bool enableAutoRefresh = true; // 改为public以便测试
        [SerializeField] private float refreshDelay = 0.1f; // 延迟刷新，避免频繁刷新
        
        private bool refreshScheduled = false;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // 订阅所有资源列表变化事件
            ResourceEvents.OnModelListChanged += ScheduleLayoutRefresh;
            ResourceEvents.OnMotionListChanged += ScheduleLayoutRefresh;
            ResourceEvents.OnCameraListChanged += ScheduleLayoutRefresh;
            ResourceEvents.OnMusicListChanged += ScheduleLayoutRefresh;
            
            // 订阅场景显示事件
            SceneDisplayEvents.OnActorListChanged += ScheduleLayoutRefresh;
            SceneDisplayEvents.OnModelMotionAssociationChanged += ScheduleLayoutRefresh;
            
            // 订阅EventManager的事件（兼容现有系统）
            EventManager.OnActorListChanged += ScheduleLayoutRefresh;
            EventManager.OnMotionListChanged += ScheduleLayoutRefresh;
            EventManager.OnCameraListChanged += ScheduleLayoutRefresh;
            EventManager.OnMusicListChanged += ScheduleLayoutRefresh;
            EventManager.OnCameraActivated += ScheduleLayoutRefresh;
            EventManager.OnMusicActivated += ScheduleLayoutRefresh;
            
            Debug.Log("MainControlPanelManager: 已注册所有布局刷新事件");
        }        void OnDestroy()
        {
            // 取消订阅事件
            ResourceEvents.OnModelListChanged -= ScheduleLayoutRefresh;
            ResourceEvents.OnMotionListChanged -= ScheduleLayoutRefresh;
            ResourceEvents.OnCameraListChanged -= ScheduleLayoutRefresh;
            ResourceEvents.OnMusicListChanged -= ScheduleLayoutRefresh;
            
            SceneDisplayEvents.OnActorListChanged -= ScheduleLayoutRefresh;
            SceneDisplayEvents.OnModelMotionAssociationChanged -= ScheduleLayoutRefresh;
            
            // 取消订阅EventManager的事件
            EventManager.OnActorListChanged -= ScheduleLayoutRefresh;
            EventManager.OnMotionListChanged -= ScheduleLayoutRefresh;
            EventManager.OnCameraListChanged -= ScheduleLayoutRefresh;
            EventManager.OnMusicListChanged -= ScheduleLayoutRefresh;
            EventManager.OnCameraActivated -= ScheduleLayoutRefresh;
            EventManager.OnMusicActivated -= ScheduleLayoutRefresh;
        }        /// <summary>
        /// 计划布局刷新（延迟执行，避免频繁刷新）
        /// </summary>
        private void ScheduleLayoutRefresh()
        {
            if (!enableAutoRefresh || refreshScheduled)
                return;

            // 检查GameObject是否激活，如果未激活则直接执行同步刷新
            if (!gameObject.activeInHierarchy)
            {
                Debug.Log("MainControlPanelManager: GameObject未激活，执行同步布局刷新");
                RefreshAllLayouts();
                return;
            }

            refreshScheduled = true;
            StartCoroutine(DelayedLayoutRefresh());
        }

        /// <summary>
        /// 适配带参数的事件（忽略参数，只触发刷新）
        /// </summary>
        private void ScheduleLayoutRefresh(string param1, string param2, bool param3)
        {
            ScheduleLayoutRefresh();
        }        /// <summary>
        /// 适配带参数的事件（忽略参数，只触发刷新）
        /// </summary>
        private void ScheduleLayoutRefresh(string param1)
        {
            ScheduleLayoutRefresh();
        }

        /// <summary>
        /// 适配CameraData参数的事件（忽略参数，只触发刷新）
        /// </summary>
        private void ScheduleLayoutRefresh(CameraData cameraData)
        {
            ScheduleLayoutRefresh();
        }

        /// <summary>
        /// 适配MusicComponent参数的事件（忽略参数，只触发刷新）
        /// </summary>
        private void ScheduleLayoutRefresh(MusicComponent musicComponent)
        {
            ScheduleLayoutRefresh();
        }

        /// <summary>
        /// 延迟布局刷新协程
        /// </summary>
        private IEnumerator DelayedLayoutRefresh()
        {
            yield return new WaitForSeconds(refreshDelay);
            RefreshAllLayouts();
            refreshScheduled = false;
        }

        /// <summary>
        /// 立即刷新所有布局
        /// </summary>
        public void RefreshAllLayouts()
        {
            Debug.Log("MainControlPanelManager: 开始刷新所有布局");
            
            // 刷新所有子对象的布局组件
            RefreshLayoutGroupsRecursively(transform);
            
            // 强制Canvas立即更新
            Canvas.ForceUpdateCanvases();
            
            Debug.Log("MainControlPanelManager: 布局刷新完成");
        }

        /// <summary>
        /// 递归刷新所有布局组件
        /// </summary>
        private void RefreshLayoutGroupsRecursively(Transform parent)
        {
            // 刷新当前对象的布局组件
            RefreshLayoutComponents(parent);
            
            // 递归刷新所有子对象
            for (int i = 0; i < parent.childCount; i++)
            {
                RefreshLayoutGroupsRecursively(parent.GetChild(i));
            }
        }

        /// <summary>
        /// 刷新单个GameObject的布局组件
        /// </summary>
        private void RefreshLayoutComponents(Transform target)
        {
            GameObject obj = target.gameObject;
            
            // 刷新LayoutGroup组件
            LayoutGroup layoutGroup = obj.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            }
            
            // 刷新ContentSizeFitter组件
            ContentSizeFitter sizeFitter = obj.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(sizeFitter.GetComponent<RectTransform>());
            }
            
            // 刷新GridLayoutGroup组件
            GridLayoutGroup gridLayout = obj.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridLayout.GetComponent<RectTransform>());
            }
        }

        /// <summary>
        /// 静态方法：尝试触发全局布局刷新（处理实例可能未激活的情况）
        /// </summary>
        public static void TriggerGlobalLayoutRefresh()
        {
            if (Instance != null)
            {
                if (Instance.gameObject.activeInHierarchy)
                {
                    Instance.ScheduleLayoutRefresh();
                }
                else
                {
                    // 如果MainControlPanelManager未激活，直接执行同步刷新
                    Debug.Log("MainControlPanelManager: 实例未激活，执行静态同步刷新");
                    Instance.RefreshAllLayouts();
                }
            }
            else
            {
                Debug.LogWarning("MainControlPanelManager: 实例不存在，无法执行布局刷新");
            }
        }

        /// <summary>
        /// 手动触发布局刷新（用于拖拽操作完成后立即刷新）
        /// </summary>
        public void TriggerImmediateRefresh()
        {
            if (!enableAutoRefresh) return;
            
            Debug.Log("MainControlPanelManager: 手动触发立即刷新");
            RefreshAllLayouts();
        }

        /// <summary>
        /// 手动触发布局刷新（用于测试或特殊情况）
        /// </summary>
        [ContextMenu("Force Refresh All Layouts")]
        public void ForceRefreshAllLayouts()
        {
            RefreshAllLayouts();
        }
    }
}
