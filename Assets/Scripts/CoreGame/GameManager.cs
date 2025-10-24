using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public Button playButton;
    
    public NetworkVariable<bool> roundInProgress = new NetworkVariable<bool>(false);
    public NetworkVariable<int> playersPlayedThisRound = new NetworkVariable<int>(0);
    
    private List<ulong> playersWhoPlayed = new List<ulong>();
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void PlayerPlayedCardsServerRpc(ulong playerId, int cardsPlayed)
    {
        if (!playersWhoPlayed.Contains(playerId))
        {
            playersWhoPlayed.Add(playerId);
            playersPlayedThisRound.Value = playersWhoPlayed.Count;

            // Check if all players have played
            if (playersWhoPlayed.Count >= TurnManager.Instance.GetPlayerCount())
            {
                // All players have played, reveal cards and determine winner
                RevealCardsAndDetermineWinnerServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void RevealCardsAndDetermineWinnerServerRpc()
    {
        Debug.Log("All players have played! Revealing cards...");

        // Get card IDs
        ulong[] cardIds = new ulong[PlayingArea.Instance.cardsInArea.Count];
        for (int i = 0; i < PlayingArea.Instance.cardsInArea.Count; i++)
        {
            cardIds[i] = PlayingArea.Instance.cardsInArea[i].NetworkObjectId;
        }

        // Reveal all played cards to all clients
        RevealPlayedCardsClientRpc(cardIds);

        // TODO: Add game logic to determine winner
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void RevealPlayedCardsClientRpc(ulong[] cardNetworkIds)
    {
        foreach (ulong netId in cardNetworkIds)
        {
            NetworkObject netObj;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out netObj))
            {
                Card card = netObj.GetComponent<Card>();
                if (card != null)
                    card.ShowFront();
            }
        }
    }


    private PlayerManager GetPlayerById(ulong playerId)
    {
        PlayerManager[] players = FindObjectsByType<PlayerManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PlayerManager player in players)
        {
            if (player.OwnerClientId == playerId)
                return player;
        }
        return null;
    }

    public bool IsValidCardPlayCount(int count)
    {
        return count >= 2 && count <= 5;
    }
}