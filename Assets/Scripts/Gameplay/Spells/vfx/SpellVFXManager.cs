using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpellVFXManager : MonoBehaviour
{
    public static readonly Color PhysicalColor = new Color(0.86f, 0.88f, 0.94f, 0.95f);
    public static readonly Color FireColor = new Color(1.00f, 0.47f, 0.18f, 0.95f);
    public static readonly Color HolyColor = new Color(1.00f, 0.84f, 0.35f, 0.95f);
    public static readonly Color ArcaneColor = new Color(0.42f, 0.88f, 1.00f, 0.95f);
    public static readonly Color MysticColor = new Color(0.76f, 0.42f, 1.00f, 0.93f);
    public static readonly Color StunColor = new Color(1.00f, 0.88f, 0.36f, 0.92f);
    public static readonly Color MindControlColor = MysticColor;
    public static readonly Color BuffColor = new Color(0.55f, 1.00f, 0.72f, 0.92f);

    private static SpellVFXManager instance;
    private Material lineMaterial;

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
                PlayPrismaticBarrier(caster, targetSquare);
                break;
            case PhoenixDive:
                PlayQueenDive(caster, targetSquare);
                break;
            case MindControl:
                PlayMindControl(caster, logicManager, targetSquare);
                break;
            case BattleRally:
                PlayBattleRally(caster, logicManager);
                break;
            case FortifiedRampart:
                PlayRampartAura(caster, logicManager);
                break;
            case HeartOfTheMountain:
                PlayTeamBuff(caster, logicManager, BuffColor, "VFX_Klaus/HCFX_Energy_06");
                break;
            case SunwellAnthem:
                PlayTeamBuff(caster, logicManager, HolyColor, "VFX_Klaus/HCFX_Shine_07");
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
        SpawnEffect("VFX_Klaus/HCFX_Hit_10",
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
        Vector3 end = target != null ? GetChestPoint(target) : GetGroundPoint(targetSquare) + Vector3.up * 0.42f;

        PlayOrbCharge(chargePoint, tint, true, 0.34f, 0.48f);
        SpawnPolyline(new[] { beamStart, end }, MultiplyColor(tint, 0.95f), 0.050f, 0.18f);
        SpawnEffect("SpellVFX/fx_light_particle_b", end, Quaternion.identity, 0.30f, tint, 0.55f);
    }

    private void PlayPrismaticBarrier(Piece caster, Vector2 targetSquare)
    {
        Vector3 casterPoint = GetChestPoint(caster) + Vector3.up * 0.10f;
        PlayOrbCharge(casterPoint, MysticColor, false, 0.30f, 0.36f);
        SpawnEffect("VFX_Klaus/HCFX_Shine_07", casterPoint, Quaternion.identity, 0.16f, MultiplyColor(MysticColor, 1.10f), 0.24f);
    }

    private void PlayQueenDive(Piece caster, Vector2 targetSquare)
    {
        PlayDiveSequence(caster, targetSquare);
    }

    private void PlayMindControl(Piece caster, LogicManager logicManager, Vector2 targetSquare)
    {
        Vector3 casterPoint = GetChestPoint(caster) + Vector3.up * 0.10f;
        PlayOrbCharge(casterPoint, MindControlColor, false, 0.32f, 0.34f);
        SpawnEffect("VFX_Klaus/HCFX_Shine_07", casterPoint, Quaternion.identity, 0.14f, MultiplyColor(MindControlColor, 1.12f), 0.22f);
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
        SpawnEffect("VFX_Klaus/HCFX_Shine_07", GetGroundPoint(targetSquare) + Vector3.up * 0.12f, Quaternion.identity, 0.46f, HolyColor, 1.2f);
    }

    private void PlayRampartAura(Piece caster, LogicManager logicManager)
    {
        PlaySubtleCharge(GetGroundPoint(caster.GetCoordinates()), HolyColor);
        SpawnRingPulse(GetGroundPoint(caster.GetCoordinates()), HolyColor, 0.55f, 0.055f, 0.34f);
        SpawnTileField(GetRampartProtectedSquares(caster, logicManager), MultiplyColor(HolyColor, 0.9f), "VFX_Klaus/HCFX_Shine_07");
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
        SpawnEffect("VFX_Klaus/HCFX_Portal_01", targetPoint, Quaternion.identity, 0.42f, FireColor, 1.8f);
        SpawnRingPulse(GetGroundPoint(targetSquare), FireColor, 0.36f, 0.04f, 0.38f);
        SpawnPolyline(new[] { casterPoint, targetPoint + Vector3.up * 0.55f }, MultiplyColor(FireColor, 0.85f), 0.04f, 0.20f);
    }

    private void PlaySubtleCharge(Vector3 position, Color color)
    {
        SpawnRingPulse(position, MultiplyColor(color, 0.9f), 0.18f, 0.03f, 0.18f);
    }

    private void PlayBeamBurst(Vector3 start, Vector3 end, Color color, float width)
    {
        SpawnPolyline(new[] { start, end }, color, width, 0.22f);
        SpawnEffect("VFX_Klaus/HCFX_Hit_10", end, Quaternion.identity, 0.33f, color, 1f);
    }

    private void PlayDiveSequence(Piece caster, Vector2 targetSquare)
    {
        Vector3 chargePoint = GetChestPoint(caster) + Vector3.up * 0.10f;
        Vector3 ventPoint = chargePoint + Vector3.up * 0.75f;
        Vector3 impactPoint = GetGroundPoint(targetSquare) + Vector3.up * 0.12f;
        Vector3 strikeStart = impactPoint + Vector3.up * 2.40f;

        PlayOrbCharge(chargePoint, FireColor, true, 0.36f, 0.50f);
        SpawnPolyline(new[] { chargePoint, ventPoint }, MultiplyColor(FireColor, 0.88f), 0.030f, 0.15f);
        SpawnEffect("SpellVFX/fx_light_particle_c", ventPoint, Quaternion.identity, 0.22f, FireColor, 0.28f);

        SpawnEffect("SpellVFX/fx_light_particle_c", strikeStart, Quaternion.identity, 0.26f, FireColor, 0.38f);
        SpawnPolyline(new[] { strikeStart, impactPoint }, MultiplyColor(FireColor, 0.98f), 0.065f, 0.20f);
        SpawnEffect("SpellVFX/fx_light_particle_b", impactPoint, Quaternion.identity, 0.36f, FireColor, 0.50f);
        SpawnRingPulse(GetGroundPoint(targetSquare), MultiplyColor(FireColor, 0.90f), 0.20f, 0.032f, 0.18f);
    }

    private void PlayOrbCharge(Vector3 position, Color color, bool addFlame, float orbScale, float lifetime)
    {
        GameObject orb = SpawnEffect("VFX_Klaus/HCFX_Energy_06", position, Quaternion.identity, orbScale, color, lifetime);
        if (orb != null)
        {
            StartCoroutine(AnimateRise(orb.transform, Vector3.up * 0.16f, Mathf.Min(lifetime, 0.20f)));
        }

        SpawnEffect("VFX_Klaus/HCFX_Shine_07", position, Quaternion.identity, orbScale * 0.48f, MultiplyColor(color, 1.08f), lifetime * 0.75f);

        if (addFlame)
        {
            SpawnEffect("VFX_Klaus/HCFX_Fire_01", position, Quaternion.identity, orbScale * 0.82f, color, lifetime);
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
        foreach (string candidate in GetCandidateResourcePaths(path))
        {
            GameObject prefab = Resources.Load<GameObject>(candidate);
            if (prefab != null)
            {
                return prefab;
            }
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
            yield return normalized.Substring(slashIndex + 1);
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

    private Color MultiplyColor(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a);
    }
}
