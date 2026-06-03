using UnityEngine;
using FuzzyBrain;

/// <summary>
/// Condition: performs a random roll each evaluation. Passes when the roll lands on 1.
/// Useful for adding non-determinism to act selection (e.g. random idle animations).
/// Note: uses Actor as the required type because it is always in the component cache,
/// but does not access any Actor state directly.
/// </summary>
[CreateAssetMenu(fileName = "RandomRoll", menuName = "FuzzyBrain/Conditions/RandomRoll")]
public class RandomRollCondition : Condition<Actor>
{
    [Tooltip("1-in-outOf chance of passing each evaluation. E.g. outOf = 5 → 20% chance.")]
    public int outOf = 5;

    protected override bool Verify(Actor actor)
    {
        bool result = Random.Range(1, outOf + 1) == 1;
        return inverted ? !result : result;
    }
}
