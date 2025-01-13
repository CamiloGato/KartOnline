using Unity.Netcode;
using UnityEngine;

namespace Tools
{
    public class NetworkHelper : MonoBehaviour
    {
        private static NetworkManager _networkManager;

        private void Start()
        {
            _networkManager = NetworkManager.Singleton;

#if UNITY_SERVER
            _networkManager.StartServer();
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