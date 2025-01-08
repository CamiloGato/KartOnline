using Unity.Netcode.Components;
using UnityEngine;

namespace Tools {
    public enum AuthorityMode {
        Server,
        Client
    }
    
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform {
        public AuthorityMode authorityMode = Tools.AuthorityMode.Client;

        protected override bool OnIsServerAuthoritative() => authorityMode == Tools.AuthorityMode.Server;
    }
}