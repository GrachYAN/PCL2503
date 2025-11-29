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
