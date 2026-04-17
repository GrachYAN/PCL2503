using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkillPreviewGalleryBuilder
{
    private const string PreviewScenePath = "Assets/Scenes/VFXTempReview.unity";
    private const string GalleryRootName = "SkillEffectPreviewGallery";
    private const string PreviewCameraName = "PreviewCamera";
    private const string PreviewFolder = "Assets/preview";

    private static readonly string[] GroupNames =
    {
        "01_CrystallinePush_hit",
        "02_BattleRally_cast",
        "03_ScorchingRay_cast",
        "04_PrismaticBarrier_cast",
        "05_FortifiedRampart_cast",
        "06_QueenDive_cast",
        "07_QueenDive_impact",
        "08_AshenRebirth_cast",
        "09_MindControl_cast",
        "10_MindControl_status",
        "11_SunwellAnthem_cast",
        "12_CarryAlly_cast",
        "13_GemstoneSmash_hit",
        "14_HeartOfTheMountain_cast",
        "15_Stun_status",
        "16_Burning_status"
    };

    private static readonly Vector3[] GroupPositions =
    {
        new Vector3(-180f, 0f, -180f),
        new Vector3(-60f, 0f, -180f),
        new Vector3(60f, 0f, -180f),
        new Vector3(180f, 0f, -180f),
        new Vector3(-180f, 0f, -60f),
        new Vector3(-60f, 0f, -60f),
        new Vector3(60f, 0f, -60f),
        new Vector3(180f, 0f, -60f),
        new Vector3(-180f, 0f, 60f),
        new Vector3(-60f, 0f, 60f),
        new Vector3(60f, 0f, 60f),
        new Vector3(180f, 0f, 60f),
        new Vector3(-180f, 0f, 180f),
        new Vector3(-60f, 0f, 180f),
        new Vector3(60f, 0f, 180f),
        new Vector3(180f, 0f, 180f)
    };

    private static readonly Color PhysicalColor = new Color(0.86f, 0.88f, 0.94f, 0.95f);
    private static readonly Color FireColor = new Color(1.00f, 0.47f, 0.18f, 0.95f);
    private static readonly Color HolyColor = new Color(1.00f, 0.84f, 0.35f, 0.95f);
    private static readonly Color ArcaneColor = new Color(0.42f, 0.88f, 1.00f, 0.95f);
    private static readonly Color MysticColor = new Color(0.76f, 0.42f, 1.00f, 0.93f);
    private static readonly Color BuffColor = new Color(0.55f, 1.00f, 0.72f, 0.92f);

    private static Material sharedLineMaterial;

    [MenuItem("Tools/VFX/Build Skill Preview Gallery")]
    public static void BuildGalleryMenu()
    {
        Debug.Log(BuildGallery());
    }

    [MenuItem("Tools/VFX/Export Skill Preview Screenshots")]
    public static void ExportScreenshotsMenu()
    {
        Debug.Log(ExportScreenshots());
    }

    [MenuItem("Tools/VFX/Build And Export Skill Preview")]
    public static void BuildAndExportMenu()
    {
        Debug.Log(BuildGalleryAndExport());
    }

    public static string BuildGalleryAndExport()
    {
        string buildResult = BuildGallery();
        string exportResult = ExportScreenshots();
        return buildResult + "\n" + exportResult;
    }

    public static string BuildGallery()
    {
        SceneSetupResult setup = PrepareScene();
        if (!setup.Success)
        {
            return setup.Message;
        }

        sharedLineMaterial = CreateLineMaterial();

        GameObject existingGallery = GameObject.Find(GalleryRootName);
        if (existingGallery != null)
        {
            Object.DestroyImmediate(existingGallery);
        }

        GameObject strayManager = GameObject.Find("SpellVFXManager");
        if (strayManager != null)
        {
            Object.DestroyImmediate(strayManager);
        }

        SetRootActive("PackageSelectionPreviewRoot", false);
        SetRootActive("SkillPreviewSceneRoot", false);

        GameObject galleryRoot = new GameObject(GalleryRootName);
        CreateGalleryFloor(galleryRoot.transform);

        for (int i = 0; i < GroupNames.Length; i++)
        {
            BuildGroup(galleryRoot.transform, GroupNames[i], GroupPositions[i]);
        }

        Camera previewCamera = FindPreviewCamera();
        if (previewCamera != null)
        {
            previewCamera.transform.position = new Vector3(-20f, 18f, -20f);
            previewCamera.transform.rotation = Quaternion.Euler(32f, 42f, 0f);
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        }

        EditorSceneManager.MarkSceneDirty(setup.Scene);
        EditorSceneManager.SaveScene(setup.Scene);
        AssetDatabase.SaveAssets();

        return $"Built {GalleryRootName} with {GroupNames.Length} preview groups in {setup.Scene.name}.";
    }

    public static string ExportScreenshots()
    {
        SceneSetupResult setup = PrepareScene();
        if (!setup.Success)
        {
            return setup.Message;
        }

        Camera previewCamera = FindPreviewCamera();
        if (previewCamera == null)
        {
            return "PreviewCamera was not found in VFXTempReview.";
        }

        EnsurePreviewFolder();

        List<string> savedPaths = new List<string>();
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        float previousFov = previewCamera.fieldOfView;
        GameObject galleryRoot = GameObject.Find(GalleryRootName);
        if (galleryRoot == null)
        {
            return "SkillEffectPreviewGallery was not found in scene.";
        }
        Transform floorTransform = galleryRoot.transform.Find("GalleryFloor");
        if (floorTransform != null)
        {
            floorTransform.gameObject.SetActive(false);
        }

        for (int i = 0; i < GroupNames.Length; i++)
        {
            Transform groupTransform = galleryRoot.transform.Find(GroupNames[i]);
            GameObject group = groupTransform != null ? groupTransform.gameObject : null;
            if (group == null)
            {
                Debug.LogWarning("Missing preview group: " + GroupNames[i]);
                continue;
            }

            SetActiveExportGroup(galleryRoot.transform, GroupNames[i]);
            SetPieceRootsVisible(group.transform, false);
            CameraShot shot = GetShotForGroup(GroupNames[i], group);
            previewCamera.fieldOfView = shot.FieldOfView;
            previewCamera.transform.position = shot.Position;
            previewCamera.transform.rotation = Quaternion.LookRotation((shot.Target - shot.Position).normalized, Vector3.up);
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);

            string assetPath = Path.Combine(PreviewFolder, GroupNames[i] + ".png").Replace("\\", "/");
            string absolutePath = Path.Combine(projectRoot, assetPath);
            RenderCameraToPng(previewCamera, absolutePath, 1600, 900);
            SetPieceRootsVisible(group.transform, true);
            savedPaths.Add(assetPath);
        }

        SetActiveExportGroup(galleryRoot.transform, null);
        if (floorTransform != null)
        {
            floorTransform.gameObject.SetActive(true);
        }
        previewCamera.fieldOfView = previousFov;
        AssetDatabase.Refresh();
        return "Exported preview screenshots:\n" + string.Join("\n", savedPaths.ToArray());
    }

    private static void BuildGroup(Transform parent, string groupName, Vector3 center)
    {
        GameObject group = new GameObject(groupName);
        group.transform.SetParent(parent, true);
        group.transform.position = center;

        CreateShotAnchor(group.transform, center);

        switch (groupName)
        {
            case "01_CrystallinePush_hit":
                BuildCrystallinePush(group.transform, center);
                break;
            case "02_BattleRally_cast":
                BuildBattleRally(group.transform, center);
                break;
            case "03_ScorchingRay_cast":
                BuildScorchingRay(group.transform, center);
                break;
            case "04_PrismaticBarrier_cast":
                BuildPrismaticBarrier(group.transform, center);
                break;
            case "05_FortifiedRampart_cast":
                BuildFortifiedRampart(group.transform, center);
                break;
            case "06_QueenDive_cast":
                BuildQueenDiveCast(group.transform, center);
                break;
            case "07_QueenDive_impact":
                BuildQueenDiveImpact(group.transform, center);
                break;
            case "08_AshenRebirth_cast":
                BuildAshenRebirth(group.transform, center);
                break;
            case "09_MindControl_cast":
                BuildMindControlCast(group.transform, center);
                break;
            case "10_MindControl_status":
                BuildMindControlStatus(group.transform, center);
                break;
            case "11_SunwellAnthem_cast":
                BuildSunwellAnthem(group.transform, center);
                break;
            case "12_CarryAlly_cast":
                BuildCarryAlly(group.transform, center);
                break;
            case "13_GemstoneSmash_hit":
                BuildGemstoneSmash(group.transform, center);
                break;
            case "14_HeartOfTheMountain_cast":
                BuildHeartOfTheMountain(group.transform, center);
                break;
            case "15_Stun_status":
                BuildStunStatus(group.transform, center);
                break;
            case "16_Burning_status":
                BuildBurningStatus(group.transform, center);
                break;
        }
    }

    private static void BuildCrystallinePush(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "CrystallineStage", center + new Vector3(0f, 0f, 0.10f), 4.20f, 3.40f, new Color(0.11f, 0.13f, 0.16f, 1f));

        Vector3 source = center + new Vector3(-1.30f, 0.86f, -0.62f);
        Vector3 mid = center + new Vector3(-0.08f, 0.98f, -0.04f);
        Vector3 impact = center + new Vector3(1.18f, 0.76f, 0.54f);

        CreateOrb(group, "CrystallineSource", source, MultiplyAlpha(ArcaneColor, 0.86f), 0.26f, 0.58f);
        CreateLine(group, "CrystallineStrikeOuter", new[] { source, mid, impact }, MultiplyColor(ArcaneColor, 0.86f), 0.085f);
        CreateLine(group, "CrystallineStrikeInner", new[] { source, impact }, MultiplyAlpha(ArcaneColor, 0.96f), 0.040f);
        CreateImpactHit(group, impact, MultiplyAlpha(ArcaneColor, 0.92f), 0.42f, 0.42f);
        CreateRing(group, "CrystallineImpactEcho", new Vector3(impact.x, 0.06f, impact.z), MultiplyAlpha(ArcaneColor, 0.44f), 0.62f, 0.024f);
    }

    private static void BuildBattleRally(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "BattleRallyStage", center + new Vector3(0f, 0f, 0.12f), 4.70f, 4.20f, new Color(0.11f, 0.16f, 0.13f, 1f));

        Vector3 centerNode = center + new Vector3(-0.05f, 0.06f, -0.18f);
        Vector3 allyA = center + new Vector3(-1.08f, 0.06f, 0.78f);
        Vector3 allyB = center + new Vector3(0.98f, 0.06f, 0.88f);
        Vector3 allyC = center + new Vector3(1.18f, 0.06f, -0.56f);

        CreateRing(group, "BattleRallyCasterRing", centerNode, MultiplyAlpha(BuffColor, 0.96f), 0.74f, 0.060f);
        CreateRing(group, "BattleRallyInner", centerNode + Vector3.up * 0.08f, MultiplyAlpha(BuffColor, 0.42f), 0.40f, 0.022f);
        CreateRing(group, "BattleRallyA", allyA, MultiplyColor(BuffColor, 0.86f), 0.36f, 0.036f);
        CreateRing(group, "BattleRallyB", allyB, MultiplyColor(BuffColor, 0.86f), 0.34f, 0.036f);
        CreateRing(group, "BattleRallyC", allyC, MultiplyColor(BuffColor, 0.86f), 0.32f, 0.036f);

        Vector3 hub = centerNode + Vector3.up * 0.78f;
        SpawnPrefabFx("Assets/Resources/QK/Qk_light_arrow_01_ready_01.prefab", group, hub, Quaternion.identity, 0.30f, MultiplyAlpha(BuffColor, 0.44f), 0.16f);
        CreateOrb(group, "BattleRallyHub", hub + new Vector3(0.02f, 0.02f, -0.04f), MultiplyAlpha(BuffColor, 0.82f), 0.24f, 0.46f);
        CreateLine(group, "BattleRallyLinkA", new[] { hub, allyA + Vector3.up * 0.14f }, MultiplyAlpha(BuffColor, 0.54f), 0.042f);
        CreateLine(group, "BattleRallyLinkB", new[] { hub, allyB + Vector3.up * 0.14f }, MultiplyAlpha(BuffColor, 0.54f), 0.042f);
        CreateLine(group, "BattleRallyLinkC", new[] { hub, allyC + Vector3.up * 0.14f }, MultiplyAlpha(BuffColor, 0.54f), 0.042f);
    }

    private static void BuildScorchingRay(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "ScorchingStage", center + new Vector3(0.12f, 0f, 0.08f), 5.60f, 3.80f, new Color(0.18f, 0.11f, 0.08f, 1f));

        Vector3 charge = center + new Vector3(-1.38f, 0.92f, -0.52f);
        Vector3 beamStart = center + new Vector3(-1.00f, 1.38f, -0.28f);
        Vector3 beamMid = center + new Vector3(-0.10f, 1.68f, 0.05f);
        Vector3 impact = center + new Vector3(1.34f, 0.84f, 0.62f);

        CreateFireCharge(group, charge, 1.06f, 0.68f, 0.20f, true);
        CreateOrb(group, "ScorchingSourceOrb", beamStart + new Vector3(-0.06f, 0.02f, -0.02f), MultiplyAlpha(FireColor, 0.90f), 0.34f, 0.54f);
        CreateLine(group, "ScorchingRayOuter", new[] { beamStart, beamMid, impact }, MultiplyColor(FireColor, 0.92f), 0.180f);
        CreateLine(group, "ScorchingRayInner", new[] { beamStart, impact }, MultiplyColor(FireColor, 1.04f), 0.084f);
        CreateLine(group, "ScorchingRayGlow", new[] { beamStart + new Vector3(0f, 0.05f, -0.02f), impact + new Vector3(0f, 0.04f, 0f) }, MultiplyAlpha(FireColor, 0.50f), 0.040f);
        CreateImpactHit(group, impact, MultiplyAlpha(FireColor, 0.92f), 0.38f, 0.34f);
        CreateRing(group, "ScorchingImpactEcho", new Vector3(impact.x, 0.06f, impact.z), MultiplyAlpha(FireColor, 0.42f), 0.58f, 0.022f);
    }

    private static void BuildPrismaticBarrier(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "PrismaticStage", center + new Vector3(0f, 0f, 0.08f), 3.60f, 3.40f, new Color(0.14f, 0.10f, 0.18f, 1f));
        Vector3 point = center + new Vector3(0f, 1.02f, 0.02f);

        CreateMysticCharge(
            group,
            point,
            MultiplyAlpha(MultiplyColor(MysticColor, 0.90f), 0.80f),
            MultiplyAlpha(MultiplyColor(MysticColor, 1.04f), 0.88f),
            MultiplyAlpha(new Color(0.94f, 0.76f, 1.00f, 1.00f), 0.46f),
            1.20f,
            0.78f,
            0.54f,
            0.24f);

        CreateOrb(group, "BarrierShell", point + new Vector3(0.02f, 0.06f, -0.02f), MultiplyAlpha(new Color(0.84f, 0.60f, 1f, 1f), 0.22f), 0.84f, 0.18f);
        CreateRing(group, "BarrierHaloLower", point + Vector3.up * 0.02f, MultiplyAlpha(MysticColor, 0.34f), 0.56f, 0.024f);
        CreateRing(group, "BarrierHaloUpper", point + Vector3.up * 0.22f, MultiplyAlpha(MysticColor, 0.28f), 0.38f, 0.018f);
        CreateOrb(group, "BarrierSatelliteA", point + new Vector3(-0.36f, 0.10f, 0.18f), MultiplyAlpha(MysticColor, 0.60f), 0.18f, 0.38f);
        CreateOrb(group, "BarrierSatelliteB", point + new Vector3(0.42f, -0.06f, 0.12f), MultiplyAlpha(MysticColor, 0.56f), 0.14f, 0.32f);
        CreateLine(group, "BarrierSatelliteLinkA", new[] { point + Vector3.up * 0.02f, point + new Vector3(-0.22f, 0.06f, 0.12f), point + new Vector3(-0.36f, 0.10f, 0.18f) }, MultiplyAlpha(MysticColor, 0.42f), 0.024f);
        CreateLine(group, "BarrierSatelliteLinkB", new[] { point + Vector3.up * 0.02f, point + new Vector3(0.20f, -0.02f, 0.10f), point + new Vector3(0.42f, -0.06f, 0.12f) }, MultiplyAlpha(MysticColor, 0.38f), 0.022f);
        CreateRing(group, "BarrierFootRing", center + new Vector3(0f, 0.06f, 0.02f), MultiplyAlpha(MysticColor, 0.22f), 0.86f, 0.028f);
    }

    private static void BuildFortifiedRampart(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "RampartStage", center + new Vector3(0f, 0f, 0.12f), 4.80f, 4.30f, new Color(0.18f, 0.15f, 0.10f, 1f));

        Vector3 caster = center + new Vector3(-0.10f, 0.06f, -0.20f);
        Vector3 allyA = center + new Vector3(1.22f, 0.06f, -0.72f);
        Vector3 allyB = center + new Vector3(-1.10f, 0.06f, 0.84f);
        Vector3 allyC = center + new Vector3(1.16f, 0.06f, 0.86f);

        CreateRing(group, "RampartCasterRing", caster, MultiplyAlpha(HolyColor, 0.96f), 0.76f, 0.060f);
        CreateRing(group, "RampartCasterInner", caster + Vector3.up * 0.08f, MultiplyAlpha(HolyColor, 0.42f), 0.42f, 0.022f);
        CreateTileBlessing(group, allyA, MultiplyColor(HolyColor, 0.90f), "Assets/Resources/HCFX/HCFX_Shine_07.prefab", 0.34f);
        CreateTileBlessing(group, allyB, MultiplyColor(HolyColor, 0.90f), "Assets/Resources/HCFX/HCFX_Shine_07.prefab", 0.34f);
        CreateTileBlessing(group, allyC, MultiplyColor(HolyColor, 0.90f), "Assets/Resources/HCFX/HCFX_Shine_07.prefab", 0.34f);
        CreateLine(group, "RampartSupportA", new[] { caster + Vector3.up * 0.24f, allyA + Vector3.up * 0.14f }, MultiplyAlpha(HolyColor, 0.34f), 0.030f);
        CreateLine(group, "RampartSupportB", new[] { caster + Vector3.up * 0.24f, allyB + Vector3.up * 0.14f }, MultiplyAlpha(HolyColor, 0.34f), 0.030f);
        CreateLine(group, "RampartSupportC", new[] { caster + Vector3.up * 0.24f, allyC + Vector3.up * 0.14f }, MultiplyAlpha(HolyColor, 0.34f), 0.030f);
    }

    private static void BuildQueenDiveCast(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "QueenDiveCastStage", center + new Vector3(0f, 0f, 0.08f), 3.80f, 3.80f, new Color(0.18f, 0.11f, 0.08f, 1f));

        Vector3 charge = center + new Vector3(-0.10f, 0.92f, -0.08f);
        Vector3 vent = center + new Vector3(0.00f, 1.72f, 0.00f);
        Vector3 overhead = center + new Vector3(0.54f, 2.26f, 0.34f);
        Vector3 trailMid = center + new Vector3(0.30f, 1.72f, 0.18f);

        CreateFireCharge(group, charge, 1.04f, 0.68f, 0.18f, false);
        CreateLine(group, "QueenDiveVent", new[] { charge, charge + new Vector3(0.02f, 0.34f, 0.02f), vent }, MultiplyAlpha(FireColor, 0.84f), 0.050f);
        CreateOrb(group, "QueenDiveOrb", overhead, MultiplyAlpha(FireColor, 0.92f), 0.32f, 0.68f);
        CreateLine(group, "QueenDiveStrikeGuide", new[] { overhead, trailMid, center + new Vector3(0.10f, 1.10f, 0.08f) }, MultiplyAlpha(FireColor, 0.50f), 0.060f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_fire_arrow_01_hit_01.prefab", group, overhead, Quaternion.identity, 0.34f, MultiplyAlpha(FireColor, 0.84f), 0.10f);
        CreateRing(group, "QueenDiveMark", center + new Vector3(0.16f, 0.06f, 0.10f), MultiplyAlpha(FireColor, 0.30f), 0.34f, 0.024f);
    }

    private static void BuildQueenDiveImpact(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "QueenDiveImpactStage", center + new Vector3(0f, 0f, 0.08f), 4.20f, 4.00f, new Color(0.18f, 0.11f, 0.08f, 1f));

        Vector3 impact = center + new Vector3(0.28f, 0.10f, 0.18f);
        Vector3 strikeMid = center + new Vector3(0.02f, 1.08f, 0.02f);
        Vector3 strikeStart = center + new Vector3(-0.42f, 2.04f, -0.34f);

        CreateOrb(group, "DiveStrikeOrb", strikeStart, MultiplyAlpha(FireColor, 0.94f), 0.44f, 0.80f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_fire_arrow_01_hit_01.prefab", group, strikeStart, Quaternion.identity, 0.30f, MultiplyAlpha(FireColor, 0.82f), 0.08f);
        CreateLine(group, "DiveStrikeOuter", new[] { strikeStart, strikeMid, impact }, MultiplyAlpha(FireColor, 0.94f), 0.130f);
        CreateLine(group, "DiveStrikeInner", new[] { strikeStart, impact }, MultiplyAlpha(FireColor, 1.00f), 0.060f);
        CreateImpactHit(group, impact, MultiplyAlpha(FireColor, 0.92f), 0.40f, 0.40f);
        SpawnPrefabFx("Assets/Resources/BUFF/ranshao.prefab", group, impact + Vector3.up * 0.52f, Quaternion.identity, 0.44f, MultiplyAlpha(FireColor, 0.62f), 0.26f);
        CreateRing(group, "DiveBurnRing", new Vector3(impact.x, 0.06f, impact.z), MultiplyAlpha(FireColor, 0.30f), 0.66f, 0.026f);
    }

    private static void BuildAshenRebirth(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "AshenStage", center + new Vector3(0f, 0f, 0.10f), 4.60f, 3.80f, new Color(0.18f, 0.11f, 0.08f, 1f));

        Vector3 casterPoint = center + new Vector3(-1.28f, 0.92f, -0.46f);
        Vector3 targetPoint = center + new Vector3(1.10f, 0.18f, 0.68f);

        CreateRing(group, "AshenCasterRing", center + new Vector3(-1.28f, 0.06f, -0.46f), MultiplyAlpha(FireColor, 0.64f), 0.34f, 0.032f);
        SpawnPrefabFx("Assets/Resources/HCFX/HCFX_Portal_01.prefab", group, targetPoint, Quaternion.identity, 0.76f, MultiplyAlpha(FireColor, 0.88f), 0.18f);
        CreateOrb(group, "AshenTargetOrb", targetPoint + new Vector3(0.02f, 0.18f, -0.02f), MultiplyAlpha(FireColor, 0.84f), 0.26f, 0.36f);
        CreateRing(group, "AshenTargetRing", new Vector3(targetPoint.x, 0.06f, targetPoint.z), MultiplyAlpha(FireColor, 0.72f), 0.54f, 0.042f);
        CreateLine(group, "AshenLink", new[] { casterPoint, center + new Vector3(0.10f, 1.32f, 0.10f), targetPoint + Vector3.up * 0.55f }, MultiplyColor(FireColor, 0.86f), 0.060f);
        CreateOrb(group, "AshenSource", casterPoint + new Vector3(0.02f, 0.06f, -0.04f), MultiplyAlpha(FireColor, 0.82f), 0.24f, 0.42f);
    }

    private static void BuildMindControlCast(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "MindControlStage", center + new Vector3(0f, 0f, 0.08f), 3.80f, 3.40f, new Color(0.14f, 0.10f, 0.18f, 1f));
        Vector3 casterPoint = center + new Vector3(0f, 1.00f, 0.02f);

        CreateMysticCharge(
            group,
            casterPoint,
            MultiplyAlpha(new Color(0.56f, 0.32f, 0.82f, 1.00f), 0.72f),
            MultiplyAlpha(new Color(0.74f, 0.44f, 0.96f, 1.00f), 0.76f),
            MultiplyAlpha(new Color(0.66f, 0.40f, 0.92f, 1.00f), 0.34f),
            1.08f,
            0.68f,
            0.46f,
            0.22f);

        CreateLine(group, "MindBandOuter", new[]
        {
            casterPoint + new Vector3(-0.84f, 0.02f, 0.10f),
            casterPoint + new Vector3(0f, 0.08f, 0f),
            casterPoint + new Vector3(0.84f, -0.02f, 0.08f)
        }, MultiplyAlpha(new Color(0.72f, 0.45f, 0.94f, 1.00f), 0.42f), 0.060f);
        CreateLine(group, "MindBandInner", new[]
        {
            casterPoint + new Vector3(-0.56f, 0.00f, 0.02f),
            casterPoint + new Vector3(0f, 0.04f, 0f),
            casterPoint + new Vector3(0.56f, 0.00f, 0.02f)
        }, MultiplyAlpha(new Color(0.88f, 0.62f, 1.00f, 1.00f), 0.46f), 0.026f);
        CreateRing(group, "MindHaloUpper", casterPoint + Vector3.up * 0.22f, MultiplyAlpha(MysticColor, 0.28f), 0.36f, 0.018f);
        CreateRing(group, "MindHaloLower", casterPoint + Vector3.down * 0.16f, MultiplyAlpha(MysticColor, 0.24f), 0.58f, 0.022f);
        CreateOrb(group, "MindNodeLeft", casterPoint + new Vector3(-0.52f, 0.04f, 0.12f), MultiplyAlpha(MysticColor, 0.56f), 0.16f, 0.30f);
        CreateOrb(group, "MindNodeRight", casterPoint + new Vector3(0.56f, -0.02f, 0.14f), MultiplyAlpha(MysticColor, 0.56f), 0.16f, 0.30f);
        CreateRing(group, "MindFootRing", center + new Vector3(0f, 0.06f, 0.02f), MultiplyAlpha(MysticColor, 0.20f), 0.78f, 0.026f);
    }

    private static void BuildMindControlStatus(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "MindStatusStage", center + new Vector3(0f, 0f, 0.10f), 3.80f, 3.40f, new Color(0.13f, 0.10f, 0.17f, 1f));

        Vector3 body = center + new Vector3(0f, 0.82f, 0.10f);
        CreateFocusSilhouette(group, "MindStatusHost", body, 1.50f, 0.24f, new Color(0.12f, 0.12f, 0.16f, 0.28f));
        Vector3 ground = center + new Vector3(0f, 0.06f, 0.10f);
        Vector3 chest = center + new Vector3(0f, 1.00f, 0.10f);
        Vector3 head = center + new Vector3(0f, 1.58f, 0.10f);

        CreateRing(group, "MindStatusBaseRing", ground, MultiplyAlpha(MysticColor, 0.22f), 0.78f, 0.026f);
        SpawnPrefabFx("Assets/Resources/BUFF/shuimian.prefab", group, head + Vector3.up * 0.18f, Quaternion.identity, 0.82f, SpellVFXManager.MindControlColor, 0.55f);
        CreateMindStatus(group, ground, chest, head);
    }

    private static void BuildSunwellAnthem(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "SunwellStage", center + new Vector3(0f, 0f, 0.10f), 4.70f, 4.20f, new Color(0.18f, 0.15f, 0.10f, 1f));

        Vector3 caster = center + new Vector3(-0.10f, 0.06f, -0.20f);
        Vector3 allyA = center + new Vector3(1.12f, 0.06f, -0.70f);
        Vector3 allyB = center + new Vector3(0.96f, 0.06f, 0.88f);
        Vector3 allyC = center + new Vector3(-1.06f, 0.06f, 0.82f);

        CreateRing(group, "SunwellCasterRing", caster, MultiplyAlpha(HolyColor, 0.94f), 0.72f, 0.058f);
        CreateRing(group, "SunwellCasterInner", caster + Vector3.up * 0.08f, MultiplyAlpha(HolyColor, 0.40f), 0.38f, 0.022f);
        CreateTileBlessing(group, allyA, HolyColor, "Assets/Resources/HCFX/HCFX_Shine_07.prefab", 0.32f);
        CreateTileBlessing(group, allyB, HolyColor, "Assets/Resources/HCFX/HCFX_Shine_07.prefab", 0.32f);
        CreateTileBlessing(group, allyC, MultiplyAlpha(HolyColor, 0.84f), "Assets/Resources/HCFX/HCFX_Shine_07.prefab", 0.28f);
        CreateLine(group, "SunwellLinkA", new[] { caster + Vector3.up * 0.20f, allyA + Vector3.up * 0.12f }, MultiplyAlpha(HolyColor, 0.28f), 0.028f);
        CreateLine(group, "SunwellLinkB", new[] { caster + Vector3.up * 0.20f, allyB + Vector3.up * 0.12f }, MultiplyAlpha(HolyColor, 0.28f), 0.028f);
        CreateLine(group, "SunwellLinkC", new[] { caster + Vector3.up * 0.20f, allyC + Vector3.up * 0.12f }, MultiplyAlpha(HolyColor, 0.24f), 0.026f);
    }

    private static void BuildCarryAlly(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "CarryStage", center + new Vector3(0f, 0f, 0.08f), 4.40f, 3.60f, new Color(0.18f, 0.15f, 0.10f, 1f));

        Vector3 source = center + new Vector3(-1.16f, 1.02f, -0.36f);
        Vector3 target = center + new Vector3(1.08f, 0.18f, 0.62f);

        SpawnPrefabFx("Assets/Resources/HCFX/HCFX_Shine_07.prefab", group, target, Quaternion.identity, 0.72f, HolyColor, 0.16f);
        CreateOrb(group, "CarryTargetOrb", target + new Vector3(0.02f, 0.16f, -0.02f), MultiplyAlpha(HolyColor, 0.80f), 0.22f, 0.30f);
        CreateRing(group, "CarryRing", new Vector3(target.x, 0.06f, target.z), HolyColor, 0.54f, 0.050f);
        CreateRing(group, "CarryRingInner", new Vector3(target.x, 0.08f, target.z), MultiplyAlpha(HolyColor, 0.44f), 0.28f, 0.020f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_light_arrow_01_ready_01.prefab", group, source, Quaternion.identity, 0.24f, MultiplyAlpha(HolyColor, 0.28f), 0.12f);
        CreateOrb(group, "CarrySourceOrb", source + new Vector3(0.02f, 0.04f, -0.04f), MultiplyAlpha(HolyColor, 0.66f), 0.18f, 0.34f);
        CreateLine(group, "CarryLink", new[] { source, center + new Vector3(0.06f, 1.22f, 0.04f), target + Vector3.up * 0.42f }, MultiplyAlpha(HolyColor, 0.44f), 0.050f);
    }

    private static void BuildGemstoneSmash(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "GemstoneStage", center + new Vector3(0f, 0f, 0.10f), 4.30f, 3.40f, new Color(0.18f, 0.15f, 0.10f, 1f));

        Vector3 source = center + new Vector3(-1.26f, 0.86f, -0.58f);
        Vector3 impact = center + new Vector3(1.18f, 0.78f, 0.56f);
        Vector3 mid = center + new Vector3(-0.04f, 1.02f, 0.02f);

        CreateOrb(group, "GemstoneSource", source, MultiplyAlpha(HolyColor, 0.84f), 0.24f, 0.52f);
        CreateLine(group, "GemstoneStrikeOuter", new[] { source, mid, impact }, MultiplyColor(HolyColor, 0.84f), 0.110f);
        CreateLine(group, "GemstoneStrikeInner", new[] { source, impact }, MultiplyAlpha(HolyColor, 0.96f), 0.052f);
        CreateImpactHit(group, impact, MultiplyAlpha(HolyColor, 0.96f), 0.50f, 0.58f);
        CreateRing(group, "GemstoneEcho", new Vector3(impact.x, 0.06f, impact.z), MultiplyAlpha(HolyColor, 0.36f), 0.82f, 0.026f);
    }

    private static void BuildHeartOfTheMountain(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "HeartStage", center + new Vector3(0f, 0f, 0.10f), 4.70f, 4.20f, new Color(0.11f, 0.16f, 0.13f, 1f));

        Vector3 caster = center + new Vector3(-0.10f, 0.06f, -0.20f);
        Vector3 allyA = center + new Vector3(1.08f, 0.06f, -0.76f);
        Vector3 allyB = center + new Vector3(0.98f, 0.06f, 0.88f);
        Vector3 allyC = center + new Vector3(-1.08f, 0.06f, 0.84f);

        CreateRing(group, "HeartCasterRing", caster, MultiplyAlpha(BuffColor, 0.94f), 0.72f, 0.058f);
        CreateRing(group, "HeartCasterInner", caster + Vector3.up * 0.08f, MultiplyAlpha(BuffColor, 0.36f), 0.40f, 0.020f);
        CreateTileBlessing(group, allyA, MultiplyColor(BuffColor, 0.96f), "Assets/Resources/HCFX/HCFX_Energy_06.prefab", 0.30f);
        CreateTileBlessing(group, allyB, MultiplyColor(BuffColor, 0.96f), "Assets/Resources/HCFX/HCFX_Energy_06.prefab", 0.30f);
        CreateTileBlessing(group, allyC, MultiplyColor(BuffColor, 0.90f), "Assets/Resources/HCFX/HCFX_Energy_06.prefab", 0.28f);
        CreateLine(group, "HeartLinkA", new[] { caster + Vector3.up * 0.20f, allyA + Vector3.up * 0.12f }, MultiplyAlpha(BuffColor, 0.28f), 0.028f);
        CreateLine(group, "HeartLinkB", new[] { caster + Vector3.up * 0.20f, allyB + Vector3.up * 0.12f }, MultiplyAlpha(BuffColor, 0.28f), 0.028f);
        CreateLine(group, "HeartLinkC", new[] { caster + Vector3.up * 0.20f, allyC + Vector3.up * 0.12f }, MultiplyAlpha(BuffColor, 0.24f), 0.026f);
    }

    private static void BuildStunStatus(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "StunStatusStage", center + new Vector3(0f, 0f, 0.10f), 3.80f, 3.40f, new Color(0.18f, 0.14f, 0.08f, 1f));

        Vector3 body = center + new Vector3(0f, 0.82f, 0.10f);
        Vector3 ground = center + new Vector3(0f, 0.06f, 0.10f);
        Vector3 head = center + new Vector3(0f, 1.58f, 0.10f);

        CreateFocusSilhouette(group, "StunStatusHost", body, 1.50f, 0.24f, new Color(0.14f, 0.14f, 0.14f, 0.30f));
        CreateRing(group, "StunStatusBaseRing", ground, MultiplyAlpha(SpellVFXManager.StunColor, 0.24f), 0.74f, 0.026f);
        CreateRing(group, "StunStatusHeadRing", head + Vector3.down * 0.10f, MultiplyAlpha(SpellVFXManager.StunColor, 0.26f), 0.28f, 0.018f);
        SpawnPrefabFx("Assets/Resources/BUFF/xuanyun.prefab", group, head + Vector3.up * 0.10f, Quaternion.identity, 1.12f, SpellVFXManager.StunColor, 0.68f);
    }

    private static void BuildBurningStatus(Transform group, Vector3 center)
    {
        CreatePreviewStage(group, "BurningStatusStage", center + new Vector3(0f, 0f, 0.10f), 3.80f, 3.40f, new Color(0.18f, 0.11f, 0.08f, 1f));

        Vector3 body = center + new Vector3(0f, 0.82f, 0.10f);
        Vector3 ground = center + new Vector3(0f, 0.06f, 0.10f);
        Vector3 chest = center + new Vector3(0f, 1.00f, 0.10f);
        Vector3 fireRoot = chest + Vector3.down * 0.14f;

        CreateFocusSilhouette(group, "BurningStatusHost", body, 1.50f, 0.24f, new Color(0.14f, 0.13f, 0.12f, 0.28f));
        CreateRing(group, "BurningStatusBaseRing", ground, MultiplyAlpha(FireColor, 0.22f), 0.76f, 0.024f);
        CreateRing(group, "BurningStatusWaistRing", fireRoot + new Vector3(0f, 0.08f, 0f), MultiplyAlpha(FireColor, 0.26f), 0.34f, 0.018f);
        SpawnPrefabFx("Assets/Resources/BUFF/ranshao.prefab", group, fireRoot, Quaternion.identity, 0.78f, FireColor, 0.85f);
    }

    private static void CreateShotAnchor(Transform parent, Vector3 center)
    {
        GameObject anchor = new GameObject("ShotAnchor");
        anchor.transform.SetParent(parent, true);
        anchor.transform.position = center + new Vector3(0f, 0.85f, 0f);
    }

    private static void CreateGalleryFloor(Transform parent)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "GalleryFloor";
        plane.transform.SetParent(parent, true);
        plane.transform.position = Vector3.zero;
        plane.transform.localScale = new Vector3(42f, 1f, 42f);

        GameObject previewFloor = GameObject.Find("PreviewFloor");
        Renderer planeRenderer = plane.GetComponent<Renderer>();
        if (previewFloor != null)
        {
            Renderer previewRenderer = previewFloor.GetComponent<Renderer>();
            if (previewRenderer != null && previewRenderer.sharedMaterial != null)
            {
                planeRenderer.sharedMaterial = new Material(previewRenderer.sharedMaterial);
                SetMaterialColor(planeRenderer.sharedMaterial, new Color(0.18f, 0.19f, 0.21f, 1f));
                return;
            }
        }

        planeRenderer.sharedMaterial = CreateTintedMaterial(new Color(0.18f, 0.19f, 0.21f, 1f), false);
    }

    private static void CreateStage(Transform parent, Vector3 center)
    {
        GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stage.name = "Stage";
        stage.transform.SetParent(parent, true);
        stage.transform.position = center + new Vector3(0f, -0.06f, 0f);
        stage.transform.localScale = new Vector3(26f, 0.12f, 26f);
        RemoveCollider(stage);

        Renderer renderer = stage.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateTintedMaterial(new Color(0.21f, 0.22f, 0.24f, 1f), false);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private static void CreatePreviewStage(Transform parent, string name, Vector3 center, float width, float depth, Color tint)
    {
        GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stage.name = name;
        stage.transform.SetParent(parent, true);
        stage.transform.position = center + new Vector3(0f, -0.04f, 0f);
        stage.transform.localScale = new Vector3(width, 0.08f, depth);
        RemoveCollider(stage);

        Renderer renderer = stage.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateTintedMaterial(tint, false);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        CreateRing(parent, name + "_Outline", center + new Vector3(0f, 0.01f, 0f), new Color(1f, 1f, 1f, 0.05f), Mathf.Min(width, depth) * 0.34f, 0.010f);
    }

    private static void CreateFocusSilhouette(Transform parent, string name, Vector3 position, float height, float width, Color tint)
    {
        GameObject silhouette = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        silhouette.name = name;
        silhouette.transform.SetParent(parent, true);
        silhouette.transform.position = position;
        silhouette.transform.localScale = new Vector3(width, height * 0.5f, width);
        RemoveCollider(silhouette);

        Renderer renderer = silhouette.GetComponent<Renderer>();
        renderer.sharedMaterial = CreateTintedMaterial(tint, true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static GameObject SpawnPiece(string assetPath, Transform parent, Vector3 position, float yaw)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing piece prefab: " + assetPath);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Piece_" + prefab.name.Trim();
        instance.transform.SetParent(parent, true);
        float previewScale = assetPath.Contains("Pawn") ? 0.10f : 0.12f;
        instance.transform.localScale = Vector3.one * previewScale;
        instance.transform.position = new Vector3(position.x, 0f, position.z);
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Bounds bounds = GetBounds(instance);
        instance.transform.position += Vector3.up * (-bounds.min.y + 0.02f);
        return instance;
    }

    private static GameObject SpawnPrefabFx(string assetPath, Transform parent, Vector3 position, Quaternion rotation, float scaleMultiplier, Color tint, float simulateTime)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Missing VFX prefab: " + assetPath);
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = prefab.name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = instance.transform.localScale * scaleMultiplier;

        ApplyTint(instance, tint);
        SimulateParticles(instance, simulateTime);
        return instance;
    }

    private static void ApplyTint(GameObject instanceObject, Color tint)
    {
        if (instanceObject == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = instanceObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            main.startColor = tint;
        }

        Renderer[] renderers = instanceObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] sharedMaterials = renderers[i].sharedMaterials;
            for (int m = 0; m < sharedMaterials.Length; m++)
            {
                if (sharedMaterials[m] == null)
                {
                    continue;
                }

                Material clone = new Material(sharedMaterials[m]);
                SetMaterialColor(clone, tint);
                sharedMaterials[m] = clone;
            }

            renderers[i].sharedMaterials = sharedMaterials;
        }

        TrailRenderer[] trails = instanceObject.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
        {
            trails[i].startColor = tint;
            Color endColor = tint;
            endColor.a = 0f;
            trails[i].endColor = endColor;
        }

        if (instanceObject.name.Contains("xuanyun"))
        {
            Transform[] transforms = instanceObject.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == "Quad")
                {
                    transforms[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private static void SetMaterialColor(Material material, Color tint)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tint);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", tint * 0.45f);
        }
    }

    private static void SimulateParticles(GameObject go, float time)
    {
        if (go == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystems[i].useAutoRandomSeed = false;
            particleSystems[i].randomSeed = 11u;
            particleSystems[i].Simulate(time, true, true, true);
            particleSystems[i].Pause(true);
        }
    }

    private static void CreateFireCharge(Transform parent, Vector3 position, float readyScale, float burstScale, float ringRadius, bool addHeatLine)
    {
        Color readyTint = MultiplyAlpha(MultiplyColor(FireColor, 0.92f), 0.82f);
        Color burstTint = MultiplyAlpha(MultiplyColor(FireColor, 1.04f), 0.84f);
        Vector3 previewOffset = new Vector3(-0.20f, 0.16f, -0.18f);
        Vector3 previewPoint = position + previewOffset;

        SpawnPrefabFx("Assets/Resources/QK/Qk_fire_arrow_01_ready_01.prefab", parent, position, Quaternion.identity, readyScale, readyTint, 0.28f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_fire_arrow_01_hit_01.prefab", parent, position + Vector3.up * 0.01f, Quaternion.identity, burstScale, burstTint, 0.08f);
        CreateOrb(parent, "FireCoreOrb", previewPoint, MultiplyAlpha(FireColor, 0.96f), Mathf.Max(ringRadius * 1.80f, 0.50f), 0.62f);
        CreateRing(parent, "FirePreviewRing", new Vector3(previewPoint.x, position.y + 0.08f, previewPoint.z), MultiplyAlpha(FireColor, 0.46f), ringRadius * 1.18f, 0.018f);
        CreateLine(parent, "FirePreviewLink", new[] { position + Vector3.up * 0.06f, previewPoint }, MultiplyAlpha(FireColor, 0.68f), 0.040f);
        CreateLine(parent, "FirePreviewSpark", new[] { position + new Vector3(-0.02f, 0.12f, 0f), previewPoint + new Vector3(0.06f, 0.04f, 0f) }, MultiplyAlpha(FireColor, 0.44f), 0.022f);
        CreateRing(parent, "FireChargeRing", position, MultiplyAlpha(FireColor, 0.54f), ringRadius, 0.018f);
        CreateRing(parent, "FireChargeHalo", position + Vector3.up * 0.14f, MultiplyAlpha(FireColor, 0.24f), ringRadius * 1.16f, 0.014f);

        if (addHeatLine)
        {
            CreateLine(parent, "HeatLine", new[] { position, position + Vector3.up * 0.30f }, MultiplyAlpha(FireColor, 0.78f), 0.026f);
        }
    }

    private static void CreateMysticCharge(Transform parent, Vector3 position, Color auraColor, Color burstColor, Color coreColor, float auraScale, float burstScale, float coreScale, float ringRadius)
    {
        Vector3 previewOffset = new Vector3(-0.12f, 0.10f, -0.16f);
        Vector3 previewPoint = position + previewOffset;
        SpawnPrefabFx("Assets/Resources/QK/Qk_ice_arrow_01_ready_01.prefab", parent, position, Quaternion.identity, auraScale, auraColor, 0.25f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_light_arrow_01_hit_01.prefab", parent, position + Vector3.up * 0.01f, Quaternion.identity, burstScale, burstColor, 0.10f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_light_arrow_01_ready_01.prefab", parent, position + Vector3.up * 0.02f, Quaternion.identity, coreScale, coreColor, 0.16f);
        CreateOrb(parent, "MysticCoreOrb", previewPoint, MultiplyAlpha(coreColor, 0.98f), Mathf.Max(coreScale * 1.90f, 0.54f), 0.60f);
        CreateOrb(parent, "MysticAuraShell", position + new Vector3(0.02f, 0.06f, -0.02f), MultiplyAlpha(auraColor, 0.24f), Mathf.Max(auraScale * 0.62f, 0.58f), 0.14f);
        CreateRing(parent, "MysticPreviewRing", new Vector3(previewPoint.x, position.y + 0.08f, previewPoint.z), MultiplyAlpha(burstColor, 0.40f), ringRadius * 1.26f, 0.018f);
        CreateLine(parent, "MysticPreviewLink", new[] { position + Vector3.up * 0.06f, previewPoint }, MultiplyAlpha(burstColor, 0.58f), 0.034f);
        CreateRing(parent, "MysticChargeRing", position, MultiplyAlpha(burstColor, 0.48f), ringRadius, 0.018f);
        CreateRing(parent, "MysticChargeHalo", position + Vector3.up * 0.16f, MultiplyAlpha(burstColor, 0.20f), ringRadius * 1.12f, 0.014f);
    }

    private static void CreateImpactHit(Transform parent, Vector3 impactPoint, Color tint, float hitScale, float ringRadius)
    {
        SpawnPrefabFx("Assets/Resources/HCFX/HCFX_Hit_10.prefab", parent, impactPoint, Quaternion.identity, hitScale, tint, 0.10f);
        CreateRing(parent, "ImpactRing", new Vector3(impactPoint.x, 0.05f, impactPoint.z), MultiplyAlpha(tint, 0.72f), ringRadius, 0.018f);
    }

    private static void CreateTileBlessing(Transform parent, Vector3 groundPoint, Color tint, string fxPath, float fxScale)
    {
        CreateRing(parent, "TileRing", groundPoint, MultiplyColor(tint, 0.92f), 0.30f, 0.030f);
        if (!string.IsNullOrEmpty(fxPath))
        {
            SpawnPrefabFx(fxPath, parent, groundPoint + Vector3.up * 0.10f, Quaternion.identity, fxScale, tint, 0.18f);
        }
    }

    private static void CreateMindStatus(Transform parent, GameObject target)
    {
        if (target == null)
        {
            return;
        }

        CreateMindStatus(
            parent,
            GetGroundPoint(target),
            GetChestPoint(target) + Vector3.up * 0.06f,
            GetHeadPoint(target) + Vector3.up * 0.10f);
    }

    private static void CreateMindStatus(Transform parent, Vector3 ground, Vector3 chest, Vector3 head)
    {
        Color aura = MultiplyAlpha(new Color(0.56f, 0.32f, 0.82f, 1.00f), 0.74f);
        Color burst = MultiplyAlpha(new Color(0.74f, 0.44f, 0.96f, 1.00f), 0.72f);
        Color core = MultiplyAlpha(new Color(0.66f, 0.40f, 0.92f, 1.00f), 0.36f);

        CreateRing(parent, "MindStatusGroundRing", ground, MultiplyAlpha(burst, 0.60f), 0.72f, 0.028f);
        CreateRing(parent, "MindStatusChestRing", chest + Vector3.down * 0.08f, MultiplyAlpha(burst, 0.40f), 0.42f, 0.020f);
        CreateRing(parent, "MindStatusHeadRing", head, MultiplyAlpha(burst, 0.70f), 0.26f, 0.018f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_ice_arrow_01_ready_01.prefab", parent, chest, Quaternion.identity, 0.64f, aura, 0.20f);
        SpawnPrefabFx("Assets/Resources/QK/Qk_light_arrow_01_ready_01.prefab", parent, head, Quaternion.identity, 0.44f, core, 0.16f);
        CreateOrb(parent, "MindStatusChestOrb", chest + new Vector3(0.01f, 0.02f, -0.02f), MultiplyAlpha(core, 0.84f), 0.24f, 0.22f);
        CreateOrb(parent, "MindStatusHeadOrb", head + new Vector3(0.00f, 0.02f, -0.02f), MultiplyAlpha(core, 0.92f), 0.14f, 0.26f);
        CreateLine(parent, "MindStatusBand", new[] { chest + Vector3.left * 0.34f, chest + Vector3.right * 0.34f }, MultiplyAlpha(burst, 0.42f), 0.026f);
        CreateLine(parent, "MindStatusBandUpper", new[] { head + Vector3.left * 0.24f, head + Vector3.right * 0.24f }, MultiplyAlpha(burst, 0.48f), 0.022f);
        CreateLine(parent, "MindStatusTether", new[] { head + Vector3.down * 0.10f, chest + Vector3.up * 0.10f }, MultiplyAlpha(core, 0.34f), 0.016f);
    }

    private static GameObject CreateLine(Transform parent, string name, IList<Vector3> points, Color color, float width)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, true);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sharedMaterial = sharedLineMaterial;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.positionCount = points.Count;
        line.startWidth = width;
        line.endWidth = width * 0.74f;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }

        return lineObject;
    }

    private static GameObject CreateRing(Transform parent, string name, Vector3 center, Color color, float radius, float width)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(parent, true);
        ringObject.transform.position = center;

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.sharedMaterial = sharedLineMaterial;
        ring.textureMode = LineTextureMode.Stretch;
        ring.alignment = LineAlignment.View;
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 33;
        ring.startWidth = width;
        ring.endWidth = width;
        ring.startColor = color;
        ring.endColor = color;
        ring.numCapVertices = 4;
        ring.numCornerVertices = 4;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;

        const int segments = 32;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        return ringObject;
    }

    private static void CreateOrb(Transform parent, string name, Vector3 position, Color color, float radius, float alpha)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name;
        orb.transform.SetParent(parent, true);
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * radius;
        RemoveCollider(orb);

        Renderer renderer = orb.GetComponent<Renderer>();
        Color tint = color;
        tint.a = alpha;
        renderer.sharedMaterial = CreateTintedMaterial(tint, true);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        return new Material(shader);
    }

    private static Material CreateTintedMaterial(Color color, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        SetMaterialColor(mat, color);

        if (transparent && mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
            {
                mat.SetFloat("_Blend", 0f);
            }
            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (mat.HasProperty("_DstBlend"))
            {
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetInt("_ZWrite", 0);
            }

            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        return mat;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void SetFacing(GameObject source, Vector3 target)
    {
        if (source == null)
        {
            return;
        }

        Vector3 flat = target - source.transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f)
        {
            return;
        }

        source.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
    }

    private static Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(go.transform.position + Vector3.up * 0.5f, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static Bounds GetActiveBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(false);
        if (renderers == null || renderers.Length == 0)
        {
            return new Bounds(go.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static Vector3 GetGroundPoint(GameObject go)
    {
        Bounds bounds = GetBounds(go);
        return new Vector3(bounds.center.x, bounds.min.y + 0.05f, bounds.center.z);
    }

    private static Vector3 GetChestPoint(GameObject go)
    {
        Bounds bounds = GetBounds(go);
        return new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);
    }

    private static Vector3 GetHeadPoint(GameObject go)
    {
        Bounds bounds = GetBounds(go);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private static Color MultiplyColor(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a);
    }

    private static Color MultiplyAlpha(Color color, float alphaMultiplier)
    {
        return new Color(color.r, color.g, color.b, color.a * alphaMultiplier);
    }

    private static Camera FindPreviewCamera()
    {
        GameObject go = GameObject.Find(PreviewCameraName);
        return go != null ? go.GetComponent<Camera>() : null;
    }

    private static void SetRootActive(string rootName, bool isActive)
    {
        GameObject root = GameObject.Find(rootName);
        if (root != null)
        {
            root.SetActive(isActive);
        }
    }

    private static void EnsurePreviewFolder()
    {
        if (!Directory.Exists(PreviewFolder))
        {
            Directory.CreateDirectory(PreviewFolder);
        }
    }

    private static void RenderCameraToPng(Camera cam, string absolutePath, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        RenderTexture previousTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(absolutePath, bytes);

        cam.targetTexture = previousTarget;
        RenderTexture.active = previous;

        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
    }

    private static CameraShot GetShotForGroup(string groupName, GameObject group)
    {
        Vector3 center = group != null ? group.transform.position : Vector3.zero;
        Vector3 target = center + new Vector3(0f, 0.88f, 0.10f);
        Vector3 position = center + new Vector3(-1.20f, 1.45f, -4.90f);
        float fieldOfView = 25f;

        switch (groupName)
        {
            case "01_CrystallinePush_hit":
            case "13_GemstoneSmash_hit":
                target = center + new Vector3(0.08f, 0.84f, 0.12f);
                position = center + new Vector3(-1.30f, 1.46f, -4.90f);
                fieldOfView = 24f;
                break;
            case "02_BattleRally_cast":
            case "05_FortifiedRampart_cast":
            case "11_SunwellAnthem_cast":
            case "14_HeartOfTheMountain_cast":
                target = center + new Vector3(0.04f, 0.62f, 0.14f);
                position = center + new Vector3(-0.92f, 1.36f, -5.70f);
                fieldOfView = 28f;
                break;
            case "03_ScorchingRay_cast":
                target = center + new Vector3(0.14f, 1.12f, 0.12f);
                position = center + new Vector3(-1.40f, 1.72f, -5.95f);
                fieldOfView = 24f;
                break;
            case "04_PrismaticBarrier_cast":
                target = center + new Vector3(0.02f, 1.08f, 0.08f);
                position = center + new Vector3(-0.58f, 1.46f, -4.32f);
                fieldOfView = 22f;
                break;
            case "06_QueenDive_cast":
                target = center + new Vector3(0.24f, 1.46f, 0.10f);
                position = center + new Vector3(-0.78f, 1.90f, -5.50f);
                fieldOfView = 24.5f;
                break;
            case "07_QueenDive_impact":
                target = center + new Vector3(0.18f, 0.96f, 0.16f);
                position = center + new Vector3(-1.30f, 1.62f, -5.38f);
                fieldOfView = 25f;
                break;
            case "08_AshenRebirth_cast":
                target = center + new Vector3(0.18f, 0.82f, 0.16f);
                position = center + new Vector3(-1.12f, 1.40f, -5.30f);
                fieldOfView = 25f;
                break;
            case "09_MindControl_cast":
                target = center + new Vector3(0.00f, 1.08f, 0.10f);
                position = center + new Vector3(-0.58f, 1.54f, -4.36f);
                fieldOfView = 22f;
                break;
            case "10_MindControl_status":
                target = center + new Vector3(0.00f, 1.12f, 0.12f);
                position = center + new Vector3(-0.66f, 1.56f, -4.36f);
                fieldOfView = 23f;
                break;
            case "15_Stun_status":
                target = center + new Vector3(0.00f, 1.14f, 0.12f);
                position = center + new Vector3(-0.62f, 1.58f, -4.28f);
                fieldOfView = 23f;
                break;
            case "16_Burning_status":
                target = center + new Vector3(0.00f, 1.00f, 0.12f);
                position = center + new Vector3(-0.74f, 1.48f, -4.30f);
                fieldOfView = 23.5f;
                break;
            case "12_CarryAlly_cast":
                target = center + new Vector3(0.12f, 0.74f, 0.14f);
                position = center + new Vector3(-1.00f, 1.34f, -4.96f);
                fieldOfView = 25f;
                break;
        }

        return new CameraShot
        {
            Position = position,
            Target = target,
            FieldOfView = fieldOfView
        };
    }

    private static void SetActiveExportGroup(Transform galleryRoot, string activeGroupName)
    {
        if (galleryRoot == null)
        {
            return;
        }

        for (int i = 0; i < GroupNames.Length; i++)
        {
            Transform group = galleryRoot.Find(GroupNames[i]);
            if (group != null)
            {
                group.gameObject.SetActive(string.IsNullOrEmpty(activeGroupName) || GroupNames[i] == activeGroupName);
            }
        }
    }

    private static void SetPieceRootsVisible(Transform groupRoot, bool visible)
    {
        if (groupRoot == null)
        {
            return;
        }

        for (int i = 0; i < groupRoot.childCount; i++)
        {
            Transform child = groupRoot.GetChild(i);
            if (child.name.StartsWith("Piece_"))
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    private static SceneSetupResult PrepareScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != PreviewScenePath)
        {
            scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
        }

        if (scene.path != PreviewScenePath)
        {
            return new SceneSetupResult(false, "Failed to open VFXTempReview scene.", scene);
        }

        return new SceneSetupResult(true, "OK", scene);
    }

    private struct CameraShot
    {
        public Vector3 Position;
        public Vector3 Target;
        public float FieldOfView;
    }

    private struct SceneSetupResult
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly UnityEngine.SceneManagement.Scene Scene;

        public SceneSetupResult(bool success, string message, UnityEngine.SceneManagement.Scene scene)
        {
            Success = success;
            Message = message;
            Scene = scene;
        }
    }
}
