using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Network.ServerData;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Network
{
    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, INetworkRunnerCallbacks
    {
        // Server settings
        [Header("Server Settings")]
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private Transform[] spawnPositions;
        
        // Input actions
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;
        
        public MovementSettings movementSettings;
        
        private readonly Dictionary<PlayerRef, NetworkObject> _activePlayers = new ();
        private NetworkRunner _networkRunner;
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnNewPlayerJoined;
        public event Action<string> OnJoinedPlayerLeft;
        
        public PlayerController LocalPlayer { get; set; }

        private async void Start()
        {
            bool sessionStarted = await StartGameSession();

            if (!sessionStarted)
                Debug.LogError("Could not start game session!");
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }
        
        private async Task<bool> StartGameSession()
        {
            var networkRunnerObject = new GameObject(typeof(NetworkRunner).Name, typeof(NetworkRunner));

            _networkRunner = networkRunnerObject.GetComponent<NetworkRunner>();
            _networkRunner.AddCallbacks(this);

            var startGameArgs = new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SceneManager = _networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                PlayerCount = spawnPositions.Length
            };

            var startTask = _networkRunner.StartGame(startGameArgs);
            await startTask;

            return startTask.Result.Ok;
        }
        
        private void Shutdown()
        {
            if (_networkRunner)
                _networkRunner.Shutdown();
        }
        
        private void SpawnNewPlayer(NetworkRunner runner, PlayerRef player)
        {
            var spawnPosition = spawnPositions[_activePlayers.Count].position;
            var networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
            _activePlayers[player] = networkPlayerObject;
        }
        
        private void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (!_activePlayers.TryGetValue(player, out var activePlayer)) return;
            
            runner.Despawn(activePlayer);
            _activePlayers.Remove(player);
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            if (_networkRunner.IsClient)
                OnConnected?.Invoke();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (_networkRunner.IsClient)
                Shutdown();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason == ShutdownReason.GameNotFound)
                return;

            if (_networkRunner.IsServer)
                _activePlayers.Clear();

            _networkRunner = null;

            OnDisconnected?.Invoke();
        }
        
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
                SpawnNewPlayer(runner, player);

            OnNewPlayerJoined?.Invoke("Player_" + player.PlayerId);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                DespawnPlayer(runner, player);

                if (_activePlayers.Count == 0)
                    Shutdown();
            }

            OnJoinedPlayerLeft?.Invoke("Player_" + player.PlayerId);
        }
        
        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!LocalPlayer)
                return;

            var networkInput = new NetworkMoveInputData();

            var moveInput = moveAction.action.ReadValue<Vector2>();

            switch (moveInput.x)
            {
                case < 0f:
                    networkInput.AddInput(NetworkMoveInputType.MoveLeft);
                    break;
                case > 0f:
                    networkInput.AddInput(NetworkMoveInputType.MoveRight);
                    break;
            }
            
            var jumpInput = jumpAction.action.ReadValue<float>();
            if (jumpInput > 0f)
                networkInput.AddInput(NetworkMoveInputType.Jump);
            
            input.Set(networkInput);
        }
        
        // Empty required callbacks
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    }
}