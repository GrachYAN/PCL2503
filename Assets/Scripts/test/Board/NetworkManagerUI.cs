// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro; // We need this for the Input Field
// using Unity.Netcode.Transports.UTP; // We need this to change the IP

// public class NetworkManagerUI : MonoBehaviour
// {
//     [SerializeField] private Button hostButton;
//     [SerializeField] private Button clientButton;
//     [SerializeField] private TMP_InputField ipAddressField;

//     private void Awake()
//     {
//         if (hostButton == null)
//         {
//             Debug.LogError("NetworkManagerUI: Host Button is not assigned in the Inspector!");
//             return;
//         }
//         if (clientButton == null)
//         {
//             Debug.LogError("NetworkManagerUI: Client Button is not assigned in the Inspector!");
//             return;
//         }
//         if (ipAddressField == null)
//         {
//             Debug.LogError("NetworkManagerUI: IP Address Field is not assigned in the Inspector!");
//             return;
//         }

//         hostButton.onClick.AddListener(() =>
//         {
//             Debug.Log("Starting as Host...");
//             NetworkManager.Singleton.StartHost();
//             HideInteractionUI(); // Changed to HideInteractionUI
//         });

//         clientButton.onClick.AddListener(() =>
//         {
//             string ipAddress = ipAddressField.text;
//             if (string.IsNullOrWhiteSpace(ipAddress))
//             {
//                 Debug.Log("IP Address is empty, using localhost (127.0.0.1)");
//                 ipAddress = "127.0.0.1";
//             }
            
//             Debug.Log($"Starting as Client, connecting to {ipAddress}...");

//             UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
//             if (transport != null)
//             {
//                 transport.ConnectionData.Address = ipAddress;
//             }
//             else
//             {
//                 Debug.LogError("Could not find UnityTransport on the NetworkManager!");
//                 return;
//             }

//             NetworkManager.Singleton.StartClient();
//             HideInteractionUI(); // Changed to HideInteractionUI
//         });
//     }

//     // New method to only hide the interactive elements
//     private void HideInteractionUI()
//     {
//         hostButton.gameObject.SetActive(false);
//         clientButton.gameObject.SetActive(false);
//         ipAddressField.gameObject.SetActive(false);
//     }

//     public void ShowInteractionUI()
//     {
//         // This method is now valid because the variables are defined
//         hostButton.gameObject.SetActive(true);
//         clientButton.gameObject.SetActive(true);
//         ipAddressField.gameObject.SetActive(true);
//     }
// }

using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject hostWaitingPanel;
    [SerializeField] private GameObject joinGamePanel;

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

    private void Awake()
    {
        // Set initial state
        ShowMainPanel();

        // --- Assign Listeners ---
        // Main Panel
        startGameButton.onClick.AddListener(OnStartHostClicked);
        joinGameButton.onClick.AddListener(OnJoinGameClicked);

        // Host Panel
        cancelHostButton.onClick.AddListener(OnCancelHostClicked);

        // Join Panel
        connectButton.onClick.AddListener(OnConnectClicked);
        returnToMainButton.onClick.AddListener(ShowMainPanel);
    }

    /// <summary>
    /// Shows the Main Panel and hides the others.
    /// </summary>
    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        hostWaitingPanel.SetActive(false);
        joinGamePanel.SetActive(false);
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
        mainPanel.SetActive(false);
        hostWaitingPanel.SetActive(true);
        joinGamePanel.SetActive(false);
        
        waitingForPlayerText.text = "Waiting for a player to join...";
        
        NetworkManager.Singleton.StartHost();
    }

    /// <summary>
    /// Called by "Join a Game" button.
    /// Shows the IP panel.
    /// </summary>
    private void OnJoinGameClicked()
    {
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
        
        connectButton.gameObject.SetActive(false);
        ipAddressField.gameObject.SetActive(false);
        returnToMainButton.gameObject.SetActive(false);
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