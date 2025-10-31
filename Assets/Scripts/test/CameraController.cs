using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform boardTransform;

    [Header("相机设置")]
    public float distance = 15f;         // ✅ 增加距离
    public float height = 12f;           // ✅ 增加高度
    public float angle = 45f;            // ✅ 调整角度
    public float fieldOfView = 60f;      // ✅ 视野角度

    [Header("调试选项")]
    public bool showDebugInfo = true;

    private bool isWhitePerspective = true;

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
                return;
            }
        }

        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }

        WhitePerspective();
    }

    void Update()
    {
        // 按 Space 键切换视角
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isWhitePerspective)
            {
                BlackPerspective();
            }
            else
            {
                WhitePerspective();
            }
        }
    }

    public void BlackPerspective()
    {
        if (boardTransform == null) return;

        isWhitePerspective = false;

        // ✅ 棋盘中心：(3.5, 0, 3.5)
        Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

        // ✅ 从黑方视角看（Z轴正方向）
        float radAngle = angle * Mathf.Deg2Rad;
        float offsetZ = distance * Mathf.Cos(radAngle);
        float offsetY = distance * Mathf.Sin(radAngle);

        Vector3 cameraPosition = boardCenter + new Vector3(0f, offsetY, offsetZ);
        transform.position = cameraPosition;
        transform.LookAt(boardCenter);

        if (showDebugInfo)
        {
            Debug.Log($"🖤 黑方视角 | 相机位置: {transform.position}");
        }
    }

    public void WhitePerspective()
    {
        if (boardTransform == null) return;

        isWhitePerspective = true;

        // ✅ 棋盘中心：(3.5, 0, 3.5)
        Vector3 boardCenter = boardTransform.position + new Vector3(3.5f, 0f, 3.5f);

        // ✅ 从白方视角看（Z轴负方向）
        float radAngle = angle * Mathf.Deg2Rad;
        float offsetZ = distance * Mathf.Cos(radAngle);
        float offsetY = distance * Mathf.Sin(radAngle);

        Vector3 cameraPosition = boardCenter + new Vector3(0f, offsetY, -offsetZ);
        transform.position = cameraPosition;
        transform.LookAt(boardCenter);

        if (showDebugInfo)
        {
            Debug.Log($"🤍 白方视角 | 相机位置: {transform.position}");
        }
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

        // 绘制棋盘边界
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
