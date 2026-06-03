using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: checks whether the actor's Rigidbody2D linear velocity magnitude
/// is below the specified threshold (i.e. the actor is approximately stationary).
/// </summary>
[CreateAssetMenu(fileName = "IsStill", menuName = "FuzzyBrain/Conditions/IsStill")]
public class IsStill : Condition<Rigidbody2D>
{
    [Tooltip("Speed magnitude below which the actor is considered still.")]
    public float threshold = 1f;

    protected override bool Verify(Rigidbody2D rb)
    {
        bool result = rb.linearVelocity.magnitude < threshold;
        return inverted ? !result : result;
    }
}
