using System.Collections.Generic;
using UnityEngine;

namespace MMDVR.Managers
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("场景中所有相机（Cameras）")]
        public List<GameObject> cameras = new List<GameObject>();

        [Header("自由相机（Free Camera）")]
        public GameObject freeCamera;
        [Header("MMD相机（MMD Camera）")]
        public GameObject mmdCamera;

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
        }

        public void ActivateCamera(int index)
        {
            // index==0: FreeCamera，index>0: MMDCamera from UI list perspective
            // The UI list has Free Camera at index 0, then VMDs.
            // So, if UI sends index 0, it's Free Camera.
            // If UI sends index > 0, it's a VMD camera. We need to map this UI index
            // to the MMDCameraManager's vmdCameraPaths index.

            if (freeCamera != null)
                freeCamera.SetActive(index == 0);
            if (mmdCamera != null)
                mmdCamera.SetActive(index > 0);

            if (MMDCameraManager.Instance != null)
            {
                if (index == 0) // Free Camera selected
                {
                    MMDCameraManager.Instance.currentIndex = -1; // Indicate no VMD is active
                    MMDCameraManager.Instance.Pause(); // Stop MMD camera playback if it was running
                }
                else // MMD Camera selected from UI
                {
                    // The 'index' from CameraListController is 1-based for VMDs (0 is FreeCam).
                    // MMDCameraManager.vmdCameraPaths is 0-based for VMDs.
                    int mmdManagerIndex = index - 1; 
                    if (mmdManagerIndex >= 0 && mmdManagerIndex < MMDCameraManager.Instance.vmdCameraPaths.Count)
                    {
                        MMDCameraManager.Instance.SetActiveVmdCamera(mmdManagerIndex);
                        // MMDCameraManager.Instance.Play(); // Optionally auto-play, or let playback controls handle this
                    }
                    else
                    {
                        Debug.LogWarning($"CameraManager: Invalid MMD camera index {mmdManagerIndex} from UI index {index}. Activating Free Camera instead.");
                        ActivateCamera(0); // Fallback to Free Camera
                    }
                }
            }
            EventManager.OnCameraListChanged?.Invoke(); // Notify UI to update visuals if needed
        }

        // 加载MMD相机（VMD），添加到MMDCameraManager
        public void LoadMmdCamera(string vmdPath)
        {
            MMDCameraManager.Instance?.AddVmdCamera(vmdPath);
            EventManager.OnCameraListChanged?.Invoke();
            ActivateCamera(1); // 激活MMD相机
        }
    }
}