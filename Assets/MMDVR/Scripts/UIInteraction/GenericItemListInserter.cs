using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using MMDVR.Scripts.Managers; // for ModelData, MotionData, MusicData
using MMDVR.Scripts.Model; // 修复因重命名 Data 目录导致的命名空间错误

/// <summary>
/// 通用列表项插入器 - 在拖拽排序时显示插入预览
/// </summary>
public class GenericItemListInserter : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Settings")]
    public GameObject insertIndicatorPrefab; // 插入指示器预制体
    public Color highlightColor = Color.green;
    public float insertLineHeight = 2f;

    private GameObject currentInsertIndicator;
    private Transform listContainer;
    private bool isDraggedOver = false;

    void Start()
    {
        // 自动查找父级的列表容器
        listContainer = transform.parent;
        if (listContainer == null)
        {
            Debug.LogError("GenericItemListInserter: 无法找到列表容器");
            enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggable != null && IsValidForInsertion(draggable))
        {
            ShowInsertIndicator();
            isDraggedOver = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideInsertIndicator();
        isDraggedOver = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        DraggableItem draggable = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggable != null && IsValidForInsertion(draggable))
        {
            PerformInsertion(draggable);
        }

        HideInsertIndicator();
        isDraggedOver = false;
    }

    /// <summary>
    /// 检查拖拽项是否适合插入到此位置
    /// </summary>
    private bool IsValidForInsertion(DraggableItem draggable)
    {
        if (draggable == null || draggable.Data == null) return false;

        // 检查是否是同类型的资源且来自同一个列表
        Transform draggedItemParent = draggable.transform.parent;
        return draggedItemParent == listContainer;
    }

    /// <summary>
    /// 显示插入指示器
    /// </summary>
    private void ShowInsertIndicator()
    {
        if (currentInsertIndicator != null) return;

        if (insertIndicatorPrefab != null)
        {
            currentInsertIndicator = Instantiate(insertIndicatorPrefab, transform.position, Quaternion.identity, listContainer);
        }
        else
        {
            // 创建默认的插入指示器
            currentInsertIndicator = new GameObject("InsertIndicator");
            currentInsertIndicator.transform.SetParent(listContainer);
            currentInsertIndicator.transform.position = transform.position;

            // 添加视觉组件
            var lineRenderer = currentInsertIndicator.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            // 修正：LineRenderer 没有 color 属性，需分别设置 startColor 和 endColor
            lineRenderer.startColor = highlightColor;
            lineRenderer.endColor = highlightColor;
            lineRenderer.startWidth = insertLineHeight;
            lineRenderer.endWidth = insertLineHeight;
            lineRenderer.positionCount = 2;

            // 设置线条位置
            Vector3[] positions = new Vector3[2];
            positions[0] = transform.position + Vector3.left * 50f;
            positions[1] = transform.position + Vector3.right * 50f;
            lineRenderer.SetPositions(positions);
        }

        // 确保指示器在正确的位置
        int insertIndex = GetInsertIndex();
        currentInsertIndicator.transform.SetSiblingIndex(insertIndex);
    }

    /// <summary>
    /// 隐藏插入指示器
    /// </summary>
    private void HideInsertIndicator()
    {
        if (currentInsertIndicator != null)
        {
            DestroyImmediate(currentInsertIndicator);
            currentInsertIndicator = null;
        }
    }

    /// <summary>
    /// 执行插入操作
    /// </summary>
    private void PerformInsertion(DraggableItem draggable)
    {
        int insertIndex = GetInsertIndex();
        
        // 移动拖拽项到新位置
        draggable.transform.SetSiblingIndex(insertIndex);
        
        Debug.Log($"Inserted item {draggable.Data.DisplayName} at index {insertIndex}");

        // 通知相关的列表控制器进行数据更新
        NotifyListControllerOfReorder();
    }

    /// <summary>
    /// 获取插入位置的索引
    /// </summary>
    private int GetInsertIndex()
    {
        // 在当前位置之前插入
        return transform.GetSiblingIndex();
    }

    /// <summary>
    /// 通知列表控制器进行重新排序
    /// </summary>
    private void NotifyListControllerOfReorder()
    {
        // 根据列表容器的名称或类型来确定是哪个控制器
        if (listContainer.name.Contains("Model"))
        {
            if (ModelListController.Instance != null)
            {
                // 创建一个虚拟的GameObject来触发HandleDropOnListArea
                GameObject dummyGO = new GameObject("DummyReorderEvent");
                var dummyDraggable = dummyGO.AddComponent<DraggableItem>();
                dummyDraggable.Data = new ModelData { id = "dummy", displayName = "dummy", filePath = "" };
                
                ModelListController.Instance.HandleDropOnListArea(dummyGO);
                DestroyImmediate(dummyGO);
            }
        }
        else if (listContainer.name.Contains("Motion"))
        {
            if (MotionListController.Instance != null)
            {
                // 创建一个虚拟的GameObject来触发HandleDropOnListArea
                GameObject dummyGO = new GameObject("DummyReorderEvent");
                var dummyDraggable = dummyGO.AddComponent<DraggableItem>();
                dummyDraggable.Data = new MotionData { id = "dummy", displayName = "dummy", filePath = "", assignedActorId = "" };
                
                MotionListController.Instance.HandleDropOnListArea(dummyGO);
                DestroyImmediate(dummyGO);
            }
        }
        else if (listContainer.name.Contains("Music"))
        {
            if (MusicListController.Instance != null)
            {
                // 创建一个虚拟的GameObject来触发HandleDropOnListArea
                GameObject dummyGO = new GameObject("DummyReorderEvent");
                var dummyDraggable = dummyGO.AddComponent<DraggableItem>();
                dummyDraggable.Data = new MusicData { id = "dummy", displayName = "dummy", filePath = "" };
                
                MusicListController.Instance.HandleDropOnListArea(dummyGO);
                DestroyImmediate(dummyGO);
            }
        }
        else if (listContainer.name.Contains("Camera"))
        {
            if (CameraListController.Instance != null)
            {
                // 创建一个虚拟的GameObject来触发HandleDropOnListArea
                GameObject dummyGO = new GameObject("DummyReorderEvent");
                var dummyDraggable = dummyGO.AddComponent<DraggableItem>();
                dummyDraggable.Data = new CameraData { id = "dummy", displayName = "dummy", filePath = "", isFreeCamera = false };
                
                CameraListController.Instance.HandleDropOnListArea(dummyGO);
                DestroyImmediate(dummyGO);
            }
        }
    }
}
