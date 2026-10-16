using System.Collections.Generic;
using System.Threading.Tasks;
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
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startGameButton; // Added for host to start when >= 3 players
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInputField; // Manual code entry field
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject lobbyWaitingPanel; // Panel shown in the lobby waiting room

    private Lobby currentLobby; 
    private float heartbeatTimer;

    private string lobbyName = "BluffGameLobby";
    private int maxPlayers = 4;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);
        disconnectButton.onClick.AddListener(OnDisconnect);
        leaveButton.onClick.AddListener(LeaveGame);
        
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            startGameButton.gameObject.SetActive(false); // Only host sees this when conditions met
        }

        loadingScreenPanel.SetActive(false);
        mainPanel.SetActive(true);
        if (lobbyWaitingPanel != null) lobbyWaitingPanel.SetActive(false);
    }

    void Update()
    {
        HandleLobbyHeartbeat();
        CheckHostStartConditions();
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
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                { "Playing", new DataObject(DataObject.VisibilityOptions.Public, "0") } // 0 = Waiting, 1 = In Progress
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);

        var relayData = AllocationUtils.ToRelayServerData(allocation, "udp");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

        NetworkManager.Singleton.StartHost();

        loadingScreenPanel.SetActive(false);
        mainPanel.SetActive(false);
        if (lobbyWaitingPanel != null) lobbyWaitingPanel.SetActive(true);
        if (startGameButton != null) startGameButton.gameObject.SetActive(true);
    }
    
    private async void OnJoin()
    {
        if (GameManager.Instance.InputFieldValidation())
        {
            StartCoroutine(GameManager.Instance.FlashAnimation());
            return;
        }

        string manualCode = joinCodeInputField != null ? joinCodeInputField.text.Trim().ToUpper() : string.Empty;
        if (string.IsNullOrEmpty(manualCode))
        {
            Debug.LogError("Please enter a valid join code.");
            return;
        }

        loadingScreenPanel.SetActive(true);

        try
        {
            // Join lobby by code instead of querying by name
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(manualCode);

            // Check if game has already started
            if (currentLobby.Data.ContainsKey("Playing") && currentLobby.Data["Playing"].Value == "1")
            {
                Debug.LogError("Game has already started. Cannot join.");
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                currentLobby = null;
                loadingScreenPanel.SetActive(false);
                return;
            }

            string relayJoinCode = currentLobby.Data["joinCode"].Value;
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

            var relayData = AllocationUtils.ToRelayServerData(joinAlloc, "udp");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);

            NetworkManager.Singleton.StartClient();

            loadingScreenPanel.SetActive(false);
            mainPanel.SetActive(false);
            if (lobbyWaitingPanel != null) lobbyWaitingPanel.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
            loadingScreenPanel.SetActive(false);
        }
    }

    private async void OnStartGameClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        // Ensure at least 3 players are connected
        if (TurnManager.Instance.players.Count < 3)
        {
            Debug.LogWarning("Need at least 3 players to start!");
            return;
        }

        // Lock the lobby so no one else can join mid-game
        try
        {
            UpdateLobbyOptions updateOptions = new UpdateLobbyOptions
            {
                IsLocked = true,
                Data = new Dictionary<string, DataObject>
                {
                    { "Playing", new DataObject(DataObject.VisibilityOptions.Public, "1") }
                }
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to lock lobby: {e.Message}");
        }

        if (lobbyWaitingPanel != null) lobbyWaitingPanel.SetActive(false);
        startGameButton.gameObject.SetActive(false);
    }

    private void CheckHostStartConditions()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost && startGameButton != null)
        {
            // Enable start button only if 3 or more players are registered
            startGameButton.interactable = TurnManager.Instance != null && TurnManager.Instance.players.Count >= 3;
        }
    }

    private async void OnDisconnect()
    {
        await CleanUpLobbyAsync();
        NetworkManager.Singleton.Shutdown();

        mainPanel.SetActive(true);
        loadingScreenPanel.SetActive(false);
        if (lobbyWaitingPanel != null) lobbyWaitingPanel.SetActive(false);
    }
    
    public async void LeaveGame()
    {
        await CleanUpLobbyAsync();
        
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        mainPanel.SetActive(true);
        loadingScreenPanel.SetActive(false);
        if (lobbyWaitingPanel != null) lobbyWaitingPanel.SetActive(false);
    }

    private async Task CleanUpLobbyAsync()
    {
        if (currentLobby != null)
        {
            try
            {
                if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error cleaning up lobby: {e.Message}");
            }
            currentLobby = null;
        }
    }
    
    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby == null || currentLobby.HostId != AuthenticationService.Instance.PlayerId)
            return;

        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = 15f;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Heartbeat error: {e.Message}");
            }
        }
    }
}