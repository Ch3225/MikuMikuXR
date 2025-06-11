using System.Collections.Generic;
using UnityEngine;
using MMDVR.Scripts.UIInteraction; // Added this using directive
using UICameraData = MMDVR.Scripts.UIInteraction.CameraData; // Added alias

namespace MMDVR.Managers
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [Header("场景中所有相机（Cameras）")]
        public List<GameObject> cameras = new List<GameObject>(); // This list seems unused if Free and MMD are specific GameObjects

        [Header("自由相机（Free Camera）")]
        public GameObject freeCamera;
        [Header("MMD相机（MMD Camera）")]
        public GameObject mmdCamera;

        private UICameraData _activeCameraData; // To store the currently active camera data // Changed

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
            // Start by activating the default Free Camera resource
            ActivateCameraByResource(new UICameraData { id = "BUILTIN_FREE_CAMERA", displayName = "Free Camera", filePath = null, isFreeCamera = true }); // Changed
        }

        // public void AddCamera(GameObject camera) // Potentially unused
        // {
        //     if (!cameras.Contains(camera)) cameras.Add(camera);
        // }
        // public void RemoveCamera(GameObject camera) // Potentially unused
        // {
        //     if (cameras.Contains(camera)) cameras.Remove(camera);
        // }
        // public GameObject GetCamera(int index) // Potentially unused
        // {
        //     if (index < 0 || index >= cameras.Count) return null;
        //     return cameras[index];
        // }

        // New method to activate camera based on CameraData object
        public void ActivateCameraByResource(UICameraData camDataToActivate) // Changed
        {
            if (camDataToActivate == null) // Null means activate Free Camera by default
            {
                camDataToActivate = new UICameraData { id = "BUILTIN_FREE_CAMERA", displayName = "Free Camera", filePath = null, isFreeCamera = true }; // Changed
            }

            _activeCameraData = camDataToActivate; // Store active camera data

            if (camDataToActivate.isFreeCamera)
            {
                if (freeCamera != null) freeCamera.SetActive(true);
                if (mmdCamera != null) mmdCamera.SetActive(false);
                if (MMDCameraManager.Instance != null)
                {
                    MMDCameraManager.Instance.currentIndex = -1; // Signal MMD manager that no VMD is active
                    // MMDCameraManager.Instance.Pause(); // Removed
                }
                Debug.Log($"CameraManager: Activated Free Camera: {camDataToActivate.DisplayName}");
            }
            else // It's a VMD camera
            {
                if (freeCamera != null) freeCamera.SetActive(false);
                if (mmdCamera != null) mmdCamera.SetActive(true);
                if (MMDCameraManager.Instance != null)
                {
                    int mmdManagerIndex = MMDCameraManager.Instance.vmdCameraPaths.IndexOf(camDataToActivate.FilePath);
                    if (mmdManagerIndex != -1)
                    {
                        MMDCameraManager.Instance.SetActiveVmdCamera(mmdManagerIndex); // This sets currentIndex internally
                        Debug.Log($"CameraManager: Activated MMD Camera: {camDataToActivate.DisplayName} (Path: {camDataToActivate.FilePath}, MMD Index: {mmdManagerIndex})");
                    }
                    else
                    {
                        Debug.LogError($"CameraManager: VMD path {camDataToActivate.FilePath} not found in MMDCameraManager. Activating Free Camera as fallback.");
                        _activeCameraData = new UICameraData { id = "BUILTIN_FREE_CAMERA", displayName = "Free Camera", filePath = null, isFreeCamera = true }; // Fallback active data // Changed
                        if (freeCamera != null) freeCamera.SetActive(true);
                        if (mmdCamera != null) mmdCamera.SetActive(false);
                        MMDCameraManager.Instance.currentIndex = -1;
                        // MMDCameraManager.Instance.Pause(); // Removed
                    }
                }
            }
            // EventManager.OnCameraListChanged?.Invoke(); // Notify UI to update visuals - Replaced by OnCameraActivated
            EventManager.OnCameraActivated?.Invoke(_activeCameraData); // Notify UI about activation
        }

        // Deprecated: ActivateCamera(int uiListIndex) - use ActivateCameraByResource instead
        // public void ActivateCamera(int uiListIndex) { ... }

        public UICameraData GetActiveCameraData() // New method for UI to query active camera // Changed
        {
            return _activeCameraData;
        }

        // 加载MMD相机（VMD），添加到MMDCameraManager
        public void LoadMmdCamera(string vmdPath)
        {
            if (MMDCameraManager.Instance != null && !string.IsNullOrEmpty(vmdPath))
            {
                MMDCameraManager.Instance.AddVmdCamera(vmdPath); // Add to MMD Manager's list

                // Create CameraData for the newly loaded VMD
                UICameraData newVmdCamData = new UICameraData // Changed
                {
                    id = vmdPath, // Or generate a more unique ID if necessary
                    displayName = System.IO.Path.GetFileNameWithoutExtension(vmdPath),
                    filePath = vmdPath,
                    isFreeCamera = false
                };

                // Optionally, activate the newly loaded camera
                // ActivateCameraByResource(newVmdCamData); 
                // Or let the UI decide. For now, just adding and invoking event.
                
                EventManager.OnCameraListChanged?.Invoke(); // Notify UI to refresh
            }
        }
    }
}