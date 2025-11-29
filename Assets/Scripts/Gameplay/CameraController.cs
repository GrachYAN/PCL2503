using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform boardTransform;

    [Header("初始相机设置")]
    [Tooltip("默认与棋盘中心的距离")]
    public float distance = 10f;
    [Tooltip("默认俯视角（度）")]
    public float angle = 15f;
    [Tooltip("相机视野角度 FOV")]
    public float fieldOfView = 60f;

    [Header("相机控制设置")]
    [Tooltip("右键拖动时的旋转速度")]
    public float orbitSpeed = 120f;
    [Tooltip("滚轮缩放速度")]
    public float zoomSpeed = 12f;
    [Tooltip("最小距离（控制能放多近）")]
    public float minDistance = 5f;
    [Tooltip("最大距离（控制能拉多远）")]
    public float maxDistance = 25f;
    [Tooltip("最小俯角，越小越接近平视")]
    public float minPitch = 5f;
    [Tooltip("最大俯角，越大越垂直")]
    public float maxPitch = 50f;
    [Tooltip("相机最低高度(相对于棋盘中心)")]
    public float minHeightAboveBoard = 0.1f;

    [Header("调试选项")]
    public bool showDebugInfo = true;

    // 当前相机状态（内部使用）
    private float currentYaw;         // 绕棋盘旋转角度（水平）
    private float currentPitch;       // 俯视角（垂直）
    private float currentDistance;    // 与棋盘中心的距离

    private bool isWhitePerspective = true;
    private Camera cam;

    void Start()
    {
        if (boardTransform == null)
        {
            Debug.LogError("CameraController: boardTransform 未设置！");
            enabled = false;
            return;
        }

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }

        // 初始化为当前阵营的默认视角
        if (isWhitePerspective)
        {
            currentYaw = 180f;    // 从 Z 负方向看向棋盘
        }
        else
        {
            currentYaw = 0f;      // 从 Z 正方向看向棋盘
        }

        currentPitch = Mathf.Clamp(angle, minPitch, maxPitch);
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        if (showDebugInfo)
        {
            Debug.Log($"Camera Start | yaw={currentYaw}, pitch={currentPitch}, dist={currentDistance}");
        }

        ApplyCameraTransform();
    }

    void Update()
    {
        // 1. 如果时间暂停，禁止操作
        if (Time.timeScale == 0f) return;

        if (boardTransform == null) return;

        HandlePerspectiveToggle();
        HandleOrbitAndZoomInput();
        ApplyCameraTransform();
    }

    /// <summary>
    /// Space 键切换白/黑方默认视角（重置视角）
    /// </summary>
    void HandlePerspectiveToggle()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isWhitePerspective)
                BlackPerspective();
            else
                WhitePerspective();
        }
    }

    /// <summary>
    /// 鼠标右键拖动旋转 + 滚轮缩放
    /// </summary>
    void HandleOrbitAndZoomInput()
    {
        // 右键拖动旋转
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw   += mouseX * orbitSpeed * Time.deltaTime;
            currentPitch -= mouseY * orbitSpeed * 0.5f * Time.deltaTime;
            currentPitch  = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance  = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    /// <summary>
    /// 给白方视角用的公开方法（兼容 LogicManager）
    /// </summary>
    public void WhitePerspective()
    {
        if (boardTransform == null) return;

        isWhitePerspective = true;
        currentYaw         = 180f; // 从 Z 负方向看向棋盘中心
        currentPitch       = Mathf.Clamp(angle, minPitch, maxPitch);
        currentDistance    = Mathf.Clamp(distance, minDistance, maxDistance);

        if (showDebugInfo)
            Debug.Log("🤍 重置到白方视角");
    }

    /// <summary>
    /// 给黑方视角用的公开方法（兼容 LogicManager）
    /// </summary>
    public void BlackPerspective()
    {
        if (boardTransform == null) return;

        isWhitePerspective = false;
        currentYaw         = 0f; // 从 Z 正方向看向棋盘中心
        currentPitch       = Mathf.Clamp(angle, minPitch, maxPitch);
        currentDistance    = Mathf.Clamp(distance, minDistance, maxDistance);

        if (showDebugInfo)
            Debug.Log("🖤 重置到黑方视角");
    }

    /// <summary>
    /// 根据 currentYaw/currentPitch/currentDistance 计算并应用相机位置
    /// </summary>
    void ApplyCameraTransform()
    {
        if (boardTransform == null) return;

        // 棋盘中心：(3.5, 0, 3.5) —— 以 0~7 的格子为例
        Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

        float yawRad   = currentYaw   * Mathf.Deg2Rad;
        float pitchRad = currentPitch * Mathf.Deg2Rad;

        float cosPitch = Mathf.Cos(pitchRad);

        Vector3 offset;
        offset.x = currentDistance * Mathf.Sin(yawRad) * cosPitch;
        offset.z = currentDistance * Mathf.Cos(yawRad) * cosPitch;
        offset.y = currentDistance * Mathf.Sin(pitchRad);

        Vector3 desiredPosition = boardCenter + offset;

        // 防止相机低于棋盘
        float minY = boardCenter.y + minHeightAboveBoard;
        if (desiredPosition.y < minY)
        {
            desiredPosition.y = minY;
        }

        transform.position = desiredPosition;
        transform.LookAt(boardCenter);
    }

    void OnDrawGizmos()
    {
        if (boardTransform == null) return;

        Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

        // 绘制棋盘中心
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(boardCenter, 0.2f);

        // 绘制相机到棋盘中心的连线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, boardCenter);

        // 绘制棋盘边界 (0,0) ~ (7,7)
        Gizmos.color = Color.green;
        Vector3 p1 = boardTransform.position;
        Vector3 p2 = boardTransform.position + new Vector3(7f, 0f, 0f);
        Vector3 p3 = boardTransform.position + new Vector3(7f, 0f, 7f);
        Vector3 p4 = boardTransform.position + new Vector3(0f, 0f, 7f);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
}
