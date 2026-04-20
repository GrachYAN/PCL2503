using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoadGuard
{
    public static bool TryLoadScene(string sceneName, bool resetTimeScale = false)
    {
        return TryLoadScene(sceneName, null, resetTimeScale);
    }

    public static bool TryLoadScene(string sceneName, string fallbackSceneName, bool resetTimeScale = false, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (resetTimeScale)
        {
            Time.timeScale = 1f;
        }

        if (TryLoadInternal(sceneName, loadMode))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSceneName) &&
            !string.Equals(sceneName, fallbackSceneName, StringComparison.Ordinal) &&
            TryLoadInternal(fallbackSceneName, loadMode))
        {
            GlobalErrorReporter.ReportRecoverableMessage(
                "Scene load fallback",
                $"Primary scene '{sceneName}' could not be loaded. Loaded fallback '{fallbackSceneName}' instead.",
                "A fallback scene was loaded after a scene transition failed.");
            return true;
        }

        GlobalErrorReporter.ReportRecoverableMessage(
            "Scene load failed",
            $"Unable to load scene '{sceneName}'.",
            "The requested scene could not be loaded.",
            LogType.Error);
        return false;
    }

    private static bool TryLoadInternal(string sceneName, LoadSceneMode loadMode)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            return false;
        }

        try
        {
            SceneManager.LoadScene(sceneName, loadMode);
            return true;
        }
        catch (Exception ex)
        {
            GlobalErrorReporter.ReportRecoverableError(
                $"Scene load '{sceneName}'",
                ex,
                "The requested scene could not be loaded.");
            return false;
        }
    }
}
