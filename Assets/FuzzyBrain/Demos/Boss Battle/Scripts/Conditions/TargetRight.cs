using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "TargetRight", menuName = "FuzzyBrain/Conditions/TargetRight")]
    public class TargetRight : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.DirectionToTarget() == 1;
            return inverted ? !result : result;
        }
    }
}
