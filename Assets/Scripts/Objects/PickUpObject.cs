using Fusion;
using Network;
using Player;
using UnityEngine;

namespace Objects
{
    public class PickUpObject : NetworkBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log("Object collided with something!");
            if (!Object.HasStateAuthority) return;
            Debug.Log("Object has state authority!");
            if (!collision.TryGetComponent<PlayerController>(out var player)) return;
            Debug.Log("Object picked up by player!");
                
            // Notify network so it replicates data
            NetworkManager.Instance.OnCoinCollected(player);
            
            // Despawn
            Runner.Despawn(Object);
        }
    }
}
