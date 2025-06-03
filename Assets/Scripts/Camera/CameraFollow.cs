using UnityEngine;

namespace Camera
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Transform minBounds;
        [SerializeField] private Transform maxBounds;
        
        private Transform _target;
        private const float FixedZ = -10f;

        public void SetTarget(Transform followTarget)
        {
            _target = followTarget;
        }

        private void LateUpdate()
        {
            if (!_target) return;

            var nextPosition = Vector3.Lerp(transform.position, _target.position, smoothSpeed * Time.deltaTime);
            nextPosition.x = Mathf.Clamp(nextPosition.x, minBounds.position.x, maxBounds.position.x);
            nextPosition.y = Mathf.Clamp(nextPosition.y, minBounds.position.y, maxBounds.position.y);
            transform.position = new Vector3(nextPosition.x, nextPosition.y, FixedZ);
        }
    }
}
