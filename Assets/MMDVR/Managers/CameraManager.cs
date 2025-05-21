using System.Collections.Generic;
using UnityEngine;

namespace MMDVR.Managers
{    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("场景中所有相机（Cameras）")]
        public List<GameObject> cameras = new List<GameObject>();

        [Header("自由相机（Free Camera）")]
        public GameObject freeCamera;
        [Header("MMD相机（MMD Camera）")]
        public GameObject mmdCamera;
        [Header("VR相机（VR Camera）")]
        public GameObject vrCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 启动时激活Free Camera，其余禁用
            ActivateCamera(0);
        }

        public void AddCamera(GameObject camera)
        {
            if (!cameras.Contains(camera)) cameras.Add(camera);
        }
        public void RemoveCamera(GameObject camera)
        {
            if (cameras.Contains(camera)) cameras.Remove(camera);
        }
        public GameObject GetCamera(int index)
        {
            if (index < 0 || index >= cameras.Count) return null;
            return cameras[index];
        }        public void ActivateCamera(int index)
        {
            // index==0: FreeCamera，index==1: MMDCamera，index==2: VRCamera
            if (freeCamera != null)
                freeCamera.SetActive(index == 0);
            if (mmdCamera != null)
                mmdCamera.SetActive(index == 1);
            if (vrCamera != null)
                vrCamera.SetActive(index == 2);
            
            // 如果启用了VR相机，通知VR系统
            if (index == 2 && vrCamera != null && MMDVR.VR.VRCameraController.Instance != null)
            {
                MMDVR.VR.VRCameraController.Instance.InitializeVRCamera();
            }
        }

        // 加载MMD相机（VMD），添加到MMDCameraManager
        public void LoadMmdCamera(string vmdPath)
        {
            MMDCameraManager.Instance?.AddVmdCamera(vmdPath);
            EventManager.OnCameraListChanged?.Invoke();
        }
    }
}