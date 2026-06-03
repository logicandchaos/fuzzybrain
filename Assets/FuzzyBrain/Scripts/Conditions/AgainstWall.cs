using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: checks whether the actor's Collider2D is flush against a wall
/// in the specified horizontal direction by casting a short ray sideways.
/// Implements IGizmoDrawable — select the actor in the Scene view to see the ray.
/// </summary>
[CreateAssetMenu(fileName = "AgainstWall", menuName = "FuzzyBrain/Conditions/AgainstWall")]
public class AgainstWall : Condition<Collider2D>, IGizmoDrawable
{
    public enum XDirection { Left, Right }

    [Tooltip("Direction to cast the wall-check ray.")]
    public XDirection xDirection;

    [Tooltip("Physics layer index to treat as a wall.")]
    public LayerMask layer;

    protected override bool Verify(Collider2D col)
    {
        float rayLength = col.bounds.extents.x + 0.1f;
        Vector2 dir = xDirection == XDirection.Left ? Vector2.left : Vector2.right;
        bool hit = Physics2D.Raycast(col.bounds.center, dir, rayLength, layer);
        return inverted ? !hit : hit;
    }

    /// <summary>Draws the wall-check ray. Grey in edit mode, green/red in play mode.</summary>
    public void DrawGizmo(ActContext ctx)
    {
        Collider2D col = ctx.Get<Collider2D>();
        if (col == null) return;

        float rayLength = col.bounds.extents.x + 0.1f;
        Vector2 dir2D = xDirection == XDirection.Left ? Vector2.left : Vector2.right;
        bool hit = Physics2D.Raycast(col.bounds.center, dir2D, rayLength, layer);

        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;
        Vector3 start = col.bounds.center;
        Vector3 end   = start + new Vector3(dir2D.x, 0f, 0f) * rayLength;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.04f);
    }
}
