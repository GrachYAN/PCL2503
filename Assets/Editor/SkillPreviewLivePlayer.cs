using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[InitializeOnLoad]
public static class SkillPreviewLivePlayer
{
    private const string GalleryRootName = "SkillEffectPreviewGallery";

    private static PreviewBinding activeBinding;
    private static double playbackStartTime;
    private static float pausedSampleTime;
    private static bool isPlaying;

    static SkillPreviewLivePlayer()
    {
        EditorApplication.update += UpdatePlayback;
        AssemblyReloadEvents.beforeAssemblyReload += StopPlaybackSilently;
    }

    [MenuItem("Tools/VFX/Preview/Play Or Resume Selected Effect")]
    public static void PlayOrResumeSelectedEffect()
    {
        GameObject root = ResolvePreviewRoot(Selection.activeGameObject);
        if (root == null)
        {
            Debug.LogWarning("Select a preview group or one of its children first.");
            return;
        }

        if (activeBinding == null || activeBinding.Root != root)
        {
            activeBinding = CreateBinding(root);
            pausedSampleTime = 0f;
        }

        if (!activeBinding.HasPlayableContent)
        {
            Debug.LogWarning("The selected preview does not contain particle systems or legacy animations.");
            return;
        }

        playbackStartTime = EditorApplication.timeSinceStartup - pausedSampleTime;
        isPlaying = true;
        SampleBinding(activeBinding, pausedSampleTime);

        Debug.Log(
            $"Playing edit-mode preview for {activeBinding.Root.name}. " +
            "Use Tools/VFX/Preview/Pause Playback to freeze a frame for screenshots.");
    }

    [MenuItem("Tools/VFX/Preview/Play Or Resume Selected Effect", true)]
    private static bool ValidatePlayOrResumeSelectedEffect()
    {
        return ResolvePreviewRoot(Selection.activeGameObject) != null;
    }

    [MenuItem("Tools/VFX/Preview/Pause Playback")]
    public static void PausePlayback()
    {
        if (!isPlaying || activeBinding == null || activeBinding.Root == null)
        {
            return;
        }

        pausedSampleTime = GetCurrentSampleTime();
        isPlaying = false;
        SampleBinding(activeBinding, pausedSampleTime);
        Debug.Log($"Paused preview playback for {activeBinding.Root.name} at {pausedSampleTime:0.00}s.");
    }

    [MenuItem("Tools/VFX/Preview/Pause Playback", true)]
    private static bool ValidatePausePlayback()
    {
        return isPlaying && activeBinding != null && activeBinding.Root != null;
    }

    [MenuItem("Tools/VFX/Preview/Restart Selected Effect")]
    public static void RestartSelectedEffect()
    {
        GameObject root = ResolvePreviewRoot(Selection.activeGameObject);
        if (root == null)
        {
            Debug.LogWarning("Select a preview group or one of its children first.");
            return;
        }

        activeBinding = CreateBinding(root);
        pausedSampleTime = 0f;
        isPlaying = true;
        playbackStartTime = EditorApplication.timeSinceStartup;
        SampleBinding(activeBinding, 0f);

        Debug.Log($"Restarted edit-mode preview for {activeBinding.Root.name}.");
    }

    [MenuItem("Tools/VFX/Preview/Restart Selected Effect", true)]
    private static bool ValidateRestartSelectedEffect()
    {
        return ResolvePreviewRoot(Selection.activeGameObject) != null;
    }

    [MenuItem("Tools/VFX/Preview/Stop Playback And Reset")]
    public static void StopPlaybackAndReset()
    {
        if (activeBinding == null || activeBinding.Root == null)
        {
            return;
        }

        string rootName = activeBinding.Root.name;
        StopPlaybackSilently();
        Debug.Log($"Stopped preview playback for {rootName} and reset it to the first frame.");
    }

    [MenuItem("Tools/VFX/Preview/Stop Playback And Reset", true)]
    private static bool ValidateStopPlaybackAndReset()
    {
        return activeBinding != null && activeBinding.Root != null;
    }

    private static void UpdatePlayback()
    {
        if (!isPlaying || activeBinding == null || activeBinding.Root == null)
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            return;
        }

