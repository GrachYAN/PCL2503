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
    private Faction? localSelection;

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
        return selectedFactions.Count >= requiredPlayers && selectedFactions.Count >= expectedPlayers;
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

        SyncSelectionsClientRpc(selectedFactions.ToArray());
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitFactionServerRpc(Faction faction, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        Faction resolved = ResolveFaction(faction);

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

        selectedFactions.Add(resolved);

        SyncSelectionsClientRpc(selectedFactions.ToArray());

        SelectionConfirmedClientRpc(resolved, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        });
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
    }

    [ClientRpc]
    private void SyncSelectionsClientRpc(Faction[] selections)
    {
        selectedFactions.Clear();
        foreach (Faction faction in selections)
        {
            selectedFactions.Add(ResolveFaction(faction));
        }
    }
}