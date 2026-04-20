using UnityEngine;
using UnityEngine.SceneManagement;

// This automatically adds an AudioSource component to the GameObject.
[RequireComponent(typeof(AudioSource))]
public class LoginMusicManager : MonoBehaviour
{
    // Make this a Singleton so it's easy to find.
    public static LoginMusicManager Instance { get; private set; }
    
    [SerializeField] private AudioClip loginSoundtrack;
    private AudioSource audioSource;
    private bool isStopping;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!string.Equals(scene.name, ProjectSceneNames.Login, System.StringComparison.Ordinal))
        {
            StopMusic();
        }
    }

    /// <summary>
    /// Call this to stop the music and clean up the manager.
    /// </summary>
    public void StopMusic()
    {
        if (isStopping)
        {
            return;
        }

        isStopping = true;

        // You could add a fade-out here later.
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        
        // Destroy this object, as its job is done.
        Destroy(this.gameObject); 
    }
}
