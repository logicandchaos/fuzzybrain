using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: checks whether the actor's Collider2D is touching the ground layer
/// by casting a short ray downward from the collider's centre.
/// Implements IGizmoDrawable — select the actor in the Scene view to see the ray.
/// </summary>
[CreateAssetMenu(fileName = "IsGrounded", menuName = "DynamicBehaviour/Conditions/IsGrounded")]
public class OnGround : Condition<Collider2D>, IGizmoDrawable
{
    [Tooltip("Physics layer index to treat as ground.")]
    public LayerMask groundLayer;

    protected override bool Verify(Collider2D col)
    {
        float rayLength = col.bounds.extents.y + 0.1f;
        bool grounded = Physics2D.Raycast(col.transform.position, Vector2.down, rayLength, groundLayer);
        return inverted ? !grounded : grounded;
    }

    /// <summary>Draws the ground-check ray. Grey in edit mode, green/red in play mode.</summary>
    public void DrawGizmo(ActContext ctx)
    {
        Collider2D col = ctx.Get<Collider2D>();
        if (col == null) return;

        float rayLength = col.bounds.extents.y + 0.1f;
        bool hit = Physics2D.Raycast(col.transform.position, Vector2.down, rayLength, groundLayer);

        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;
        Vector3 start = col.transform.position;
        Vector3 end   = start + Vector3.down * rayLength;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.04f);
    }
}
