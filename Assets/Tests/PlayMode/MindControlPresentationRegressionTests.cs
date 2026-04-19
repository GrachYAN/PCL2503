using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MindControlPresentationRegressionTests
{
    private const float DefaultGroundY = 0.5f;

    [UnityTest]
    public IEnumerator ImmediateMindControlTransfer_DropsOriginalCasterBeforeKeepingTargetSelected()
    {
        Type inputManagerType = FindRuntimeType("InputManager");
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type kingType = FindRuntimeType("King");
        Type bishopType = FindRuntimeType("Bishop");
        Type factionType = FindRuntimeType("Faction");
        Type spellType = FindRuntimeType("Spell");

        Assert.That(inputManagerType, Is.Not.Null);
        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(kingType, Is.Not.Null);
        Assert.That(bishopType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);
        Assert.That(spellType, Is.Not.Null);

        GameObject logicObject = null;
        GameObject inputObject = null;
        GameObject casterObject = null;
        GameObject targetObject = null;
        GameObject targetSquareObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            Component logicManager = logicObject.AddComponent(logicManagerType);

            inputObject = new GameObject("InputManager");
            Component inputManager = inputObject.AddComponent(inputManagerType);
            inputManager.GetType().GetProperty("enabled")?.SetValue(inputManager, false);
            ConfigureMinimalInputManagerUi(inputManager);
            SetPrivateField(inputManager, "logicManager", logicManager);
            SetPrivateField(inputManager, "isOfflineMode", true);

            casterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            casterObject.name = "MindControlCaster";
            casterObject.transform.position = new Vector3(4f, DefaultGroundY, 4f);
            casterObject.transform.rotation = Quaternion.identity;
            Component caster = casterObject.AddComponent(kingType);
            object elfFaction = Enum.Parse(factionType, "Elf");
            InvokeMethod(
                caster,
                "Initialize",
                new[] { typeof(string), typeof(bool), factionType },
                "King",
                true,
                elfFaction);
            InvokeMethod(caster, "SetMana", new[] { typeof(int) }, 10);

            targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            targetObject.name = "MindControlTarget";
            targetObject.transform.position = new Vector3(5f, DefaultGroundY, 5f);
            targetObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Component target = targetObject.AddComponent(bishopType);
            object dwarfFaction = Enum.Parse(factionType, "Dwarf");
            InvokeMethod(
                target,
                "Initialize",
                new[] { typeof(string), typeof(bool), factionType },
                "Bishop",
                false,
                dwarfFaction);

            SetBoardPiece(logicManager, 4, 4, caster);
            SetBoardPiece(logicManager, 5, 5, target);

            targetSquareObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetSquareObject.name = "TargetSquare";
            targetSquareObject.transform.position = new Vector3(5f, 0f, 5f);
            Component square = targetSquareObject.AddComponent(FindRuntimeType("Square"));
            square.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(square, null);
            SetPrivateField(inputManager, "highlightedSquares", CreateSingleSquareList(square));

            object casterAnimator = caster.GetType().GetProperty("MotionAnimator")?.GetValue(caster);
            Assert.That(casterAnimator, Is.Not.Null);
            casterAnimator.GetType().GetMethod("CaptureGroundState")?.Invoke(casterAnimator, null);
            casterAnimator.GetType().GetMethod("PlayLiftAnimation", new[] { typeof(Action) })?.Invoke(casterAnimator, new object[] { null });

            yield return WaitUntilPieceStopsAnimating(caster);

            Assert.That((bool)caster.GetType().GetProperty("IsSelected")?.GetValue(caster), Is.True);

            SetPrivateField(inputManager, "selectedPiece", caster);
            SetPrivateEnumField(inputManager, "currentState", "CastingSpell");

            object selectedSpell = GetMindControlSpell(caster, spellType);
            Assert.That(selectedSpell, Is.Not.Null, "Elf king should still own Mind Control for this regression.");
            SetPrivateField(inputManager, "selectedSpell", selectedSpell);

            Assert.That(
                Physics.Raycast(new Ray(new Vector3(5f, 10f, 5f), Vector3.down), out RaycastHit hit, 20f),
                Is.True,
                "Expected a raycast hit on the controlled target for TryCastAtTarget regression.");

            MethodInfo tryCastAtTarget = inputManagerType.GetMethod(
                "TryCastAtTarget",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(RaycastHit) },
                null);

            Assert.That(tryCastAtTarget, Is.Not.Null);
            tryCastAtTarget.Invoke(inputManager, new object[] { hit });

            Assert.That((bool)caster.GetType().GetProperty("IsSelected")?.GetValue(caster), Is.False);
            Assert.That(GetPrivateField(inputManager, "selectedPiece"), Is.Null,
                "Control handoff should stay locked until the target finishes the Mind Control turn animation.");
            Assert.That(GetPrivateField(inputManager, "currentState").ToString(), Is.EqualTo("None"));

            Assert.That((bool)caster.GetType().GetProperty("IsSelected")?.GetValue(caster), Is.False);
            yield return WaitUntilPieceStopsAnimating(caster);
            yield return WaitUntilPieceStopsAnimating(target);
            Assert.That(casterObject.transform.position.y, Is.EqualTo(DefaultGroundY).Within(0.01f));
            Assert.That(GetPrivateField(inputManager, "selectedPiece"), Is.EqualTo(target));
            Assert.That(GetPrivateField(inputManager, "currentState").ToString(), Is.EqualTo("PieceSelected"));
        }
        finally
        {
            DestroyImmediateSafe(targetSquareObject);
            DestroyImmediateSafe(targetObject);
            DestroyImmediateSafe(casterObject);
            DestroyImmediateSafe(logicObject);
        }
    }

    [UnityTest]
    public IEnumerator MindControl_FlipsFacingToControlledSide_AndRevertRestoresOriginalFacing()
    {
        Type logicManagerType = FindRuntimeType("LogicManager");
        Type bishopType = FindRuntimeType("Bishop");
        Type factionType = FindRuntimeType("Faction");

        Assert.That(logicManagerType, Is.Not.Null);
        Assert.That(bishopType, Is.Not.Null);
        Assert.That(factionType, Is.Not.Null);

        GameObject logicObject = null;
        GameObject pieceObject = null;

        try
        {
            logicObject = new GameObject("LogicManager");
            logicObject.AddComponent(logicManagerType);

            pieceObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pieceObject.name = "MindControlledBishop";
            pieceObject.transform.position = new Vector3(3f, DefaultGroundY, 3f);
            pieceObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Component piece = pieceObject.AddComponent(bishopType);
            object elfFaction = Enum.Parse(factionType, "Elf");
            InvokeMethod(
                piece,
                "Initialize",
                new[] { typeof(string), typeof(bool), factionType },
                "Bishop",
                false,
                elfFaction);

            object animator = piece.GetType().GetProperty("MotionAnimator")?.GetValue(piece);
            Assert.That(animator, Is.Not.Null);
            animator.GetType().GetMethod("CaptureGroundState")?.Invoke(animator, null);

            Assert.That(NormalizeYaw(pieceObject.transform.eulerAngles.y), Is.EqualTo(180f).Within(0.5f));

            InvokeMethod(piece, "MindControl", new[] { typeof(bool) }, true);

            Assert.That(animator.GetType().GetProperty("CurrentState")?.GetValue(animator).ToString(), Is.EqualTo("Turning"));
            Assert.That((bool)piece.GetType().GetProperty("IsAnimating")?.GetValue(piece), Is.True);

            yield return WaitUntilPieceStopsAnimating(piece);

            Assert.That((bool)piece.GetType().GetProperty("IsWhite")?.GetValue(piece), Is.True);
            Assert.That(NormalizeYaw(pieceObject.transform.eulerAngles.y), Is.EqualTo(0f).Within(0.5f));
            Assert.That(NormalizeYaw(GetAnimatorGroundRotation(animator).eulerAngles.y), Is.EqualTo(0f).Within(0.5f));

            InvokeMethod(piece, "RevertMindControl", Type.EmptyTypes);

            Assert.That(animator.GetType().GetProperty("CurrentState")?.GetValue(animator).ToString(), Is.EqualTo("Turning"));
            Assert.That((bool)piece.GetType().GetProperty("IsAnimating")?.GetValue(piece), Is.True);

            yield return WaitUntilPieceStopsAnimating(piece);

            Assert.That((bool)piece.GetType().GetProperty("IsWhite")?.GetValue(piece), Is.False);
            Assert.That(NormalizeYaw(pieceObject.transform.eulerAngles.y), Is.EqualTo(180f).Within(0.5f));
            Assert.That(NormalizeYaw(GetAnimatorGroundRotation(animator).eulerAngles.y), Is.EqualTo(180f).Within(0.5f));
        }
        finally
        {
            DestroyImmediateSafe(pieceObject);
            DestroyImmediateSafe(logicObject);
        }

        yield return null;
    }

    private static IEnumerator WaitUntilPieceStopsAnimating(Component piece)
    {
        PropertyInfo isAnimatingProperty = piece.GetType().GetProperty("IsAnimating");
        Assert.That(isAnimatingProperty, Is.Not.Null);

        while ((bool)isAnimatingProperty.GetValue(piece))
        {
            yield return null;
        }
    }

    private static Quaternion GetAnimatorGroundRotation(object animator)
    {
        PropertyInfo property = animator.GetType().GetProperty("GroundRotation");
        Assert.That(property, Is.Not.Null);
        return (Quaternion)property.GetValue(animator);
    }

    private static float NormalizeYaw(float yaw)
    {
        float normalized = yaw % 360f;
        return normalized < 0f ? normalized + 360f : normalized;
    }

    private static object GetMindControlSpell(Component caster, Type spellType)
    {
        FieldInfo spellsField = caster.GetType().GetField("Spells", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(spellsField, Is.Not.Null);
        System.Collections.IEnumerable spells = spellsField.GetValue(caster) as System.Collections.IEnumerable;
        Assert.That(spells, Is.Not.Null);

        foreach (object spell in spells)
        {
            if (spell?.GetType().Name == "MindControl")
            {
                return spell;
            }
        }

        return null;
    }

    private static object CreateSingleSquareList(Component square)
    {
        Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(square.GetType());
        object list = Activator.CreateInstance(listType);
        listType.GetMethod("Add")?.Invoke(list, new object[] { square });
        return list;
    }

    private static void SetBoardPiece(Component logicManager, int x, int y, Component piece)
    {
        FieldInfo boardMapField = logicManager.GetType().GetField("boardMap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(boardMapField, Is.Not.Null);
        Array boardMap = boardMapField.GetValue(logicManager) as Array;
        Assert.That(boardMap, Is.Not.Null);
        boardMap.SetValue(piece, x, y);
    }

    private static void ConfigureMinimalInputManagerUi(Component inputManager)
    {
        GameObject unitFramePanel = new GameObject("UnitFramePanel");
        GameObject actionBarPanel = new GameObject("ActionBarPanel");
        GameObject moveButtonObject = new GameObject("MoveButton");
        GameObject spellButton1Object = new GameObject("SpellButton1");
        GameObject spellButton2Object = new GameObject("SpellButton2");

        SetPublicField(inputManager, "unitFramePanel", unitFramePanel);
        SetPublicField(inputManager, "actionBarPanel", actionBarPanel);
        SetPublicField(inputManager, "moveButton", moveButtonObject.AddComponent<UnityEngine.UI.Button>());
        SetPublicField(inputManager, "moveButtonIcon", moveButtonObject.AddComponent<UnityEngine.UI.Image>());
        SetPublicField(inputManager, "spellButton1", spellButton1Object.AddComponent<UnityEngine.UI.Button>());
        SetPublicField(inputManager, "spellButton1Icon", spellButton1Object.AddComponent<UnityEngine.UI.Image>());
        SetPublicField(inputManager, "spellButton2", spellButton2Object.AddComponent<UnityEngine.UI.Button>());
        SetPublicField(inputManager, "spellButton2Icon", spellButton2Object.AddComponent<UnityEngine.UI.Image>());

        SetPublicField(inputManager, "spellIcons", CreateEmptyListForField(inputManager, "spellIcons"));
        SetPublicField(inputManager, "piecePortraits", CreateEmptyListForField(inputManager, "piecePortraits"));
        SetPublicField(inputManager, "factionMoveIcons", CreateEmptyListForField(inputManager, "factionMoveIcons"));
    }

    private static object CreateEmptyListForField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(field, Is.Not.Null);
        return Activator.CreateInstance(field.FieldType);
    }

    private static void SetPublicField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        Assert.That(field, Is.Not.Null, $"Expected public field '{fieldName}' to exist on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist on {target.GetType().Name}.");
        return field.GetValue(target);
    }

    private static void SetPrivateEnumField(object target, string fieldName, string enumName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist on {target.GetType().Name}.");
        field.SetValue(target, Enum.Parse(field.FieldType, enumName));
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

    private static void DestroyImmediateSafe(UnityEngine.Object target)
    {
        if (target != null)
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
