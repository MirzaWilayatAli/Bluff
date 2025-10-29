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
    [SerializeField] TextMeshProUGUI joinCodeText;
    [SerializeField] TMP_InputField joinInput;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        
        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
    }

    void OnJoinButtonClicked()
    {
        if (!string.IsNullOrEmpty(joinInput.text))
        {
            JoinRelay(joinInput.text);
        }
    }

    async void CreateRelay()
    {
        hostButton.interactable = false;
        
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        
        joinCodeText.text = "Code: " + joinCode;

        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));

        NetworkManager.Singleton.StartHost();
    }

    async void JoinRelay(string joinCode)
    {
        joinButton.interactable = false;
        
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "udp"));

        NetworkManager.Singleton.StartClient();
    }
}