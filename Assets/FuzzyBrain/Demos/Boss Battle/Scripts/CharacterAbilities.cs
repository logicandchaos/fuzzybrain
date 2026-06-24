using UnityEngine;

/// <summary>
/// Shared physical capability component for any character in the Boss Battle demo.
/// Exposes all movement abilities as public methods — Acts call these to perform moves,
/// while exposed stats let each character instance be tuned independently in the Inspector.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CharacterAbilities : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Health in HitPoints")]
    public int health = 100;

    [Header("Run")]
    [Tooltip("Horizontal movement speed in units per second.")]
    [SerializeField] private float moveSpeed = 10f;

    [Header("Jump")]
    [Tooltip("Vertical impulse applied on a grounded jump.")]
    [SerializeField] private float jumpForce = 18f;

    [Tooltip("Vertical impulse applied on a double jump.")]
    [SerializeField] private float doubleJumpForce = 15f;

    [Header("Dash")]
    [Tooltip("Horizontal speed in units per second applied when dashing. Duration is controlled by maxLockTime on the Dash act asset.")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Shoot")]
    [Tooltip("Prefab instantiated when shooting. Must have a Rigidbody2D.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("World-space point from which projectiles are spawned. Falls back to this transform if unassigned.")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Tooltip("Speed of the spawned projectile in units per second.")]
    [SerializeField] private float projectileSpeed = 20f;

    [SerializeField] private Transform gun;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundMask = ~0; // default: everything; assign Ground layer in Inspector


    // ── Public state ─────────────────────────────────────────────────────────────

    /// <summary>True when the character is touching the ground layer.</summary>    
    [field: SerializeField, FuzzyBrain.ReadOnly] public bool IsGrounded { get; private set; }

    /// <summary>True when a double jump is available (reset on landing).</summary>
    [SerializeField, FuzzyBrain.ReadOnly] private bool _canDoubleJump;
    public bool CanDoubleJump => _canDoubleJump;

    /// <summary>Last horizontal direction the character moved in. 1 = right, -1 = left.</summary>
    [field: SerializeField, FuzzyBrain.ReadOnly] public float FacingDirection { get; private set; } = 1f;

    // ── Private state ─────────────────────────────────────────────────────────────

    private Rigidbody2D _rigidbody;

    // ── Unity lifecycle ───────────────────────────────────────────────────────────

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        RefreshGroundState();
    }

    // ── Abilities ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves the character horizontally. Pass a normalised direction: 1 (right), -1 (left), 0 (stop).
    /// </summary>
    public void MoveHorizontal(float direction)
    {
        _rigidbody.linearVelocity = new Vector2(direction * moveSpeed, _rigidbody.linearVelocity.y);

        if (direction != 0f)
            FacingDirection = Mathf.Sign(direction);
    }

    /// <summary>
    /// Applies a vertical impulse when grounded. Has no effect in the air — use DoubleJump for that.
    /// </summary>
    public void Jump()
    {
        if (!IsGrounded) return;

        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, jumpForce);
    }

    /// <summary>
    /// Applies a secondary vertical impulse when airborne. Consumes the double jump charge.
    /// Has no effect if already grounded or the charge has been spent.
    /// </summary>
    public void DoubleJump()
    {
        if (IsGrounded || !_canDoubleJump) return;

        _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, doubleJumpForce);
        _canDoubleJump = false;
    }

    /// <summary>
    /// Applies a horizontal impulse in the facing direction.
    /// Use FuzzyBrain act cooldowns to control how often this can be triggered.
    /// </summary>
    public void Dash()
    {
        _rigidbody.linearVelocity = new Vector2(FacingDirection * dashSpeed, _rigidbody.linearVelocity.y);
        CancelInvoke(nameof(StopDash)); // cancel any previous dash that was interrupted
        Invoke(nameof(StopDash), dashDuration);
    }

    private void StopDash()
    {
        _rigidbody.linearVelocityX = 0f;
    }

    /// <summary>
    /// Aims the gun at an angle relative to the character's current facing direction.
    /// Positive angles rotate upward from the facing axis; negative rotate downward.
    /// </summary>
    public void Aim(float angle)
    {
        if (gun == null) return;
        float worldAngle = FacingDirection > 0f ? angle : 180f - angle;
        gun.rotation = Quaternion.Euler(0f, 0f, worldAngle);
    }

    /// <summary>
    /// Instantiates the projectile prefab at the spawn point and launches it in the direction of gun.
    /// Has no effect if no projectile prefab is assigned. Use FuzzyBrain act cooldowns to control fire rate.
    /// </summary>
    public void Shoot()
    {
        if (projectilePrefab == null) return;

        Transform spawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);

        if (projectile.TryGetComponent(out Rigidbody2D projectileRb))
            projectileRb.linearVelocity = spawnPoint.right * projectileSpeed;
    }

    // ── Private helpers ───────────────────────────────────────────────────────────

    private void RefreshGroundState()
    {
        if (_rigidbody.linearVelocity.y > 0.1f)
        {
            IsGrounded = false;
            return;
        }

        if (!TryGetComponent(out Collider2D col)) return;

        float rayLength = col.bounds.extents.y + 0.1f;
        bool grounded = Physics2D.Raycast(col.bounds.center, Vector2.down, rayLength, _groundMask);

        if (!IsGrounded && grounded)
            _canDoubleJump = true;

        IsGrounded = grounded;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (!TryGetComponent(out Collider2D col)) return;

        float rayLength = col.bounds.extents.y + 0.1f;
        bool hit = Application.isPlaying && IsGrounded;

        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;

        Vector3 start = col.bounds.center;
        Vector3 end = start + Vector3.down * rayLength;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.05f);
    }
}
