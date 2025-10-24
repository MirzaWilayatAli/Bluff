using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class PlayerManager : NetworkBehaviour
{
    public PlayerName playerNameComponent;
    private GameObject selectedCard;
    private Vector3 grabOffset;
    public List<GameObject> hand = new List<GameObject>();
    private List<GameObject> cardsToPlay = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        // Get or add PlayerName component
        if (playerNameComponent == null)
            playerNameComponent = GetComponent<PlayerName>();
            
        if (playerNameComponent == null)
            playerNameComponent = gameObject.AddComponent<PlayerName>();

        if (IsServer)
        {
            DeckManager.Instance.RegisterPlayer(this);
            TurnManager.Instance.RegisterPlayer(this);
        }

        if (IsOwner)
        {
            GameManager.Instance.playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        // Start checking for name updates
        StartCoroutine(WaitForNameAndNotify());
    }

    private IEnumerator WaitForNameAndNotify()
    {
        // Wait until we have a non-empty name
        while (string.IsNullOrEmpty(playerNameComponent.playerName.Value.ToString()) || 
               playerNameComponent.playerName.Value.ToString() == "Player")
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
        }

        // Once we have the name, notify chat
        if (IsServer)
        {
            string finalName = playerNameComponent.playerName.Value.ToString();
            Chat.Instance?.PlayerJoinedWithNameClientRpc(OwnerClientId, finalName);
        }
    }

    // Helper method to get the player name
    public string GetPlayerName()
    {
        if (playerNameComponent != null && !string.IsNullOrEmpty(playerNameComponent.playerName.Value.ToString()))
        {
            return playerNameComponent.playerName.Value.ToString();
        }
        return $"Player {OwnerClientId}";
    }
    void Update()
    {
        if (!IsOwner) return;
        if (!TurnManager.Instance.IsMyTurn(OwnerClientId)) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null && hand.Contains(hit.collider.gameObject))
            {
                selectedCard = hit.collider.gameObject;
                grabOffset = selectedCard.transform.position - mouseWorld;
                
                // Toggle card selection
                if (cardsToPlay.Contains(selectedCard))
                {
                    cardsToPlay.Remove(selectedCard);
                }
                else
                {
                    // Check if we can select more cards
                    if (cardsToPlay.Count < 5)
                    {
                        cardsToPlay.Add(selectedCard);
                    }
                }
            }
        }

        if (Input.GetMouseButton(0) && selectedCard != null)
        {
            selectedCard.transform.position = mouseWorld + grabOffset;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (selectedCard != null)
            {
                NetworkAudioPlayer.Instance.PlayCardsSoundServerRpc();
            }
            selectedCard = null;
        }
    }

    public void OnPlayButtonClicked()
    {
        if (!IsOwner) return;
        if (!TurnManager.Instance.IsMyTurn(OwnerClientId)) return;

        // Send card IDs to server
        ulong[] cardIds = new ulong[cardsToPlay.Count];
        for (int i = 0; i < cardsToPlay.Count; i++)
        {
            cardIds[i] = cardsToPlay[i].GetComponent<NetworkObject>().NetworkObjectId;
        }
        
        PlayCardsServerRpc(cardIds);
    }

    private void UpdateCardsVisibility()
    {
        foreach (GameObject card in hand)
        {
            if (card == null) continue;
        
            Card cardToUpdate = card.GetComponent<Card>();
            NetworkObject cardNetObj = card.GetComponent<NetworkObject>();
        
            if (cardToUpdate != null && cardNetObj != null)
            {
                // Show front only to the player who owns this card, back to everyone else
                if (cardNetObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                {
                    cardToUpdate.ShowFront();
                }
                else
                {
                    cardToUpdate.ShowBack();
                }
            }
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PlayCardsServerRpc(ulong[] cardIds)
    {
        if (!GameManager.Instance.CheckEnoughCardsWerePlayed(cardIds.Length))
        {
            Chat.Instance.SendMessageToServerFromClient_Rpc("You must play at least 2 cards at the start", playerNameComponent.playerName.Value.ToString());
            return;
        }
        
        foreach (ulong cardId in cardIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out NetworkObject cardObj))
            {
                hand.Remove(cardObj.gameObject);
                Card card = cardObj.GetComponent<Card>();
                PlayingArea.Instance.AddCardToArea(card);
            }
        }
        TurnManager.Instance.AdvanceTurnServerRpc();
        ClearSelectionClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ClearSelectionClientRpc()
    {
        if (IsOwner)
        {
            cardsToPlay.Clear();
        }
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void AddCardToHandClientRpc(ulong cardId)
    {
        StartCoroutine(WaitForCardSpawnAndAdd(cardId));
    }

    private IEnumerator WaitForCardSpawnAndAdd(ulong cardId)
    {
        float timeout = 10f;
        float timer = 0f;
        NetworkObject cardNetObj = null;

        // Instant attempt (covers host / very fast clients)
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out cardNetObj))
        {
            // Debug.Log($"[Player {OwnerClientId}] Card {cardId} already spawned locally.");
        }
        else
        {
            // Wait loop
            while (timer < timeout)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out cardNetObj))
                {
                    Debug.Log($"[Player {OwnerClientId}] Card {cardId} spawned after {timer:F2}s.");
                    break;
                }
                yield return null;
                timer += Time.deltaTime;
            }
        }

        if (cardNetObj == null)
        {
            yield break;
        }

        // double-check
        Card cardComp = cardNetObj.GetComponent<Card>();
        if (cardComp == null)
        {
            yield break;
        }

        GameObject cardGO = cardNetObj.gameObject;
        if (!hand.Contains(cardGO))
        {
            hand.Add(cardGO);
        }
        UpdateCardsVisibility();
    }
}