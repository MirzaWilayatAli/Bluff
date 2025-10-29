using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Chat : NetworkBehaviour
{
    [Header("UI References")]
    public TMP_InputField chatInputField;
    public TextMeshProUGUI messageText;

    // Singleton for easy access
    public static Chat Instance { get; private set; }

    // Track which clients we've already welcomed
    private HashSet<ulong> welcomedClients = new HashSet<ulong>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener((text) => SendChatMessage());
        }
    }

    [ClientRpc]
    public void PlayerJoinedWithNameClientRpc(ulong clientId, string playerName)
    {
        if (!welcomedClients.Contains(clientId))
        {
            AddMessageToChat($"{playerName} joined the game");
            welcomedClients.Add(clientId);
        }
    }

    private string GetPlayerName(ulong clientId)
    {
        foreach (var player in FindObjectsByType<PlayerManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (player.OwnerClientId == clientId)
            {
                return player.GetPlayerName();
            }
        }
        return $"Player {clientId}";
    }

    private void AddMessageToChat(string message)
    {
        messageText.text += message + "\n";
    }

    public void SendChatMessage()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text))
            return;

        string message = chatInputField.text.Trim();
        chatInputField.text = "";
        
        chatInputField.Select();
        chatInputField.ActivateInputField();

        string playerName = GameManager.Instance.playerNameInput.text;

        if (IsServer)
        {
            SendMessageToClientsFromServer_Rpc(message, playerName);
        }
        else
        {
            SendMessageToServerFromClient_Rpc(message, playerName);
        }
    }

    // Server to Clients
    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SendMessageToClientsFromServer_Rpc(string message, string playerName)
    {
        if (message.Contains("You must play at least 2 cards at the start"))
        {
            AddMessageToChat($"<color=red><b>{playerName}, {message}</b></color>\n");
        }
        else
        {
            AddMessageToChat($"<b><color=#FFFF00>{playerName}</color></b>: {message}");
        }
    }

    // Client to Server
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SendMessageToServerFromClient_Rpc(string message, string playerName, RpcParams rpcParams = default)
    {
        SendMessageToClientsFromServer_Rpc(message, playerName);
    }
}