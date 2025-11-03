using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

public class TurnManager : NetworkBehaviour
{
    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>(0); // Changed to ulong
    public NetworkVariable<ulong> lastPlayerId = new NetworkVariable<ulong>(0); // Changed default from -1 to 0
    public List<PlayerManager> players = new List<PlayerManager>();
    [SerializeField] private TextMeshProUGUI turnText;
    
    public static TurnManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
        
        // Wait a frame to ensure all players are registered, then update
        StartCoroutine(UpdateTurnAfterDelay());
    }
    
    private IEnumerator UpdateTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateTurnText(currentPlayerId.Value);
    }
    
    private void OnCurrentPlayerChanged(ulong oldValue, ulong newValue) // Changed to ulong
    {
        UpdateTurnText(newValue);
    }
    
    private void UpdateTurnText(ulong playerId) // Changed to ulong
    {
        if (turnText != null)
        {
            string playerName = GetPlayerName(playerId);
            turnText.text = $"{playerName}'s turn";
        }
    }
    
    public string GetPlayerName(ulong playerId)
    {
        // Try to find the player by ClientId instead of list index
        foreach (var player in players)
        {
            if (player != null && playerId == player.OwnerClientId)
            {
                PlayerName playerNameComponent = player.GetComponent<PlayerName>();
                if (playerNameComponent != null && !string.IsNullOrEmpty(playerNameComponent.playerName.Value.ToString()))
                {
                    return playerNameComponent.playerName.Value.ToString();
                }
            }
        }
        
        // Fallback: search all PlayerManager objects in scene
        var allPlayers = FindObjectsByType<PlayerManager>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (playerId == player.OwnerClientId)
            {
                PlayerName playerNameComponent = player.GetComponent<PlayerName>();
                if (playerNameComponent != null && !string.IsNullOrEmpty(playerNameComponent.playerName.Value.ToString()))
                {
                    return playerNameComponent.playerName.Value.ToString();
                }
            }
        }
        
        return $"Player {playerId}";
    }
    
    public void RegisterPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"Registered player: {player.OwnerClientId} at index {players.IndexOf(player)}");
        }
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void AdvanceTurnServerRpc()
    {
        lastPlayerId.Value = currentPlayerId.Value;
        
        // Find the current player index and advance to next
        int currentIndex = -1;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].OwnerClientId == currentPlayerId.Value)
            {
                currentIndex = i;
                break;
            }
        }
        
        if (currentIndex != -1 && players.Count > 0)
        {
            int nextIndex = (currentIndex + 1) % players.Count;
            currentPlayerId.Value = players[nextIndex].OwnerClientId;
        }
    }
    
    public ulong GetLastPlayerId()
    {
        return lastPlayerId.Value; // No need to cast now
    }
    
    public bool IsMyTurn(ulong playerId)
    {
        return currentPlayerId.Value == playerId; // No need to cast now
    }
}