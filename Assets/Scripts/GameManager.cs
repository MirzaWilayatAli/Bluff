using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    
    public Button playButton;
    public Button bluffButton;
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        // Setup UI buttons
        playButton.onClick.AddListener(OnPlayButtonClicked);
        bluffButton.onClick.AddListener(OnBluffButtonClicked);
    }
    
    private void OnPlayButtonClicked()
    {
        // Player confirms they want to play selected cards
        // You'll need to track selected cards first
    }
    
    private void OnBluffButtonClicked()
    {
        // Player calls bluff on previous player
        CallBluffServerRpc();
    }
    
    [ServerRpc]
    private void CallBluffServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"Player {rpcParams.Receive.SenderClientId} called BLUFF!");
        // Implement bluff logic here
    }
}