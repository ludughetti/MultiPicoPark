using Camera;
using Fusion;
using Network;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(NetworkTransform), typeof(Rigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        // Movement bounds
        private Transform _minBounds;
        private Transform _maxBounds;
        
        // Server settings
        private float _gravity;
        private float _maxFallSpeed;
        private float _moveSpeed;
        private float _jumpForce;
        private float _verticalRaycastDistance;
        
        // Player calculated settings
        private float _playerHalfHeight;
        private Vector2 _velocity;
        private int _groundLayer;
        
        // Network data
        private int _currentScore = 0;
        
        private void Awake()
        {
            // Setup player height
            var playerCollider = GetComponent<BoxCollider2D>();
            _playerHalfHeight = playerCollider.size.y / 2;
        }

        public override void Spawned()
        {
            if (Object.HasInputAuthority)
            {
                NetworkManager.Instance.LocalPlayer = this;
                
                // Setup camera follow if present
                var cameraFollow = UnityEngine.Camera.main?.GetComponent<CameraFollow>();
                cameraFollow?.SetTarget(transform);
            }
            
            // Reset player settings
            _velocity = Vector2.zero;
            
            // Initialize movement bounds
            _minBounds = NetworkManager.Instance.playerMinBounds;
            _maxBounds = NetworkManager.Instance.playerMaxBounds;
            
            // Initialize movement settings from NetworkManager
            var settings = NetworkManager.Instance.movementSettings;
            _gravity = settings.gravity;
            _maxFallSpeed = settings.maxFallSpeed;
            _moveSpeed = settings.moveSpeed;
            _jumpForce = settings.jumpForce;
            _verticalRaycastDistance = settings.verticalRaycastDistance;
            _groundLayer = LayerMask.GetMask(settings.groundLayerName);
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Object.HasInputAuthority)
                NetworkManager.Instance.LocalPlayer = null;
        }

        public override void FixedUpdateNetwork()
        {
            if (!GetInput(out NetworkMoveInputData networkInput))
                return;

            // Calculate movements
            CalculateHorizontalMovement(networkInput);
            CalculateVerticalMovement(networkInput);

            var newPosition = (Vector2)transform.position + _velocity * Runner.DeltaTime;

            // Adjust vertical position after applying gravity if necessary
            var clampedPos = ClampToGroundIfOverlapping();
            if (clampedPos.HasValue)
            {
                newPosition.y = clampedPos.Value.y;
                _velocity.y = 0;
            }
            
            // Clamp movement within bounds
            newPosition.x = Mathf.Clamp(newPosition.x, _minBounds.position.x, _maxBounds.position.x);
            newPosition.y = Mathf.Clamp(newPosition.y, _minBounds.position.y, _maxBounds.position.y);

            // Move manually
            transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        }

        private void CalculateHorizontalMovement(NetworkMoveInputData networkMoveInput)
        {
            var moveDir = GetMoveDirection(networkMoveInput);
            _velocity.x = moveDir.x * _moveSpeed;
        }

        private void CalculateVerticalMovement(NetworkMoveInputData networkMoveInput)
        {
            if (CheckGrounded())
            {
                if (networkMoveInput.IsInputDown(NetworkMoveInputType.Jump))
                    _velocity.y = _jumpForce;
                else if (_velocity.y < 0)
                    _velocity.y = 0;
            }
            else
            {
                _velocity.y -= _gravity * Runner.DeltaTime;
                if (_velocity.y < _maxFallSpeed)
                    _velocity.y = _maxFallSpeed;
            }
        }
        
        private Vector2 GetMoveDirection(NetworkMoveInputData networkMoveInput)
        {
            if (networkMoveInput.IsInputDown(NetworkMoveInputType.MoveLeft))
                return Vector2.left;
            else if (networkMoveInput.IsInputDown(NetworkMoveInputType.MoveRight))
                return Vector2.right;
            
            return Vector2.zero;
        }
        
        // Pre-gravity ground check 
        private bool CheckGrounded()
        {
            var rayOrigin = (Vector2) transform.position + Vector2.down * _playerHalfHeight;
            
            var hit = Physics2D.Raycast(rayOrigin, Vector2.down, _verticalRaycastDistance, _groundLayer);
            return hit.collider != null;
        }

        /*
         * Post-gravity ground check:
         * Returns null if player is not overlapping with the ground after calculating gravity,
         * otherwise returns Vector2 with the clamped Y
         */
        private Vector2? ClampToGroundIfOverlapping()
        {
            var rayOrigin = (Vector2) transform.position + Vector2.down * _playerHalfHeight;
            var rayLength = _verticalRaycastDistance;

            var hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, _groundLayer);
            Debug.DrawRay(rayOrigin, Vector2.down * rayLength, Color.green);
            
            if (hit.collider == null || !(_velocity.y < 0)) return null;
            
            // If raycast hit the ground, and we're still falling, clamp the Y position
            var groundY = hit.point.y;
            var correctedY = groundY + _playerHalfHeight;
            return new Vector2(transform.position.x, correctedY);
        } 
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        public void OnPickUpCollectedRPC()
        {
            _currentScore++;
            Debug.Log($"Player current score is: {_currentScore}");
            //coinCounterUI.UpdateCount(_coinCount);
            //coinPickupSound.Play();
        }
    }
}
