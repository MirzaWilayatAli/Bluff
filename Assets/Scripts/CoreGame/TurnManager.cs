using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public NetworkVariable<int> currentPlayerId = new NetworkVariable<int>(0);
    public List<PlayerManager> players = new List<PlayerManager>();
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
        currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
        
        // Wait a frame to ensure all players are registered, then update
        StartCoroutine(UpdateTurnAfterDelay());
    }
    
    private System.Collections.IEnumerator UpdateTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        UpdateTurnText(currentPlayerId.Value);
    }
    
    private void OnCurrentPlayerChanged(int oldValue, int newValue)
    {
        UpdateTurnText(newValue);
    }
    
    private void UpdateTurnText(int playerId)
    {
        if (turnText != null)
        {
            string playerName = GetPlayerName(playerId);
            turnText.text = $"{playerName}'s turn";
        }
    }
    
    private string GetPlayerName(int playerId)
    {
        // Try to find the player by ClientId instead of list index
        foreach (var player in players)
        {
            if (player != null && (ulong)playerId == player.OwnerClientId)
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
            if ((ulong)playerId == player.OwnerClientId)
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
        currentPlayerId.Value = (currentPlayerId.Value + 1) % players.Count;
    }
    
    public bool IsMyTurn(ulong playerId)
    {
        return (ulong)currentPlayerId.Value == playerId;
    }
    
    public int GetPlayerCount()
    {
        return players.Count;
    }
}