using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace MMDVR.Managers
{
    public class MMDCameraManager : MonoBehaviour
    {
        public static MMDCameraManager Instance { get; private set; }
        public List<string> vmdCameraPaths = new List<string>();
        public int currentIndex = -1;
        public Camera targetCamera; // 需要在Inspector中指定

        private CameraData[] camFrames;
        private int totalFrames = 0;
        private bool vmdLoaded = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        public void AddVmdCamera(string vmdPath)
        {
            if (!vmdCameraPaths.Contains(vmdPath))
            {
                vmdCameraPaths.Add(vmdPath);
                // EventManager.OnCameraListChanged?.Invoke(); // REVERTED: Let UI/CameraManager handle this
                if (vmdCameraPaths.Count == 1 && currentIndex == -1) 
                {
                    // SetActiveVmdCamera(0); 
                    // Let CameraManager handle activation logic via UI interaction or explicit calls
                }
            }
        }        public void RemoveVmdCamera(string vmdPath) // New method
        {
            Debug.Log($"MMDCameraManager.RemoveVmdCamera called with path: {vmdPath}");
            Debug.Log($"Current vmdCameraPaths count: {vmdCameraPaths.Count}");
            Debug.Log($"Current vmdCameraPaths: [{string.Join(", ", vmdCameraPaths)}]");
            
            int indexToRemove = vmdCameraPaths.IndexOf(vmdPath);
            Debug.Log($"Index to remove: {indexToRemove}");
            
            if (indexToRemove != -1)
            {
                vmdCameraPaths.RemoveAt(indexToRemove);
                Debug.Log($"After removal, vmdCameraPaths count: {vmdCameraPaths.Count}");
                Debug.Log($"After removal, vmdCameraPaths: [{string.Join(", ", vmdCameraPaths)}]");
                
                if (currentIndex == indexToRemove)
                {
                    currentIndex = -1; // Active camera removed, default to free camera behavior
                    Debug.Log("Active camera was removed, currentIndex set to -1");
                }
                else if (currentIndex > indexToRemove)
                {
                    currentIndex--; // Adjust index if a preceding item was removed
                    Debug.Log($"Adjusted currentIndex to: {currentIndex}");
                }
            }
            else
            {
                Debug.LogWarning($"VMD path not found in vmdCameraPaths: {vmdPath}");
            }
        }

        public void SetActiveVmdCamera(int index)
        {
            if (index < 0 || index >= vmdCameraPaths.Count)
            {
                // Optionally, deactivate current VMD if index is out of bounds, e.g., by setting vmdLoaded = false
                vmdLoaded = false; 
                currentIndex = -1;
                // Or, log an error, or switch to a default state
                Debug.LogWarning($"SetActiveVmdCamera called with invalid index: {index}");
                return; 
            }
            currentIndex = index;
            LoadCameraVmd(vmdCameraPaths[index]);
            // playTime = 0f; // Removed, SceneStatesManager will set initial time
        }

        public void SetTime(float timeInSeconds)
        {
            // playTime = time; // Renamed parameter to timeInSeconds
            if (!vmdLoaded || camFrames == null || camFrames.Length == 0 || targetCamera == null) return;
            
            // Ensure totalFrames is positive to prevent division by zero or negative array access
            if (totalFrames <= 0) return;

            int frame = Mathf.Clamp(Mathf.FloorToInt(timeInSeconds * 30f), 0, totalFrames - 1); // 30fps
            ApplyCameraFrame(frame);
        }

        private void ApplyCameraFrame(int frame)
        {
            var data = camFrames[frame];
            // 统一缩放到与MMD模型一致（1/12.5）
            const float scale = 1f / 12.5f;
            targetCamera.transform.position = new Vector3(data.Pos_x * scale, data.Pos_y * scale, (data.Pos_z + data.distans) * scale);
            targetCamera.transform.rotation = Quaternion.Euler(-data.Rot_x, -data.Rot_y, -data.Rot_z);
            targetCamera.fieldOfView = data.viewAngle;
        }

        public void LoadCameraVmd(string vmdPath)
        {
            if (!File.Exists(vmdPath)) return;
            byte[] raw_data_org = File.ReadAllBytes(vmdPath);
            int HEADER = 50;
            int MOTIONCOUNT = 4;
            int SKINCOUNT = 4;
            int index = HEADER + MOTIONCOUNT + SKINCOUNT;
            int frameSum_int = System.BitConverter.ToInt32(raw_data_org, index);
            index += 4;
            CameraData[] Cam = new CameraData[frameSum_int];
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
            camFrames = new CameraData[totalFrames];
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
            vmdLoaded = true;
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

        private void Qsort(ref CameraData[] data, int left, int right)
        {
            int i = left, j = right;
            int pivot = data[(left + right) / 2].frame;
            CameraData tmp;
            do
            {
                while ((i < right) && (data[i].frame < pivot)) i++;
                while ((j > left) && (pivot < data[j].frame)) j--;
                if (i <= j)
                {
                    tmp = data[i];
                    data[i] = data[j];
                    data[j] = tmp;
                    i++; j--;
                }
            } while (i <= j);
            if (left < j) Qsort(ref data, left, j);
            if (i < right) Qsort(ref data, i, right);
        }

        private struct CameraData
        {
            public int frame;
            public float distans;
            public float Pos_x, Pos_y, Pos_z;
            public float Rot_x, Rot_y, Rot_z;
            public float viewAngle;
            public int[] Bezier;
            public bool originalframe;
        }
    }
}
