using FuzzyBrain;

/// <summary>
/// Condition: checks whether the Actor's canAct flag is true.
/// Use this as a gate to prevent acts from firing during a cooldown.
/// </summary>
[UnityEngine.CreateAssetMenu(fileName = "CanAct", menuName = "DynamicBehaviour/Conditions/CanAct")]
public class CanAct : Condition<Actor>
{
    protected override bool Verify(Actor actor)
    {
        bool result = actor.canAct;
        return inverted ? !result : result;
    }
}
