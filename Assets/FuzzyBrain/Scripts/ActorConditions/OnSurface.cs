using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: checks whether the actor's Collider2D is touching any surface
/// that is NOT in its own collision layer (i.e. any external surface).
/// Uses an inverted layer mask — useful as a general "touching anything" check.
/// Implements IGizmoDrawable — draws the downward surface-check ray.
/// </summary>
[CreateAssetMenu(fileName = "OnSurface", menuName = "DynamicBehaviour/Conditions/OnSurface")]
public class OnSurface : Condition<Collider2D>, IGizmoDrawable
{
    [Tooltip("Physics layer index of the actor itself. The check will exclude this layer.")]
    public int actorLayer;

    protected override bool Verify(Collider2D col)
    {
        int mask = ~(1 << actorLayer);
        float rayLength = col.bounds.extents.y + 0.1f;
        bool hit = Physics2D.Raycast(col.transform.position, Vector2.down, rayLength, mask);
        return inverted ? !hit : hit;
    }

    /// <summary>Draws the surface-check ray. Grey in edit mode, green/red in play mode.</summary>
    public void DrawGizmo(ActContext ctx)
    {
        Collider2D col = ctx.Get<Collider2D>();
        if (col == null) return;

        int mask = ~(1 << actorLayer);
        float rayLength = col.bounds.extents.y + 0.1f;
        bool hit = Physics2D.Raycast(col.transform.position, Vector2.down, rayLength, mask);

        Gizmos.color = Application.isPlaying ? (hit ? Color.green : Color.red) : Color.grey;
        Vector3 start = col.transform.position;
        Vector3 end   = start + Vector3.down * rayLength;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.04f);
    }
}
