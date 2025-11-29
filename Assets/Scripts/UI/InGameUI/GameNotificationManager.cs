using UnityEngine;

public class GameNotificationManager : MonoBehaviour
{
    public static GameNotificationManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject errorTextPrefab; // 拖入刚才做的 WarcraftErrorText Prefab
    public Transform messageContainer; // 提示框生成的父节点

    [Header("Warcraft Style Colors")]
    public Color errorColor = new Color(1f, 0.1f, 0.1f); // 红色报错 (法力不足/无效目标)
    public Color warningColor = new Color(1f, 0.8f, 0f); // 黄色警告 (冷却中)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 显示一条屏幕中央的错误提示
    /// </summary>
    public void ShowSystemMessage(string message, bool isWarning = false)
    {
        if (errorTextPrefab == null || messageContainer == null) return;

        // 生成文字
        GameObject obj = Instantiate(errorTextPrefab, messageContainer);

        // 初始化内容
        FloatingMessage floatingMsg = obj.GetComponent<FloatingMessage>();
        if (floatingMsg != null)
        {
            floatingMsg.Setup(message, isWarning ? warningColor : errorColor);
        }

        // 稍微随机一点点水平偏移，防止多条消息完全重叠（可选）
        // obj.transform.localPosition += new Vector3(Random.Range(-10f, 10f), 0, 0);
    }
}
