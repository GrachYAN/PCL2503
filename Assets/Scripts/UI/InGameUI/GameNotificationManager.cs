using UnityEngine;

public class GameNotificationManager : MonoBehaviour
{
    public static GameNotificationManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject errorTextPrefab; // 拖入刚才做的 WarcraftErrorText Prefab
    public Transform messageContainer; // 提示框生成的父节点
    public GameObject damageTextPrefab;  // 用于显示伤害数字
    public Transform damageContainer;    // 放伤害数字

    [Header("Warcraft Style Colors")]
    public Color errorColor = new Color(1f, 0.1f, 0.1f); // 红色报错 (法力不足/无效目标)
    public Color warningColor = new Color(1f, 0.8f, 0f); // 黄色警告 (冷却中)

    public Color physicalDamageColor = Color.white;  // 物理伤害，白色
    public Color fireDamageColor = new Color(1f, 0.5f, 0f); // 火焰伤害，橙色
    public Color arcaneDamageColor = new Color(0.6f, 0f, 1f); // 奥术伤害，紫色
    public Color otherDamageColor = Color.yellow;   // 其他伤害/魔法伤害，黄色

    private Camera mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCam = Camera.main;
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

    /// <summary>
    /// 显示伤害数字 (跟随棋子位置)
    /// </summary>
    /// <param name="worldPos">棋子的世界坐标</param>
    /// <param name="amount">伤害数值</param>
    /// <param name="type">伤害类型</param>
    public void ShowDamageText(Vector3 worldPos, int amount, DamageType type)
    {
        // 安全检查
        if (mainCam == null) mainCam = Camera.main;
        if (damageTextPrefab == null) return;

        // 如果没设置专门的damageContainer，就暂时用messageContainer兜底
        Transform parent = damageContainer != null ? damageContainer : messageContainer;

        // 1. 确定颜色
        Color targetColor = physicalDamageColor;
        switch (type)
        {
            case DamageType.Physical: targetColor = physicalDamageColor; break;
            case DamageType.Fire: targetColor = fireDamageColor; break;
            case DamageType.Arcane: targetColor = arcaneDamageColor; break;
            default: targetColor = otherDamageColor; break;
        }

        // 2. 生成对象 (使用 damageTextPrefab)
        GameObject obj = Instantiate(damageTextPrefab, parent);
        obj.transform.localScale = Vector3.one; // 强制重置缩放

        // 3. 坐标转换 (世界 -> 屏幕)
        // 【调整高度】：之前是 2.5f 可能太高了，改成 1.5f 或 1.8f 试试
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos + Vector3.up * 0.5f);

        // 确保Z轴为0，防止UI被裁剪
        screenPos.z = 0;
        obj.transform.position = screenPos;

        // 4. 设置内容
        FloatingMessage floatingMsg = obj.GetComponent<FloatingMessage>();
        if (floatingMsg != null)
        {
            floatingMsg.Setup("-" + amount.ToString(), targetColor);

            // 随机偏移一点点，防止数字重叠
            obj.transform.position += new Vector3(Random.Range(-20f, 20f), Random.Range(-10f, 10f), 0);
        }
    }
}