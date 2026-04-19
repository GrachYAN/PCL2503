using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

public class FortifiedRampartPresentationRegressionTests
{
    [Test]
    public void TooltipTrigger_OnPointerEnterWithoutTooltipSystem_DoesNotThrow()
    {
        Type tooltipTriggerType = FindRuntimeType("TooltipTrigger");
        Assert.That(tooltipTriggerType, Is.Not.Null);

        GameObject eventSystemObject = null;
        GameObject triggerObject = null;

        try
        {
            eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();

            triggerObject = new GameObject("TooltipTrigger", typeof(RectTransform), tooltipTriggerType);
            Component trigger = triggerObject.GetComponent(tooltipTriggerType);
            tooltipTriggerType.GetMethod("SetContent")?.Invoke(trigger, new object[] { "Fortified Rampart" });

            PointerEventData eventData = new PointerEventData(EventSystem.current);
            MethodInfo enterMethod = tooltipTriggerType.GetMethod("OnPointerEnter");
            MethodInfo exitMethod = tooltipTriggerType.GetMethod("OnPointerExit");

            Assert.That(enterMethod, Is.Not.Null);
            Assert.That(exitMethod, Is.Not.Null);
            Assert.DoesNotThrow(() => enterMethod.Invoke(trigger, new object[] { eventData }));
            Assert.DoesNotThrow(() => exitMethod.Invoke(trigger, new object[] { eventData }));
        }
        finally
        {
            DestroyImmediateSafe(triggerObject);
            DestroyImmediateSafe(eventSystemObject);
        }
    }

