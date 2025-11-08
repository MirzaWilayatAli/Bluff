using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Chat : NetworkBehaviour
{
    public TMP_InputField chatInputField;
    public TextMeshProUGUI messageText;

    public static Chat Instance { get; private set; }

    private HashSet<ulong> welcomedClients = new HashSet<ulong>();
    private Dictionary<ulong, string> clientPlayerNames = new Dictionary<ulong, string>();

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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }
        else if (IsClient && !IsHost)
        {
            string playerName = GameManager.Instance.playerNameInputField.text;
            NotifyServerOfJoinServerRpc(playerName);
        }
        else if (IsHost)
        {
            string hostName = GameManager.Instance.playerNameInputField.text;
            PlayerJoinedWithNameClientRpc(NetworkManager.LocalClientId, hostName);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!clientPlayerNames.ContainsKey(clientId))
        {
            clientPlayerNames[clientId] = $"Player {clientId}";
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void NotifyServerOfJoinServerRpc(string playerName, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!clientPlayerNames.ContainsKey(senderId))
            clientPlayerNames[senderId] = playerName;

        // tell everyone this client joined
        PlayerJoinedWithNameClientRpc(senderId, playerName);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientPlayerNames.ContainsKey(clientId))
        {
            string playerName = clientPlayerNames[clientId];
            PlayerLeftClientRpc(clientId, playerName);
            clientPlayerNames.Remove(clientId);
            welcomedClients.Remove(clientId);
        }
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void PlayerJoinedWithNameClientRpc(ulong clientId, string playerName)
    {
        if (!welcomedClients.Contains(clientId))
        {
            AddMessageToChat($"<color=#027148><b>{playerName} joined the game</b></color>");
            welcomedClients.Add(clientId);

            if (IsServer)
                clientPlayerNames[clientId] = playerName;
        }
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void PlayerLeftClientRpc(ulong clientId, string playerName)
    {
        AddMessageToChat($"<color=red><b>{playerName} left the game</b></color>");
    }

    public void SendChatMessage()
    {
        if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text))
            return;

        string message = chatInputField.text.Trim();
        chatInputField.text = "";

        chatInputField.Select();
        chatInputField.ActivateInputField();

        string playerName = GameManager.Instance.playerNameInputField.text;

        if (IsServer)
        {
            SendMessageToClientsFromServer_Rpc(message, playerName);
        }
        else
        {
            SendMessageToServerFromClient_Rpc(message, playerName);
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void SendMessageToServerFromClient_Rpc(string message, string playerName, RpcParams rpcParams = default)
    {
        SendMessageToClientsFromServer_Rpc(message, playerName);
    }

    [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Reliable)]
    public void SendMessageToClientsFromServer_Rpc(string message, string playerName)
    {
        if (message.Contains("You must play at least 2 cards at the start"))
        {
            AddMessageToChat($"<color=#b30000>{playerName}, {message}</color>\n");
        }
        else
        {
            AddMessageToChat($"<b><color=#ff8000>{playerName}</color></b>: {message}");
        }
    }

    private void AddMessageToChat(string message)
    {
        messageText.text += message + "\n";
    }
}
