using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "TargetAbove", menuName = "FuzzyBrain/Conditions/TargetAbove")]
    public class TargetAbove : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.TargetAbove();
            return inverted ? !result : result;
        }
    }
}
