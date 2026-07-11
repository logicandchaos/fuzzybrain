using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "TargetLeft", menuName = "FuzzyBrain/Conditions/TargetLeft")]
    public class TargetLeft : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.DirectionToTarget() == -1;
            return inverted ? !result : result;
        }
    }
}
