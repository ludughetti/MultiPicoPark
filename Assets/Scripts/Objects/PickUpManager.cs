using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Objects
{
    public class PickUpManager : NetworkBehaviour
    {
        [SerializeField] private NetworkPrefabRef coinPrefab;
        
        private List<Vector3> _coinSpawnPositions;
        private List<NetworkObject> _spawnedPickups = new();
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_SetSpawnPositions(Vector3[] spawnPositions)
        {
            _coinSpawnPositions = new List<Vector3>(spawnPositions);
            
            SpawnCoins();
        }

        private void SpawnCoins()
        {
            foreach (var pos in _coinSpawnPositions)
            {
                var pickup = Runner.Spawn(coinPrefab, pos, Quaternion.identity);
                _spawnedPickups.Add(pickup);
            }
        }
    }
}
