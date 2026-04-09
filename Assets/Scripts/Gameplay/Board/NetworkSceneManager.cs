using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : MonoBehaviour
{
    public string GameSceneName = "GameScene"; 

    private NetworkManagerUI networkManagerUI;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted += OnClientStartedCallback;
            NetworkManager.Singleton.OnServerStarted += OnServerStartedCallback;
            
            // --- NEW LOGIC ---
            // Subscribe to the "client connected" event, which runs on the HOST
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            // --- END NEW LOGIC ---

            NetworkManager.Singleton.OnClientStopped += OnClientStoppedCallback;
            NetworkManager.Singleton.OnServerStopped += OnServerStoppedCallback;
        }

        networkManagerUI = FindFirstObjectByType<NetworkManagerUI>();
        if (networkManagerUI == null)
        {
            Debug.LogError("NetworkSceneManager could not find NetworkManagerUI in the scene!");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStartedCallback;
            NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
            
            // --- NEW LOGIC ---
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            // --- END NEW LOGIC ---
            
            NetworkManager.Singleton.OnClientStopped -= OnClientStoppedCallback;
            NetworkManager.Singleton.OnServerStopped -= OnServerStoppedCallback;
        }
    }

    private void OnClientStartedCallback()
    {
        // This is called when the client (or host-as-client) successfully starts
        // We just log it. We wait for the server to change the scene.
        Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} has started!");
    }

    private void OnServerStartedCallback()
    {
        // --- LOGIC REMOVED ---
        // We no longer load the scene here.
        // We wait for a client to connect first.
        Debug.Log("Server has started and is waiting for a client...");
        // --- END LOGIC REMOVED ---
    }

    // --- NEW LOGIC ---
    /// <summary>
    /// This callback runs ON THE HOST (SERVER) whenever a new client connects.
    /// </summary>
    /// <param name="clientId">The ID of the client that just connected.</param>
    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        // Host branch
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"Client {clientId} has connected to the server.");

            if (clientId == NetworkManager.ServerClientId)
            {
                Debug.Log("Host's own client connected. Waiting for a remote client...");
                return;
            }

            Debug.Log("A remote client has joined! Waiting for faction selections before loading the GameScene...");

            FactionSelectionManager factionSelectionManager = FindFirstObjectByType<FactionSelectionManager>();
            if (factionSelectionManager != null)
            {
                factionSelectionManager.RegisterClientConnection(clientId);
            }

            if (networkManagerUI != null)
            {
                if (factionSelectionManager != null && factionSelectionManager.HasHostSelection())
                {
                    networkManagerUI.ShowHostWaitingForClientSelection();
                }
                else
                {
                    networkManagerUI.BeginOnlineHostFactionSelection();
                }
            }

            StartCoroutine(WaitForFactionsThenLoad());
        }
        else if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Client branch - only react to the event for our own client connection
            if (networkManagerUI != null)
            {
                networkManagerUI.ShowClientWaitingForHost();
            }
        }
    }
    // --- END NEW LOGIC ---

    private IEnumerator WaitForFactionsThenLoad()
    {
        FactionSelectionManager factionSelectionManager = FindFirstObjectByType<FactionSelectionManager>();

        if (factionSelectionManager == null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
            yield break;
        }

        while (!factionSelectionManager.AreSelectionsReadyForOnline())
        {
            yield return null;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    private void OnClientStoppedCallback(bool reconnecting)
    {
        Debug.Log($"Client has stopped. Reconnecting: {reconnecting}");

        if (reconnecting) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        // This logic is still good. If we disconnect, go back to LoginScene.
        if (currentSceneName != "LoginScene")
        {
            SceneManager.LoadScene("LoginScene");
        }
        else if (networkManagerUI != null)
        {
            // THIS IS THE NEW LOGIC FOR FAILED CONNECTION
            Debug.Log("Connection failed or client stopped on LoginScene. Showing error.");
            networkManagerUI.ShowConnectionFailedError(); // <-- NEW FUNCTION CALL
        }
    }

    // private void OnServerStoppedCallback(bool reconnecting)
    // {
    //     Debug.Log($"Server has stopped. Reconnecting: {reconnecting}");

    //     if (reconnecting) return;
        
    //     string currentSceneName = SceneManager.GetActiveScene().name;

    //     if (currentSceneName != "LoginScene")
    //     {
    //         SceneManager.LoadScene("LoginScene"); 
    //     }
    //     else if (networkManagerUI != null)
    //     {
    //         // THIS IS THE NEW LOGIC FOR CANCELLING HOST
    //         Debug.Log("Server stopped on LoginScene. Re-showing UI.");
    //         networkManagerUI.ReturnToMainPanel(); // <-- NEW FUNCTION CALL
    //     }
    // }
    
    private void OnServerStoppedCallback(bool reconnecting)
    {
        Debug.Log($"Server has stopped. Reconnecting: {reconnecting}");

        string currentSceneName = SceneManager.GetActiveScene().name;

        // --- NEW LOGIC ---
        // If we stop on the LoginScene, the user *must* have clicked "Cancel".
        // Show the main panel *regardless* of the reconnecting flag.
        if (currentSceneName == "LoginScene")
        {
            if (networkManagerUI != null)
            {
                Debug.Log("Server stopped on LoginScene. Re-showing UI.");
                networkManagerUI.ReturnToMainPanel();
            }
            return; // We're done, stay on this scene.
        }
        // --- END NEW LOGIC ---

        // If we are not on the LoginScene, check the reconnecting flag.
        if (reconnecting) return;
        
        // If not reconnecting, go back to the LoginScene.
        SceneManager.LoadScene("LoginScene"); 
    }
}