using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Optional Inspector Fallbacks")]
    public GameObject physicalImpactPrefab;
    public GameObject fireImpactPrefab;
    public GameObject arcaneImpactPrefab;
    public GameObject holyImpactPrefab;

    [Header("Settings")]
    public float vfxYOffset = 0.85f;
    public float physicalScale = 0.74f;
    public float fireScale = 0.82f;
    public float arcaneScale = 0.80f;
    public float holyScale = 0.82f;
    public float vfxDuration = 1.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayImpactVFX(Vector3 position, DamageType type)
    {
        GameObject prefab = LoadImpactPrefab(type);
        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPos = position + Vector3.up * vfxYOffset;
        GameObject vfx = Instantiate(prefab, spawnPos, Quaternion.identity);
        vfx.transform.localScale *= GetScale(type);
        ApplyTint(vfx, GetTint(type));
        Destroy(vfx, vfxDuration);
    }

    private GameObject LoadImpactPrefab(DamageType type)
    {
        string resourcePath = type switch
        {
            DamageType.Physical => "Prefab/texiao/ImpactEffects/PhysicalImpact",
            DamageType.Fire => "Prefab/texiao/ImpactEffects/FireImpact",
            DamageType.Arcane => "Prefab/texiao/ImpactEffects/ArcaneImpact",
            DamageType.Holy => "Prefab/texiao/ImpactEffects/HolyImpact",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(resourcePath))
        {
            GameObject loaded = Resources.Load<GameObject>(resourcePath);
            if (loaded != null)
            {
                return loaded;
            }
        }

        return type switch
        {
            DamageType.Physical => physicalImpactPrefab,
            DamageType.Fire => fireImpactPrefab,
            DamageType.Arcane => arcaneImpactPrefab,
            DamageType.Holy => holyImpactPrefab,
            _ => null
        };
    }

    private float GetScale(DamageType type)
    {
        return type switch
        {
            DamageType.Physical => 0.78f,
            DamageType.Fire => 0.82f,
            DamageType.Arcane => 0.76f,
            DamageType.Holy => 0.84f,
            _ => 1f
        };
    }

    private Color GetTint(DamageType type)
    {
        return type switch
        {
            DamageType.Physical => SpellVFXManager.PhysicalColor,
            DamageType.Fire => SpellVFXManager.FireColor,
            DamageType.Arcane => SpellVFXManager.ArcaneColor,
            DamageType.Holy => SpellVFXManager.HolyColor,
            _ => Color.white
        };
    }

    private void ApplyTint(GameObject instanceObject, Color tint)
    {
        if (instanceObject == null)
        {
            return;
        }

        foreach (ParticleSystem particleSystem in instanceObject.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = tint;
        }

        foreach (Renderer renderer in instanceObject.GetComponentsInChildren<Renderer>(true))
        {
            Material material = renderer.material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }
        }
    }
}
