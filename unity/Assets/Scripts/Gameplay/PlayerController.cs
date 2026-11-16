using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Playable.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 6;
        public float jumpSpeed = 9;
        public float respawnBelowY = -20;
        public int Coins { get; private set; }
        public bool Completed { get; private set; }
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private Vector2 spawn;
        private float move;
        private bool jumpRequested;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            spawn = body.position;
        }
        private void Update()
        {
            move = 0;
            if (Completed) return;
#if ENABLE_INPUT_SYSTEM
            var keys = Keyboard.current;
            if (keys != null)
            {
                move = ((keys.dKey.isPressed || keys.rightArrowKey.isPressed) ? 1 : 0)
                     - ((keys.aKey.isPressed || keys.leftArrowKey.isPressed) ? 1 : 0);
                jumpRequested |= keys.spaceKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            move = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                 - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            jumpRequested |= Input.GetKeyDown(KeyCode.Space);
#endif
        }
        private void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = Completed ? 0 : move * moveSpeed;
            if (!Completed && jumpRequested && IsGrounded()) velocity.y = jumpSpeed;
            jumpRequested = false;
            body.linearVelocity = velocity;
            if (body.position.y < respawnBelowY) Respawn();
        }
        private bool IsGrounded()
        {
            var filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            int count = shape.Cast(Vector2.down, filter, hits, 0.08f);
            for (int i = 0; i < count; i++)
                if (hits[i].normal.y > 0.65f) return true;
            return false;
        }
        public void AddCoin()
        {
            Coins++;
            Debug.Log($"Coins: {Coins}", this);
        }
        public void Respawn()
        {
            if (Completed) return;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            body.position = spawn;
            jumpRequested = false;
        }
        public void CompleteLevel()
        {
            if (Completed) return;
            Completed = true;
            move = 0;
            Debug.Log($"Level complete! Coins collected: {Coins}", this);
        }
    }
}
