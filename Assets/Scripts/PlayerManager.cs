using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    private GameObject selectedCard;
    private Vector3 grabOffset;
    public List<GameObject> hand = new List<GameObject>();
    private List<GameObject> selectedCards = new List<GameObject>();
    private bool canPlayCards = false;
    void Update()
    {
        if (!IsOwner) return;
        if (!TurnManager.Instance.IsMyTurn(OwnerClientId))
        {
            canPlayCards = false;
            return;
        }
    
        canPlayCards = true;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null && OwnsCard(hit.collider.gameObject))
            {
                selectedCard = hit.collider.gameObject;
                grabOffset = selectedCard.transform.position - mouseWorld;
            }
        }

        if (Input.GetMouseButton(0) && selectedCard != null)
        {
            Vector3 newPos = mouseWorld + grabOffset;
            newPos.z = selectedCard.transform.position.z;
            selectedCard.transform.position = newPos;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // CHECK IF CARD IS IN PLAY AREA
            if (selectedCard != null && PlayingArea.Instance.IsPointInArea(selectedCard.transform.position))
            {
                TryPlaySelectedCards();
            }
            selectedCard = null; // Reset selection
        }
    }
    private void TryPlaySelectedCards()
    {
        // For now, just play the single dragged card
        if (selectedCard != null)
        {
            PlayCardsServerRpc(new ulong[] { selectedCard.GetComponent<NetworkObject>().NetworkObjectId });
        }
    }

    [ServerRpc]
    private void PlayCardsServerRpc(ulong[] cardIds)
    {
        Debug.Log($"Player {OwnerClientId} played {cardIds.Length} cards");
    
        // Remove from hand
        foreach (ulong cardId in cardIds)
        {
            NetworkObject cardObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardId];
            hand.Remove(cardObj.gameObject);
        }
    
        // Advance turn
        TurnManager.Instance.AdvanceTurnServerRpc();
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Register with DeckManager
            DeckManager.Instance.RegisterPlayer(this);
            TurnManager.Instance.RegisterPlayer(this);
        }
        UpdateCardsVisibility();
    }

    [ClientRpc]
    public void AddCardToHandClientRpc(ulong cardId)
    {
        NetworkObject cardNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardId];
        hand.Add(cardNetObj.gameObject);
        Debug.Log($"Player {OwnerClientId} received card on client. Total: {hand.Count}");
        UpdateCardsVisibility();
    }

    // Simple check if this player owns the card
    public bool OwnsCard(GameObject card)
    {
        return hand.Contains(card);
    }
    private void UpdateCardsVisibility()
    {
        if (!IsOwner) return;
    
        // Get all cards in the scene
        Card[] allCards = FindObjectsByType<Card>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
    
        foreach (Card card in allCards)
        {
            if (hand.Contains(card.gameObject))
            {
                // Show front of cards I own
                card.ShowFront();
            }
            else
            {
                // Show back of other players' cards
                card.ShowBack();
            }
        }
    
        Debug.Log($"Player {OwnerClientId} updated card visibility - can see {hand.Count} cards");
    }
    [ClientRpc]
    public void UpdateAllCardVisibilityClientRpc()
    {
        UpdateCardsVisibility();
    }
}