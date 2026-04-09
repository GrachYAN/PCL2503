using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance;

    [Header("UI Components")]
    public TextMeshProUGUI contentText;
    public RectTransform rectTransform;
    public LayoutElement layoutElement;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public int characterWrapLimit = 80;
    public float maxWindowWidth = 300f;
    public float heightOffset = 10f;
    public float screenPadding = 10f; // 新增：屏幕边缘的内边距

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
        canvasGroup.alpha = 0;
    }

    public void Show(string content, RectTransform targetRect)
    {
        gameObject.SetActive(true);

        // 1. 设置内容
        contentText.text = content;

        // 2. 智能调整宽度
        layoutElement.enabled = Mathf.Max(contentText.preferredWidth, maxWindowWidth) > maxWindowWidth;
        layoutElement.preferredWidth = (contentText.preferredWidth > maxWindowWidth) ? maxWindowWidth : -1;

        // 3. 强制刷新布局，确保此时 rectTransform 的宽/高是正确的
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // 4. 计算位置与边界修正
        CalculatePosition(targetRect);

        // 5. 显示动画
        StopAllCoroutines();
        canvasGroup.alpha = 1;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        canvasGroup.alpha = 0;
    }

    private void CalculatePosition(RectTransform targetRect)
    {
        // 获取目标图标的世界坐标四个角
        // corners[0]=左下, [1]=左上, [2]=右上, [3]=右下
        Vector3[] targetCorners = new Vector3[4];
        targetRect.GetWorldCorners(targetCorners);

        // --- 步骤 A: 默认尝试放在图标上方 ---
        Vector3 iconTopCenter = (targetCorners[1] + targetCorners[2]) / 2f;
        rectTransform.pivot = new Vector2(0.5f, 0f); // 锚点在底部中心
        transform.position = iconTopCenter + new Vector3(0, heightOffset, 0);

        // --- 步骤 B: 检测是否超出屏幕边界 ---

        // 获取 Tooltip 当前的世界坐标四个角
        Vector3[] tooltipCorners = new Vector3[4];
        rectTransform.GetWorldCorners(tooltipCorners);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 1. 垂直检测：如果上方超出了屏幕 (Tooltip顶部 > 屏幕高度)
        if (tooltipCorners[2].y > screenHeight - screenPadding)
        {
            // 改为放在图标下方
            Vector3 iconBottomCenter = (targetCorners[0] + targetCorners[3]) / 2f;
            rectTransform.pivot = new Vector2(0.5f, 1f); // 锚点改为顶部中心
            transform.position = iconBottomCenter - new Vector3(0, heightOffset, 0);

            // 重新获取坐标以便进行后续的水平检测
            rectTransform.GetWorldCorners(tooltipCorners);
        }

        // 2. 水平检测：修正左右溢出
        float shiftX = 0;

        // 如果左边超出了 (x < 0)
        if (tooltipCorners[0].x < screenPadding)
        {
            shiftX = screenPadding - tooltipCorners[0].x;
        }
        // 如果右边超出了 (x > width)
        else if (tooltipCorners[2].x > screenWidth - screenPadding)
        {
            shiftX = (screenWidth - screenPadding) - tooltipCorners[2].x;
        }

        // 应用水平位移
        if (shiftX != 0)
        {
            transform.position += new Vector3(shiftX, 0, 0);
        }
    }
}

/*
 * Max Window Width 300 (推荐值：300~400)
作用：最大宽度限制。
如果文字很短（宽度 < 300），Tooltip 背景框会自适应文字宽度，保持紧凑。
如果文字很长（宽度 > 300），Tooltip 会强制锁定宽度为 300，并让文字自动换行。
意义：防止长文本导致 Tooltip 变成一条横跨屏幕的长条，强制它变成方块状，易于阅读。

Height Offset 10 (推荐值：10~20)
作用：垂直间距。
意义：Tooltip 显示的位置距离图标（或鼠标）的像素距离。
如果不设置（为0），Tooltip 会紧贴着图标，可能会遮挡住图标本身。
设置为 10，就会在图标上方留出一点空隙，视觉上更舒服。

Screen Padding 10 (推荐值：10~20)
作用：屏幕边缘安全距离（这是刚才新加的功能）。
意义：当 Tooltip 被推到屏幕边缘时，保留多少像素的空隙。
如果没有这个（为0），Tooltip 会紧贴着屏幕边缘。
设置为 10，Tooltip 即使在最角落，也会离屏幕边框保持 10px 的距离，显得很精致。
 */