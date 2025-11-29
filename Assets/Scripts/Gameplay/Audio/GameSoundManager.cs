using UnityEngine;

/// <summary>
/// Centralized sound manager for gameplay sounds (movement, damage types, spell errors).
/// Provides volume normalization to ensure all sounds play at the same perceived loudness.
/// </summary>
public class GameSoundManager : MonoBehaviour
{
    public static GameSoundManager Instance { get; private set; }

    [Header("Sound Clips - Gameplay")]
    [Tooltip("Sound played when a piece moves")]
    public AudioClip moveSound;

    [Tooltip("Sound played for Physical damage")]
    public AudioClip physicalDamageSound;

    [Tooltip("Sound played for Fire damage")]
    public AudioClip fireDamageSound;

    [Tooltip("Sound played for Holy damage")]
    public AudioClip holyDamageSound;

    [Tooltip("Sound played for Arcane damage")]
    public AudioClip arcaneDamageSound;

    [Header("Sound Clips - Spell Errors")]
    [Tooltip("Sound played when not enough mana to cast")]
    public AudioClip notEnoughManaSound;

    [Tooltip("Sound played when spell is on cooldown")]
    public AudioClip cooldownNotReadySound;

    [Tooltip("Sound played when target is invalid")]
    public AudioClip cannotTargetSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [Tooltip("Master volume for all gameplay sounds")]
    public float masterVolume = 0.5f;

    [Header("Volume Normalization (dB offsets to equalize loudness)")]
    [Tooltip("Volume multiplier for Move sound (adjust if too loud/quiet)")]
    [Range(0.1f, 2f)]
    public float moveVolumeMultiplier = 1f;

    [Tooltip("Volume multiplier for Physical damage sound")]
    [Range(0.1f, 2f)]
    public float physicalVolumeMultiplier = 1f;

    [Tooltip("Volume multiplier for Fire damage sound")]
    [Range(0.1f, 2f)]
    public float fireVolumeMultiplier = 1f;

    [Tooltip("Volume multiplier for Holy damage sound")]
    [Range(0.1f, 2f)]
    public float holyVolumeMultiplier = 1f;

    [Tooltip("Volume multiplier for Arcane damage sound")]
    [Range(0.1f, 2f)]
    public float arcaneVolumeMultiplier = 1f;

    [Tooltip("Volume multiplier for error sounds")]
    [Range(0.1f, 2f)]
    public float errorVolumeMultiplier = 1f;

    private AudioSource audioSource;

    // Flag to suppress move sound when a spell is being executed
    // (spells that move and deal damage should only play damage sound)
    private bool suppressMoveSound = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create or get AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Call this before executing a spell to suppress move sounds.
    /// Spells that involve both movement and damage should only play damage sound.
    /// </summary>
    public void BeginSpellExecution()
    {
        suppressMoveSound = true;
    }

    /// <summary>
    /// Call this after spell execution completes to re-enable move sounds.
    /// </summary>
    public void EndSpellExecution()
    {
        suppressMoveSound = false;
    }

    /// <summary>
    /// Plays the movement sound (unless suppressed during spell execution).
    /// </summary>
    public void PlayMoveSound()
    {
        // Don't play move sound if a spell is being executed
        if (suppressMoveSound)
        {
            return;
        }

        if (moveSound != null)
        {
            PlaySound(moveSound, moveVolumeMultiplier);
        }
        else
        {
            Debug.LogWarning("[GameSoundManager] Move sound clip is not assigned!");
        }
    }

    /// <summary>
    /// Plays the appropriate damage sound based on damage type.
    /// </summary>
    /// <param name="damageType">The type of damage dealt</param>
    public void PlayDamageSound(DamageType damageType)
    {
        AudioClip clip = null;
        float volumeMultiplier = 1f;

        switch (damageType)
        {
            case DamageType.Physical:
                clip = physicalDamageSound;
                volumeMultiplier = physicalVolumeMultiplier;
                break;
            case DamageType.Fire:
                clip = fireDamageSound;
                volumeMultiplier = fireVolumeMultiplier;
                break;
            case DamageType.Holy:
                clip = holyDamageSound;
                volumeMultiplier = holyVolumeMultiplier;
                break;
            case DamageType.Arcane:
                clip = arcaneDamageSound;
                volumeMultiplier = arcaneVolumeMultiplier;
                break;
        }

        if (clip != null)
        {
            PlaySound(clip, volumeMultiplier);
        }
        else
        {
            Debug.LogWarning($"[GameSoundManager] {damageType} damage sound clip is not assigned!");
        }
    }

    /// <summary>
    /// Plays the "Not enough mana" error sound.
    /// </summary>
    public void PlayNotEnoughManaSound()
    {
        if (notEnoughManaSound != null)
        {
            PlaySound(notEnoughManaSound, errorVolumeMultiplier);
        }
        else
        {
            Debug.LogWarning("[GameSoundManager] Not enough mana sound clip is not assigned!");
        }

        if (GameNotificationManager.Instance != null)
        {
            GameNotificationManager.Instance.ShowSystemMessage("I do not have enough mana.", false); // ºìÉ«
        }
    }

    /// <summary>
    /// Plays the "Spell on cooldown" error sound.
    /// </summary>
    public void PlayCooldownNotReadySound()
    {
        if (cooldownNotReadySound != null)
        {
            PlaySound(cooldownNotReadySound, errorVolumeMultiplier);
        }
        else
        {
            Debug.LogWarning("[GameSoundManager] Cooldown not ready sound clip is not assigned!");
        }

        if (GameNotificationManager.Instance != null)
        {
            GameNotificationManager.Instance.ShowSystemMessage("This spell is not ready yet.", true); // »ÆÉ«
        }
    }

    /// <summary>
    /// Plays the "Cannot target that" error sound.
    /// </summary>
    public void PlayCannotTargetSound()
    {
        if (cannotTargetSound != null)
        {
            PlaySound(cannotTargetSound, errorVolumeMultiplier);
        }
        else
        {
            Debug.LogWarning("[GameSoundManager] Cannot target sound clip is not assigned!");
        }

        if (GameNotificationManager.Instance != null)
        {
            GameNotificationManager.Instance.ShowSystemMessage("I cannot target that", false); // ºìÉ«
        }
    }

    /// <summary>
    /// Plays a sound with the given volume multiplier applied to master volume.
    /// </summary>
    private void PlaySound(AudioClip clip, float volumeMultiplier)
    {
        if (audioSource == null || clip == null) return;

        float finalVolume = masterVolume * volumeMultiplier;
        audioSource.PlayOneShot(clip, finalVolume);
    }

    /// <summary>
    /// Sets the master volume for all gameplay sounds.
    /// </summary>
    /// <param name="volume">Volume level from 0 to 1</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Mutes or unmutes all gameplay sounds.
    /// </summary>
    public void SetMuted(bool muted)
    {
        if (audioSource != null)
        {
            audioSource.mute = muted;
        }
    }
}
