using TMPro;
using Unity.Netcode;
using Unity.Collections;

public class PlayerName : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
    
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetPlayerNameServerRpc(GameManager.Instance.playerNameInputField.text);
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void SetPlayerNameServerRpc(string playerNameInput)
    {
        playerName.Value = playerNameInput;
    }
}