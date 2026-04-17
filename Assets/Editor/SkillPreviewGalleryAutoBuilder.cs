using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SkillPreviewGalleryAutoBuilder
{
    private const string MarkerPath = "Assets/Editor/.skill_preview_gallery_refresh";

    static SkillPreviewGalleryAutoBuilder()
    {
        EditorApplication.delayCall += TryBuildIfRequested;
    }

    private static void TryBuildIfRequested()
    {
        if (!File.Exists(MarkerPath))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryBuildIfRequested;
            return;
        }

        try
        {
            File.Delete(MarkerPath);
        }
        catch (IOException ex)
        {
            Debug.LogWarning("Failed to clear skill preview refresh marker: " + ex.Message);
            EditorApplication.delayCall += TryBuildIfRequested;
            return;
        }

        Debug.Log("Auto-refreshing SkillEffectPreviewGallery after script update.");
        Debug.Log(SkillPreviewGalleryBuilder.BuildGalleryAndExport());
    }
}
