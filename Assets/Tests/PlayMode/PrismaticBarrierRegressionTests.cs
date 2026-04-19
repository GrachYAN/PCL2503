using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PrismaticBarrierRegressionTests
{
    private const float DefaultGroundY = 0.5f;

    [UnityTest]
    public IEnumerator EnemySlidingMovement_IsStoppedByPrismaticBarrier()
    {
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type bishopType = FindRuntimeType("Bishop");
        Type factionType = FindRuntimeType("Faction");

        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(bishopType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);

        GameObject logicObject = null;
        GameObject barrierPrefab = null;
        GameObject bishopObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            Component logicManager = logicObject.AddComponent(logicManagerType);

            barrierPrefab = new GameObject("BarrierPrefab");
            SetFieldValue(logicManager, "prismaticBarrierPrefab", barrierPrefab);

            bishopObject = new GameObject("BlackBishop");
            bishopObject.transform.position = new Vector3(0f, DefaultGroundY, 0f);

            Component bishop = bishopObject.AddComponent(bishopType);
            object dwarfFaction = Enum.Parse(factionType, "Dwarf");
            InvokeMethod(
                bishop,
                "Initialize",
                new[] { typeof(string), typeof(bool), factionType },
                "Bishop",
                false,
                dwarfFaction);
            InvokeMethod(bishop, "UpdateBoardMap", Type.EmptyTypes);

            MethodInfo placeBarrier = logicManagerType.GetMethod(
                "PlacePrismaticBarrier",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Vector2), typeof(int), typeof(bool) },
                null);

            Assert.That(
                placeBarrier,
                Is.Not.Null,
                "LogicManager should expose a team-aware barrier placement API so multiplayer/runtime rules can tell who is blocked.");

            placeBarrier.Invoke(logicManager, new object[] { new Vector2(2f, 2f), 3, true });

            List<Vector2> legalMoves = InvokeMethod<List<Vector2>>(bishop, "GetLegalMoves", Type.EmptyTypes);

            Assert.That(ContainsSquare(legalMoves, 1f, 1f), Is.True);
            Assert.That(ContainsSquare(legalMoves, 2f, 2f), Is.False);
            Assert.That(ContainsSquare(legalMoves, 3f, 3f), Is.False);
        }
        finally
        {
            DestroyImmediateSafe(bishopObject);
            DestroyImmediateSafe(barrierPrefab);
            DestroyImmediateSafe(logicObject);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator EnemyScorchingRay_CannotTargetThroughPrismaticBarrier()
    {
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type bishopType = FindRuntimeType("Bishop");
        Type factionType = FindRuntimeType("Faction");

        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(bishopType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);

        GameObject logicObject = null;
        GameObject barrierPrefab = null;
        GameObject casterObject = null;
        GameObject targetObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            Component logicManager = logicObject.AddComponent(logicManagerType);

            barrierPrefab = new GameObject("BarrierPrefab");
            SetFieldValue(logicManager, "prismaticBarrierPrefab", barrierPrefab);

            casterObject = new GameObject("BlackCaster");
            casterObject.transform.position = new Vector3(0f, DefaultGroundY, 0f);

            Component caster = casterObject.AddComponent(bishopType);
            object dwarfFaction = Enum.Parse(factionType, "Dwarf");
            InvokeMethod(
                caster,
                "Initialize",
                new[] { typeof(string), typeof(bool), factionType },
                "Bishop",
                false,
                dwarfFaction);
            InvokeMethod(caster, "UpdateBoardMap", Type.EmptyTypes);

            targetObject = new GameObject("WhiteTarget");
            targetObject.transform.position = new Vector3(4f, DefaultGroundY, 4f);

            Component target = targetObject.AddComponent(bishopType);
            object elfFaction = Enum.Parse(factionType, "Elf");
            InvokeMethod(
                target,
                "Initialize",
                new[] { typeof(string), typeof(bool), factionType },
                "Bishop",
                true,
                elfFaction);
            InvokeMethod(target, "UpdateBoardMap", Type.EmptyTypes);

            MethodInfo placeBarrier = logicManagerType.GetMethod(
                "PlacePrismaticBarrier",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Vector2), typeof(int), typeof(bool) },
                null);

            Assert.That(placeBarrier, Is.Not.Null);
            placeBarrier.Invoke(logicManager, new object[] { new Vector2(2f, 2f), 3, true });

            object scorchingRay = FindSpellByTypeName(caster, "ScorchingRay");
            Assert.That(scorchingRay, Is.Not.Null, "Bishop should still own ScorchingRay for this regression test.");

            List<Vector2> validTargets = InvokeMethod<List<Vector2>>(scorchingRay, "GetValidTargetSquares", Type.EmptyTypes);

            Assert.That(
                ContainsSquare(validTargets, 4f, 4f),
                Is.False,
                "Enemy beam spells should not be able to target through an active Prismatic Barrier.");
        }
        finally
        {
            DestroyImmediateSafe(targetObject);
            DestroyImmediateSafe(casterObject);
            DestroyImmediateSafe(barrierPrefab);
            DestroyImmediateSafe(logicObject);
        }

        yield return null;
    }

    private static object FindSpellByTypeName(Component piece, string spellTypeName)
    {
        object spellsValue = piece.GetType().GetField("Spells", BindingFlags.Instance | BindingFlags.Public)?.GetValue(piece);
        if (spellsValue is not IEnumerable spellEnumerable)
        {
            return null;
        }

        foreach (object spell in spellEnumerable)
        {
            if (spell?.GetType().Name == spellTypeName)
            {
                return spell;
            }
        }

        return null;
    }

    private static bool ContainsSquare(IEnumerable<Vector2> squares, float x, float y)
    {
        return squares.Any(square => Mathf.Approximately(square.x, x) && Mathf.Approximately(square.y, y));
    }

    private static void SetFieldValue(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void InvokeMethod(object target, string methodName, Type[] signature, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, signature, null);
        Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {target.GetType().Name}.");
        method.Invoke(target, args);
    }

    private static T InvokeMethod<T>(object target, string methodName, Type[] signature, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, signature, null);
        Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {target.GetType().Name}.");
        return (T)method.Invoke(target, args);
    }

    private static Type FindRuntimeType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(typeName, false))
            .FirstOrDefault(type => type != null);
    }

    private static void DestroyImmediateSafe(UnityEngine.Object target)
    {
        if (target != null)
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
