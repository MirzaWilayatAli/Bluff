using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkAudioPlayer : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dealCardSound;
    [SerializeField] private AudioClip playCardSound;
    public static NetworkAudioPlayer Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void PlayDealCardsSoundServerRpc()
    {
        PlayDealCardsSoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PlayDealCardsSoundClientRpc()
    {
        if (audioSource != null && dealCardSound != null)
        {
            audioSource.PlayOneShot(dealCardSound);
        }
    }
    [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    public void PlayCardsSoundServerRpc()
    {
        PlayCardSoundClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void PlayCardSoundClientRpc()
    {
        if (audioSource != null && dealCardSound != null)
        {
            audioSource.PlayOneShot(playCardSound);
        }
    }
}