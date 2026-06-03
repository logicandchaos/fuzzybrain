using FuzzyBrain;

/// <summary>
/// Condition: checks whether the Actor's isIdle flag is true.
/// isIdle is reset to false at the start of each tick and set to true by any act
/// that calls ctx.Actor.isIdle = true in PerformAct — the built-in IdleAct does this automatically.
/// </summary>
[UnityEngine.CreateAssetMenu(fileName = "IsIdle", menuName = "FuzzyBrain/Conditions/IsIdle")]
public class IdleCondition : Condition<Actor>
{
    protected override bool Verify(Actor actor)
    {
        bool result = actor.isIdle;
        return inverted ? !result : result;
    }
}
