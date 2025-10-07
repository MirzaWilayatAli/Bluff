using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    //public static GameManager Instance { get; private set; }

    //[SerializeField] private DeckManager deckManager;
    //[SerializeField] private TurnManager turnManager;
    //[SerializeField] private PileManager pileManager;
    //[SerializeField] private Button startButton; // Drag your UI button here

    //public Sprite[] heartsSprites;
    //public Sprite[] diamondsSprites;
    //public Sprite[] clubsSprites;
    //public Sprite[] spadesSprites;

    //private bool gameStarted = false;

    //private void Awake()
    //{
    //    Instance = this;
    //    CardAssetManager.Initialize(heartsSprites, diamondsSprites, clubsSprites, spadesSprites);
    //}

    //public override void OnNetworkSpawn()
    //{
    //    if (IsServer)
    //    {
    //        Debug.Log("Server ready. Waiting for start button...");
    //        // Add this debug to GameManager OnNetworkSpawn
    //        if (IsServer)
    //        {
    //            Debug.Log("Network Prefabs registered:");
    //            foreach (var prefab in NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs)
    //            {
    //                Debug.Log($"- {prefab.Prefab.name}");
    //            }
    //        }
    //        // Show start button only to host
    //        if (IsHost)
    //        {
    //            startButton.gameObject.SetActive(true);
    //            startButton.onClick.AddListener(StartGame);
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Client connected, waiting for server to start game");
    //    }
    //}

    //// Called by the start button (host only)
    //public void StartGame()
    //{
    //    if (!IsServer) return;
    //    if (gameStarted) return;

    //    StartGameServerRpc();
    //}

    //[ServerRpc]
    //private void StartGameServerRpc()
    //{
    //    gameStarted = true;
    //    Debug.Log("Game starting! Player count: " + NetworkManager.Singleton.ConnectedClients.Count);

    //    // Hide start button on all clients
    //    StartGameClientRpc();

    //    // Start the game
    //    deckManager.SetupDeck();
    //    turnManager.InitTurnOrder();
    //}

    //[ClientRpc]
    //private void StartGameClientRpc()
    //{
    //    startButton.gameObject.SetActive(false);
    //    Debug.Log("Game is starting!");
    //}

    //public void ProcessPlayerTurn(PlayerManager player, List<Card> playedCards)
    //{
    //    if (playedCards.Count < 2)
    //    {
    //        Debug.LogWarning("Invalid move: must play at least 2 cards.");
    //        return;
    //    }

    //    player.RemovePlayedCards(playedCards);
    //    turnManager.AdvanceTurn();
    //    Debug.Log($"Player {player.OwnerClientId} played {playedCards.Count} cards");
    //}

    //public DeckManager DeckManager => deckManager;
    //public TurnManager TurnManager => turnManager;
    //public PileManager PileManager => pileManager;
}