        pausedSampleTime = GetCurrentSampleTime();
        SampleBinding(activeBinding, pausedSampleTime);
    }

    private static float GetCurrentSampleTime()
    {
        if (activeBinding == null)
        {
            return 0f;
        }

        float sampleTime = (float)(EditorApplication.timeSinceStartup - playbackStartTime);
        if (activeBinding.CycleLength > 0.01f)
        {
            sampleTime = Mathf.Repeat(sampleTime, activeBinding.CycleLength);
        }

        return sampleTime;
    }

    private static void StopPlaybackSilently()
    {
        isPlaying = false;

        if (activeBinding != null && activeBinding.Root != null)
        {
            SampleBinding(activeBinding, 0f);
        }

        activeBinding = null;
        pausedSampleTime = 0f;
        InternalEditorUtility.RepaintAllViews();
    }

    private static GameObject ResolvePreviewRoot(GameObject selection)
    {
        if (selection == null)
        {
            return null;
        }

        Transform current = selection.transform;
        while (current != null)
        {
            if (current.parent != null && current.parent.name == GalleryRootName)
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return selection;
    }

    private static PreviewBinding CreateBinding(GameObject root)
    {
        List<ParticleSystem> particleRoots = new List<ParticleSystem>();
        foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (!HasParticleAncestor(particleSystem.transform, root.transform))
            {
                particleRoots.Add(particleSystem);
            }
        }

        List<AnimationBinding> animations = new List<AnimationBinding>();
        foreach (Animation animation in root.GetComponentsInChildren<Animation>(true))
        {
            AnimationClip clip = GetPrimaryClip(animation);
            if (clip == null)
            {
                continue;
            }

            animations.Add(new AnimationBinding
            {
                Animation = animation,
                Clip = clip,
                Duration = Mathf.Max(clip.length, 0.1f)
            });
        }

        float cycleLength = 0.1f;
        for (int i = 0; i < particleRoots.Count; i++)
        {
            cycleLength = Mathf.Max(cycleLength, EstimateParticleCycleLength(particleRoots[i]));
        }

        for (int i = 0; i < animations.Count; i++)
        {
            cycleLength = Mathf.Max(cycleLength, animations[i].Duration);
        }

        return new PreviewBinding
        {
            Root = root,
            ParticleRoots = particleRoots,
            Animations = animations,
            CycleLength = cycleLength,
            HasPlayableContent = particleRoots.Count > 0 || animations.Count > 0
        };
    }

    private static bool HasParticleAncestor(Transform candidate, Transform root)
    {
        Transform current = candidate.parent;
        while (current != null && current != root.parent)
        {
            if (current.GetComponent<ParticleSystem>() != null)
            {
                return true;
            }

            if (current == root)
            {
                break;
            }

            current = current.parent;
        }

        return false;
    }

    private static AnimationClip GetPrimaryClip(Animation animation)
    {
        if (animation == null)
        {
            return null;
        }

        if (animation.clip != null)
        {
            return animation.clip;
        }

        foreach (AnimationState state in animation)
        {
            if (state != null && state.clip != null)
            {
                return state.clip;
            }
        }

        return null;
    }

    private static float EstimateParticleCycleLength(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
        {
            return 0.1f;
        }

        ParticleSystem.MainModule main = particleSystem.main;
        return Mathf.Max(
            0.1f,
            GetCurveMax(main.startDelay) +
            main.duration +
            GetCurveMax(main.startLifetime));
    }

    private static float GetCurveMax(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return curve.constantMax;
            case ParticleSystemCurveMode.Curve:
                return GetAnimationCurveMax(curve.curve) * curve.curveMultiplier;
            case ParticleSystemCurveMode.TwoCurves:
                return Mathf.Max(
                    GetAnimationCurveMax(curve.curveMin),
                    GetAnimationCurveMax(curve.curveMax)) * curve.curveMultiplier;
            default:
                return 0f;
        }
    }

    private static float GetAnimationCurveMax(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return 0f;
        }

        float maxValue = curve.keys[0].value;
        for (int i = 1; i < curve.length; i++)
        {
            maxValue = Mathf.Max(maxValue, curve.keys[i].value);
        }

        return maxValue;
    }

    private static void SampleBinding(PreviewBinding binding, float sampleTime)
    {
        if (binding == null || binding.Root == null)
        {
            return;
        }

        for (int i = 0; i < binding.ParticleRoots.Count; i++)
        {
            ParticleSystem particleRoot = binding.ParticleRoots[i];
            if (particleRoot == null)
            {
                continue;
            }

            particleRoot.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleRoot.useAutoRandomSeed = false;
            particleRoot.randomSeed = 11u;
            particleRoot.Simulate(sampleTime, true, true, true);
            particleRoot.Pause(true);
        }

        for (int i = 0; i < binding.Animations.Count; i++)
        {
            AnimationBinding animation = binding.Animations[i];
            if (animation == null || animation.Animation == null || animation.Clip == null)
            {
                continue;
            }

            float localTime = animation.Duration > 0.01f
                ? Mathf.Repeat(sampleTime, animation.Duration)
                : 0f;

            animation.Clip.SampleAnimation(animation.Animation.gameObject, localTime);
        }

        SceneView.RepaintAll();
        InternalEditorUtility.RepaintAllViews();
    }

    private sealed class PreviewBinding
    {
        public GameObject Root;
        public List<ParticleSystem> ParticleRoots;
        public List<AnimationBinding> Animations;
        public float CycleLength;
        public bool HasPlayableContent;
    }

    private sealed class AnimationBinding
    {
        public Animation Animation;
        public AnimationClip Clip;
        public float Duration;
    }
}
