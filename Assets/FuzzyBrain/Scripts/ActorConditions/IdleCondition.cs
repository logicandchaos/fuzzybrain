using FuzzyBrain;

/// <summary>
/// Condition: checks whether the Actor's isIdle flag is true.
/// Combine with AddIdleTime() wired to a low-priority act's onFire to drive idle animations.
/// </summary>
[UnityEngine.CreateAssetMenu(fileName = "IsIdle", menuName = "DynamicBehaviour/Conditions/IsIdle")]
public class IdleCondition : Condition<Actor>
{
    protected override bool Verify(Actor actor)
    {
        bool result = actor.isIdle;
        return inverted ? !result : result;
    }
}
