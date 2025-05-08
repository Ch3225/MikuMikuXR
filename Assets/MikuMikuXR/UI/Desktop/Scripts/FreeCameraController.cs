using UnityEngine;

/// <summary>
/// 自由摄像机控制器，支持WASD移动，空格上升，左Shift下降，鼠标右键拖动旋转视角。
/// </summary>
public class FreeCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float fastMoveMultiplier = 2f;
    public float mouseSensitivity = 2f;
    public float upDownSpeed = 3f;

    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isRightMouseHeld = false;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }

    void Update()
    {
        // 鼠标右键控制视角旋转
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseHeld = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRightMouseHeld = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (isRightMouseHeld)
        {
            rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotationY = Mathf.Clamp(rotationY, -89f, 89f);
            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }

        // 移动
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftControl) ? fastMoveMultiplier : 1f);
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.Space)) move += transform.up;
        if (Input.GetKey(KeyCode.LeftShift)) move -= transform.up;
        transform.position += move.normalized * speed * Time.deltaTime;
    }
}
