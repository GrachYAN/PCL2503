using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class InputManagerLifecycleRegressionTests
{
    [Test]
    public void Destroying_InputManager_Before_Start_DoesNotThrow()
    {
        Type inputManagerType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("InputManager", false))
            .FirstOrDefault(type => type != null);

        Assert.That(inputManagerType, Is.Not.Null, "InputManager type should exist.");

        GameObject gameObject = new GameObject("InputManagerLifecycleTest");

        try
        {
            Component inputManager = gameObject.AddComponent(inputManagerType);
            MethodInfo onDestroy = inputManagerType.GetMethod(
                "OnDestroy",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(onDestroy, Is.Not.Null, "InputManager should define OnDestroy.");

            Assert.DoesNotThrow(
                () => onDestroy.Invoke(inputManager, null),
                "OnDestroy should stay safe even when Start has not initialized clickAction.");
        }
        finally
        {
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
