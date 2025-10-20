using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public NetworkVariable<int> currentPlayerId = new NetworkVariable<int>(0);
    private List<PlayerManager> players = new List<PlayerManager>();
    [SerializeField] private TextMeshProUGUI turnText;
    
    public static TurnManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public override void OnNetworkSpawn()
    {
        // Subscribe to NetworkVariable changes
        currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
        
        // Update UI immediately with current value
        UpdateTurnText(currentPlayerId.Value);
    }
    
    private void OnCurrentPlayerChanged(int oldValue, int newValue)
    {
        // This runs on ALL clients when currentPlayerId changes
        UpdateTurnText(newValue);
    }
    
    private void UpdateTurnText(int playerId)
    {
        if (turnText != null)
        {
            turnText.text = $"Now Player {playerId}'s turn";
            Debug.Log($"Turn UI updated: Player {playerId}'s turn");
        }
    }
    
    public void RegisterPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"TurnManager: Registered Player {player.OwnerClientId}");
        }
    }
    
    [ServerRpc]
    public void AdvanceTurnServerRpc()
    {
        currentPlayerId.Value = (currentPlayerId.Value + 1) % players.Count;
        // Text update is now handled by OnCurrentPlayerChanged on all clients
    }
    
    public bool IsMyTurn(ulong playerId)
    {
        return (ulong)currentPlayerId.Value == playerId;
    }
}