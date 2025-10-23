using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerManager : NetworkBehaviour
{
    public string playerName;
    private GameObject selectedCard;
    private Vector3 grabOffset;
    public List<GameObject> hand = new List<GameObject>();
    private List<GameObject> cardsToPlay = new List<GameObject>();

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
                    // Visual feedback for deselection
                    selectedCard.transform.localScale = Vector3.one;
                }
                else
                {
                    // Check if we can select more cards
                    if (cardsToPlay.Count < 5)
                    {
                        cardsToPlay.Add(selectedCard);
                        // Visual feedback for selection
                        selectedCard.transform.localScale = Vector3.one * 1.1f;
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
            selectedCard = null;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DeckManager.Instance.RegisterPlayer(this);
            TurnManager.Instance.RegisterPlayer(this);
        }
        
        // Register with GameManager for button events
        if (IsOwner)
        {
            GameManager.Instance.playButton.onClick.AddListener(OnPlayButtonClicked);
        }
    }

    public void OnPlayButtonClicked()
    {
        if (!IsOwner) return;
        if (!TurnManager.Instance.IsMyTurn(OwnerClientId)) return;
        
        // Validate card count
        if (!GameManager.Instance.IsValidCardPlayCount(cardsToPlay.Count))
        {
            Debug.LogError("You must play between 2 and 5 cards!");
            return;
        }

        if (cardsToPlay.Count == 0) 
        {
            Debug.LogError("Please select cards to play!");
            return;
        }

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

    private void ClearSelection()
    {
        // Reset visual feedback
        foreach (GameObject card in cardsToPlay)
        {
            if (card != null)
                card.transform.localScale = Vector3.one;
        }
        cardsToPlay.Clear();
    }

    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PlayCardsServerRpc(ulong[] cardIds)
    {
        if (!GameManager.Instance.IsValidCardPlayCount(cardIds.Length))
        {
            Debug.LogWarning($"Invalid card play count: {cardIds.Length}");
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

        GameManager.Instance.PlayerPlayedCardsServerRpc(OwnerClientId, cardIds.Length); 
        TurnManager.Instance.AdvanceTurnServerRpc();
        ClearSelectionClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ClearSelectionClientRpc()
    {
        if (IsOwner)
        {
            ClearSelection();
        }
    }
    
    

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void AddCardToHandClientRpc(ulong cardId)
    {
        // run the coroutine so we wait for spawn on this client
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
            Debug.Log($"[Player {OwnerClientId}] Card {cardId} already spawned locally.");
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
            Debug.LogError($"[Player {OwnerClientId}] Timeout waiting for card {cardId} to spawn (waited {timeout}s).");
            yield break;
        }

        // double-check Card component exists
        Card cardComp = cardNetObj.GetComponent<Card>();
        if (cardComp == null)
        {
            Debug.LogError($"[Player {OwnerClientId}] Spawned object {cardId} missing Card component!");
            yield break;
        }

        GameObject cardGO = cardNetObj.gameObject;
        if (!hand.Contains(cardGO))
        {
            hand.Add(cardGO);
            Debug.Log($"[Player {OwnerClientId}] Added card {cardId} to hand (hand count now {hand.Count}).");
        }
        else
        {
            Debug.Log($"[Player {OwnerClientId}] Card {cardId} already in hand.");
        }

        UpdateCardsVisibility();
    }

}