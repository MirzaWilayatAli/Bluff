using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;

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
                    selectedCard.transform.localScale = Vector3.one;
                }
                else
                {
                    cardsToPlay.Add(selectedCard);
                    selectedCard.transform.localScale = Vector3.one * 1.1f;
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

    public void OnPlayButtonClicked()
    {
        if (!IsOwner) return;
        if (!TurnManager.Instance.IsMyTurn(OwnerClientId)) return;
        if (cardsToPlay.Count == 0) return;

        // Send card IDs to server
        ulong[] cardIds = new ulong[cardsToPlay.Count];
        for (int i = 0; i < cardsToPlay.Count; i++)
        {
            cardIds[i] = cardsToPlay[i].GetComponent<NetworkObject>().NetworkObjectId;
        }
        
        PlayCardsServerRpc(cardIds);
        ClearSelection();
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
        foreach (ulong cardId in cardIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out NetworkObject cardObj))
            {
                hand.Remove(cardObj.gameObject);
            }
        }
        
        // Advance turn after playing cards
        TurnManager.Instance.AdvanceTurnServerRpc();
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
        
        UpdateCardsVisibility();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void AddCardToHandClientRpc(ulong cardId)
    {
        NetworkObject cardNetObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardId];
        if (!hand.Contains(cardNetObj.gameObject))
        {
            hand.Add(cardNetObj.gameObject);
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