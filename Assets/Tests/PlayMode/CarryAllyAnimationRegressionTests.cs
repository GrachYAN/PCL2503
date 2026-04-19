using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CarryAllyAnimationRegressionTests
{
    private const float DefaultGroundY = 0.5f;

    [Test]
    public void CarryAlly_SuppressesDefaultDeselectionDrop()
    {
        Type inputManagerType = FindRuntimeType("InputManager");
        Type carryAllyType = FindRuntimeType("CarryAlly");
        Type fortifiedRampartType = FindRuntimeType("FortifiedRampart");

        Assert.That(inputManagerType, Is.Not.Null);
        Assert.That(carryAllyType, Is.Not.Null);
        Assert.That(fortifiedRampartType, Is.Not.Null);

        MethodInfo helper = inputManagerType.GetMethod(
            "ShouldSuppressDeselectionDropAfterCast",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(helper, Is.Not.Null);

        object carryAlly = Activator.CreateInstance(carryAllyType);
        object fortifiedRampart = Activator.CreateInstance(fortifiedRampartType);

        bool carryAllySuppresses = (bool)helper.Invoke(null, new[] { carryAlly });
        bool fortifiedRampartSuppresses = (bool)helper.Invoke(null, new[] { fortifiedRampart });

        Assert.That(carryAllySuppresses, Is.True);
        Assert.That(fortifiedRampartSuppresses, Is.False);
    }

    [UnityTest]
    public IEnumerator BlackCarryAllyLiftOff_PreservesForwardFacingAtEndOfSpiral()
    {
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type queenType = FindRuntimeType("Queen");
        Type carryAllyType = FindRuntimeType("CarryAlly");
        Type factionType = FindRuntimeType("Faction");
        Type spellType = FindRuntimeType("Spell");

        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(queenType, Is.Not.Null);
        Assert.That(carryAllyType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);
        Assert.That(spellType, Is.Not.Null);

        GameObject logicObject = new GameObject("LogicManager");
        Component logicManager = logicObject.AddComponent(logicManagerType);

        GameObject queenObject = new GameObject("BlackQueen");
        queenObject.transform.position = new Vector3(3f, DefaultGroundY, 7f);
        queenObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Component queen = queenObject.AddComponent(queenType);
        object dwarfFaction = Enum.Parse(factionType, "Dwarf");
        queenType.GetMethod("Initialize", new[] { typeof(string), typeof(bool), factionType })
            ?.Invoke(queen, new object[] { "Queen", false, dwarfFaction });

        object motionAnimator = queenType.GetProperty("MotionAnimator")?.GetValue(queen);
        Assert.That(motionAnimator, Is.Not.Null);
        motionAnimator.GetType().GetMethod("CaptureGroundState")?.Invoke(motionAnimator, null);

        object spell = Activator.CreateInstance(carryAllyType);
        spellType.GetMethod("Initialize")?.Invoke(spell, new[] { queen, logicManager });

        MethodInfo liftOffMethod = carryAllyType.GetMethod(
            "PlayGryphonLiftOffAnimation",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(liftOffMethod, "Expected CarryAlly to keep a dedicated lift-off coroutine.");

        IEnumerator routine = (IEnumerator)liftOffMethod.Invoke(spell, null);
        while (routine.MoveNext())
        {
            yield return routine.Current;
        }

        float yaw = queenObject.transform.eulerAngles.y;
        Assert.That(Mathf.DeltaAngle(yaw, 180f), Is.EqualTo(0f).Within(0.5f),
            "Black queen should finish the spiral still facing the board-forward direction.");

        UnityEngine.Object.DestroyImmediate(queenObject);
        UnityEngine.Object.DestroyImmediate(logicObject);
    }

    private static Type FindRuntimeType(string typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(typeName, false))
            .FirstOrDefault(type => type != null);
    }
}
