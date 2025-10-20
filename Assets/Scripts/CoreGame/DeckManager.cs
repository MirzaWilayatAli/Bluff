using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class DeckManager : NetworkBehaviour
{
    [SerializeField] private List<Card> deck = new List<Card>();
    private List<Card> asideCards = new List<Card>();
    private List<PlayerManager> connectedPlayers = new List<PlayerManager>(); // DYNAMIC list!

    public static DeckManager Instance { get; private set; }

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

    // Players register themselves when they join
    public void RegisterPlayer(PlayerManager player)
    {
        if (!connectedPlayers.Contains(player))
        {
            connectedPlayers.Add(player);
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShuffleDeck();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                // Now deals to ACTUAL connected players, not static objects
                DealCardsToPlayers(connectedPlayers);
            }
        }
    }

    // Shuffle the deck using Fisher-Yates algorithm.
    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }

        Debug.Log("Deck shuffled!");
    }

    public void DealCardsToPlayers(List<PlayerManager> players)
    {
        if (!IsServer) return;

        Debug.Log($"Dealing cards to {players.Count} players...");

        int cardIndex = 0;
        foreach (Card card in deck)
        {
            PlayerManager player = players[cardIndex % players.Count];
            NetworkObject cardNetObj = card.GetComponent<NetworkObject>();

            if (!cardNetObj.IsSpawned)
            {
                cardNetObj.Spawn(true);
            }

            cardNetObj.ChangeOwnership(player.OwnerClientId);

            player.AddCardToHandClientRpc(cardNetObj.NetworkObjectId);

            cardIndex++;
        }

        Debug.Log($"Dealt all {deck.Count} cards to players!");
    }
    
    public List<PlayerManager> GetAllPlayers()
    {
        return new List<PlayerManager>(connectedPlayers);
    }

}