using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Unity.Netcode.Samples
{
    public class BootstrapManager : MonoBehaviour
    {
        [SerializeField] private float buttonWidth = 70f;
        [SerializeField] private float buttonHeight = 40f;

        public UnityTransport transport;

        string ipAddress = "127.0.0.1";
        string playerName = "Player";

        public static BootstrapManager Instance { get; private set; }
        public string PlayerName => playerName;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));

            var networkManager = NetworkManager.Singleton;
            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                GUIStyle myButtonStyle = new GUIStyle(GUI.skin.button);
                myButtonStyle.fontSize = 20;
                myButtonStyle.fixedWidth = buttonWidth;
                myButtonStyle.fixedHeight = buttonHeight;

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 16;

                GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
                textFieldStyle.fontSize = 16;

                // Player Name Input
                GUILayout.Label("Player Name:", labelStyle);
                playerName = GUILayout.TextField(playerName, 20, textFieldStyle);
                GUILayout.Space(15);
                GUILayout.Label("IP Address:", labelStyle);
                ipAddress = GUILayout.TextField(ipAddress, 15, textFieldStyle);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host", myButtonStyle))
                {
                    networkManager.StartHost();
                }
                if (GUILayout.Button("Server", myButtonStyle))
                {
                    networkManager.StartServer();
                }
                if (GUILayout.Button("Client", myButtonStyle))
                {
                    NewIPAddress(ipAddress);
                    networkManager.StartClient();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
            }
            else
            {
                // Show connected status and player name
                GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
                statusStyle.fontSize = 16;

                string status;

                if (networkManager.IsHost)
                {
                    status = "Host";
                }
                else if (networkManager.IsServer)
                {
                    status = "Server";
                }
                else
                {
                    status = "Client";
                }
                
                GUILayout.Label($"Status: {status}", statusStyle);
                GUILayout.Label($"Player Name: {playerName}", statusStyle);
                GUILayout.Label($"Client ID: {networkManager.LocalClientId}", statusStyle);
            }

            GUILayout.EndArea();
        }

        public void NewIPAddress(string _ipAddress)
        {
            transport.SetConnectionData(_ipAddress, 7777);
        }
    }
}