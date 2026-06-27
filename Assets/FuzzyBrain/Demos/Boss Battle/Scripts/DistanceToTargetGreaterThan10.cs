using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "DistanceToTargetGreaterThan10", menuName = "FuzzyBrain/Conditions/DistanceToTargetGreaterThan10")]
public class DistanceToTargetGreaterThan10 : Condition<EnemyAi>
{
    protected override bool Verify(EnemyAi component)
    {
        bool result = component.DistanceToTarget() > 10f;
        return inverted ? !result : result;
    }
}
