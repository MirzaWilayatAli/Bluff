using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    public NetworkVariable<int> currentPlayerId = new NetworkVariable<int>(0);
    private List<PlayerManager> players = new List<PlayerManager>();
    
    public static TurnManager Instance { get; private set; }
    
    private void Update()
    {
        // TEST: Host advances turn with T key
        if (IsServer && Input.GetKeyDown(KeyCode.T))
        {
            AdvanceTurnServerRpc();
        }
    }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        Debug.Log($"Now Player {currentPlayerId.Value}'s turn");
    }
    
    public bool IsMyTurn(ulong playerId)
    {
        return (ulong)currentPlayerId.Value == playerId;
    }
}