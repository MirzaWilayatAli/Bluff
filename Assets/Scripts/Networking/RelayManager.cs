using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine.UI;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button  leaveButton;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject mainPanel;

    private Lobby currentLobby; 
    private float heartbeatTimer;

    private string lobbyName = "Will's Lobby";
    private int maxPlayers = 4;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);
        disconnectButton.onClick.AddListener(OnDisconnect);
        leaveButton.onClick.AddListener(LeaveGame);

        loadingScreenPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    void Update()
    {
        HandleLobbyHeartbeat();
    }
    
    private async void OnHost()
    {
        if (GameManager.Instance.InputFieldValidation())
        {
            StartCoroutine(GameManager.Instance.FlashAnimation());
            return;
        }

        loadingScreenPanel.SetActive(true);

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        joinCodeText.text = "Room Code: " + joinCode;

        var lobbyOptions = new CreateLobbyOptions()
        {
            Data = new Dictionary<string, DataObject>()
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);

        var relayData = AllocationUtils.ToRelayServerData(allocation, "udp");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        NetworkManager.Singleton.StartHost();

        loadingScreenPanel.SetActive(false);
        mainPanel.SetActive(false);
    }
    
    private async void OnJoin()
    {
        if (GameManager.Instance.InputFieldValidation())
        {
            StartCoroutine(GameManager.Instance.FlashAnimation());
            return;
        }

        loadingScreenPanel.SetActive(true);

        QueryLobbiesOptions queryOptions = new QueryLobbiesOptions()
        {
            Count = 1,
            Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.Name,
                    op: QueryFilter.OpOptions.EQ,
                    value: lobbyName)
            }
        };

        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

        if (response.Results.Count == 0)
        {
            loadingScreenPanel.SetActive(false);
            return;
        }

        Lobby foundLobby = response.Results[0];

        string joinCode = foundLobby.Data["joinCode"].Value;

        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

        currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(foundLobby.Id);

        var relayData = AllocationUtils.ToRelayServerData(joinAlloc, "udp");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        NetworkManager.Singleton.StartClient();

        loadingScreenPanel.SetActive(false);
        mainPanel.SetActive(false);
    }

    private async void OnDisconnect()
    {
        if (currentLobby != null)
        {
            if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }

            currentLobby = null;
        }
        NetworkManager.Singleton.Shutdown();

        mainPanel.SetActive(true);
        loadingScreenPanel.SetActive(false);
    }
    
    public async void LeaveGame()
    {
        if (currentLobby != null)
        {
            if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            currentLobby = null;
        }
        
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        mainPanel.SetActive(true);
        loadingScreenPanel.SetActive(false);
    }
    
    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby == null || currentLobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = 15f;
            await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
    }
}
