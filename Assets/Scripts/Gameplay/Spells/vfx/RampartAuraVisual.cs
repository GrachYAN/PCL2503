using System.Collections.Generic;
using UnityEngine;

public class RampartAuraVisual : MonoBehaviour
{
    private const float BaseHeight = 0.04f;
    private const float ReferenceDiameter = 5f;

    private readonly List<ParticleVisualState> particleStates = new List<ParticleVisualState>();
    private readonly List<LightVisualState> lightStates = new List<LightVisualState>();
    private Vector3 centerPosition;
    private int tileRadius;
    private float durationSeconds;
    private float elapsedSeconds;
    private bool initialized;

    private sealed class ParticleVisualState
    {
        public ParticleSystem ParticleSystem;
        public ParticleSystemRenderer Renderer;
        public Material RuntimeMaterial;
        public Vector3 OriginalLocalPosition;
        public float OriginalStartSize;
        public float OriginalEmissionRate;
    }

    private sealed class LightVisualState
    {
        public Light Light;
        public Vector3 OriginalLocalPosition;
        public float OriginalIntensity;
        public float OriginalRange;
    }

    private void Awake()
    {
        CacheVisualState();
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        elapsedSeconds += Time.deltaTime;
        float lifeProgress = durationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / durationSeconds);
        float strength = 1f - lifeProgress;

        transform.position = centerPosition;
        ApplyVisualState(strength);

        if (elapsedSeconds >= durationSeconds)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < particleStates.Count; i++)
        {
            Material runtimeMaterial = particleStates[i].RuntimeMaterial;
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }

    public void InitializeTransient(Vector3 center, int auraTileRadius, float lifetimeSeconds)
    {
        CacheVisualState();

        tileRadius = Mathf.Max(0, auraTileRadius);
        durationSeconds = Mathf.Max(0.1f, lifetimeSeconds);
        elapsedSeconds = 0f;
        centerPosition = new Vector3(center.x, BaseHeight, center.z);
        initialized = true;

        transform.position = centerPosition;
        ApplyVisualState(1f);
    }

    public static float GetTargetDiameterWorldUnits(int auraTileRadius)
    {
        return auraTileRadius * 2f + 1f;
    }

    public static Color EvaluateGoldTint(float strength)
    {
        strength = Mathf.Clamp01(strength);
        Color weak = new Color(1.00f, 0.68f, 0.18f, 0.02f);
        Color strong = new Color(1.00f, 0.84f, 0.28f, 0.24f);
        return Color.Lerp(weak, strong, strength);
    }

    private void CacheVisualState()
    {
        if (particleStates.Count > 0 || lightStates.Count > 0)
        {
            return;
        }

        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            Material runtimeMaterial = null;
            if (renderer != null && renderer.sharedMaterial != null)
            {
                runtimeMaterial = new Material(renderer.sharedMaterial);
                renderer.material = runtimeMaterial;
            }

            particleStates.Add(new ParticleVisualState
            {
                ParticleSystem = particleSystem,
                Renderer = renderer,
                RuntimeMaterial = runtimeMaterial,
                OriginalLocalPosition = particleSystem.transform.localPosition,
                OriginalStartSize = particleSystem.main.startSizeMultiplier,
                OriginalEmissionRate = particleSystem.emission.rateOverTimeMultiplier
            });
        }

        Light[] lights = GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lightStates.Add(new LightVisualState
            {
                Light = lights[i],
                OriginalLocalPosition = lights[i].transform.localPosition,
                OriginalIntensity = lights[i].intensity,
                OriginalRange = lights[i].range
            });
        }
    }

    private void ApplyVisualState(float strength)
    {
        float diameter = GetTargetDiameterWorldUnits(tileRadius);
        float footprintScale = diameter / ReferenceDiameter;
        float horizontalScale = footprintScale * Mathf.Lerp(1.02f, 1.18f, strength);
        float verticalScale = footprintScale * Mathf.Lerp(0.24f, 0.38f, strength);

        for (int i = 0; i < particleStates.Count; i++)
        {
            ApplyParticleVisualState(particleStates[i], strength, horizontalScale, verticalScale);
        }

        for (int i = 0; i < lightStates.Count; i++)
        {
            ApplyLightVisualState(lightStates[i], strength, horizontalScale, verticalScale);
        }
    }

    private void ApplyParticleVisualState(ParticleVisualState state, float strength, float horizontalScale, float verticalScale)
    {
        if (state.ParticleSystem == null)
        {
            return;
        }

        Transform particleTransform = state.ParticleSystem.transform;
        if (particleTransform != transform)
        {
            Vector3 originalPosition = state.OriginalLocalPosition;
            particleTransform.localPosition = new Vector3(
                originalPosition.x * horizontalScale,
                originalPosition.y * verticalScale,
                originalPosition.z * horizontalScale);
        }

        ParticleSystem.MainModule main = state.ParticleSystem.main;
        main.startSizeMultiplier = state.OriginalStartSize * horizontalScale;
        main.startColor = BuildParticleTint(state.ParticleSystem.gameObject.name, strength);

        ParticleSystem.EmissionModule emission = state.ParticleSystem.emission;
        emission.rateOverTimeMultiplier = state.OriginalEmissionRate * Mathf.Lerp(0.12f, 0.72f, strength);

        if (state.RuntimeMaterial != null)
        {
            Color tint = BuildParticleTint(state.ParticleSystem.gameObject.name, strength);
            if (state.RuntimeMaterial.HasProperty("_TintColor"))
            {
                state.RuntimeMaterial.SetColor("_TintColor", tint);
            }
            else if (state.RuntimeMaterial.HasProperty("_Color"))
            {
                state.RuntimeMaterial.SetColor("_Color", tint);
            }
        }
    }

    private void ApplyLightVisualState(LightVisualState state, float strength, float horizontalScale, float verticalScale)
    {
        if (state.Light == null)
        {
            return;
        }

        Vector3 originalPosition = state.OriginalLocalPosition;
        state.Light.transform.localPosition = new Vector3(
            originalPosition.x * horizontalScale,
            originalPosition.y * verticalScale,
            originalPosition.z * horizontalScale);
        state.Light.color = Color.Lerp(new Color(1.00f, 0.56f, 0.14f, 1f), new Color(1.00f, 0.86f, 0.36f, 1f), strength);
        state.Light.intensity = state.OriginalIntensity * Mathf.Lerp(0.05f, 0.28f, strength);
        state.Light.range = state.OriginalRange * horizontalScale * Mathf.Lerp(0.75f, 1.10f, strength);
    }

    private Color BuildParticleTint(string particleName, float strength)
    {
        switch (particleName)
        {
            case "Glow":
                return new Color(1.00f, 0.56f, 0.12f, Mathf.Lerp(0.01f, 0.06f, strength));
            case "Cylinder":
                return new Color(1.00f, 0.75f, 0.20f, Mathf.Lerp(0.02f, 0.12f, strength));
            case "Circle":
                return new Color(1.00f, 0.86f, 0.28f, Mathf.Lerp(0.03f, 0.18f, strength));
            default:
                return EvaluateGoldTint(strength);
        }
    }
}
