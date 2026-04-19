using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TeleportPrefabDependencyRegressionTests
{
    private const string PrefabPath = "Assets/Resource/Prefab/texiao/Teleport_1.prefab";
    private const string AllowedDependencyRoot = "Assets/Resource/Prefab/texiao/";
    private const string ForbiddenDependencyRoot = "Assets/Portal Particle/";

    [Test]
    public void Teleport_1_Prefab_Dependencies_AreLocalizedOutsidePortalParticlePackage()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, "Teleport_1 prefab should exist.");

        string[] dependencies = AssetDatabase.GetDependencies(PrefabPath, true)
            .Where(path => path != PrefabPath)
            .ToArray();

        string[] forbiddenDependencies = dependencies
            .Where(path => path.StartsWith(ForbiddenDependencyRoot))
            .ToArray();

        Assert.That(
            forbiddenDependencies,
            Is.Empty,
            "Teleport_1 prefab should not keep runtime dependencies inside Assets/Portal Particle once the effect is localized.");

        Assert.That(
            dependencies.Any(path => path.StartsWith(AllowedDependencyRoot)),
            Is.True,
            "Teleport_1 prefab should keep its required runtime dependencies inside texiao.");
    }
}
