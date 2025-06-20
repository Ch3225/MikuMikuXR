using UnityEngine;

namespace MMDVR.Scripts.Managers
{
    public class FreeCameraManager : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float lookSpeed = 2f;
        private Camera cam;
        private void Awake()
        {
            cam = GetComponent<Camera>();
        }        void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            // 简单 WASD 移动
            float h = UnityEngine.Input.GetAxis("Horizontal");
            float v = UnityEngine.Input.GetAxis("Vertical");
            float up = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.Space)) up += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)) up -= 1f;
            Vector3 move = (transform.right * h + transform.forward * v + transform.up * up) * moveSpeed * Time.deltaTime;
            transform.position += move;
            // 鼠标控制旋转
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                float mx = UnityEngine.Input.GetAxis("Mouse X") * lookSpeed;
                float my = -UnityEngine.Input.GetAxis("Mouse Y") * lookSpeed;
                transform.eulerAngles += new Vector3(my, mx, 0);
            }
            else if (UnityEngine.Input.GetMouseButton(1))
            {
                float mx = UnityEngine.Input.GetAxis("Mouse X") * lookSpeed;
                float my = -UnityEngine.Input.GetAxis("Mouse Y") * lookSpeed;
                transform.eulerAngles += new Vector3(my, mx, 0);
            }
        }
    }
}
