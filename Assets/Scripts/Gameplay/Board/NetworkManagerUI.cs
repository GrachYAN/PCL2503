using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;
using System.Net;
using UnityEngine.SceneManagement;

public class NetworkManagerUI : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private NetworkSceneManager networkSceneManager;
    
    [Header("Panels")]
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostWaitingPanel;
    [SerializeField] private GameObject joinGamePanel;
    [SerializeField] private GameObject factionSelectPanel;

    [Header("Mode Select Panel")]
    [SerializeField] private Button onlineModeButton;
    [SerializeField] private Button offlineModeButton;

    [Header("Main Panel Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button joinGameButton;

    [Header("Host Panel")]
    [SerializeField] private Button cancelHostButton;
    [SerializeField] private TMP_Text waitingForPlayerText; 

    [Header("Join Panel")]
    [SerializeField] private TMP_InputField ipAddressField;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button returnToMainButton;
    [SerializeField] private TMP_Text errorMessageText;

    [Header("Faction Select Panel")]
    [SerializeField] private TMP_Text factionPromptText;
    [SerializeField] private TMP_Text factionErrorText;
    [SerializeField] private Button bloodElfButton;
    [SerializeField] private Button dwarfButton;
    [SerializeField] private Button undeadButton;
    [SerializeField] private Button pandarenButton;

    private enum FactionSelectionContext
    {
        None,
        Offline,
        Online
    }

    private FactionSelectionManager factionSelectionManager;
    private FactionSelectionContext currentFactionContext = FactionSelectionContext.None;
    private int offlineSelectionIndex = 0;

    private void Awake()
    {
        factionSelectionManager = FindFirstObjectByType<FactionSelectionManager>();

        // Set initial state
        ShowModeSelectPanel();

        //Mode Select Panel
        onlineModeButton.onClick.AddListener(OnOnlineModeClicked);
        offlineModeButton.onClick.AddListener(OnOfflineModeClicked);

        // Main Panel
        startGameButton.onClick.AddListener(OnStartHostClicked);
        joinGameButton.onClick.AddListener(OnJoinGameClicked);

        // Host Panel
        cancelHostButton.onClick.AddListener(OnCancelHostClicked);

        // Join Panel
        connectButton.onClick.AddListener(OnConnectClicked);
        returnToMainButton.onClick.AddListener(ShowMainPanel);

        // Faction Select Panel
        bloodElfButton.onClick.AddListener(() => OnFactionButtonClicked(Faction.Elf));
        dwarfButton.onClick.AddListener(() => OnFactionButtonClicked(Faction.Dwarf));
        undeadButton.onClick.AddListener(() => OnFactionButtonClicked(Faction.Undead));
        pandarenButton.onClick.AddListener(() => OnFactionButtonClicked(Faction.Pandaren));
    }

    public void ShowModeSelectPanel()
    {
        modeSelectPanel.SetActive(true);
        mainPanel.SetActive(false);
        hostWaitingPanel.SetActive(false);
        joinGamePanel.SetActive(false);
        factionSelectPanel.SetActive(false);
    }
    private void ShowMainPanel()
    {
        modeSelectPanel.SetActive(false);
        mainPanel.SetActive(true);
        hostWaitingPanel.SetActive(false);
        joinGamePanel.SetActive(false);
        factionSelectPanel.SetActive(false);
    }

    private void OnOnlineModeClicked()
    {
        GameModeManager.Instance.SetMode(GameModeManager.GameMode.Online);
        BeginFactionSelection(FactionSelectionContext.Online);
    }

    private void OnOfflineModeClicked()
    {
        GameModeManager.Instance.SetMode(GameModeManager.GameMode.Offline);
        BeginFactionSelection(FactionSelectionContext.Offline);
    }

    /// <summary>
    /// Shows the Join Game Panel and hides the others.
    /// </summary>
    private void ShowJoinGamePanel()
    {
        mainPanel.SetActive(false);
        hostWaitingPanel.SetActive(false);
        joinGamePanel.SetActive(true);
    }

    /// <summary>
    /// Called by "Start a Game" button.
    /// Shows the waiting panel and starts the host.
    /// </summary>
    private void OnStartHostClicked()
    {
        if (!EnsureHostFactionSelected())
        {
            return;
        }

        mainPanel.SetActive(false);
        hostWaitingPanel.SetActive(true);
        joinGamePanel.SetActive(false);

        waitingForPlayerText.text = "Waiting for a player to join...";

        NetworkManager.Singleton.StartHost();

        if (factionSelectionManager != null)
        {
            factionSelectionManager.SetExpectedPlayers(2);
            factionSelectionManager.EnsureNetworkObjectSpawned();
            factionSelectionManager.RegisterHostSelection();
        }
    }

    /// <summary>
    /// Called by "Join a Game" button.
    /// Shows the IP panel.
    /// </summary>
    private void OnJoinGameClicked()
    {
        if (!EnsureClientFactionSelected())
        {
            return;
        }

        mainPanel.SetActive(false);
        hostWaitingPanel.SetActive(false);
        joinGamePanel.SetActive(true);
        
        errorMessageText.gameObject.SetActive(false); // Hide old errors
    }

    /// <summary>
    /// Called by "Cancel" button on Host panel.
    /// Shuts down the network session.
    /// The NetworkSceneManager will detect this and tell us to return to the main panel.
    /// </summary>
    private void OnCancelHostClicked()
    {
        NetworkManager.Singleton.Shutdown();
    }

    /// <summary>
    /// Called by "Connect" button on Join panel.
    /// Tries to connect as a client.
    /// </summary>
    private void OnConnectClicked()
    {
        string ip = ipAddressField.text;
        if (string.IsNullOrEmpty(ip))
        {
            ip = "127.0.0.1";
        }

        bool isValid = false;

        if (ip.ToLower() == "localhost")
        {
            isValid = true;
            ip = "127.0.0.1"; // Standardize for the transport
        }
        else
        {
            // Check 2: Check for 4 parts (e.g., "192.168.1.1")
            // This will fail "5.5", which only has 2 parts
            var parts = ip.Split('.');
            if (parts.Length == 4 && IPAddress.TryParse(ip, out _))
            {
                // It has 4 parts AND it's a valid IP format
                isValid = true;
            }
        }

        // Check 3: Final validation
        if (!isValid)
        {
            errorMessageText.text = $"'{ip}' is not a valid IP address format.";
            errorMessageText.color = Color.red;
            errorMessageText.gameObject.SetActive(true);
            connectButton.gameObject.SetActive(false);
            ipAddressField.gameObject.SetActive(false);
            return; // Stop here, don't try to connect
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
        }
        
        Debug.Log($"Starting as Client, connecting to {ip}...");
        NetworkManager.Singleton.StartClient();
        
        // Show neutral "Connecting..." message
        errorMessageText.text = $"Connecting to {ip}...";
        errorMessageText.color = Color.white;
        errorMessageText.gameObject.SetActive(true);

        if (factionSelectionManager != null)
        {
            factionSelectionManager.SetExpectedPlayers(2);
        }

        connectButton.gameObject.SetActive(false);
        ipAddressField.gameObject.SetActive(false);
        returnToMainButton.gameObject.SetActive(false);
    }

    private void BeginFactionSelection(FactionSelectionContext context)
    {
        if (factionSelectionManager == null)
        {
            Debug.LogWarning("FactionSelectionManager not found.");
            return;
        }

        currentFactionContext = context;
        factionErrorText.text = string.Empty;

        switch (context)
        {
            case FactionSelectionContext.Offline:
                factionSelectionManager.ResetSelections(2);
                offlineSelectionIndex = 0;
                factionPromptText.text = "Player 1: Choose your faction";
                break;
            case FactionSelectionContext.Online:
                factionSelectionManager.ResetSelections(2);
                factionPromptText.text = "Host: Choose your faction";
                break;
            default:
                break;
        }

        modeSelectPanel.SetActive(false);
        mainPanel.SetActive(false);
        hostWaitingPanel.SetActive(false);
        joinGamePanel.SetActive(false);
        factionSelectPanel.SetActive(true);
    }

    private void OnFactionButtonClicked(Faction faction)
    {
        if (factionSelectionManager == null)
        {
            return;
        }

        bool success = factionSelectionManager.TrySelectFaction(faction);
        if (!success)
        {
            factionErrorText.text = "Faction already selected. Choose another.";
            return;
        }

        factionErrorText.text = string.Empty;

        if (currentFactionContext == FactionSelectionContext.Offline)
        {
            offlineSelectionIndex++;
            if (offlineSelectionIndex >= 2)
            {
                StartOfflineGame();
            }
            else
            {
                factionPromptText.text = $"Player {offlineSelectionIndex + 1}: Choose your faction";
            }
        }
        else if (currentFactionContext == FactionSelectionContext.Online)
        {
            factionSelectPanel.SetActive(false);
            ShowMainPanel();
            currentFactionContext = FactionSelectionContext.None;
        }
    }

    private void StartOfflineGame()
    {
        if (networkSceneManager != null && !string.IsNullOrEmpty(networkSceneManager.GameSceneName))
        {
            SceneManager.LoadScene(networkSceneManager.GameSceneName);
            LoginMusicManager.Instance.StopMusic();
        }
        else
        {
            Debug.LogError("GameSceneName is not set in NetworkSceneManager! Cannot load game.");
        }
    }

    private bool EnsureHostFactionSelected()
    {
        if (factionSelectionManager == null)
        {
            return true;
        }

        if (!factionSelectionManager.HasSelectionForSeat(0))
        {
            BeginFactionSelection(FactionSelectionContext.Online);
            factionErrorText.text = "Host must choose a faction before hosting.";
            return false;
        }

        return true;
    }

    private bool EnsureClientFactionSelected()
    {
        if (factionSelectionManager == null)
        {
            return true;
        }

        if (factionSelectionManager.GetSelectionCount() == 0)
        {
            BeginFactionSelection(FactionSelectionContext.Online);
            factionPromptText.text = "Client: Choose your faction";
            return false;
        }

        return true;
    }

    // --- Public Methods for NetworkSceneManager ---

    /// <summary>
    /// Called by NetworkSceneManager when a connection fails.
    /// </summary>
    public void ShowConnectionFailedError()
    {
        // This method assumes we are already on the joinGamePanel
        if (!joinGamePanel.activeSelf)
        {
            ShowJoinGamePanel(); // Failsafe
        }
        
        errorMessageText.text = "The game does not exist or connection failed.";
        errorMessageText.color = Color.red;
        errorMessageText.gameObject.SetActive(true);

        returnToMainButton.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Called by NetworkSceneManager when the server stops
    /// (e.g., "Cancel" was clicked).
    /// </summary>
    public void ReturnToMainPanel()
    {
        ShowMainPanel();
    }
}