using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: checks whether the actor's Rigidbody2D vertical velocity indicates it is falling.
/// Passes when linearVelocity.y is below the threshold (negative = moving downward).
/// </summary>
[CreateAssetMenu(fileName = "IsFalling", menuName = "FuzzyBrain/Conditions/IsFalling")]
public class FallingCondition : Condition<Rigidbody2D>
{
    [Tooltip("Velocity threshold below which the actor is considered to be falling.")]
    public float threshold = -1f;

    protected override bool Verify(Rigidbody2D rb)
    {
        bool result = rb.linearVelocity.y < threshold;
        return inverted ? !result : result;
    }
}
