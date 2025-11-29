using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Damage Impact VFX Prefabs")]
    public GameObject physicalImpactPrefab; // 物理：白刃/打击
    public GameObject fireImpactPrefab;     // 火焰：爆炸/燃烧
    public GameObject arcaneImpactPrefab;   // 奥术：紫色能量/波纹
    public GameObject holyImpactPrefab;     // 神圣：金光/闪光

    [Header("Settings")]
    public float vfxYOffset = 1.0f; // 特效生成的垂直偏移量（防止生成在脚底）
    public float vfxDuration = 2.0f; // 特效自动销毁时间

    void Awake()
    {
        // 单例模式，方便全局调用
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
            case DamageType.Holy: // 假设你有 Holy 类型
                prefabToSpawn = holyImpactPrefab;
                break;
        }

        if (prefabToSpawn != null)
        {
            // 稍微抬高一点位置，让特效出现在棋子身体上而不是脚底
            Vector3 spawnPos = position + Vector3.up * vfxYOffset;

            GameObject vfx = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // 确保特效会朝向摄像机（如果是 2D 贴图类特效）或者保持默认旋转
            // vfx.transform.LookAt(Camera.main.transform); 

            Destroy(vfx, vfxDuration);
        }
    }
}
