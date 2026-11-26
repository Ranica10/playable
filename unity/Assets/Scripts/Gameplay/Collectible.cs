using UnityEngine;
using UnityEngine.Events;
namespace Playable.Gameplay
{
    public sealed class Collectible : MonoBehaviour
    {
        public UnityEvent onCollected = new UnityEvent();
        private bool collected;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.attachedRigidbody == null) return;
            var player = other.attachedRigidbody.GetComponent<PlayerController>();
            if (player == null || player.Completed) return;
            collected = true;
            player.AddCollectible();
            onCollected.Invoke();
            Destroy(gameObject);
        }
    }
}
