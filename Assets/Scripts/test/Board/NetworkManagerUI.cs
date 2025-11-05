using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI; // Make sure to add this for UI elements

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        // Check if the host button is linked in the inspector
        if (hostButton == null)
        {
            Debug.LogError("NetworkManagerUI: Host Button is not assigned in the Inspector!");
            return;
        }
        
        // Check if the client button is linked in the inspector
        if (clientButton == null)
        {
            Debug.LogError("NetworkManagerUI: Client Button is not assigned in the Inspector!");
            return;
        }

        // This is line 15 (or around there). It's now safe.
        hostButton.onClick.AddListener(() =>
        {
            Debug.Log("Starting as Host...");
            NetworkManager.Singleton.StartHost();
            HideButtons();
        });

        clientButton.onClick.AddListener(() =>
        {
            Debug.Log("Starting as Client...");
            NetworkManager.Singleton.StartClient();
            HideButtons();
        });
    }

    private void HideButtons()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }
}