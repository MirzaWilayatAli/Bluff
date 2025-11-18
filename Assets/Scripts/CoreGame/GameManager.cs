using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public Button playButton;
    public Button revealButton;
    public Button callBluffButton;
    public Button transferTo;
    public Button returnTo;
    public Button yesButton;

    public GameObject mainPanel;
    public GameObject hostPanel;
    public GameObject confirmationMsgPanel;
    public TextMeshProUGUI confirmationMsg;
    
    public TMP_InputField playerNameInputField;
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private int flashCount = 2;
    
    private NetworkVariable<ulong> bluffCallerId = new NetworkVariable<ulong>(ulong.MaxValue);

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
        revealButton.onClick.AddListener(OnRevealButtonClick);
        callBluffButton.onClick.AddListener(OnBluffButtonClicked);
        transferTo.onClick.AddListener(ChangeListenerToTransfer);
        returnTo.onClick.AddListener(ChangeListenerToReturn);
    }
    
    public override void OnNetworkSpawn()
    {
        hostPanel.SetActive(IsHost);
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
            revealButton.interactable = false;
        }
    }

    public void ChangeListenerToTransfer()
    {
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(GiveCardsToBluffCaller);

        // Get names
        string bluffCallerName = TurnManager.Instance.GetPlayerName(bluffCallerId.Value);
        string lastPlayerName = TurnManager.Instance.GetPlayerName(TurnManager.Instance.GetLastPlayerId());

        // Confirmation message for when the bluff caller was wrong (they get all cards)
        confirmationMsg.text = $"Are you sure you want to give all cards to <b>{bluffCallerName}</b>?";
        confirmationMsgPanel.SetActive(true);
    }


    public void ChangeListenerToReturn()
    {
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(GiveCardsToLastPlayer);

        // Get names
        string lastPlayerName = TurnManager.Instance.GetPlayerName(TurnManager.Instance.GetLastPlayerId());

        // Confirmation message for when the last player was bluffing
        confirmationMsg.text = $"Are you sure you want to return the cards to <b>{lastPlayerName}</b>?";
        confirmationMsgPanel.SetActive(true);
    }


    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void CallBluffServerRpc(RpcParams rpcParams = default)
    {
        // Get the player who called bluff (ANY player who clicked the button)
        ulong callerId = rpcParams.Receive.SenderClientId;
        bluffCallerId.Value = callerId;
    
        string bluffCallerName = TurnManager.Instance.GetPlayerName(callerId);
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();
        string lastPlayerName = TurnManager.Instance.GetPlayerName(lastPlayerId);

        // Send bluff call message to chat
        string bluffMessage = $"<b>{bluffCallerName}</b> called BLUFF on <b>{lastPlayerName}</b>!";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(bluffMessage, "<color=red>ALERT</color>");
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
        ulong targetPlayerId = bluffCallerId.Value;
        
        if (targetPlayerId == ulong.MaxValue) return;
        
        TransferCardsToPlayerServerRpc(targetPlayerId);
        
        string message = $"<b>{TurnManager.Instance.GetPlayerName(lastPlayerId)} was not bluffing! All cards go to " +
            $"{TurnManager.Instance.GetPlayerName(targetPlayerId)}</b>";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(message, "<color=red>SYSTEM</color>");
        
        // Reset bluff caller
        bluffCallerId.Value = ulong.MaxValue;
        
        // close the confirmation panel after transferring cards
        confirmationMsgPanel.SetActive(false);
    }

    public void GiveCardsToLastPlayer()
    {
        if (!IsServer) return;
        
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();        
        TransferCardsToPlayerServerRpc(lastPlayerId);
        
        string message = $"<b>{TurnManager.Instance.GetPlayerName(lastPlayerId)} was bluffing!</b>";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(message, "<color=red>SYSTEM</color>");
        
        // Reset bluff caller
        bluffCallerId.Value = ulong.MaxValue;
        
        // close the confirmation panel after returning cards
        confirmationMsgPanel.SetActive(false);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void TransferCardsToPlayerServerRpc(ulong targetPlayerId)
    {
        _ = TransferCardsAsync(targetPlayerId);
    }

    private async Task TransferCardsAsync(ulong targetPlayerId)
{
    PlayerManager targetPlayer = GetPlayerManagerById(targetPlayerId);
    if (targetPlayer == null)
    {
        return;
    }

    // Create a NEW list to avoid modification during iteration
    List<Card> cardsToTransfer = PlayingArea.Instance.cardsPlayedList.ToList();
    
    // Clear the playing area FIRST
    PlayingArea.Instance.cardsPlayedList.Clear();
    PlayingArea.Instance.turnHistory.Clear();

    List<ulong> successfullyTransferred = new List<ulong>();

    foreach (Card card in cardsToTransfer)
    {
        if (card == null || card.NetworkObject == null)
        {
            continue;
        }

        ulong cardId = card.NetworkObjectId;
        
        try
        {
            // Verify the card still exists in the network
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(cardId))
            {
                continue;
            }

            // Remove from any player's hand that might have it
            foreach (PlayerManager player in TurnManager.Instance.players)
            {
                if (player != null && player.networkHand.Contains(cardId))
                {
                    player.ServerRemoveCardFromHand(cardId);
                }
            }

            // Transfer ownership
            card.NetworkObject.ChangeOwnership(targetPlayerId);
            successfullyTransferred.Add(cardId);
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TRANSFER] Error transferring card {cardId}: {e.Message}");
        }
    }

    // Wait for ownership to propagate
    await Task.Delay(100);

    // Add all successfully transferred cards to target player
    foreach (ulong cardId in successfullyTransferred)
    {
        if (!targetPlayer.networkHand.Contains(cardId))
        {
            targetPlayer.ServerAddCardToHand(cardId);
        }
    }
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

    public IEnumerator FlashAnimation()
    {
        for (int i = 0; i < flashCount; i++)
        {
            // Flash red and scale up
            playerNameInputField.image.color = Color.red;
            yield return new WaitForSeconds(flashDuration / 2);

            // Return to normal
            playerNameInputField.image.color = Color.white;
            yield return new WaitForSeconds(flashDuration / 2);
        }
    }
}