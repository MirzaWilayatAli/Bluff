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
        currentPlayerId.OnValueChanged += OnCurrentPlayerChanged;
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
            turnText.text = $"Now Player {playerId}'s turn";
        }
    }
    
    public void RegisterPlayer(PlayerManager player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"TurnManager says registered player: {player.OwnerClientId}");
        }
    }
    
    [Rpc(SendTo.Server,RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
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