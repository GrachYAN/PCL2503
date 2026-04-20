using UnityEngine;

public class GameNotificationManager : MonoBehaviour
{
    public static GameNotificationManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject errorTextPrefab; // ����ղ����� WarcraftErrorText Prefab
    public Transform messageContainer; // ��ʾ�����ɵĸ��ڵ�
    public GameObject damageTextPrefab;  // ������ʾ�˺�����
    public Transform damageContainer;    // ���˺�����

    [Header("Warcraft Style Colors")]
    public Color errorColor = new Color(1f, 0.1f, 0.1f); // ��ɫ���� (��������/��ЧĿ��)
    public Color warningColor = new Color(1f, 0.8f, 0f); // ��ɫ���� (��ȴ��)

    public Color physicalDamageColor = Color.white;  // �����˺�����ɫ
    public Color fireDamageColor = new Color(1f, 0.5f, 0f); // �����˺�����ɫ
    public Color arcaneDamageColor = new Color(0.6f, 0f, 1f); // �����˺�����ɫ
    public Color otherDamageColor = Color.yellow;   // �����˺�/ħ���˺�����ɫ

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
    /// ��ʾһ����Ļ����Ĵ�����ʾ
    /// </summary>
    public void ShowSystemMessage(string message, bool isWarning = false)
    {
        if (errorTextPrefab == null || messageContainer == null) return;

        // ��������
        GameObject obj = Instantiate(errorTextPrefab, messageContainer);

        // ��ʼ������
        FloatingMessage floatingMsg = obj.GetComponent<FloatingMessage>();
        if (floatingMsg != null)
        {
            floatingMsg.Setup(message, isWarning ? warningColor : errorColor);
        }

        // ��΢���һ���ˮƽƫ�ƣ���ֹ������Ϣ��ȫ�ص�����ѡ��
        // obj.transform.localPosition += new Vector3(Random.Range(-10f, 10f), 0, 0);
    }

    /// <summary>
    /// ��ʾ�˺����� (��������λ��)
    /// </summary>
    /// <param name="worldPos">���ӵ���������</param>
    /// <param name="amount">�˺���ֵ</param>
    /// <param name="type">�˺�����</param>
    public void ShowDamageText(Vector3 worldPos, int amount, DamageType type)
    {
        // ��ȫ���
        if (mainCam == null) mainCam = Camera.main;
        if (damageTextPrefab == null) return;

        // ���û����ר�ŵ�damageContainer������ʱ��messageContainer����
        Transform parent = GetDamageTextParent();

        // 1. ȷ����ɫ
        Color targetColor = physicalDamageColor;
        switch (type)
        {
            case DamageType.Physical: targetColor = physicalDamageColor; break;
            case DamageType.Fire: targetColor = fireDamageColor; break;
            case DamageType.Arcane: targetColor = arcaneDamageColor; break;
            default: targetColor = otherDamageColor; break;
        }

        // 2. ���ɶ��� (ʹ�� damageTextPrefab)
        GameObject obj = Instantiate(damageTextPrefab, parent);
        obj.transform.localScale = Vector3.one; // ǿ����������

        FloatingMessage floatingMsg = obj.GetComponent<FloatingMessage>();
        if (floatingMsg != null)
        {
            // ����������� worldPos ��Ϊ��������������ȥ
            floatingMsg.Setup("-" + amount.ToString(), targetColor, worldPos);
        }

        // 3. ����ת�� (���� -> ��Ļ)
        // �������߶ȡ���֮ǰ�� 2.5f ����̫���ˣ��ĳ� 1.5f �� 1.8f ����
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos + Vector3.up * 0.5f);

        // ȷ��Z��Ϊ0����ֹUI���ü�
        screenPos.z = 0;
        obj.transform.position = screenPos;

        /*
        if (floatingMsg != null)
        {
            floatingMsg.Setup("-" + amount.ToString(), targetColor);

            // ���ƫ��һ��㣬��ֹ�����ص�
            obj.transform.position += new Vector3(Random.Range(-20f, 20f), Random.Range(-10f, 10f), 0);
        }
        */
    }
    public void ClearDamageTexts()
    {
        Transform parent = GetDamageTextParent();
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.transform.SetParent(null);
            child.SetActive(false);

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    private Transform GetDamageTextParent()
    {
        return damageContainer != null ? damageContainer : messageContainer;
    }
}
