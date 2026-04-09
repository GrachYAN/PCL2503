/*
using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingMessage : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveSpeed = 100f;      // 向上飘的速度
    public float duration = 2.0f;       // 存在时间
    public float fadeDuration = 0.5f;   // 最后淡出的时间

    private TextMeshProUGUI tmpText;
    private CanvasGroup canvasGroup;
    private float timer = 0f;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(string message, Color color)
    {
        tmpText.text = message;
        tmpText.color = color;
        timer = 0f;
        canvasGroup.alpha = 1f;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. 向上飘动
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // 2. 处理淡出
        if (timer > (duration - fadeDuration))
        {
            float fadeAlpha = 1f - ((timer - (duration - fadeDuration)) / fadeDuration);
            canvasGroup.alpha = fadeAlpha;
        }

        // 3. 销毁
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
*/

using UnityEngine;
using TMPro;

public class FloatingMessage : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveSpeed = 50f;       // 飘动速度 (建议设低一点，比如30-50)
    public float duration = 1.5f;       // 存活时间
    public float fadeDuration = 0.5f;   // 淡出时间

    private TextMeshProUGUI tmpText;
    private CanvasGroup canvasGroup;
    private float timer = 0f;

    // --- 新增：跟随逻辑变量 ---
    private Vector3? targetWorldPos;        // 目标的世界坐标 (如果是null就不跟随)
    private Vector3 initialWorldOffset = Vector3.up * 1.5f; // 头顶高度偏移
    private float verticalOffset = 0f;      // 累积的向上飘动距离
    private Camera mainCam;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        mainCam = Camera.main;
    }

    /// <summary>
    /// 初始化方法 (支持重载)
    /// </summary>
    /// <param name="message">文字内容</param>
    /// <param name="color">颜色</param>
    /// <param name="worldPos">【关键】传入棋子坐标，就会开启跟随模式</param>
    public void Setup(string message, Color color, Vector3? worldPos = null)
    {
        if (tmpText != null)
        {
            tmpText.text = message;
            tmpText.color = color;
        }

        timer = 0f;
        verticalOffset = 0f;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // 记录目标位置
        targetWorldPos = worldPos;

        // 如果是跟随模式，初始化时立即更新一次位置，防止第一帧闪烁
        if (targetWorldPos.HasValue && mainCam != null)
        {
            UpdateScreenPosition();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. 计算垂直飘动量 (每一帧都增加一点高度)
        float moveStep = moveSpeed * Time.deltaTime;
        verticalOffset += moveStep;

        // 2. 如果是普通模式 (系统提示)，直接移动 Transform
        if (!targetWorldPos.HasValue)
        {
            transform.Translate(Vector3.up * moveStep);
        }
        // (如果是跟随模式，移动逻辑在 LateUpdate 里处理，防止抖动)

        // 3. 处理淡出
        if (timer > (duration - fadeDuration))
        {
            float fadeAlpha = 1f - ((timer - (duration - fadeDuration)) / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = fadeAlpha;
        }

        // 4. 销毁
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    // 使用 LateUpdate 确保在摄像机移动之后再更新UI位置，这样文字就不会抖动或滞后
    void LateUpdate()
    {
        if (targetWorldPos.HasValue && mainCam != null)
        {
            UpdateScreenPosition();
        }
    }

    private void UpdateScreenPosition()
    {
        // 核心魔法：每一帧都重新计算 3D -> 2D 的位置

        // 1. 拿到棋子原本的位置
        Vector3 targetPos = targetWorldPos.Value;

        // 2. 加上固定的头顶高度 (initialWorldOffset)
        // 3. 转成屏幕坐标
        Vector3 screenPos = mainCam.WorldToScreenPoint(targetPos + initialWorldOffset);

        // 4. 加上由于“飘动动画”产生的垂直偏移 (verticalOffset)
        screenPos.y += verticalOffset;

        // 5. 赋值给 UI
        // 注意：WorldToScreenPoint 的 Z 轴是距离摄像机的深度
        // 我们需要把 Z 轴设为 0 (或者保持原样取决于你的Canvas Render Mode，通常设为0比较安全)
        screenPos.z = 0;
        transform.position = screenPos;

        // 优化：如果目标跑到了摄像机背面（Z < 0），就隐藏文字，不然文字会反向出现在屏幕上
        Vector3 rawScreenPos = mainCam.WorldToScreenPoint(targetPos);
        if (rawScreenPos.z < 0)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }
        else if (timer <= (duration - fadeDuration)) // 只有在没开始淡出时才恢复显示
        {
            if (canvasGroup != null) canvasGroup.alpha = 1;
        }
    }
}
