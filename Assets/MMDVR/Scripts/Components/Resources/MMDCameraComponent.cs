using UnityEngine;
using System.IO;

namespace MMDVR.Scripts.Components
{
    /// <summary>
    /// VMD帧数据结构 - 存储单个VMD摄像机帧的数据
    /// </summary>
    [System.Serializable]
    public struct VMDFrameData
    {
        public int frame;
        public float distans;
        public float Pos_x, Pos_y, Pos_z;
        public float Rot_x, Rot_y, Rot_z;
        public float viewAngle;
        public int[] Bezier;
        public bool originalframe;
    }

    /// <summary>
    /// MMD摄像机组件 - 存储和处理VMD摄像机数据
    /// 复用现有MMDCameraManager的解析逻辑
    /// </summary>
    public class MMDCameraComponent : MonoBehaviour
    {        [Header("组件标识")]
        public string cameraId;
        public string displayName;
        public string filePath;
        
          // 内部数据结构（VMD帧数据）
        private VMDFrameData[] camFrames;
        private int totalFrames = 0;
          // 公开的VMD数据访问接口
        public VMDCameraData vmdCameraData { get; private set; }
        
        public bool LoadVMDData(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"VMD file not found: {filePath}");
                return false;
            }
            
            try
            {
                // 复用MMDCameraManager的解析逻辑
                ParseVMDFile(filePath);
                
                // 初始化vmdCameraData - 转换为正确的类型
                vmdCameraData = new VMDCameraData(camFrames);
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load VMD camera data: {e.Message}");
                return false;
            }
        }
        
        public void ApplyAtTime(float timeInSeconds)
        {
            if (camFrames == null || camFrames.Length == 0)
                return;
                
            if (totalFrames <= 0) return;
            
            int frame = Mathf.Clamp(Mathf.FloorToInt(timeInSeconds * 30f), 0, totalFrames - 1); // 30fps
            ApplyCameraFrame(frame);
        }
        
        private void ApplyCameraFrame(int frame)
        {
            if (frame >= camFrames.Length) return;
            
            var data = camFrames[frame];
            // 统一缩放到与MMD模型一致（1/12.5）
            const float scale = 1f / 12.5f;
            // 这里不再赋值到成员变量，仅做本地变量处理或直接用于相机控制
            Vector3 position = new Vector3(data.Pos_x * scale, data.Pos_y * scale, (data.Pos_z + data.distans) * scale);
            Quaternion rotation = Quaternion.Euler(-data.Rot_x, -data.Rot_y, -data.Rot_z);
            float fov = data.viewAngle;
            // 可在此处直接应用到目标相机对象
        }
        
        private void ParseVMDFile(string vmdPath)
        {
            // 复用MMDCameraManager中的解析逻辑
            byte[] raw_data_org = File.ReadAllBytes(vmdPath);
            int HEADER = 50;
            int MOTIONCOUNT = 4;
            int SKINCOUNT = 4;
            int index = HEADER + MOTIONCOUNT + SKINCOUNT;
            int frameSum_int = System.BitConverter.ToInt32(raw_data_org, index);
            index += 4;
            VMDFrameData[] Cam = new VMDFrameData[frameSum_int];
            byte[] frame_data = new byte[4];
            byte[] frame_data_1byte = new byte[1];
            
            for (int i = 0; i < frameSum_int; i++)
            {
                // frame
                Cam[i].frame = System.BitConverter.ToInt32(raw_data_org, index);
                index += 4;
                // distans
                Cam[i].distans = GetVmdFloat(ref index, raw_data_org);
                // pos
                Cam[i].Pos_x = GetVmdFloat(ref index, raw_data_org);
                Cam[i].Pos_y = GetVmdFloat(ref index, raw_data_org);
                Cam[i].Pos_z = GetVmdFloat(ref index, raw_data_org);
                // rot
                Cam[i].Rot_x = GetVmdFloat(ref index, raw_data_org); ConversionAngle(ref Cam[i].Rot_x);
                Cam[i].Rot_y = GetVmdFloat(ref index, raw_data_org); ConversionAngle(ref Cam[i].Rot_y);
                Cam[i].Rot_z = GetVmdFloat(ref index, raw_data_org); ConversionAngle(ref Cam[i].Rot_z);
                // bezier
                Cam[i].Bezier = new int[24];
                for (int j = 0; j < 24; j++)
                {
                    frame_data_1byte[0] = raw_data_org[index++];
                    Cam[i].Bezier[j] = System.Convert.ToInt32(System.BitConverter.ToString(frame_data_1byte, 0), 16);
                }
                // viewAngle
                Cam[i].viewAngle = System.BitConverter.ToInt32(raw_data_org, index);
                index += 4;
                index += 1; // skip 1 byte
            }
            
            // 排序
            Qsort(ref Cam, 0, Cam.Length - 1);
            totalFrames = Cam[frameSum_int - 1].frame + 1;
            camFrames = new VMDFrameData[totalFrames];
            
            // 插值处理
            camFrames[0] = Cam[0];
            camFrames[0].originalframe = true;
            int Addframe = 0;
            int wIndex = 1;
            for (int i = 0; i < frameSum_int - 1; i++)
            {
                Addframe = Cam[i + 1].frame - Cam[i].frame;
                for (int j = 1; j < Addframe; j++)
                {
                    camFrames[wIndex].frame = wIndex;
                    camFrames[wIndex].Pos_x = Cam[i].Pos_x + (Cam[i + 1].Pos_x - Cam[i].Pos_x) * (float)j / Addframe;
                    camFrames[wIndex].Pos_y = Cam[i].Pos_y + (Cam[i + 1].Pos_y - Cam[i].Pos_y) * (float)j / Addframe;
                    camFrames[wIndex].Pos_z = Cam[i].Pos_z + (Cam[i + 1].Pos_z - Cam[i].Pos_z) * (float)j / Addframe;
                    camFrames[wIndex].Rot_x = Cam[i].Rot_x + (Cam[i + 1].Rot_x - Cam[i].Rot_x) * (float)j / Addframe;
                    camFrames[wIndex].Rot_y = Cam[i].Rot_y + (Cam[i + 1].Rot_y - Cam[i].Rot_y) * (float)j / Addframe;
                    camFrames[wIndex].Rot_z = Cam[i].Rot_z + (Cam[i + 1].Rot_z - Cam[i].Rot_z) * (float)j / Addframe;
                    camFrames[wIndex].distans = Cam[i].distans + (Cam[i + 1].distans - Cam[i].distans) * (float)j / Addframe;
                    camFrames[wIndex].viewAngle = Cam[i].viewAngle + (Cam[i + 1].viewAngle - Cam[i].viewAngle) * (float)j / Addframe;
                    wIndex++;
                }
                camFrames[wIndex] = Cam[i + 1];
                camFrames[wIndex++].originalframe = true;
            }
        }
        
        private float GetVmdFloat(ref int index, byte[] data)
        {
            byte[] raw_data = new byte[4];
            raw_data[0] = data[index++];
            raw_data[1] = data[index++];
            raw_data[2] = data[index++];
            raw_data[3] = data[index++];
            return System.BitConverter.ToSingle(raw_data, 0);
        }
        
        private void ConversionAngle(ref float rot)
        {
            rot = (float)(rot * 180 / System.Math.PI);
        }
        
        private void Qsort(ref VMDFrameData[] data, int left, int right)
        {
            if (left < right)
            {
                int i = left, j = right;
                VMDFrameData pivot = data[left];
                while (i <= j)
                {
                    while (data[i].frame < pivot.frame) i++;
                    while (data[j].frame > pivot.frame) j--;
                    if (i <= j)
                    {
                        VMDFrameData temp = data[i];
                        data[i] = data[j];
                        data[j] = temp;
                        i++; j--;
                    }
                }
                if (left < j) Qsort(ref data, left, j);
                if (i < right) Qsort(ref data, i, right);            }
        }
    }    /// <summary>
    /// VMD摄像机数据访问类
    /// </summary>
    public class VMDCameraData
    {
        private VMDFrameData[] frames;
        
        public VMDCameraData(VMDFrameData[] cameraFrames)
        {
            frames = cameraFrames;
        }
        
        public CameraState GetCameraStateAtTime(float time)
        {
            // 根据时间获取摄像机状态的逻辑
            // 这里需要实现插值计算
            
            if (frames == null || frames.Length == 0)
                return null;
                
            // 简化版本：返回第一帧的状态
            var frame = frames[0];
            return new CameraState
            {
                position = new Vector3(frame.Pos_x, frame.Pos_y, frame.Pos_z),
                rotation = Quaternion.Euler(frame.Rot_x, frame.Rot_y, frame.Rot_z),
                fieldOfView = frame.viewAngle
            };
        }
    }

    /// <summary>
    /// 摄像机状态类 - 用于VMD摄像机状态传递
    /// </summary>
    public class CameraState
    {
        public Vector3 position;
        public Quaternion rotation;        public float fieldOfView;
    }
}
