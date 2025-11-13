// using UnityEngine;

// public class CameraController : MonoBehaviour
// {
//     public Transform boardTransform;

//     [Header("相机设置")]
//     public float distance = 15f;         // ✅ 增加距离
//     public float height = 12f;           // ✅ 增加高度
//     public float angle = 45f;            // ✅ 调整角度
//     public float fieldOfView = 60f;      // ✅ 视野角度

//     [Header("调试选项")]
//     public bool showDebugInfo = true;

//     private bool isWhitePerspective = true;

//     void Start()
//     {
//         if (boardTransform == null)
//         {
//             GameObject board = GameObject.Find("Board");
//             if (board != null)
//             {
//                 boardTransform = board.transform;
//                 Debug.Log($"✅ 找到 Board 对象，位置: {board.transform.position}");
//             }
//             else
//             {
//                 Debug.LogError("❌ 找不到 Board 对象！");
//                 return;
//             }
//         }

//         Camera cam = GetComponent<Camera>();
//         if (cam != null)
//         {
//             cam.fieldOfView = fieldOfView;
//         }

//         WhitePerspective();
//     }

//     void Update()
//     {
//         // 按 Space 键切换视角
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             if (isWhitePerspective)
//             {
//                 BlackPerspective();
//             }
//             else
//             {
//                 WhitePerspective();
//             }
//         }
//     }

//     public void BlackPerspective()
//     {
//         if (boardTransform == null) return;

//         isWhitePerspective = false;

//         // ✅ 棋盘中心：(3.5, 0, 3.5)
//         Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

//         // ✅ 从黑方视角看（Z轴正方向）
//         float radAngle = angle * Mathf.Deg2Rad;
//         float offsetZ = distance * Mathf.Cos(radAngle);
//         float offsetY = distance * Mathf.Sin(radAngle);

//         Vector3 cameraPosition = boardCenter + new Vector3(0f, offsetY, offsetZ);
//         transform.position = cameraPosition;
//         transform.LookAt(boardCenter);

//         if (showDebugInfo)
//         {
//             Debug.Log($"🖤 黑方视角 | 相机位置: {transform.position}");
//         }
//     }

//     public void WhitePerspective()
//     {
//         if (boardTransform == null) return;

//         isWhitePerspective = true;

//         // ✅ 棋盘中心：(3.5, 0, 3.5)
//         Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

//         // ✅ 从白方视角看（Z轴负方向）
//         float radAngle = angle * Mathf.Deg2Rad;
//         float offsetZ = distance * Mathf.Cos(radAngle);
//         float offsetY = distance * Mathf.Sin(radAngle);

//         Vector3 cameraPosition = boardCenter + new Vector3(0f, offsetY, -offsetZ);
//         transform.position = cameraPosition;
//         transform.LookAt(boardCenter);

//         if (showDebugInfo)
//         {
//             Debug.Log($"🤍 白方视角 | 相机位置: {transform.position}");
//         }
//     }

//     void OnDrawGizmos()
//     {
//         if (boardTransform == null) return;

//         Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

//         // 绘制棋盘中心
//         Gizmos.color = Color.red;
//         Gizmos.DrawSphere(boardCenter, 0.5f);

//         // 绘制相机到棋盘中心的连线
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawLine(transform.position, boardCenter);

//         // 绘制棋盘边界
//         Gizmos.color = Color.green;
//         Vector3 p1 = boardTransform.position;
//         Vector3 p2 = boardTransform.position + new Vector3(7f, 0f, 0f);
//         Vector3 p3 = boardTransform.position + new Vector3(7f, 0f, 7f);
//         Vector3 p4 = boardTransform.position + new Vector3(0f, 0f, 7f);

//         Gizmos.DrawLine(p1, p2);
//         Gizmos.DrawLine(p2, p3);
//         Gizmos.DrawLine(p3, p4);
//         Gizmos.DrawLine(p4, p1);
//     }
// }


using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform boardTransform;

    [Header("初始相机设置")]
    public float distance = 15f;          // 初始距离
    public float angle = 45f;             // 初始俯视角
    public float fieldOfView = 60f;       // 视野角度

    [Header("相机控制设置")]
    public float orbitSpeed = 120f;       // 旋转速度（右键拖动）
    public float zoomSpeed = 10f;         // 缩放速度（滚轮）
    public float minDistance = 10f;       // 离棋盘最近距离
    public float maxDistance = 25f;       // 离棋盘最远距离
    public float minPitch = 20f;          // 最小俯角（防止太平，看不到棋盘）
    public float maxPitch = 80f;          // 最大俯角（防止太垂直）
    public float minHeightAboveBoard = 1.0f; // 相机最低高度 = 棋盘高度 + 这个值

    [Header("调试选项")]
    public bool showDebugInfo = true;

    private bool isWhitePerspective = true;
    private float currentYaw;       // 绕棋盘水平方向角度
    private float currentPitch;     // 俯视角
    private float currentDistance;  // 当前半径（与棋盘中心距离）

    private Camera cam;

    void Start()
    {
        if (boardTransform == null)
        {
            GameObject board = GameObject.Find("Board");
            if (board != null)
            {
                boardTransform = board.transform;
                Debug.Log($"✅ 找到 Board 对象，位置: {board.transform.position}");
            }
            else
            {
                Debug.LogError("❌ 找不到 Board 对象！");
                enabled = false;
                return;
            }
        }

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }

        // 初始化相机参数
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        currentPitch    = Mathf.Clamp(angle,    minPitch,    maxPitch);

        // 默认从白方视角开始
        WhitePerspective();
        ApplyCameraTransform();
    }

    void Update()
    {
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
        if (boardTransform == null) return;

        // 右键拖动旋转
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw   += mouseX * orbitSpeed * Time.deltaTime;
            currentPitch -= mouseY * orbitSpeed * Time.deltaTime; // 鼠标向上 -> 抬高相机

            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // 滚轮缩放
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    /// <summary>
    /// 给白方视角用的公开方法（兼容 LogicManager）
    /// </summary>
    public void WhitePerspective()
    {
        if (boardTransform == null) return;

        isWhitePerspective = true;
        currentYaw = 180f; // 从 Z 负方向看向棋盘中心
        currentPitch = Mathf.Clamp(angle, minPitch, maxPitch);
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);

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
        currentYaw = 0f; // 从 Z 正方向看向棋盘中心
        currentPitch = Mathf.Clamp(angle, minPitch, maxPitch);
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);

        if (showDebugInfo)
            Debug.Log("🖤 重置到黑方视角");
    }

    /// <summary>
    /// 根据 currentYaw/currentPitch/currentDistance 计算并应用相机位置
    /// </summary>
    void ApplyCameraTransform()
    {
        if (boardTransform == null) return;

        // 棋盘中心：(3.5, 0, 3.5)
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
        Gizmos.DrawSphere(boardCenter, 0.5f);

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
