using UnityEngine;

namespace FuzzyBrain.Demos
{
    /// <summary>
    /// All abilities for the Demo0 wanderer actor.
    /// Wire public methods to Act onFire events via the FuzzyBrain Window.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class WandererNPC : MonoBehaviour
    {
        private const float DefaultMoveSpeed = 3f;
        private const float DefaultJumpForce = 7f;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = DefaultMoveSpeed;
        [SerializeField] private float _jumpForce = DefaultJumpForce;

        [Header("Colours")]
        [Tooltip("Default walking colour.")]
        [SerializeField] private Color _colourA = Color.white;
        [Tooltip("Falling colour.")]
        [SerializeField] private Color _colourB = new Color(0.4f, 0.8f, 1f);
        [Tooltip("Idle colour.")]
        [SerializeField] private Color _colourC = new Color(1f, 0.85f, 0.2f);

        private Rigidbody2D _rb;
        private Renderer    _renderer;
        private float       _direction = 1f;

        private void Awake()
        {
            _rb       = GetComponent<Rigidbody2D>();
            _renderer = GetComponent<Renderer>();
        }

        // ── Movement ─────────────────────────────────────────────────────────

        /// <summary>Move in the current facing direction.</summary>
        public void MoveForward()
        {
            _rb.linearVelocity = new Vector2(_direction * _moveSpeed, _rb.linearVelocity.y);
        }

        /// <summary>Reverse facing direction and move forward.</summary>
        public void FlipDirection()
        {
            _direction *= -1f;
            MoveForward();
        }

        /// <summary>Move right regardless of facing direction.</summary>
        public void MoveRight()
        {
            _rb.linearVelocity = new Vector2(_moveSpeed, _rb.linearVelocity.y);
        }

        /// <summary>Move left regardless of facing direction.</summary>
        public void MoveLeft()
        {
            _rb.linearVelocity = new Vector2(-_moveSpeed, _rb.linearVelocity.y);
        }

        /// <summary>Zero horizontal velocity.</summary>
        public void Stop()
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        /// <summary>Apply an upward impulse.</summary>
        public void Jump()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
        }

        /// <summary>Small push in the facing direction — useful for unsticking.</summary>
        public void Nudge()
        {
            _rb.AddForce(new Vector2(_direction * _moveSpeed, 0f), ForceMode2D.Impulse);
        }

        // ── Colours ──────────────────────────────────────────────────────────

        /// <summary>Set the renderer colour to Colour A (default: white / walking).</summary>
        public void SetColourA() => ApplyColour(_colourA);

        /// <summary>Set the renderer colour to Colour B (default: blue / falling).</summary>
        public void SetColourB() => ApplyColour(_colourB);

        /// <summary>Set the renderer colour to Colour C (default: yellow / idle).</summary>
        public void SetColourC() => ApplyColour(_colourC);

        private void ApplyColour(Color colour)
        {
            if (_renderer != null)
                _renderer.material.color = colour;
        }
    }
}
