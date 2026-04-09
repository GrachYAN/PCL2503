using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform boardTransform;
    public Board board;

    [Header("Default View")]
    public float defaultDistance = 11f;
    public float defaultPitch = 35f;
    public float defaultFov = 60f;
    public float defaultWhiteYaw = 180f;
    public float defaultBlackYaw = 0f;

    [Header("Controls")]
    public float panSpeed = 0.02f;
    public float orbitSpeed = 140f;
    public float zoomSpeed = 14f;

    [Header("Limits")]
    public float minDistance = 5f;
    public float maxDistance = 25f;
    public float minPitch = 5f;
    public float maxPitch = 70f;
    public float minHeightAboveBoard = 0.1f;
    public float panLimitPadding = 1.0f;

    [Header("Smoothing")]
    public float movementSmoothTime = 0.08f;
    public float rotationSmoothTime = 0.06f;
    public float zoomSmoothTime = 0.06f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private float targetYaw;
    private float targetPitch;
    private float targetDistance;
    private Vector3 targetPanOffset;

    private float currentYaw;
    private float currentPitch;
    private float currentDistance;
    private Vector3 currentPanOffset;

    private float yawVelocity;
    private float pitchVelocity;
    private float distanceVelocity;
    private Vector3 panVelocity;

    private bool isWhitePerspective = true;
    private Camera cam;
    private Vector3 previousMousePosition;

    void Start()
    {
        if (boardTransform == null)
        {
            Debug.LogError("CameraController: boardTransform 未设置！");
            enabled = false;
            return;
        }

        if (board == null)
        {
            board = boardTransform.GetComponent<Board>();
        }

        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = defaultFov;
        }

        ResetToDefaultView(forceYaw: true, instant: true);
        ApplyCameraTransform();
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (boardTransform == null) return;

        HandleInput();
        SmoothState();
        ApplyCameraTransform();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetToDefaultView(forceYaw: true, instant: false);
        }

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            targetYaw += mouseX * orbitSpeed * Time.deltaTime;
            targetPitch -= mouseY * orbitSpeed * 0.5f * Time.deltaTime;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        if (Input.GetMouseButtonDown(2))
        {
            previousMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(2))
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 delta = currentMousePosition - previousMousePosition;
            previousMousePosition = currentMousePosition;

            Vector3 right = transform.right;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 panDelta = (-right * delta.x - forward * delta.y) * panSpeed;

            targetPanOffset += panDelta;
            targetPanOffset = ClampPanOffset(targetPanOffset);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            targetDistance -= scroll * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    void SmoothState()
    {
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);
        currentPanOffset = Vector3.SmoothDamp(currentPanOffset, targetPanOffset, ref panVelocity, movementSmoothTime);
    }

    public void WhitePerspective()
    {
        isWhitePerspective = true;
    }

    public void BlackPerspective()
    {
        isWhitePerspective = false;
    }

    public void ResetToCurrentPerspectiveDefault()
    {
        ResetToDefaultView(forceYaw: true, instant: false);
    }

    private void ResetToDefaultView(bool forceYaw, bool instant)
    {
        targetPitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);
        targetDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        targetPanOffset = ClampPanOffset(Vector3.zero);

        if (forceYaw)
        {
            targetYaw = isWhitePerspective ? defaultWhiteYaw : defaultBlackYaw;
        }

        if (instant)
        {
            currentYaw = targetYaw;
            currentPitch = targetPitch;
            currentDistance = targetDistance;
            currentPanOffset = targetPanOffset;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Camera reset | white={isWhitePerspective}, yaw={targetYaw:F2}, pitch={targetPitch:F2}, dist={targetDistance:F2}");
        }
    }

    private Vector3 ClampPanOffset(Vector3 rawOffset)
    {
        float spacing = GetBoardSpacing();
        int width = board != null ? board.Width : 8;
        int height = board != null ? board.Height : 8;

        float halfX = Mathf.Max(0.1f, ((width - 1) * spacing) * 0.5f + panLimitPadding);
        float halfZ = Mathf.Max(0.1f, ((height - 1) * spacing) * 0.5f + panLimitPadding);

        rawOffset.y = 0f;
        rawOffset.x = Mathf.Clamp(rawOffset.x, -halfX, halfX);
        rawOffset.z = Mathf.Clamp(rawOffset.z, -halfZ, halfZ);
        return rawOffset;
    }

    private float GetBoardSpacing()
    {
        if (board != null && board.squareSpacing > 0.01f)
        {
            return board.squareSpacing;
        }

        return 1f;
    }

    private Vector3 GetBoardCenter()
    {
        float spacing = GetBoardSpacing();
        int width = board != null ? board.Width : 8;
        int height = board != null ? board.Height : 8;

        return boardTransform.position + new Vector3((width - 1) * spacing * 0.5f, 0f, (height - 1) * spacing * 0.5f);
    }

    void ApplyCameraTransform()
    {
        if (boardTransform == null) return;

        Vector3 focus = GetBoardCenter() + currentPanOffset;

        float yawRad = currentYaw * Mathf.Deg2Rad;
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float cosPitch = Mathf.Cos(pitchRad);

        Vector3 offset;
        offset.x = currentDistance * Mathf.Sin(yawRad) * cosPitch;
        offset.z = currentDistance * Mathf.Cos(yawRad) * cosPitch;
        offset.y = currentDistance * Mathf.Sin(pitchRad);

        Vector3 desiredPosition = focus + offset;
        float minY = focus.y + minHeightAboveBoard;
        if (desiredPosition.y < minY)
        {
            desiredPosition.y = minY;
        }

        transform.position = desiredPosition;
        transform.LookAt(focus);
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || boardTransform == null) return;

        Vector3 boardCenter = GetBoardCenter();

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(boardCenter, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, boardCenter);
    }
}