    [UnityTest]
    public IEnumerator FortifiedRampart_DoesNotAttachPersistentBuffVisual_AndDoesNotProtectEnemies()
    {
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type rookType = FindRuntimeType("Rook");
        Type pawnType = FindRuntimeType("Pawn");
        Type factionType = FindRuntimeType("Faction");
        Type statusVfxControllerType = FindRuntimeType("PieceStatusVFXController");

        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(rookType, Is.Not.Null);
        Assert.That(pawnType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);
        Assert.That(statusVfxControllerType, Is.Not.Null);

        GameObject logicObject = null;
        GameObject rookObject = null;
        GameObject allyObject = null;
        GameObject enemyObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            Component logicManager = logicObject.AddComponent(logicManagerType);

            rookObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rookObject.name = "RampartSource";
            rookObject.transform.position = new Vector3(4f, 0.5f, 4f);
            Component rook = rookObject.AddComponent(rookType);
            object dwarfFaction = Enum.Parse(factionType, "Dwarf");
            InvokeMethod(rook, "Initialize", new[] { typeof(string), typeof(bool), factionType }, "Rook", true, dwarfFaction);

            allyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            allyObject.name = "RampartAlly";
            allyObject.transform.position = new Vector3(5f, 0.5f, 4f);
            Component ally = allyObject.AddComponent(pawnType);
            InvokeMethod(ally, "Initialize", new[] { typeof(string), typeof(bool), factionType }, "Pawn", true, dwarfFaction);

            enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = "RampartEnemy";
            enemyObject.transform.position = new Vector3(4f, 0.5f, 5f);
            Component enemy = enemyObject.AddComponent(pawnType);
            InvokeMethod(enemy, "Initialize", new[] { typeof(string), typeof(bool), factionType }, "Pawn", false, dwarfFaction);

            SetBoardPiece(logicManager, 4, 4, rook);
            SetBoardPiece(logicManager, 5, 4, ally);
            SetBoardPiece(logicManager, 4, 5, enemy);
            logicManagerType.GetMethod("RegisterRampartAura")?.Invoke(logicManager, new object[] { rook, 3, 3 });

            Component statusController = allyObject.AddComponent(statusVfxControllerType);

            yield return null;
            yield return null;

            FieldInfo rampartEffectField = statusVfxControllerType.GetField("rampartEffect", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rampartEffectField != null)
            {
                GameObject rampartEffect = rampartEffectField.GetValue(statusController) as GameObject;
                Assert.That(rampartEffect, Is.Null, "Rampart should no longer keep a persistent attached buff visual during the aura.");
            }

            PropertyInfo allyHasRampartBuff = ally.GetType().GetProperty("HasRampartBuff");
            PropertyInfo enemyHasRampartBuff = enemy.GetType().GetProperty("HasRampartBuff");
            Assert.That(allyHasRampartBuff, Is.Not.Null);
            Assert.That(enemyHasRampartBuff, Is.Not.Null);
            Assert.That((bool)allyHasRampartBuff.GetValue(ally), Is.True, "Allied piece inside aura should still be protected.");
            Assert.That((bool)enemyHasRampartBuff.GetValue(enemy), Is.False, "Enemy piece inside aura radius must not receive Rampart protection.");
        }
        finally
        {
            DestroyImmediateSafe(enemyObject);
            DestroyImmediateSafe(allyObject);
            DestroyImmediateSafe(rookObject);
            DestroyImmediateSafe(logicObject);
        }
    }

    [UnityTest]
    public IEnumerator FortifiedRampart_BeneficiaryCastEffect_IsLargerAndAppearsAroundBody()
    {
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type spellVfxManagerType = FindRuntimeType("SpellVFXManager");
        Type rookType = FindRuntimeType("Rook");
        Type pawnType = FindRuntimeType("Pawn");
        Type factionType = FindRuntimeType("Faction");

        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(spellVfxManagerType, Is.Not.Null);
        Assert.That(rookType, Is.Not.Null);
        Assert.That(pawnType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);

        GameObject logicObject = null;
        GameObject vfxObject = null;
        GameObject rookObject = null;
        GameObject allyObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            Component logicManager = logicObject.AddComponent(logicManagerType);

            vfxObject = new GameObject("SpellVFXManager");
            Component spellVfxManager = vfxObject.AddComponent(spellVfxManagerType);

            rookObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rookObject.name = "RampartSource";
            rookObject.transform.position = new Vector3(4f, 0.5f, 4f);
            Component rook = rookObject.AddComponent(rookType);
            object dwarfFaction = Enum.Parse(factionType, "Dwarf");
            InvokeMethod(rook, "Initialize", new[] { typeof(string), typeof(bool), factionType }, "Rook", true, dwarfFaction);

            allyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            allyObject.name = "RampartAlly";
            allyObject.transform.position = new Vector3(5f, 0.5f, 4f);
            Component ally = allyObject.AddComponent(pawnType);
            InvokeMethod(ally, "Initialize", new[] { typeof(string), typeof(bool), factionType }, "Pawn", true, dwarfFaction);

            SetBoardPiece(logicManager, 4, 4, rook);
            SetBoardPiece(logicManager, 5, 4, ally);

            GameObject shinePrefab = Resources.Load<GameObject>("HCFX/HCFX_Shine_07");
            Assert.That(shinePrefab, Is.Not.Null, "Rampart beneficiary effect should still use HCFX_Shine_07.");

            MethodInfo playRampartAura = spellVfxManagerType.GetMethod(
                "PlayRampartAura",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(playRampartAura, Is.Not.Null);

            Bounds allyBounds = allyObject.GetComponent<Renderer>().bounds;
            float allyBodyY = allyBounds.center.y;

            playRampartAura.Invoke(spellVfxManager, new object[] { rook, logicManager });

            yield return null;

            GameObject allyEffect = FindClosestObjectByName("HCFX_Shine_07(Clone)", allyBounds.center);
            Assert.That(allyEffect, Is.Not.Null, "Rampart should spawn the beneficiary effect on allied protected pieces.");
            Assert.That(allyEffect.transform.position.y, Is.EqualTo(allyBodyY).Within(0.30f),
                "Rampart beneficiary effect should appear around the ally's body instead of near the ground.");
            Assert.That(allyEffect.transform.position.y, Is.GreaterThan(0.30f),
                "Rampart beneficiary effect should be clearly raised above the tile.");

            Vector3 expectedScale = shinePrefab.transform.localScale * 0.36f;
            Assert.That(allyEffect.transform.localScale.x, Is.EqualTo(expectedScale.x).Within(0.02f));
            Assert.That(allyEffect.transform.localScale.y, Is.EqualTo(expectedScale.y).Within(0.02f));
            Assert.That(allyEffect.transform.localScale.z, Is.EqualTo(expectedScale.z).Within(0.02f));
        }
        finally
        {
            DestroyImmediateSafe(allyObject);
            DestroyImmediateSafe(rookObject);
            DestroyImmediateSafe(vfxObject);
            DestroyImmediateSafe(logicObject);
        }
    }

    private static void SetBoardPiece(Component logicManager, int x, int y, Component piece)
    {
        FieldInfo boardMapField = logicManager.GetType().GetField("boardMap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(boardMapField, Is.Not.Null);
        Array boardMap = boardMapField.GetValue(logicManager) as Array;
        Assert.That(boardMap, Is.Not.Null);
        boardMap.SetValue(piece, x, y);
    }

    private static void InvokeMethod(object target, string methodName, Type[] signature, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, signature, null);
        Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' on {target.GetType().Name}.");
        method.Invoke(target, args);
    }

    private static Type FindRuntimeType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(typeName, false))
            .FirstOrDefault(type => type != null);
    }

    private static GameObject FindClosestObjectByName(string objectName, Vector3 targetPosition)
    {
        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject candidate in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (candidate == null || candidate.name != objectName)
            {
                continue;
            }

            float distance = Vector3.Distance(candidate.transform.position, targetPosition);
            if (distance < closestDistance)
            {
                closest = candidate;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private static void DestroyImmediateSafe(UnityEngine.Object target)
    {
        if (target != null)
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
