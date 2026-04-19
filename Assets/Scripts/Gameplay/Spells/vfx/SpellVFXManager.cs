using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpellVFXManager : MonoBehaviour
{
    private const string QueenDiveChargePreviewPath = "QK/Qk_fire_arrow_01_ready_01_QueenDivePreview";
    public static readonly Color PhysicalColor = new Color(0.86f, 0.88f, 0.94f, 0.95f);
    public static readonly Color FireColor = new Color(1.00f, 0.47f, 0.18f, 0.95f);
    public static readonly Color HolyColor = new Color(1.00f, 0.84f, 0.35f, 0.95f);
    public static readonly Color ArcaneColor = new Color(0.42f, 0.88f, 1.00f, 0.95f);
    public static readonly Color MysticColor = new Color(0.76f, 0.42f, 1.00f, 0.93f);
    public static readonly Color StunColor = new Color(1.00f, 0.88f, 0.36f, 0.92f);
    public static readonly Color MindControlColor = MysticColor;
    public static readonly Color BuffColor = new Color(0.55f, 1.00f, 0.72f, 0.92f);
    public static readonly Color SunwellAnthemColor = new Color(1.00f, 0.58f, 0.16f, 0.94f);
    private static SpellVFXManager instance;
    private Material lineMaterial;
    private Material orbMaterialTemplate;
    private readonly HashSet<string> missingPrefabWarnings = new HashSet<string>();

    public static SpellVFXManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SpellVFXManager>();
                if (instance == null)
                {
                    GameObject managerObject = new GameObject("SpellVFXManager");
                    instance = managerObject.AddComponent<SpellVFXManager>();
                    DontDestroyOnLoad(managerObject);
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void PlaySpellVFX(Spell spell, Piece caster, LogicManager logicManager, Vector2 targetSquare)
    {
        if (spell == null || caster == null)
        {
            return;
        }

        switch (spell)
        {
            case CrystallinePush:
            {
                Color tint = caster.ResolvedFaction == Faction.Dwarf ? PhysicalColor : ArcaneColor;
                PlayMeleeStrike(caster, logicManager, targetSquare, tint, false);
                break;
            }
            case GemstoneSmash:
                PlayMeleeStrike(caster, logicManager, targetSquare, HolyColor, true);
                break;
            case ScorchingRay:
                PlayScorchingRay(caster, logicManager, targetSquare);
                break;
            case PrismaticBarrier:
                PlayPrismaticBarrier(caster);
                break;
            case PhoenixDive:
                PlayQueenDive(caster, targetSquare);
                break;
            case MindControl:
                PlayMindControl(caster);
                break;
            case BattleRally:
                PlayBattleRally(caster, logicManager);
                break;
            case FortifiedRampart:
                PlayRampartAura(caster, logicManager);
                break;
            case HeartOfTheMountain:
                PlayTeamBuff(caster, logicManager, BuffColor, "HCFX/HCFX_Energy_06");
                break;
            case SunwellAnthem:
                PlaySunwellAnthemBuff(caster, logicManager);
                break;
            case AshenRebirth:
                PlayReviveBlessing(caster, targetSquare);
                break;
            case CarryAlly:
                PlayCarryAlly(caster, targetSquare);
                break;
        }
    }

    public GameObject CreateAttachedStatusEffect(Piece piece, string prefabPath, Color tint, float scale, float extraHeight)
    {
        if (piece == null)
        {
            return null;
        }

        GameObject effect = SpawnEffect(prefabPath, GetHeadPoint(piece) + Vector3.up * extraHeight, Quaternion.identity, scale, tint, 0f);
        UpdateAttachedStatusEffect(effect, piece, scale, extraHeight);
        return effect;
    }

    public void UpdateAttachedStatusEffect(GameObject effect, Piece piece, float scale, float extraHeight)
    {
        if (effect == null || piece == null)
        {
            return;
        }

        effect.transform.position = GetHeadPoint(piece) + Vector3.up * extraHeight;
        effect.transform.localScale = Vector3.one * scale;
    }

    public GameObject CreateAttachedBodyEffect(Piece piece, string prefabPath, Color tint, float scale, float extraHeight)
    {
        if (piece == null)
        {
            return null;
        }

        GameObject effect = SpawnEffect(prefabPath, GetChestPoint(piece) + Vector3.up * extraHeight, Quaternion.identity, scale, tint, 0f);
        UpdateAttachedBodyEffect(effect, piece, scale, extraHeight);
        return effect;
    }

    public void UpdateAttachedBodyEffect(GameObject effect, Piece piece, float scale, float extraHeight)
    {
        if (effect == null || piece == null)
        {
            return;
        }

        effect.transform.position = GetChestPoint(piece) + Vector3.up * extraHeight;
        effect.transform.localScale = Vector3.one * scale;
    }

    public GameObject CreatePersistentTileMarker(Vector2 position, Color tint, float radius, float width)
    {
        Vector3 center = GetGroundPoint(position);
        GameObject root = new GameObject("PersistentTileMarker");
        root.transform.position = center;

        LineRenderer ring = CreateRingRenderer("Ring", tint, width, root.transform);
        SetCirclePositions(ring, radius);
        ring.loop = true;
        ring.startColor = tint;
        ring.endColor = tint;

        return root;
    }

    public void PlayMeleeStrike(Piece caster, LogicManager logicManager, Vector2 targetSquare, Color tint, bool heavy)
    {
        Vector3 source = GetChestPoint(caster);
        Piece targetPiece = TryGetPieceAt(logicManager, targetSquare);
        Vector3 impactPoint = targetPiece != null ? GetChestPoint(targetPiece) : GetGroundPoint(targetSquare) + Vector3.up * 0.5f;
        Vector3 flatDir = impactPoint - source;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f)
        {
            flatDir = Vector3.forward;
        }

        SpawnRingPulse(GetGroundPoint(targetSquare), MultiplyColor(tint, 0.95f), heavy ? 0.38f : 0.30f, heavy ? 0.055f : 0.04f, 0.22f);
        SpawnPolyline(new[] { source, impactPoint }, MultiplyColor(tint, 0.78f), heavy ? 0.06f : 0.04f, 0.14f);
        SpawnEffect("HCFX/HCFX_Hit_10",
            impactPoint,
            Quaternion.LookRotation(flatDir.normalized, Vector3.up),
            heavy ? 0.34f : 0.24f,
            tint,
            0.8f);
    }

    private void PlayScorchingRay(Piece caster, LogicManager logicManager, Vector2 targetSquare)
    {
        Color tint = FireColor;
        Piece target = TryGetPieceAt(logicManager, targetSquare);
        Vector3 chargePoint = GetChestPoint(caster) + Vector3.up * 0.10f;
        Vector3 beamStart = GetHeadPoint(caster) + Vector3.up * 0.02f;
        Vector3 end = target != null ? GetChestPoint(target) : GetGroundPoint(targetSquare) + Vector3.up * 0.38f;

        PlayFireChestCharge(chargePoint, 0.78f, 0.42f, 0.13f, true);
        SpawnPolyline(new[] { beamStart, end }, MultiplyColor(tint, 0.96f), 0.028f, 0.16f);
        SpawnEffect("HCFX/HCFX_Hit_10", end, Quaternion.identity, 0.18f, MultiplyAlpha(tint, 0.88f), 0.6f);
        SpawnRingPulse(end, MultiplyAlpha(tint, 0.72f), 0.09f, 0.012f, 0.14f);
    }

    private void PlayPrismaticBarrier(Piece caster)
    {
        Vector3 casterPoint = GetChestPoint(caster) + Vector3.up * 0.08f;
        PlayMysticChestCharge(
            casterPoint,
            MultiplyAlpha(MultiplyColor(MysticColor, 0.90f), 0.80f),
            MultiplyAlpha(MultiplyColor(MysticColor, 1.04f), 0.88f),
            MultiplyAlpha(new Color(0.94f, 0.76f, 1.00f, 1.00f), 0.46f),
            0.84f,
            0.52f,
            0.30f,
            0.15f);
    }

    private void PlayQueenDive(Piece caster, Vector2 targetSquare)
    {
        PlayDiveSequence(caster, targetSquare);
    }

    private void PlayMindControl(Piece caster)
    {
        Vector3 casterPoint = GetChestPoint(caster) + Vector3.up * 0.08f;
        PlayMysticChestCharge(
            casterPoint,
            MultiplyAlpha(new Color(0.56f, 0.32f, 0.82f, 1.00f), 0.72f),
            MultiplyAlpha(new Color(0.74f, 0.44f, 0.96f, 1.00f), 0.76f),
            MultiplyAlpha(new Color(0.66f, 0.40f, 0.92f, 1.00f), 0.34f),
            0.74f,
            0.42f,
            0.24f,
            0.13f);
        SpawnPolyline(
            new[]
            {
                casterPoint + Vector3.left * 0.06f,
                casterPoint + Vector3.right * 0.06f
            },
            MultiplyAlpha(new Color(0.72f, 0.45f, 0.94f, 1.00f), 0.32f),
            0.010f,
            0.10f);
    }

    private void PlayBattleRally(Piece caster, LogicManager logicManager)
    {
        PlaySubtleCharge(GetGroundPoint(caster.GetCoordinates()), BuffColor);
        SpawnRingPulse(GetGroundPoint(caster.GetCoordinates()), BuffColor, 0.45f, 0.05f, 0.28f);

        foreach (Vector2 square in GetSquareArea(caster.GetCoordinates(), 2))
        {
            Piece piece = TryGetPieceAt(logicManager, square);
            if (piece == null || piece.IsWhite != caster.IsWhite)
            {
                continue;
            }

            SpawnRingPulse(GetGroundPoint(square), MultiplyColor(BuffColor, 0.85f), 0.22f, 0.03f, 0.22f);
        }
    }

    private void PlayCarryAlly(Piece caster, Vector2 targetSquare)
    {
        PlaySubtleCharge(GetHeadPoint(caster), HolyColor);
        SpawnRingPulse(GetGroundPoint(targetSquare), HolyColor, 0.42f, 0.048f, 0.34f);
        SpawnEffect("HCFX/HCFX_Shine_07", GetGroundPoint(targetSquare) + Vector3.up * 0.12f, Quaternion.identity, 0.46f, HolyColor, 1.2f);
    }

    private void PlayRampartAura(Piece caster, LogicManager logicManager)
    {
        PlaySubtleCharge(GetGroundPoint(caster.GetCoordinates()), HolyColor);
        SpawnRingPulse(GetGroundPoint(caster.GetCoordinates()), HolyColor, 0.55f, 0.055f, 0.34f);
        SpawnRampartBeneficiaryEffects(caster, logicManager, MultiplyColor(HolyColor, 1.02f), "HCFX/HCFX_Shine_07");
        SpawnTransientRampartAura(caster, logicManager);
    }

    private void PlaySunwellAnthemBuff(Piece caster, LogicManager logicManager)
    {
        Color anthemTint = SunwellAnthemColor;
        PlayTeamBuff(caster, logicManager, anthemTint, "HCFX/HCFX_Energy_06");
    }

    private void SpawnTransientRampartAura(Piece caster, LogicManager logicManager)
    {
        if (caster == null || logicManager == null || logicManager.fortifiedRampartAuraPrefab == null)
        {
            return;
        }

        Vector3 center = GetGroundPoint(caster.GetCoordinates());
        GameObject auraObject = Instantiate(
            logicManager.fortifiedRampartAuraPrefab,
            center + Vector3.up * 0.04f,
            Quaternion.identity);

        RampartAuraVisual auraVisual = auraObject.GetComponent<RampartAuraVisual>();
        if (auraVisual == null)
        {
            auraVisual = auraObject.AddComponent<RampartAuraVisual>();
        }

        auraVisual.InitializeTransient(center, 2, 1.05f);
    }

    private void PlayTeamBuff(Piece caster, LogicManager logicManager, Color tint, string tileEffectPath)
    {
        PlaySubtleCharge(GetGroundPoint(caster.GetCoordinates()), tint);
        SpawnRingPulse(GetGroundPoint(caster.GetCoordinates()), tint, 0.50f, 0.05f, 0.32f);
        SpawnTileField(GetAlliedOccupiedSquares(caster, logicManager), tint, tileEffectPath);
    }

    private void PlayReviveBlessing(Piece caster, Vector2 targetSquare)
    {
        Vector3 casterPoint = GetChestPoint(caster);
        Vector3 targetPoint = GetGroundPoint(targetSquare) + Vector3.up * 0.15f;

        PlaySubtleCharge(casterPoint, FireColor);
        SpawnEffect("HCFX/HCFX_Portal_01", targetPoint, Quaternion.identity, 0.42f, FireColor, 1.8f);
        SpawnRingPulse(GetGroundPoint(targetSquare), FireColor, 0.36f, 0.04f, 0.38f);
        SpawnPolyline(new[] { casterPoint, targetPoint + Vector3.up * 0.55f }, MultiplyColor(FireColor, 0.85f), 0.04f, 0.20f);
    }

    private void PlaySubtleCharge(Vector3 position, Color color)
    {
        SpawnRingPulse(position, MultiplyColor(color, 0.9f), 0.18f, 0.03f, 0.18f);
    }

    private void PlayDiveSequence(Piece caster, Vector2 targetSquare)
    {
        Vector3 chargePoint = GetChestPoint(caster) + Vector3.up * 0.10f;
        Vector3 ventPoint = chargePoint + Vector3.up * 0.52f;
        Vector3 impactPoint = GetGroundPoint(targetSquare) + Vector3.up * 0.10f;
        Vector3 strikeStart = impactPoint + Vector3.up * 1.85f;

        PlayFireChestCharge(chargePoint, 0.86f, 0.46f, 0.15f, false);
        SpawnPolyline(new[] { chargePoint, ventPoint }, MultiplyAlpha(FireColor, 0.85f), 0.020f, 0.12f);
        SpawnQueenDiveChargePreview(strikeStart);
        SpawnPolyline(new[] { strikeStart, impactPoint }, MultiplyAlpha(FireColor, 0.95f), 0.032f, 0.16f);
        SpawnEffect("HCFX/HCFX_Hit_10", impactPoint, Quaternion.identity, 0.20f, MultiplyAlpha(FireColor, 0.88f), 0.7f);
        SpawnRingPulse(GetGroundPoint(targetSquare), MultiplyAlpha(FireColor, 0.72f), 0.12f, 0.016f, 0.16f);
    }

    private void SpawnQueenDiveChargePreview(Vector3 position)
    {
        GameObject preview = SpawnEffect(
            QueenDiveChargePreviewPath,
            position,
            Quaternion.identity,
            0.45f,
            MultiplyAlpha(MultiplyColor(FireColor, 0.96f), 0.88f),
            0.20f);

        if (preview != null)
        {
            StartCoroutine(AnimateRise(preview.transform, Vector3.down * 0.14f, 0.12f));
        }
    }

    private void PlayFireChestCharge(Vector3 position, float readyScale, float burstScale, float ringRadius, bool addHeatLine)
    {
        Color readyTint = MultiplyAlpha(MultiplyColor(FireColor, 0.92f), 0.82f);
        Color burstTint = MultiplyAlpha(MultiplyColor(FireColor, 1.04f), 0.84f);

        SpawnEffect("QK/Qk_fire_arrow_01_ready_01", position, Quaternion.identity, readyScale, readyTint, 0.60f);
        SpawnEffect("QK/Qk_fire_arrow_01_hit_01", position + Vector3.up * 0.01f, Quaternion.identity, burstScale, burstTint, 0.34f);
        SpawnRingPulse(position, MultiplyAlpha(FireColor, 0.54f), ringRadius, 0.012f, 0.18f);

        if (addHeatLine)
        {
            SpawnPolyline(
                new[] { position, position + Vector3.up * 0.22f },
                MultiplyAlpha(FireColor, 0.78f),
                0.015f,
                0.12f);
        }
    }

    private void PlayMysticChestCharge(
        Vector3 position,
        Color auraColor,
        Color burstColor,
        Color coreColor,
        float auraScale,
        float burstScale,
        float coreScale,
        float ringRadius)
    {
        SpawnEffect("QK/Qk_ice_arrow_01_ready_01", position, Quaternion.identity, auraScale, auraColor, 0.60f);
        SpawnEffect("QK/Qk_light_arrow_01_hit_01", position + Vector3.up * 0.01f, Quaternion.identity, burstScale, burstColor, 0.32f);
        SpawnEffect("QK/Qk_light_arrow_01_ready_01", position + Vector3.up * 0.02f, Quaternion.identity, coreScale, coreColor, 0.34f);
        SpawnRingPulse(position, MultiplyAlpha(burstColor, 0.50f), ringRadius, 0.012f, 0.18f);
    }

    private void SpawnSimpleChargeOrb(Vector3 position, Color color, float startScale, float endScale, float duration, Vector3 drift)
    {
        SpawnSimpleOrb(position, color, startScale, endScale, duration, drift);
        SpawnSimpleOrb(
            position,
            MultiplyColor(color, 1.05f),
            startScale * 1.55f,
            endScale * 1.85f,
            duration * 0.92f,
            drift * 0.55f);
    }

    private void SpawnSimpleOrb(Vector3 position, Color color, float startScale, float endScale, float duration, Vector3 drift)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "SimpleChargeOrb";
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * Mathf.Max(0.01f, startScale);

        Collider collider = orb.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = orb.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = new Material(GetOrbMaterialTemplate());
            ApplyMaterialColor(renderer.material, color);
        }

        StartCoroutine(AnimateSimpleOrb(orb, renderer, position, color, startScale, endScale, duration, drift));
    }

    private IEnumerator AnimateSimpleOrb(
        GameObject orb,
        Renderer renderer,
        Vector3 startPosition,
        Color color,
        float startScale,
        float endScale,
        float duration,
        Vector3 drift)
    {
        float elapsed = 0f;

        while (elapsed < duration && orb != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            orb.transform.position = startPosition + (drift * t);
            orb.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

            if (renderer != null)
            {
                Color current = MultiplyAlpha(color, 1f - (t * 0.85f));
                ApplyMaterialColor(renderer.material, current);
            }

            yield return null;
        }

        if (renderer != null && renderer.material != null)
        {
            Destroy(renderer.material);
        }

        if (orb != null)
        {
            Destroy(orb);
        }
    }

    private void SpawnTileField(IEnumerable<Vector2> squares, Color tint, string effectPath)
    {
        foreach (Vector2 square in squares.Distinct())
        {
            Vector3 point = GetGroundPoint(square);
            SpawnRingPulse(point, MultiplyColor(tint, 0.92f), 0.22f, 0.03f, 0.22f);
            if (!string.IsNullOrEmpty(effectPath))
            {
                SpawnEffect(effectPath, point + Vector3.up * 0.10f, Quaternion.identity, 0.24f, tint, 1.0f);
            }
        }
    }

    private void SpawnRampartBeneficiaryEffects(Piece source, LogicManager logicManager, Color tint, string effectPath)
    {
        if (source == null || logicManager == null)
        {
            return;
        }

        foreach (Vector2 square in GetRampartProtectedSquares(source, logicManager).Distinct())
        {
            Piece piece = TryGetPieceAt(logicManager, square);
            if (piece == null)
            {
                continue;
            }

            Vector3 groundPoint = GetGroundPoint(square);
            SpawnRingPulse(groundPoint, MultiplyColor(tint, 0.92f), 0.22f, 0.03f, 0.22f);

            if (!string.IsNullOrEmpty(effectPath))
            {
                SpawnEffect(effectPath, GetChestPoint(piece), Quaternion.identity, 0.36f, tint, 1.0f);
            }
        }
    }

    private IEnumerator AnimateRise(Transform target, Vector3 offset, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 start = target.position;
        Vector3 end = start + offset;
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }

    private void SpawnRingPulse(Vector3 center, Color color, float radius, float width, float duration)
    {
        GameObject root = new GameObject("RingPulse");
        root.transform.position = center;

        LineRenderer ring = CreateRingRenderer("Ring", color, width, root.transform);
        SetCirclePositions(ring, radius * 0.65f);
        StartCoroutine(AnimateRingPulse(root, ring, color, radius, duration));
    }

    private IEnumerator AnimateRingPulse(GameObject root, LineRenderer ring, Color color, float targetRadius, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && ring != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float radius = Mathf.Lerp(targetRadius * 0.65f, targetRadius * 1.15f, t);
            Color pulseColor = color;
            pulseColor.a *= 1f - t;
            ring.startColor = pulseColor;
            ring.endColor = pulseColor;
            SetCirclePositions(ring, radius);
            yield return null;
        }

        if (root != null)
        {
            Destroy(root);
        }
    }

    private LineRenderer CreateRingRenderer(string name, Color color, float width, Transform parent)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(parent, false);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.sharedMaterial = GetLineMaterial();
        ring.textureMode = LineTextureMode.Stretch;
        ring.alignment = LineAlignment.View;
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 33;
        ring.startWidth = width;
        ring.endWidth = width;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.startColor = color;
        ring.endColor = color;
        ring.numCapVertices = 4;
        ring.numCornerVertices = 4;
        return ring;
    }

    private void SpawnPolyline(IReadOnlyList<Vector3> points, Color color, float width, float duration)
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        GameObject lineObject = new GameObject("Polyline");
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = GetLineMaterial();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.positionCount = points.Count;
        line.startWidth = width;
        line.endWidth = width * 0.75f;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;

        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }

        StartCoroutine(AnimatePolyline(lineObject, line, color, duration));
    }

    private IEnumerator AnimatePolyline(GameObject root, LineRenderer line, Color color, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && line != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color current = color;
            current.a *= 1f - t;
            line.startColor = current;
            line.endColor = current;
            yield return null;
        }

        if (root != null)
        {
            Destroy(root);
        }
    }

    private void SetCirclePositions(LineRenderer ring, float radius)
    {
        const int segments = 32;
        ring.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private GameObject SpawnEffect(string path, Vector3 position, Quaternion rotation, float scale, Color tint, float lifetime)
    {
        GameObject prefab = LoadPrefab(path);
        if (prefab == null)
        {
            return null;
        }

        GameObject instanceObject = Instantiate(prefab, position, rotation);
        instanceObject.transform.localScale *= scale;
        ApplyTint(instanceObject, tint);
        if (lifetime > 0f)
        {
            Destroy(instanceObject, lifetime);
        }
        return instanceObject;
    }

    private GameObject LoadPrefab(string path)
    {
        List<string> candidates = GetCandidateResourcePaths(path).ToList();
        foreach (string candidate in candidates)
        {
            GameObject prefab = Resources.Load<GameObject>(candidate);
            if (prefab != null)
            {
                return prefab;
            }
        }

        if (!string.IsNullOrWhiteSpace(path) && missingPrefabWarnings.Add(path))
        {
            Debug.LogWarning($"SpellVFXManager could not load prefab '{path}'. Tried: {string.Join(", ", candidates)}");
        }

        return null;
    }

    private IEnumerable<string> GetCandidateResourcePaths(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        string normalized = path.Replace("\\", "/").Replace(".prefab", "");
        yield return normalized;

        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < normalized.Length - 1)
        {
            string fileNameOnly = normalized.Substring(slashIndex + 1);
            if (fileNameOnly != normalized)
            {
                yield return fileNameOnly;
            }
        }
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

        foreach (TrailRenderer trail in instanceObject.GetComponentsInChildren<TrailRenderer>(true))
        {
            trail.startColor = tint;
            Color endColor = tint;
            endColor.a = 0f;
            trail.endColor = endColor;
        }

        if (instanceObject.name.Contains("xuanyun"))
        {
            foreach (Transform child in instanceObject.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Quad")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    private Vector3 GetGroundPoint(Vector2 boardPosition)
    {
        LogicManager logicManager = FindFirstObjectByType<LogicManager>();
        if (logicManager != null)
        {
            Square square = logicManager.GetSquareAtPosition(boardPosition);
            if (square != null)
            {
                return square.transform.position + Vector3.up * 0.05f;
            }
        }

        return new Vector3(boardPosition.x, 0.05f, boardPosition.y);
    }

    private Vector3 GetChestPoint(Piece piece)
    {
        Bounds bounds = GetPieceBounds(piece);
        return new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
    }

    private Vector3 GetHeadPoint(Piece piece)
    {
        Bounds bounds = GetPieceBounds(piece);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private Bounds GetPieceBounds(Piece piece)
    {
        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(piece.transform.position + Vector3.up * 0.5f, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private Vector3 GetBoardCenter()
    {
        return GetGroundPoint(new Vector2(3.5f, 3.5f));
    }

    private Piece TryGetPieceAt(LogicManager logicManager, Vector2 square)
    {
        if (logicManager == null)
        {
            return null;
        }

        int x = Mathf.RoundToInt(square.x);
        int y = Mathf.RoundToInt(square.y);
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            return null;
        }

        return logicManager.boardMap[x, y];
    }

    private List<Vector2> GetAlliedOccupiedSquares(Piece source, LogicManager logicManager)
    {
        List<Vector2> squares = new List<Vector2>();
        if (source == null || logicManager == null)
        {
            return squares;
        }

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece piece = logicManager.boardMap[x, y];
                if (piece != null && piece.IsWhite == source.IsWhite)
                {
                    squares.Add(new Vector2(x, y));
                }
            }
        }

        return squares;
    }

    private List<Vector2> GetRampartProtectedSquares(Piece source, LogicManager logicManager)
    {
        List<Vector2> squares = new List<Vector2>();
        if (source == null || logicManager == null)
        {
            return squares;
        }

        Vector2 center = source.GetCoordinates();
        foreach (Vector2 square in GetSquareArea(center, 2))
        {
            Piece piece = TryGetPieceAt(logicManager, square);
            if (piece != null && piece.IsWhite == source.IsWhite)
            {
                squares.Add(square);
            }
        }

        return squares;
    }

    private List<Vector2> GetSquareArea(Vector2 center, int radius)
    {
        List<Vector2> squares = new List<Vector2>();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int x = Mathf.RoundToInt(center.x) + dx;
                int y = Mathf.RoundToInt(center.y) + dy;
                if (x < 0 || x >= 8 || y < 0 || y >= 8)
                {
                    continue;
                }

                squares.Add(new Vector2(x, y));
            }
        }

        return squares;
    }

    private List<Vector2> GetAllBoardTiles()
    {
        List<Vector2> tiles = new List<Vector2>(64);
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                tiles.Add(new Vector2(x, y));
            }
        }

        return tiles;
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            lineMaterial = new Material(shader);
        }

        return lineMaterial;
    }

    private Material GetOrbMaterialTemplate()
    {
        if (orbMaterialTemplate == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            orbMaterialTemplate = new Material(shader);
        }

        return orbMaterialTemplate;
    }

    private void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 0.8f);
        }
    }

    private Color MultiplyColor(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a);
    }

    private Color MultiplyAlpha(Color color, float alphaMultiplier)
    {
        return new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * alphaMultiplier));
    }
}
