using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: checks whether the actor's Collider2D is currently touching any collider on the specified layer.
/// Uses Physics2D.IsTouchingLayers — accurate for contacts already computed by the physics engine.
/// Implements IGizmoDrawable — draws the collider bounds in the Scene view.
/// </summary>
[CreateAssetMenu(fileName = "IsTouchingLayer", menuName = "DynamicBehaviour/Conditions/IsTouchingLayer")]
public class IsTouchingLayer : Condition<Collider2D>, IGizmoDrawable
{
    [Tooltip("Physics layer index to check contact with.")]
    public int layer;

    protected override bool Verify(Collider2D col)
    {
        int mask = 1 << layer;
        bool result = col.IsTouchingLayers(mask);
        return inverted ? !result : result;
    }

    /// <summary>Draws the collider bounds outline. Grey in edit mode, green/red in play mode.</summary>
    public void DrawGizmo(ActContext ctx)
    {
        Collider2D col = ctx.Get<Collider2D>();
        if (col == null) return;

        int mask = 1 << layer;
        bool touching = col.IsTouchingLayers(mask);

        Gizmos.color = Application.isPlaying ? (touching ? Color.green : Color.red) : Color.grey;
        Bounds b = col.bounds;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
