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
    public Button revealButton;
    
    
    public NetworkVariable<bool> roundInProgress = new NetworkVariable<bool>(false);
    public NetworkVariable<int> playersPlayedThisRound = new NetworkVariable<int>(0);
    private List<ulong> playersWhoPlayed = new List<ulong>();
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        revealButton.onClick.AddListener(OnRevealButtonClick);
    }
    

    public void OnRevealButtonClick()
    {
        RevealCardsServerRpc();
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void RevealCardsServerRpc()
    {
        // Get card IDs
        ulong[] cardIds = new ulong[PlayingArea.Instance.cardsPlayedList.Count];
        for (int i = 0; i < PlayingArea.Instance.cardsPlayedList.Count; i++)
        {
            cardIds[i] = PlayingArea.Instance.cardsPlayedList[i].NetworkObjectId;
        }
        
        // Reveal all played cards to all clients
        RevealPlayedCardsClientRpc(cardIds);
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

    public bool CheckEnoughCardsWerePlayed(int count)
    {
        if (PlayingArea.Instance.cardsPlayedList.Count == 0)
        {
            return count >= 2;
        }
        else
        {
            return true;
        }
    }
}