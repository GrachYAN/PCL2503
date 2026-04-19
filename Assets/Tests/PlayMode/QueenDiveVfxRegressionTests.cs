using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class QueenDiveVfxRegressionTests
{
    [UnityTest]
    public IEnumerator QueenDive_UsesPreviewPrefabInsteadOfRuntimeSimpleOrb()
    {
        Type spellVfxManagerType = FindRuntimeType("SpellVFXManager");
        Type queenType = FindRuntimeType("Queen");
        Type factionType = FindRuntimeType("Faction");
        Type pieceType = FindRuntimeType("Piece");

        Assert.That(spellVfxManagerType, Is.Not.Null);
        Assert.That(queenType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);
        Assert.That(pieceType, Is.Not.Null);
        Assert.That(
            Resources.Load<GameObject>("QK/Qk_fire_arrow_01_ready_01_QueenDivePreview"),
            Is.Not.Null,
            "Queen Dive preview prefab should stay in Resources so build-time loading remains stable.");

        GameObject managerObject = null;
        GameObject logicObject = null;
        GameObject casterObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            logicObject.AddComponent(FindRuntimeType("LogicManager"));

            managerObject = new GameObject("SpellVFXManager");
            Component manager = managerObject.AddComponent(spellVfxManagerType);

            casterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            casterObject.name = "QueenDiveCaster";
            casterObject.transform.position = new Vector3(3f, 0.5f, 3f);
            Component caster = casterObject.AddComponent(queenType);
            object elfFaction = Enum.Parse(factionType, "Elf");
            queenType.GetMethod("Initialize", new[] { typeof(string), typeof(bool), factionType })
                ?.Invoke(caster, new object[] { "Queen", true, elfFaction });

            MethodInfo playDiveSequence = spellVfxManagerType.GetMethod(
                "PlayDiveSequence",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { pieceType, typeof(Vector2) },
                null);

            Assert.That(playDiveSequence, Is.Not.Null);

            playDiveSequence.Invoke(manager, new object[] { caster, new Vector2(4f, 4f) });

            Assert.That(FindSceneObjectsByName("SimpleChargeOrb"), Is.Empty,
                "Queen Dive should not fall back to runtime primitive orbs because that path is unreliable in builds.");

            Assert.That(
                FindSceneObjectsByPrefix("Qk_fire_arrow_01_ready_01_QueenDivePreview"),
                Is.Not.Empty,
                "Queen Dive should instantiate the tuned preview prefab at the strike start.");
        }
        finally
        {
            foreach (GameObject gameObject in FindSceneObjectsByName("SimpleChargeOrb"))
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            foreach (GameObject gameObject in FindSceneObjectsByPrefix("Qk_fire_arrow_01_ready_01_QueenDivePreview"))
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }

            if (casterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(casterObject);
            }

            if (managerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(managerObject);
            }

            if (logicObject != null)
            {
                UnityEngine.Object.DestroyImmediate(logicObject);
            }
        }

        yield break;
    }

    private static GameObject[] FindSceneObjectsByName(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(transform => transform.gameObject)
            .Where(gameObject => gameObject.scene.IsValid() && gameObject.name == name)
            .ToArray();
    }

    private static GameObject[] FindSceneObjectsByPrefix(string prefix)
    {
        return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(transform => transform.gameObject)
            .Where(gameObject => gameObject.scene.IsValid() && gameObject.name.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray();
    }

    private static Type FindRuntimeType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(typeName, false))
            .FirstOrDefault(type => type != null);
    }
}
