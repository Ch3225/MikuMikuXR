using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MMDVR.Scripts.UIInteraction
{
    /// <summary>
    /// UI连线组件，基于Unity UI Graphics系统绘制连线
    /// </summary>
    public class ConnectionLine : Graphic
    {
        [Header("连线配置")]
        public float lineWidth = 10f;
        public Color lineColor = Color.magenta;
        
        [Header("连线端点")]
        public RectTransform startPoint;
        public RectTransform endPoint;
        
        [Header("连线数据")]
        public string modelId;
        public string motionId;
        
        private Vector2 lastStartPos;
        private Vector2 lastEndPos;
        private bool needsUpdate = true;
        private Vector2 _localStart, _localEnd;
        
        protected override void Start()
        {
            base.Start();
            color = lineColor;
            raycastTarget = false; // 确保不会阻挡UI交互
        }
        
        void Update()
        {
            // 只在位置发生变化时才更新
            if (startPoint != null && endPoint != null)
            {
                Vector2 currentStartPos = GetUIPosition(startPoint);
                Vector2 currentEndPos = GetUIPosition(endPoint);
                
                // 检查位置是否发生变化
                if (Vector2.Distance(currentStartPos, lastStartPos) > 1f || 
                    Vector2.Distance(currentEndPos, lastEndPos) > 1f)
                {
                    lastStartPos = currentStartPos;
                    lastEndPos = currentEndPos;
                    needsUpdate = true;
                }
                
                if (needsUpdate)
                {
                    UpdateRectTransform();
                    SetVerticesDirty();
                    needsUpdate = false;
                }
            }
        }
        
        /// <summary>
        /// 设置连线的起点和终点
        /// </summary>
        public void SetPoints(RectTransform start, RectTransform end, string modelId, string motionId)
        {
            this.startPoint = start;
            this.endPoint = end;
            this.modelId = modelId;
            this.motionId = motionId;
            
            // 初始化位置记录
            if (start != null) lastStartPos = GetUIPosition(start);
            if (end != null) lastEndPos = GetUIPosition(end);
            
            needsUpdate = true;
            UpdateRectTransform();
            SetVerticesDirty();
        }
        
        /// <summary>
        /// 获取UI元素在Canvas中的位置
        /// </summary>
        private Vector2 GetUIPosition(RectTransform target)
        {
            if (target == null) return Vector2.zero;
            
            // 直接获取相对于Canvas的位置
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform, 
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, target.position), 
                canvas.worldCamera, 
                out localPos);
            return localPos;
        }
        
        private RectTransform GetLayerRect()
        {
            // ConnectionLine的父物体就是ConnectionLayer
            return transform.parent as RectTransform;
        }
        
        private Vector2 GetLocalPosInLayer(RectTransform target)
        {
            var layer = GetLayerRect();
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPos, null, out localPos);
            return localPos;
        }
        
        private void UpdateRectTransform()
        {
            if (startPoint == null || endPoint == null) return;
            var layer = GetLayerRect();
            Vector2 start = GetLocalPosInLayer(startPoint);
            Vector2 end = GetLocalPosInLayer(endPoint);
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            Vector2 center = (min + max) * 0.5f;
            Vector2 size = max - min + Vector2.one * lineWidth * 2;
            rectTransform.anchoredPosition = center;
            rectTransform.sizeDelta = size;
            _localStart = start - center;
            _localEnd = end - center;
        }        
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (startPoint == null || endPoint == null) return;
            Vector2 start = _localStart;
            Vector2 end = _localEnd;
            if (Vector2.Distance(start, end) < 1f) return;
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * lineWidth * 0.5f;
            Vector2 v1 = start + perpendicular;
            Vector2 v2 = start - perpendicular;
            Vector2 v3 = end - perpendicular;
            Vector2 v4 = end + perpendicular;
            Color32 lineColor32 = new Color32(255, 0, 255, 255);
            vh.AddVert(v1, lineColor32, Vector2.zero);
            vh.AddVert(v2, lineColor32, Vector2.zero);
            vh.AddVert(v3, lineColor32, Vector2.zero);
            vh.AddVert(v4, lineColor32, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
        
        /// <summary>
        /// 检查连线是否有效
        /// </summary>
        public bool IsValid()
        {
            return startPoint != null && endPoint != null && 
                   !string.IsNullOrEmpty(modelId) && !string.IsNullOrEmpty(motionId);
        }
    }
}