using Fusion;
using Network;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(NetworkTransform), typeof(Rigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        public float moveSpeed = 5f;
        public float jumpForce = 7f;

        private bool _isMoving;
        private Rigidbody2D _rb;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public override void Spawned()
        {
            if (!Object.HasInputAuthority) return;

            NetworkManager.Instance.LocalPlayer = this;
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (!Object.HasInputAuthority) return;

            NetworkManager.Instance.LocalPlayer = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out NetworkInputData networkInput))
                return;
            
            var moveDir = GetMoveDirection(networkInput);

            var newPosition = _rb.position + moveDir * moveSpeed * Runner.DeltaTime;
            transform.position = new Vector2(newPosition.x, newPosition.y);
        }
        
        private Vector2 GetMoveDirection(NetworkInputData networkInput)
        {
            if (networkInput.IsInputDown(NetworkMoveInputType.MoveLeft))
                return Vector2.left;
            else if (networkInput.IsInputDown(NetworkMoveInputType.MoveRight))
                return Vector2.right;
            
            return Vector2.zero;
        }
    }
}
