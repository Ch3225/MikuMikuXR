using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace LibMMD.Unity3D
{
    public class MmdCameraObject : MonoBehaviour
    {
        [Serializable]
        public struct CameraData
        {
            public int frame;
            public float distans;
            public float Pos_x, Pos_y, Pos_z;
            public float Rot_x, Rot_y, Rot_z;
            public float viewAngle;
            public int[] Bezier;
            public bool originalframe;
        }

        public TextAsset vmdFile; // 可选，直接拖VMD文件
        public GameObject CameraCenter; // 注视点
        public Camera MainCamera; // 相机
        public GameObject MMDModel; // 模型
        public bool Playing = false;
        public float PlaySpeed = 1f;
        public float CurrentFrame = 0f;

        private CameraData[] Cam_m;
        private int totalFrame = 0;
        private bool success = false;

        public void LoadVmd(byte[] vmdBytes)
        {
            // 解析VMD相机数据，仿照MMD_VmdCameraLoad.cs
            int HEADER = 50, MOTIONCOUNT = 4, SKINCOUNT = 4;
            int index = HEADER + MOTIONCOUNT + SKINCOUNT;
            byte[] frameSum = new byte[4];
            frameSum[0] = vmdBytes[index++];
            frameSum[1] = vmdBytes[index++];
            frameSum[2] = vmdBytes[index++];
            frameSum[3] = vmdBytes[index++];
            int frameSum_int = BitConverter.ToInt32(frameSum, 0);
            CameraData[] Cam = new CameraData[frameSum_int];
            byte[] frame_data = new byte[4];
            byte[] frame_data_1byte = new byte[1];
            for (int i = 0; i < frameSum_int; i++)
            {
                frame_data[0] = vmdBytes[index++];
                frame_data[1] = vmdBytes[index++];
                frame_data[2] = vmdBytes[index++];
                frame_data[3] = vmdBytes[index++];
                Cam[i].frame = BitConverter.ToInt32(frame_data, 0);
                Cam[i].distans = GetVmdFloat(ref index, vmdBytes);
                Cam[i].Pos_x = GetVmdFloat(ref index, vmdBytes);
                Cam[i].Pos_y = GetVmdFloat(ref index, vmdBytes);
                Cam[i].Pos_z = GetVmdFloat(ref index, vmdBytes);
                Cam[i].Rot_x = GetVmdFloat(ref index, vmdBytes); ConversionAngle(ref Cam[i].Rot_x);
                Cam[i].Rot_y = GetVmdFloat(ref index, vmdBytes); ConversionAngle(ref Cam[i].Rot_y);
                Cam[i].Rot_z = GetVmdFloat(ref index, vmdBytes); ConversionAngle(ref Cam[i].Rot_z);
                Cam[i].Bezier = new int[24];
                for (int j = 0; j < 24; j++)
                {
                    frame_data_1byte[0] = vmdBytes[index++];
                    Cam[i].Bezier[j] = Convert.ToInt32(BitConverter.ToString(frame_data_1byte, 0), 16);
                }
                frame_data[0] = vmdBytes[index++];
                frame_data[1] = vmdBytes[index++];
                frame_data[2] = vmdBytes[index++];
                frame_data[3] = vmdBytes[index++];
                Cam[i].viewAngle = BitConverter.ToInt32(frame_data, 0);
                index += 1;
            }
            Qsort(ref Cam, 0, Cam.Length - 1);
            Cam_m = new CameraData[Cam[frameSum_int - 1].frame + 1];
            totalFrame = Cam_m.Length;
            Cam_m[0] = Cam[0]; Cam_m[0].originalframe = true;
            int Addframe = 0, wIndex = 1;
            for (int i = 0; i < frameSum_int - 1; i++)
            {
                Addframe = Cam[i + 1].frame - Cam[i].frame;
                for (int j = 1; j < Addframe; j++)
                {
                    Cam_m[wIndex].frame = wIndex;
                    Cam_m[wIndex].Pos_x = Cam[i].Pos_x + (Cam[i + 1].Pos_x - Cam[i].Pos_x) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[0], Cam[i + 1].Bezier[2]), new Vector2(Cam[i + 1].Bezier[1], Cam[i + 1].Bezier[3]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].Pos_y = Cam[i].Pos_y + (Cam[i + 1].Pos_y - Cam[i].Pos_y) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[4], Cam[i + 1].Bezier[6]), new Vector2(Cam[i + 1].Bezier[5], Cam[i + 1].Bezier[7]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].Pos_z = Cam[i].Pos_z + (Cam[i + 1].Pos_z - Cam[i].Pos_z) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[8], Cam[i + 1].Bezier[10]), new Vector2(Cam[i + 1].Bezier[9], Cam[i + 1].Bezier[11]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].Rot_x = Cam[i].Rot_x + (Cam[i + 1].Rot_x - Cam[i].Rot_x) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[12], Cam[i + 1].Bezier[14]), new Vector2(Cam[i + 1].Bezier[13], Cam[i + 1].Bezier[15]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].Rot_y = Cam[i].Rot_y + (Cam[i + 1].Rot_y - Cam[i].Rot_y) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[12], Cam[i + 1].Bezier[14]), new Vector2(Cam[i + 1].Bezier[13], Cam[i + 1].Bezier[15]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].Rot_z = Cam[i].Rot_z + (Cam[i + 1].Rot_z - Cam[i].Rot_z) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[12], Cam[i + 1].Bezier[14]), new Vector2(Cam[i + 1].Bezier[13], Cam[i + 1].Bezier[15]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].distans = Cam[i].distans + (Cam[i + 1].distans - Cam[i].distans) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[16], Cam[i + 1].Bezier[18]), new Vector2(Cam[i + 1].Bezier[17], Cam[i + 1].Bezier[19]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    Cam_m[wIndex].viewAngle = Cam[i].viewAngle + (Cam[i + 1].viewAngle - Cam[i].viewAngle) * (BezierCurve(new Vector2(0, 0), new Vector2(Cam[i + 1].Bezier[20], Cam[i + 1].Bezier[22]), new Vector2(Cam[i + 1].Bezier[21], Cam[i + 1].Bezier[23]), new Vector2(127, 127), (float)j / Addframe).y) / 127;
                    wIndex++;
                }
                Cam_m[wIndex] = Cam[i + 1];
                Cam_m[wIndex++].originalframe = true;
            }
            success = true;
        }

        private void Update()
        {
            if (!success || !Playing || Cam_m == null || Cam_m.Length == 0) return;
            // 帧推进
            if (CurrentFrame < 0) CurrentFrame = 0;
            if (CurrentFrame >= totalFrame - 1) CurrentFrame = totalFrame - 2;
            int t = (int)CurrentFrame;
            float t_f = CurrentFrame - t;
            // 插值
            Vector3 pos, rot;
            float dist, fov;
            if (t + 1 < Cam_m.Length && !Cam_m[t + 1].originalframe)
            {
                pos = new Vector3(
                    Mathf.Lerp(Cam_m[t].Pos_x, Cam_m[t + 1].Pos_x, t_f) / 12.5f + (MMDModel ? MMDModel.transform.localPosition.x : 0),
                    Mathf.Lerp(Cam_m[t].Pos_y, Cam_m[t + 1].Pos_y, t_f) / 12.5f + (MMDModel ? MMDModel.transform.localPosition.y : 0),
                    Mathf.Lerp(Cam_m[t].Pos_z, Cam_m[t + 1].Pos_z, t_f) / 12.5f + (MMDModel ? MMDModel.transform.localPosition.z : 0)
                );
                rot = new Vector3(
                    Mathf.Lerp(-Cam_m[t].Rot_x, -Cam_m[t + 1].Rot_x, t_f),
                    Mathf.Lerp(-Cam_m[t].Rot_y, -Cam_m[t + 1].Rot_y, t_f),
                    Mathf.Lerp(-Cam_m[t].Rot_z, -Cam_m[t + 1].Rot_z, t_f)
                );
                dist = Mathf.Lerp(Cam_m[t].distans, Cam_m[t + 1].distans, t_f) / 12.5f;
                fov = Mathf.Lerp(Cam_m[t].viewAngle, Cam_m[t + 1].viewAngle, t_f);
            }
            else
            {
                pos = new Vector3(Cam_m[t].Pos_x / 12.5f + (MMDModel ? MMDModel.transform.localPosition.x : 0),
                                  Cam_m[t].Pos_y / 12.5f + (MMDModel ? MMDModel.transform.localPosition.y : 0),
                                  Cam_m[t].Pos_z / 12.5f + (MMDModel ? MMDModel.transform.localPosition.z : 0));
                rot = new Vector3(-Cam_m[t].Rot_x, -Cam_m[t].Rot_y, -Cam_m[t].Rot_z);
                dist = Cam_m[t].distans / 12.5f;
                fov = Cam_m[t].viewAngle;
            }
            if (CameraCenter) CameraCenter.transform.localPosition = pos;
            if (CameraCenter) CameraCenter.transform.localRotation = Quaternion.Euler(rot);
            if (MainCamera) MainCamera.transform.localPosition = new Vector3(0, 0, dist);
            if (MainCamera) MainCamera.fieldOfView = fov;
        }

        public void SetFrame(float frame)
        {
            CurrentFrame = frame;
        }
        public void Play() { Playing = true; }
        public void Pause() { Playing = false; }

        // 兼容旧接口：静态工厂
        public static GameObject CreateGameObject(string name = "MMDCameraObject")
        {
            var obj = new GameObject(name);
            var cameraObj = new GameObject("Camera");
            var cameraComponent = cameraObj.AddComponent<Camera>();
            cameraObj.transform.SetParent(obj.transform);
            var mmdCameraObject = obj.AddComponent<MmdCameraObject>();
            mmdCameraObject.CameraCenter = obj;
            mmdCameraObject.MainCamera = cameraComponent;
            return obj;
        }

        // 兼容旧接口：从路径加载VMD
        public bool LoadCameraMotion(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return false;
            byte[] vmdBytes = System.IO.File.ReadAllBytes(path);
            LoadVmd(vmdBytes);
            return success;
        }

        // 兼容旧接口：按秒定位
        public void SetPlayPos(double pos)
        {
            // 假设30fps
            float frame = (float)(pos * 30.0);
            SetFrame(frame);
        }

        // 工具方法
        float GetVmdFloat(ref int index, byte[] data)
        {
            byte[] raw_data = new byte[4];
            raw_data[0] = data[index++];
            raw_data[1] = data[index++];
            raw_data[2] = data[index++];
            raw_data[3] = data[index++];
            return BitConverter.ToSingle(raw_data, 0);
        }
        void ConversionAngle(ref float rot) { rot = (float)(rot * 180 / Math.PI); }
        void Qsort(ref CameraData[] data, int left, int right)
        {
            int i = left, j = right, pivot = data[(left + right) / 2].frame;
            CameraData tmp;
            do
            {
                while ((i < right) && (data[i].frame < pivot)) i++;
                while ((j > left) && (pivot < data[j].frame)) j--;
                if (i <= j)
                {
                    tmp = data[i]; data[i] = data[j]; data[j] = tmp; i++; j--;
                }
            } while (i <= j);
            if (left < j) Qsort(ref data, left, j);
            if (i < right) Qsort(ref data, i, right);
        }
        Vector2 BezierCurve(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, float t)
        {
            float BezierCurveX(float x1, float x2, float x3, float x4, float t1)
            {
                return Mathf.Pow(1 - t1, 3) * x1 + 3 * Mathf.Pow(1 - t1, 2) * t1 * x2 + 3 * (1 - t1) * Mathf.Pow(t1, 2) * x3 + Mathf.Pow(t1, 3) * x4;
            }
            float BezierCurveY(float y1, float y2, float y3, float y4, float t1)
            {
                return Mathf.Pow(1 - t1, 3) * y1 + 3 * Mathf.Pow(1 - t1, 2) * t1 * y2 + 3 * (1 - t1) * Mathf.Pow(t1, 2) * y3 + Mathf.Pow(t1, 3) * y4;
            }
            return new Vector2(
                BezierCurveX(p1.x, p2.x, p3.x, p4.x, t),
                BezierCurveY(p1.y, p2.y, p3.y, p4.y, t));
        }
    }
}