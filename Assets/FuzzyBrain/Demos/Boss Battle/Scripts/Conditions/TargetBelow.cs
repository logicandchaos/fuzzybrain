using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "TargetBelow", menuName = "FuzzyBrain/Conditions/TargetBelow")]
public class TargetBelow : Condition<EnemyAi>
{
    protected override bool Verify(EnemyAi component)
    {        
        bool result = component.TargetBelow();
        return inverted ? !result : result;
    }
}
