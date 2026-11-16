using UnityEngine;

namespace Playable.Gameplay
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 1.5f, -10);
        public float smoothTime = 0.15f;

        private Vector3 velocity;

        private void Start()
        {
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null) return;

            transform.position = target.position + offset;
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0) return;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                target.position + offset,
                ref velocity,
                smoothTime
            );
        }
    }
}