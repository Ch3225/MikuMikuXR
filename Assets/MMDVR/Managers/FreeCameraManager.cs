using UnityEngine;

namespace MMDVR.Managers
{
    public class FreeCameraManager : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public float lookSpeed = 2f;
        private Camera cam;
        private void Awake()
        {
            cam = GetComponent<Camera>();
        }
        void Update()
        {
            if (!gameObject.activeInHierarchy) return;
            // 简单 WASD 移动
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float up = 0f;
            if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) up += 1f;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) up -= 1f;
            Vector3 move = (transform.right * h + transform.forward * v + transform.up * up) * moveSpeed * Time.deltaTime;
            transform.position += move;
            // 鼠标右键旋转
            if (Input.GetMouseButton(1))
            {
                float mx = Input.GetAxis("Mouse X") * lookSpeed;
                float my = -Input.GetAxis("Mouse Y") * lookSpeed;
                transform.eulerAngles += new Vector3(my, mx, 0);
            }
        }
    }
}
