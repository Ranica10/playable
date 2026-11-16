using UnityEngine;
namespace Playable.Gameplay
{
    public sealed class Hazard : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.attachedRigidbody == null) return;
            other.attachedRigidbody.GetComponent<PlayerController>()?.Respawn();
        }
    }
}
