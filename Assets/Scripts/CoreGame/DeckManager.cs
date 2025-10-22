using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode.Components;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DeckManager : NetworkBehaviour
{
    [SerializeField] private List<Card> deck = new List<Card>();
    private List<PlayerManager> connectedPlayers = new List<PlayerManager>();
    public float cardSpacing = 1f;
    public float cardPlaceX = -7f;
    public float cardPlaceY = -2f;
    public Button dealBtn;
    public static DeckManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        dealBtn.onClick.AddListener(OnDealButtonClicked);
    }

    public void RegisterPlayer(PlayerManager player)
    {
        if (!connectedPlayers.Contains(player))
            connectedPlayers.Add(player);
    }

    private void OnDealButtonClicked()
    {
        DealCardsToPlayers(connectedPlayers);
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }
    }
    public void DealCardsToPlayers(List<PlayerManager> players)
    {
        if (!IsServer) return;
        ShuffleDeck();
        int cardIndex = 0;

        foreach (PlayerManager player in players)
        {
            if (player.OwnerClientId == 0)
            {
                cardPlaceX = -7f;
                cardPlaceY = -2f;
            }
            else if (player.OwnerClientId == 1)
            {
                cardPlaceX = 1f;
                cardPlaceY = -2f;
            }
            else if (player.OwnerClientId == 2)
            {
                cardPlaceX = -7f;
                cardPlaceY = 0f;
            }
            else if (player.OwnerClientId == 3)
            {
                cardPlaceX = 1f;
                cardPlaceY = 0f;
            }

            for (int i = 0; i < 5; i++)
            {
                Card card = deck[cardIndex++];
                NetworkObject cardNetObj = card.GetComponent<NetworkObject>();

                // Ensure server owns the card for positioning
                if (!cardNetObj.IsSpawned)
                    cardNetObj.SpawnWithOwnership(NetworkManager.ServerClientId, true);
                else if (!cardNetObj.IsOwnedByServer)
                    cardNetObj.ChangeOwnership(NetworkManager.ServerClientId);

                // Position the card using NetworkTransform to sync to all clients
                Vector3 pos = new Vector3(cardPlaceX + (i * cardSpacing), cardPlaceY, 0);
                
                NetworkTransform netTransform = card.GetComponent<NetworkTransform>();
                if (netTransform != null)
                {
                    netTransform.Teleport(pos, Quaternion.identity, Vector3.one);
                }
                else
                {
                    cardNetObj.transform.position = pos;
                    Debug.LogWarning("Card missing NetworkTransform - positions won't sync to clients!");
                }
            }
        } 
        StartCoroutine(TransferOwnershipAfterDelay(players));
    }

    private IEnumerator TransferOwnershipAfterDelay(List<PlayerManager> players)
    {
        yield return new WaitForSeconds(0.1f);
        
        int cardIndex = 0;
        foreach (PlayerManager player in players)
        {
            for (int i = 0; i < 5; i++)
            {
                Card card = deck[cardIndex++];
                NetworkObject cardNetObj = card.GetComponent<NetworkObject>();
                
                // Transfer ownership to player
                cardNetObj.ChangeOwnership(player.OwnerClientId);
                player.AddCardToHandClientRpc(cardNetObj.NetworkObjectId);
            }
        }
    }
}
