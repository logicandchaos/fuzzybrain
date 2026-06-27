using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "TargetAbove", menuName = "FuzzyBrain/Conditions/TargetAbove")]
public class TargetAbove : Condition<EnemyAi>
{
    protected override bool Verify(EnemyAi component)
    {
        bool result = component.VerticalDistance() > 5f;
        return inverted ? !result : result;
    }
}
