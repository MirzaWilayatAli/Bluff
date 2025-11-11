using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class PlayerManager : NetworkBehaviour
{
    public PlayerName playerNameComponent;
    private GameObject selectedCard;
    private Vector3 grabOffset;
    private Vector3 originalCardPosition;
    public NetworkList<ulong> networkHand = new NetworkList<ulong>();
    public List<GameObject> localHand = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DeckManager.Instance.RegisterPlayer(this);
            networkHand.Clear();
        }

        TurnManager.Instance.RegisterPlayer(this);

        if (IsOwner)
        {
            GameManager.Instance.playButton.onClick.AddListener(OnPlayButtonClicked);
        }

        // Initialize local hand from current network hand
        if (IsClient)
        {
            networkHand.OnListChanged += OnHandChanged;
            _ = RefreshLocalHandAsync(); // Fire and forget async task
        }

        _ = WaitForNameAndNotifyAsync();
    }

    private void OnDestroy()
    {
        if (IsClient)
        {
            networkHand.OnListChanged -= OnHandChanged;
        }
    }

    private async Task WaitForNameAndNotifyAsync()
    {
        while (string.IsNullOrEmpty(playerNameComponent.playerName.Value.ToString()) ||
               playerNameComponent.playerName.Value.ToString() == "Player")
        {
            await Task.Delay(500);
        }

        if (IsServer)
        {
            string finalName = playerNameComponent.playerName.Value.ToString();
            Chat.Instance.PlayerJoinedWithNameClientRpc(OwnerClientId, finalName);
        }
    }

    private async void OnHandChanged(NetworkListEvent<ulong> changeEvent)
    {
        await RefreshLocalHandAsync();
    }

    private async Task RefreshLocalHandAsync()
    {
        await Task.Delay(100); // Small delay to batch multiple changes
        
        localHand.Clear();
        
        foreach (ulong cardId in networkHand)
        {
            GameObject cardObject = await GetCardObjectAsync(cardId);
            if (cardObject != null)
            {
                localHand.Add(cardObject);
            }
        }

        UpdateCardsVisibility();
    }

    private async Task<GameObject> GetCardObjectAsync(ulong cardId)
    {
        float timer = 0f;
        while (timer < 5f)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out NetworkObject cardNetObj))
            {
                return cardNetObj.gameObject;
            }
            await Task.Delay(50);
            timer += 0.05f;
        }
        return null;
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Keep whatever Z was originally set in the scene
        mouseWorld.z = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            if (hit.collider != null && localHand.Contains(hit.collider.gameObject))
            {
                selectedCard = hit.collider.gameObject;
                originalCardPosition = selectedCard.transform.position;
                grabOffset = selectedCard.transform.position - mouseWorld;
            }
        }

        if (Input.GetMouseButton(0) && selectedCard != null)
        {
            Vector3 newPos = mouseWorld + grabOffset;
            newPos.z = selectedCard.transform.position.z; // keep its Inspector z (e.g. –2)
            selectedCard.transform.position = newPos;
        }

        if (Input.GetMouseButtonUp(0) && selectedCard != null)
        {
            NetworkAudioPlayer.Instance.PlayCardsSoundServerRpc();
            selectedCard = null;
        }
    }


    public void OnPlayButtonClicked()
    {
        if (!IsOwner) return;
        if (!TurnManager.Instance.IsMyTurn(OwnerClientId)) return;

        // Get cards that are currently in the playing area position
        List<ulong> cardsInPlayingArea = GetCardsInPlayingAreaPosition();
        PlayCardsServerRpc(cardsInPlayingArea.ToArray());
    }

    private List<ulong> GetCardsInPlayingAreaPosition()
    {
        List<ulong> cardsInArea = new List<ulong>();
        
        foreach (GameObject card in localHand)
        {
            if (PlayingArea.Instance.playingAreaCollider.OverlapPoint(card.transform.position))
            {
                cardsInArea.Add(card.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }
        return cardsInArea;
    }

    private void UpdateCardsVisibility()
    {
        foreach (GameObject card in localHand)
        {
            if (card == null) continue;

            Card cardToUpdate = card.GetComponent<Card>();
            NetworkObject cardNetObj = card.GetComponent<NetworkObject>();

            if (cardNetObj.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                cardToUpdate.ShowFront();
            else
                cardToUpdate.ShowBack();
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PlayCardsServerRpc(ulong[] cardIds)
    {
        if (cardIds == null || cardIds.Length == 0)
        {
            PlayingArea.Instance.AddEmptyTurnEntry(OwnerClientId);
            Chat.Instance.SendMessageToServerFromClient_Rpc($"{playerNameComponent.playerName.Value} skipped their turn.", "<color=red>SYSTEM</color>");
            TurnManager.Instance.AdvanceTurnServerRpc();
            return;
        }
        
        if (!GameManager.Instance.CheckEnoughCardsWerePlayed(cardIds.Length))
        {
            Chat.Instance.SendMessageToServerFromClient_Rpc("You must play at least 2 cards at the start", playerNameComponent.playerName.Value.ToString());
            return;
        }

        foreach (ulong cardId in cardIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardId, out NetworkObject cardObj))
            {
                // Only remove if the card exists in the hand
                if (networkHand.Contains(cardId))
                {
                    networkHand.Remove(cardId);
                }
                Card card = cardObj.GetComponent<Card>();
                PlayingArea.Instance.AddCardToArea(card, OwnerClientId);
            }
        }

        TurnManager.Instance.AdvanceTurnServerRpc();
    }

    // Server-only method to add cards to hand
    public void ServerAddCardToHand(ulong cardId)
    {
        if (IsServer && !networkHand.Contains(cardId))
        {
            networkHand.Add(cardId);
        }
    }

    // Server-only method to remove cards from hand
    public void ServerRemoveCardFromHand(ulong cardId)
    {
        if (IsServer && networkHand.Contains(cardId))
        {
            networkHand.Remove(cardId);
        }
    }
}