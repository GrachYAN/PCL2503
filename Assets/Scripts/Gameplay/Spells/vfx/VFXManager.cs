using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Damage Impact VFX Prefabs")]
    public GameObject physicalImpactPrefab; // ����������/���
    public GameObject fireImpactPrefab;     // ���棺��ը/ȼ��
    public GameObject arcaneImpactPrefab;   // ��������ɫ����/����
    public GameObject holyImpactPrefab;     // ��ʥ�����/����

    [Header("Settings")]
    public float vfxYOffset = 1.0f; // ��Ч���ɵĴ�ֱƫ��������ֹ�����ڽŵף�
    public float vfxDuration = 2.0f; // ��Ч�Զ�����ʱ��

    void Awake()
    {
        // ����ģʽ������ȫ�ֵ���
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayImpactVFX(Vector3 position, DamageType type)
    {
        GameObject prefabToSpawn = null;

        switch (type)
        {
            case DamageType.Physical:
                prefabToSpawn = physicalImpactPrefab;
                break;
            case DamageType.Fire:
                prefabToSpawn = fireImpactPrefab;
                break;
            case DamageType.Arcane:
                prefabToSpawn = arcaneImpactPrefab;
                break;
            case DamageType.Holy: // �������� Holy ����
                prefabToSpawn = holyImpactPrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            // ��΢̧��һ��λ�ã�����Ч���������������϶����ǽŵ�
            Vector3 spawnPos = position + Vector3.up * vfxYOffset;

            GameObject vfx = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // ȷ����Ч�ᳯ�������������� 2D ��ͼ����Ч�����߱���Ĭ����ת
            // vfx.transform.LookAt(Camera.main.transform); 

            Destroy(vfx, vfxDuration);
        }
    }
}
