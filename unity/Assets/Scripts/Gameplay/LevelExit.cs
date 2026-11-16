using UnityEngine;
using UnityEngine.Events;
namespace Playable.Gameplay
{
    public sealed class LevelExit : MonoBehaviour
    {
        public UnityEvent onCompleted = new UnityEvent();
        private bool completed;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (completed || other.attachedRigidbody == null) return;
            var player = other.attachedRigidbody.GetComponent<PlayerController>();
            if (player == null || player.Completed) return;
            completed = true;
            player.CompleteLevel();
            onCompleted.Invoke();
        }
    }
}
