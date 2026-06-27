using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "HorizontalDistanceToTargetLessThan10", menuName = "FuzzyBrain/Conditions/HorizontalDistanceToTargetLessThan10")]
public class HorizontalDistanceToTargetLessThan10 : Condition<EnemyAi>
{
    protected override bool Verify(EnemyAi component)
    {
        bool result = component.HorizontalDistance() < 10f;
        return inverted ? !result : result;
    }
}
