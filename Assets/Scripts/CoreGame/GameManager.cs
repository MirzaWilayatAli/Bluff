using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public Button playButton;
    public Button revealButton;
    
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
    private void RevealCardsServerRpc()
    {
        if (!IsServer)
        {
            return;
        }
        // Get card IDs
        ulong[] cardIds = new ulong[PlayingArea.Instance.cardsPlayedList.Count];
        for (int i = 0; i < PlayingArea.Instance.cardsPlayedList.Count; i++)
        {
            cardIds[i] = PlayingArea.Instance.cardsPlayedList[i].NetworkObjectId;
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