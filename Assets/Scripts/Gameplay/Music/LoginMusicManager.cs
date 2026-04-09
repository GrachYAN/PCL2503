using UnityEngine;

// This automatically adds an AudioSource component to the GameObject.
[RequireComponent(typeof(AudioSource))]
public class LoginMusicManager : MonoBehaviour
{
    // Make this a Singleton so it's easy to find.
    public static LoginMusicManager Instance { get; private set; }
    
    [SerializeField] private AudioClip loginSoundtrack;
    private AudioSource audioSource;

    void Awake()
    {
        // --- Singleton Pattern ---
        // If an instance already exists, destroy this one.
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // --- Make it Persist ---
        // This keeps the music playing while the new scene loads.
        DontDestroyOnLoad(this.gameObject);

        // --- Setup and Play Audio ---
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = loginSoundtrack;
        audioSource.loop = true;         // Make the music loop
        audioSource.playOnAwake = true;
        audioSource.volume = 0.5f;     // Start at a reasonable volume
        
        if (loginSoundtrack != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("LoginMusicManager is missing its soundtrack clip!");
        }
    }

    /// <summary>
    /// Call this to stop the music and clean up the manager.
    /// </summary>
    public void StopMusic()
    {
        // You could add a fade-out here later.
        audioSource.Stop();
        
        // Destroy this object, as its job is done.
        Destroy(this.gameObject); 
    }
}