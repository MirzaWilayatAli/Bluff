using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

public class TurnManager : NetworkBehaviour
{
    public NetworkVariable<ulong> currentPlayerId = new NetworkVariable<ulong>(0); 
    public NetworkVariable<ulong> lastPlayerId = new NetworkVariable<ulong>(0); 
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
        if (IsServer)
        {
            currentPlayerId.Value = players[0].OwnerClientId;
        }

        currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
        StartCoroutine(UpdateTurnAfterDelay());
    }
    
    private IEnumerator UpdateTurnAfterDelay()
    {
        yield return new WaitUntil(() => players.Count > 1);
        yield return new WaitForSeconds(0.2f); // extra delay for playerName sync
        UpdateTurnText(currentPlayerId.Value);
    }
    
    private void OnCurrentPlayerChanged(ulong oldValue, ulong newValue)
    {
        UpdateTurnText(newValue);
    }
    
    private void UpdateTurnText(ulong playerId) 
    {
        string playerName = GetPlayerName(playerId); 
        turnText.text = $"{playerName}'s turn";
    }
    
    public string GetPlayerName(ulong playerId)
    {
        // find the player by ClientId
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
        return $"Player {playerId}";
    }
    
    public void RegisterPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
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
        return lastPlayerId.Value;
    }
    
    public bool IsMyTurn(ulong playerId)
    {
        return currentPlayerId.Value == playerId;
    }
}