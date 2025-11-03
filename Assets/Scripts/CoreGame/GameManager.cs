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
    
    public TMP_InputField playerNameInputField;
    public float flashDuration = 0.5f;
    public int flashCount = 2;
    private Color originalColor;
    private Vector3 originalScale;

    public GameObject mainPanel;
    public GameObject joinPanel;
    public GameObject settingsPanel;
    public Button joinButton;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        originalColor = playerNameInputField.image.color;
        originalScale = playerNameInputField.transform.localScale;
        revealButton.onClick.AddListener(OnRevealButtonClick);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        callBluffButton.onClick.AddListener(OnBluffButtonClicked);
    }

    public void OnBluffButtonClicked()
    {
        CallBluffServerRpc();
    }

    public void OnRevealButtonClick()
    {
        if (!IsServer)
        {
            return;
        }
        RevealCardsServerRpc();
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
        string bluffCallerName = TurnManager.Instance.GetPlayerName(bluffCallerId);
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();
        string lastPlayerName = TurnManager.Instance.GetPlayerName(lastPlayerId);
    
        // Send bluff call message to chat
        string bluffMessage = $"<b>{bluffCallerName}</b> called BLUFF on <b>{lastPlayerName}</b>!";
        Chat.Instance.SendMessageToClientsFromServer_Rpc(bluffMessage, "Alert");
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void RevealCardsServerRpc()
    {
        if (!IsServer)
        {
            return;
        }
    
        // Get the last player's ID from TurnManager
        ulong lastPlayerId = TurnManager.Instance.GetLastPlayerId();
    
        // Get only the cards played by the last player
        List<Card> lastPlayerCards = PlayingArea.Instance.GetCardsByClient(lastPlayerId);
    
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