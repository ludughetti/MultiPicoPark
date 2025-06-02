using System;

namespace Network.ServerData
{
    [Serializable]
    public struct MovementSettings
    {
        // Vertical movement
        public float gravity;
        public float maxFallSpeed;
        public float verticalRaycastDistance;
        public float jumpForce;
        
        // Horizontal movement
        public float moveSpeed;
        
        // Layers
        public string groundLayerName;
    }
}
