using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // We need this for the Input Field
using Unity.Netcode.Transports.UTP; // We need this to change the IP

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_InputField ipAddressField; // Add this line

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

        // Check if the IP field is linked in the inspector
        if (ipAddressField == null)
        {
            Debug.LogError("NetworkManagerUI: IP Address Field is not assigned in the Inspector!");
            return;
        }

        hostButton.onClick.AddListener(() =>
        {
            Debug.Log("Starting as Host...");
            NetworkManager.Singleton.StartHost();
            HideUI();
        });

        clientButton.onClick.AddListener(() =>
        {
            string ipAddress = ipAddressField.text;
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                Debug.Log("IP Address is empty, using localhost (127.0.0.1)");
                ipAddress = "127.0.0.1";
            }
            
            Debug.Log($"Starting as Client, connecting to {ipAddress}...");

            // Get the Unity Transport component and set the connection data
            // This is the CRITICAL step.
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = ipAddress;
                // Port 7777 is the default. You can also add a field for this.
                // transport.ConnectionData.Port = 7777; 
            }
            else
            {
                Debug.LogError("Could not find UnityTransport on the NetworkManager!");
                return;
            }

            NetworkManager.Singleton.StartClient();
            HideUI();
        });
    }

    private void HideUI()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
        ipAddressField.gameObject.SetActive(false);
    }
}