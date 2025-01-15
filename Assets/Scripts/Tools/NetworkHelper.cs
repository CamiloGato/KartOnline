using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Tools
{
    public class NetworkHelper : MonoBehaviour
    {
        private static NetworkManager _networkManager;

        private UnityTransport _transport; 
            
        private void Start()
        {
            _networkManager = NetworkManager.Singleton;
            _transport = (UnityTransport) _networkManager.NetworkConfig.NetworkTransport;
            
#if UNITY_SERVER
            _networkManager.StartServer();
            string address = _transport.ConnectionData.Address;
            string port = _transport.ConnectionData.Port.ToString();
            print($"Server started on: {address}:{port}");
#endif
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!_networkManager.IsClient && !_networkManager.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            GUILayout.EndArea();
        }

        static void StartButtons()
        {
            if (GUILayout.Button("Host")) _networkManager.StartHost();
            if (GUILayout.Button("Client")) _networkManager.StartClient();
            if (GUILayout.Button("Server")) _networkManager.StartServer();
        }

        static void StatusLabels()
        {
            string mode = _networkManager.IsHost ?
                "Host" : _networkManager.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                            _networkManager.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);
        }
        
    }
}