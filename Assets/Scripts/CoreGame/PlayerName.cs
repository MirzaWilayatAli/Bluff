using Unity.Netcode;
using Unity.Collections;
using Unity.Netcode.Samples;

public class PlayerName : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>();
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SetPlayerNameServerRpc(BootstrapManager.Instance.PlayerName);
        }
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    private void SetPlayerNameServerRpc(string newName)
    {
        playerName.Value = newName;
    }
}