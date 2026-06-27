using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "HorizontalDistanceToTargetGreaterThan10", menuName = "FuzzyBrain/Conditions/HorizontalDistanceToTargetGreaterThan10")]
public class HorizontalDistanceToTargetGreaterThan10 : Condition<EnemyAi>
{
    protected override bool Verify(EnemyAi component)
    {
        bool result = component.HorizontalDistance() > 10f;
        return inverted ? !result : result;
    }
}
