using System;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode.Components;
using System.Collections;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DeckManager : NetworkBehaviour
{
    [SerializeField] private List<GameObject> cardPrefabs = new List<GameObject>();
    private readonly List<PlayerManager> connectedPlayers = new List<PlayerManager>();
    public int cardsPerPlayer;

    [Header("Hands Layout")]
    public float cardSpacing = 1f;
    public Vector2[] playerCardPositions = new Vector2[]
    {
        new Vector2(-7f, -2f), // Player 0 Hand Position
        new Vector2(1f, -2f),  // Player 1 Hand Position
        new Vector2(-7f, 0f),  // Player 2 Hand Position
        new Vector2(1f, 0f),   // Player 3 Hand Position
    };

    public Button dealBtn;
    public Button discardButton;
    public static DeckManager Instance { get; private set; }

    private List<GameObject> spawnedCards = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        dealBtn.onClick.AddListener(OnDealButtonClicked);
        discardButton.onClick.AddListener(OnDiscardButtonClick);
    }

    public void RegisterPlayer(PlayerManager player)
    {
        if (!connectedPlayers.Contains(player))
            connectedPlayers.Add(player);
    }

    private void OnDealButtonClicked()
    {
        if (IsServer)
        {
            DealCardsToPlayers(connectedPlayers);
        }
    }

    private void OnDiscardButtonClick()
    {
        if (IsServer)
        {
            ResetDeck();
        }
    }

    private void ShuffleDeck()
    {
        for (int i = 0; i < cardPrefabs.Count; i++)
        {
            var temp = cardPrefabs[i];
            int rand = Random.Range(i, cardPrefabs.Count);
            cardPrefabs[i] = cardPrefabs[rand];
            cardPrefabs[rand] = temp;
        }
    }

    private void DealCardsToPlayers(List<PlayerManager> players)
    {
        if (!IsServer) return;

        ShuffleDeck();
        int cardIndex = 0;

        foreach (PlayerManager player in players)
        {
            ulong clientId = player.OwnerClientId;
            Vector2 basePos = Vector2.zero;
            if (clientId < (ulong)playerCardPositions.Length)
                basePos = playerCardPositions[clientId];

            for (int i = 0; i < cardsPerPlayer; i++)
            {
                if (cardIndex >= cardPrefabs.Count)
                {
                    Debug.LogWarning("Deck ran out of cards!");
                    return;
                }

                GameObject cardPrefab = cardPrefabs[cardIndex++];
                Vector3 spawnPos = new Vector3(basePos.x + (i * cardSpacing), basePos.y, 0);

                // Instantiate from prefab and spawn over the network
                GameObject cardInstance = Instantiate(cardPrefab, spawnPos, Quaternion.identity);
                NetworkObject cardNetObj = cardInstance.GetComponent<NetworkObject>();

                if (cardNetObj == null)
                {
                    Debug.LogError($"{cardPrefab.name} is missing a NetworkObject!");
                    Destroy(cardInstance);
                    continue;
                }

                cardNetObj.SpawnWithOwnership(clientId); // Each player owns their cards
                spawnedCards.Add(cardInstance);

                // Tell player to add to their hand UI
                player.AddCardToHandClientRpc(cardNetObj.NetworkObjectId);
                NetworkAudioPlayer.Instance.PlayDealCardsSoundServerRpc();
            }
        }
    }

    public void ResetDeck()
    {
        if (!IsServer) return;

        foreach (var card in PlayingArea.Instance.cardsPlayedList)
        {
            if (card != null && card.TryGetComponent(out NetworkObject netObj))
            {
                if (netObj.IsSpawned)
                    netObj.Despawn(true);
            }
        }
    }
}
