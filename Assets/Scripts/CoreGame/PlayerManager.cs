using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public string playerName;
    private GameObject selectedCard;
    private Vector3 grabOffset;
    public List<GameObject> hand = new List<GameObject>();
    [SerializeField] private float cardSpacing = 1.2f;

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
            }
        }

        if (Input.GetMouseButton(0) && selectedCard != null)
        {
            selectedCard.transform.position = mouseWorld + grabOffset;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (selectedCard != null && PlayingArea.Instance.IsPointInArea(selectedCard.transform.position))
            {
                PlayCardsServerRpc(new ulong[] { selectedCard.GetComponent<NetworkObject>().NetworkObjectId });
            }
            else
            {
                // Only rearrange if card wasn't played
                ArrangeMyHand();
            }
            selectedCard = null;
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PlayCardsServerRpc(ulong[] cardIds)
    {
        foreach (ulong cardId in cardIds)
        {
            NetworkObject cardObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardId];
            hand.Remove(cardObj.gameObject);
        }
        TurnManager.Instance.AdvanceTurnServerRpc();
    }
    
    // Each client arranges only their own hand
    public void ArrangeMyHand()
    {
        if (!IsOwner) return; // Only arrange if we own this player
        
        // Local player always uses bottom row (index 0)
        float yPos = -3f;
        float startX = -(hand.Count - 1) * cardSpacing / 2f;
        
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != selectedCard) // Don't rearrange currently dragged card
            {
                Vector3 newPos = new Vector3(startX + i * cardSpacing, yPos, 0f);
                hand[i].transform.position = newPos;
            }
        }
        
        Debug.Log($"Player {OwnerClientId} arranged their own hand with {hand.Count} cards");
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DeckManager.Instance.RegisterPlayer(this);
            TurnManager.Instance.RegisterPlayer(this);
        }
        UpdateCardsVisibility();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void AddCardToHandClientRpc(ulong cardId)
    {
        NetworkObject cardNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardId];
        hand.Add(cardNetObj.gameObject);
        
        if (IsOwner)
        {
            ArrangeMyHand(); // Arrange when we receive new cards
        }
        UpdateCardsVisibility();
    }

    private void UpdateCardsVisibility()
    {
        if (!IsOwner) return;
        
        foreach (Card card in FindObjectsByType<Card>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hand.Contains(card.gameObject))
                card.ShowFront();
            else
                card.ShowBack();
        }
    }
}