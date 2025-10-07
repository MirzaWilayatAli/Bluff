using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class DeckManager : NetworkBehaviour
{
    [SerializeField] private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> asideCards = new List<GameObject>();

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
    private void Start()
    {
        // Clear any old entries
        deck.Clear();

        // Find all active GameObjects in the scene (use Scene handle + IncludeInactive if needed)
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            // Only check objects with NetworkObject (so we only pick up cards)
            if (obj.GetComponent<NetworkObject>() != null)
            {
                string name = obj.name;

                // Check if name starts with a valid suit
                if (name.StartsWith("Heart") || name.StartsWith("Club") || name.StartsWith("Diamond") || name.StartsWith("Spade"))
                {
                    deck.Add(obj);
                }
            }
        }

        Debug.Log($"Deck initialized with {deck.Count} cards.");
    }

    private void Update()
    {
        if (IsServer) // only server controls deck
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShuffleDeck();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                // Build a list of test players
                List<GameObject> testPlayers = new List<GameObject>
            {
                GameObject.Find("Player1"),
                GameObject.Find("Player2")
            };

                DealCards(testPlayers);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetDeck();
            }
        }
    }


    public void SetupDeck()
    {
        if (deck == null || deck.Count == 0)
        {
            Debug.LogError("Deck is empty, please assign cards in inspector!");
            return;
        }

        foreach (GameObject obj in deck)
        {
            if (obj.GetComponent<NetworkObject>() == null)
            {
                Debug.LogError("Card is missing NetworkObject component!");
                return;
            }
        }
    }

    // Shuffle the deck using Fisher–Yates algorithm.
    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            GameObject temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }

        Debug.Log("Deck shuffled!");
    }

    // Distribute cards equally to all players.
    public void DealCards(List<GameObject> playerObjects)
    {
        if (!IsServer) return; // Only server should deal cards

        int playerCount = playerObjects.Count;
        if (playerCount == 0)
        {
            Debug.LogError("No players to deal cards to!");
            return;
        }

        int cardIndex = 0;

        // Loop through deck and assign cards round-robin
        foreach (GameObject card in deck)
        {
            // Get current player
            GameObject player = playerObjects[cardIndex % playerCount];

            // Spawn the card on the network
            NetworkObject cardNetObj = card.GetComponent<NetworkObject>();
            if (!cardNetObj.IsSpawned)
            {
                cardNetObj.Spawn(true);
            }

            Vector3 offset = new Vector3((cardIndex / playerCount) * 0.5f, 0, 10);
            card.transform.position = player.transform.position + offset;

            cardIndex++;
        }

        Debug.Log("Cards dealt to " + playerCount + " players!");
    }

    public void ResetDeck()
    {
        
    }
}
