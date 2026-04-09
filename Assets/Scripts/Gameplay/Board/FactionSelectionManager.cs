using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class FactionSelectionManager : NetworkBehaviour
{
    public static FactionSelectionManager Instance { get; private set; }

    [SerializeField]
    private int expectedPlayers = 2;

    private readonly List<Faction> selectedFactions = new List<Faction>();
    private readonly Dictionary<ulong, int> clientSeatMap = new Dictionary<ulong, int>();
    private readonly List<ulong> pendingClientOrder = new List<ulong>();
    private ulong? activeClientSelectionId;
    private Faction? localSelection;

    private bool HostHasSelection => NetworkManager.Singleton != null && clientSeatMap.ContainsKey(NetworkManager.ServerClientId) && selectedFactions.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted += HandleClientStarted;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= HandleClientStarted;
        }
    }

    private void HandleClientStarted()
    {
        if (IsServer)
        {
            return;
        }

        if (localSelection.HasValue)
        {
            SubmitFactionServerRpc(localSelection.Value);
        }
    }

    public void ResetSelections(int playerCount)
    {
        expectedPlayers = Mathf.Max(1, playerCount);
        selectedFactions.Clear();
        clientSeatMap.Clear();
        pendingClientOrder.Clear();
        activeClientSelectionId = null;
        localSelection = null;
    }

    public bool TrySelectFaction(Faction faction)
    {
        Faction resolved = ResolveFaction(faction);
        if (IsFactionTaken(resolved))
        {
            return false;
        }

        if (selectedFactions.Count >= expectedPlayers)
        {
            return false;
        }

        selectedFactions.Add(resolved);
        localSelection = resolved;
        return true;
    }

    public void SetExpectedPlayers(int playerCount)
    {
        expectedPlayers = Mathf.Max(1, playerCount);
    }

    public bool HasSelectionForSeat(int seatIndex)
    {
        return seatIndex >= 0 && seatIndex < selectedFactions.Count;
    }

    public Faction GetFactionForSeat(int seatIndex)
    {
        if (seatIndex >= 0 && seatIndex < selectedFactions.Count)
        {
            return selectedFactions[seatIndex];
        }

        return Faction.Elf;
    }

    public int GetSelectionCount()
    {
        return selectedFactions.Count;
    }

    public bool AreSelectionsReadyForOnline()
    {
        if (NetworkManager.Singleton == null)
        {
            return selectedFactions.Count >= expectedPlayers;
        }

        int connectedClients = NetworkManager.Singleton.ConnectedClientsIds.Count;
        int requiredPlayers = Mathf.Min(expectedPlayers, connectedClients);
        return HostHasSelection && selectedFactions.Count >= requiredPlayers;
    }

    public Faction ResolveFaction(Faction faction)
    {
        switch (faction)
        {
            case Faction.Undead:
            case Faction.Pandaren:
                return Faction.Elf;
            default:
                return faction;
        }
    }

    public bool IsFactionTaken(Faction faction)
    {
        Faction resolved = ResolveFaction(faction);
        return selectedFactions.Exists(selected => ResolveFaction(selected) == resolved);
    }

    public void EnsureNetworkObjectSpawned()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !NetworkObject.IsSpawned)
        {
            NetworkObject.Spawn();
        }
    }

    public bool RegisterHostSelection()
    {
        if (!IsServer || !localSelection.HasValue)
        {
            return false;
        }

        if (clientSeatMap.ContainsKey(NetworkManager.ServerClientId))
        {
            Debug.Log("Host selection already registered; skipping duplicate registration.");
            return true;
        }

        Faction resolved = ResolveFaction(localSelection.Value);
        clientSeatMap[NetworkManager.ServerClientId] = 0;

        if (selectedFactions.Count == 0)
        {
            selectedFactions.Add(resolved);
        }
        else
        {
            selectedFactions[0] = resolved;
        }

        Debug.Log($"Host selected faction {resolved}; syncing to clients and prompting next client.");

        EnsurePendingClientsRegistered();

        SyncSelectionsClientRpc(selectedFactions.ToArray());

        TryPromptNextClient();

        return true;
    }

    public bool HasHostSelection()
    {
        return HostHasSelection;
    }

    public void RegisterClientConnection(ulong clientId)
    {
        if (!IsServer || clientId == NetworkManager.ServerClientId)
        {
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            expectedPlayers = Mathf.Max(expectedPlayers, NetworkManager.Singleton.ConnectedClientsIds.Count);
        }

        if (!pendingClientOrder.Contains(clientId) && !clientSeatMap.ContainsKey(clientId))
        {
            pendingClientOrder.Add(clientId);
            Debug.Log($"Registered client {clientId} for faction selection queue.");
        }

        if (selectedFactions.Count > 0)
        {
            SyncSelectionsClientRpc(selectedFactions.ToArray(), new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }

        TryPromptNextClient();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitFactionServerRpc(Faction faction, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Faction resolved = ResolveFaction(faction);

        if (!HostHasSelection && senderId != NetworkManager.ServerClientId)
        {
            SelectionFailedClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            });
            return;
        }

        if (activeClientSelectionId.HasValue && senderId != activeClientSelectionId.Value)
        {
            SelectionFailedClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            });
            return;
        }

        if (IsFactionTaken(resolved) || selectedFactions.Count >= expectedPlayers)
        {
            SelectionFailedClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            });
            return;
        }

        if (!clientSeatMap.ContainsKey(senderId))
        {
            clientSeatMap[senderId] = selectedFactions.Count;
        }

        if (selectedFactions.Count > clientSeatMap[senderId])
        {
            selectedFactions[clientSeatMap[senderId]] = resolved;
        }
        else
        {
            selectedFactions.Add(resolved);
        }

        if (activeClientSelectionId.HasValue && activeClientSelectionId.Value == senderId)
        {
            activeClientSelectionId = null;
        }

        SyncSelectionsClientRpc(selectedFactions.ToArray());

        SelectionConfirmedClientRpc(resolved, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        });

        TryPromptNextClient();
    }

    [ClientRpc]
    private void SelectionConfirmedClientRpc(Faction faction, ClientRpcParams clientRpcParams)
    {
        localSelection = ResolveFaction(faction);
    }

    [ClientRpc]
    private void SelectionFailedClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.LogWarning("Selected faction is not available. Please choose another faction.");

        NetworkManagerUI ui = FindFirstObjectByType<NetworkManagerUI>();
        if (ui != null)
        {
            ui.ShowFactionError("Faction already selected or not available yet. Choose another.");
        }
    }

    [ClientRpc]
    private void SyncSelectionsClientRpc(Faction[] selections, ClientRpcParams clientRpcParams = default)
    {
        // The host (server + client) already has authoritative data; skip to avoid overwriting.
        if (IsServer)
        {
            return;
        }

        selectedFactions.Clear();
        foreach (Faction faction in selections)
        {
            selectedFactions.Add(ResolveFaction(faction));
        }

        Debug.Log($"[Client] Synced {selectedFactions.Count} faction(s) from host.");

        // Notify UI that selections have been updated so it can refresh the display
        NetworkManagerUI ui = FindFirstObjectByType<NetworkManagerUI>();
        if (ui != null)
        {
            ui.OnFactionSelectionsUpdated(selectedFactions.Count);
        }
    }

    [ClientRpc]
    private void BeginClientFactionSelectionClientRpc(string prompt, ClientRpcParams clientRpcParams)
    {
        // The host is both server and client; skip this RPC on the host.
        if (IsServer || IsHost)
        {
            return;
        }

        Debug.Log($"[Client] Received prompt from host: '{prompt}'. Showing client faction selection UI.");

        NetworkManagerUI ui = FindFirstObjectByType<NetworkManagerUI>();
        if (ui != null)
        {
            ui.BeginOnlineClientFactionSelection(prompt);
        }
    }

    private void TryPromptNextClient()
    {
        if (!IsServer || !HostHasSelection)
        {
            return;
        }

        EnsurePendingClientsRegistered();

        if (activeClientSelectionId.HasValue)
        {
            return;
        }

        if (pendingClientOrder.Count == 0)
        {
            Debug.Log("No pending clients to prompt for faction selection.");
            return;
        }

        ulong nextClientId = pendingClientOrder[0];
        pendingClientOrder.RemoveAt(0);
        activeClientSelectionId = nextClientId;

        if (!clientSeatMap.ContainsKey(nextClientId))
        {
            clientSeatMap[nextClientId] = selectedFactions.Count;
        }

        int seatIndex = clientSeatMap[nextClientId];
        string prompt = seatIndex == 0 ? "Client: Choose your faction" : $"Client {seatIndex}: Choose your faction";

        Debug.Log($"Prompting client {nextClientId} (seat {seatIndex}) to select a faction.");

        BeginClientFactionSelectionClientRpc(prompt, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { nextClientId } }
        });
    }

    private void EnsurePendingClientsRegistered()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                continue;
            }

            if (!pendingClientOrder.Contains(clientId) && !clientSeatMap.ContainsKey(clientId))
            {
                pendingClientOrder.Add(clientId);
            }
        }
    }
}