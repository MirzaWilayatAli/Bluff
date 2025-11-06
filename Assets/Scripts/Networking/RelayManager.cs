using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] Button disconnectButton;
    [SerializeField] TextMeshProUGUI joinCodeText;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] GameObject loadingScreenPanel;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        hostButton.onClick.AddListener(OnHostButtonClick);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
        
        loadingScreenPanel.SetActive(false);
        
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    void OnHostButtonClick()
    {
        if (!GameManager.Instance.InputFieldValidation())
        {
            CreateRelay();
        }
    }

    void OnJoinButtonClicked()
    {
        if (!string.IsNullOrEmpty(joinInput.text))
        {
            JoinRelay(joinInput.text);
        }
    }

    void OnDisconnectButtonClicked()
    {
        StopClient();
    }

    async void CreateRelay()
    {
        loadingScreenPanel.SetActive(true);
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
        joinCodeText.text = "Room Code: " + joinCode;

        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));

        NetworkManager.Singleton.StartHost();
        loadingScreenPanel.SetActive(false);
    }

    async void JoinRelay(string joinCode)
    {
        loadingScreenPanel.SetActive(true);
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "udp")); 
        NetworkManager.Singleton.StartClient(); 
        loadingScreenPanel.SetActive(false);
    }

    public void StopClient()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            joinButton.interactable = true;
            hostButton.interactable = true;
            loadingScreenPanel.SetActive(false);
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        loadingScreenPanel.SetActive(false);
        
        string playerName = GameManager.Instance.playerNameInputField.text;
        if (Chat.Instance != null)
        {
            Chat.Instance.PlayerJoinedWithNameClientRpc(clientId, playerName);
        }
    }

    private void OnServerStarted()
    {
        loadingScreenPanel.SetActive(false);
        
        string playerName = GameManager.Instance.playerNameInputField.text;
        if (Chat.Instance != null)
        {
            Chat.Instance.PlayerJoinedWithNameClientRpc(NetworkManager.Singleton.LocalClientId, playerName);
        }
    }
}