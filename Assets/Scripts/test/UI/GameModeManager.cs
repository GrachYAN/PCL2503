using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }
    
    public enum GameMode
    {
        None,
        Online,
        Offline
    }

    public GameMode CurrentMode { get; private set; } = GameMode.None;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void SetMode(GameMode newMode)
    {
        CurrentMode = newMode;
    }
}