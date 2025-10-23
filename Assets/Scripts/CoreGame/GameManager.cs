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
    public Button increaseBetButton;
    public Button decreaseBetButton;
    public float bet = 500;
    public TextMeshProUGUI displayBet;
    
    public NetworkVariable<float> pot = new NetworkVariable<float>(0);
    public NetworkVariable<bool> roundInProgress = new NetworkVariable<bool>(false);
    public NetworkVariable<int> playersPlayedThisRound = new NetworkVariable<int>(0);
    
    private List<ulong> playersWhoPlayed = new List<ulong>();
    public TextMeshProUGUI potText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        increaseBetButton.onClick.AddListener(OnIncreaseBetButtonClicked);
        decreaseBetButton.onClick.AddListener(OnDecreaseBetButtonClicked);
        
        // Initialize network variables
        pot.OnValueChanged += OnPotChanged;
        UpdatePotDisplay();
    }

    private void OnPotChanged(float oldValue, float newValue)
    {
        UpdatePotDisplay();
    }

    private void UpdatePotDisplay()
    {
        if (potText != null)
            potText.text = $"Pot: ${pot.Value}";
    }

    public void OnIncreaseBetButtonClicked()
    {
        bet = bet + 500;
        displayBet.text = bet.ToString();
    }

    public void OnDecreaseBetButtonClicked()
    {
        bet = bet - 500;
        if (bet < 0)
        {
            bet = 0;
        }
        displayBet.text = bet.ToString();
    }


    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void AddToPotClientRpc(float amount)
    {
        pot.Value += amount;
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void PlayerPlayedCardsServerRpc(ulong playerId, int cardsPlayed)
    {
        if (!playersWhoPlayed.Contains(playerId))
        {
            playersWhoPlayed.Add(playerId);
            playersPlayedThisRound.Value = playersWhoPlayed.Count;
            
            // Add player's bet to pot
            PlayerManager player = GetPlayerById(playerId);
            if (player != null)
            {
                AddToPotClientRpc(bet);
            }

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
        ulong winnerId = playersWhoPlayed[0];
        AwardPotToWinnerServerRpc(winnerId);
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

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void AwardPotToWinnerServerRpc(ulong winnerId)
    {
        PlayerManager winner = GetPlayerById(winnerId);
        if (winner != null)
        {
            // Award the pot to winner
            // You might want to add a balance field to PlayerManager
            Debug.Log($"Player {winnerId} wins ${pot.Value}!");
            
            // Reset pot
            pot.Value = 0;
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