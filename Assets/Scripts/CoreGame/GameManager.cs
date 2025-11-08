using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public Button playButton;
    public Button revealButton;
    public Button callBluffButton;
    public Button joinButton;
    public Button transferTo;
    public Button returnTo;

    
    public TMP_InputField playerNameInputField;
    public float flashDuration = 0.5f;
    public int flashCount = 2;
    private Color originalColor;
    private Vector3 originalScale;

    public GameObject mainPanel;
    public GameObject joinPanel;
    public GameObject settingsPanel;
    
    
    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        originalColor = playerNameInputField.image.color;
        originalScale = playerNameInputField.transform.localScale;
        revealButton.onClick.AddListener(OnRevealButtonClick);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        callBluffButton.onClick.AddListener(OnBluffButtonClicked);
    
        // Initialize bluff resolution buttons (hidden by default)
        if (transferTo != null) 
        {
            transferTo.gameObject.SetActive(false);
            transferTo.onClick.AddListener(GiveCardsToBluffCaller);
        }
    
        if (returnTo != null) 
        {
            returnTo.gameObject.SetActive(false);
            returnTo.onClick.AddListener(GiveCardsToLastPlayer);
        }
    }

    public void OnBluffButtonClicked()
    {
        CallBluffServerRpc();
    }

    public void OnRevealButtonClick()
    {
        if (IsHost)
        {
            RevealCardsServerRpc();
        }
        else
        {
            // Only disable if not host
            revealButton.interactable = false;
        }
    }

    public void OnJoinButtonClicked()
    {
        if (!InputFieldValidation())
        {
            mainPanel.SetActive(false);
            joinPanel.SetActive(true);
        }
    }
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void CallBluffServerRpc(RpcParams rpcParams = default)
    {
        // Get the player who called bluff (ANY player who clicked the button)
        ulong bluffCallerId = rpcParams.Receive.SenderClientId;
        SetBluffCaller(bluffCallerId); // Track the bluff caller
    
        string bluffCallerName = TurnManager.Instance.GetPlayerName(bluffCallerId);
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();
        string lastPlayerName = TurnManager.Instance.GetPlayerName(lastPlayerId);

        // Send bluff call message to chat
        string bluffMessage = $"<b>{bluffCallerName}</b> called BLUFF on <b>{lastPlayerName}</b>!";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(bluffMessage, "<color=red>Alert</color>");
    
        // Enable host buttons for resolution
        EnableBluffResolutionButtonsClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void EnableBluffResolutionButtonsClientRpc()
    {
        // Only enable for host
        if (IsHost)
        {
            // Enable your transferTo and returnTo buttons
            if (transferTo != null) transferTo.gameObject.SetActive(true);
            if (returnTo != null) returnTo.gameObject.SetActive(true);
        
            // Add listeners if not already added
            transferTo.onClick.RemoveAllListeners();
            returnTo.onClick.RemoveAllListeners();
        
            transferTo.onClick.AddListener(GiveCardsToBluffCaller);
            returnTo.onClick.AddListener(GiveCardsToLastPlayer);
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void RevealCardsServerRpc()
    {
        // Get the last player's ID from TurnManager
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();

        // Get only the cards played by the last player in their last turn
        List<Card> lastPlayerCards = PlayingArea.Instance.GetLastTurnCardsForClient(lastPlayerId);

        // Convert to array of NetworkObject IDs
        ulong[] cardIds = new ulong[lastPlayerCards.Count];
        for (int i = 0; i < lastPlayerCards.Count; i++)
        {
            cardIds[i] = lastPlayerCards[i].NetworkObjectId;
        }

        RevealCardsOnAllClientsRpc(cardIds);
    }


    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void RevealCardsOnAllClientsRpc(ulong[] cardIds)
    {
        foreach (ulong cardObjId in cardIds)
        {
            NetworkObject netObj;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cardObjId, out netObj))
            {
                Card card = netObj.GetComponent<Card>();
                if (card != null)
                    card.ShowFront();
            }
        }
    }

    public bool CheckEnoughCardsWerePlayed(int count)
    {
        if (PlayingArea.Instance.cardsPlayedList.Count == 0)
        {
            return count >= 2;
        }
        else
        {
            return true;
        }
    }

    public void GiveCardsToBluffCaller()
    {
        if (!IsServer) return;
        
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();
        ulong bluffCallerId = GetBluffCallerId();
        
        TransferCardsToPlayerServerRpc(bluffCallerId);
        
        string lastPlayerName = TurnManager.Instance.GetPlayerName(lastPlayerId);
        string bluffCallerName = TurnManager.Instance.GetPlayerName(bluffCallerId);
        
        string message = $"<b>{lastPlayerName} was not bluffing! All cards go to {bluffCallerName}</b>";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(message, "<color=red>System</color>");
    }

    public void GiveCardsToLastPlayer()
    {
        if (!IsServer) return;
        
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();
        ulong bluffCallerId = GetBluffCallerId();
        
        TransferCardsToPlayerServerRpc(lastPlayerId);
        
        string lastPlayerName = TurnManager.Instance.GetPlayerName(lastPlayerId);
        string bluffCallerName = TurnManager.Instance.GetPlayerName(bluffCallerId);
        
        string message = $"<b>{lastPlayerName} was bluffing!</b>";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(message, "<color=red>System</color>");
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void TransferCardsToPlayerServerRpc(ulong targetPlayerId)
    {
        // Get all cards currently in playing area
        List<Card> allCards = new List<Card>(PlayingArea.Instance.cardsPlayedList);
        
        foreach (Card card in allCards)
        {
            if (card != null)
            {
                // Transfer ownership to target player
                card.NetworkObject.ChangeOwnership(targetPlayerId);
                
                // Add to target player's hand
                PlayerManager targetPlayer = GetPlayerManagerById(targetPlayerId);
                if (targetPlayer != null)
                {
                    PlayingArea.Instance.cardsPlayedList.Remove(card);
                    targetPlayer.AddCardToHandClientRpc(card.NetworkObjectId);
                }
            }
        }
        
        // Clear turn history after transfer
        ClearPlayingAreaClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    private void ClearPlayingAreaClientRpc()
    {
        PlayingArea.Instance.cardsPlayedList.Clear();
    }

    private PlayerManager GetPlayerManagerById(ulong clientId)
    {
        foreach (PlayerManager player in TurnManager.Instance.players)
        {
            if (player.OwnerClientId == clientId)
            {
                return player;
            }
        }
        return null;
    }

    private ulong bluffCallerId;
    public void SetBluffCaller(ulong callerId)
    {
        bluffCallerId = callerId;
    }

    private ulong GetBluffCallerId()
    {
        return bluffCallerId;
    }
    public bool InputFieldValidation()
    {
        if (string.IsNullOrEmpty(playerNameInputField.text))
        {
            StartCoroutine(FlashAnimation());
            return true;
        }
        mainPanel.SetActive(false);
        return false;
    }
    private IEnumerator FlashAnimation()
    {
        for (int i = 0; i < flashCount; i++)
        {
            // Flash red and scale up
            playerNameInputField.image.color = Color.red;
            playerNameInputField.transform.localScale = originalScale * 1.2f;
            yield return new WaitForSeconds(flashDuration / 2);

            // Return to normal
            playerNameInputField.image.color = originalColor;
            playerNameInputField.transform.localScale = originalScale;
            yield return new WaitForSeconds(flashDuration / 2);
        }
    }
}