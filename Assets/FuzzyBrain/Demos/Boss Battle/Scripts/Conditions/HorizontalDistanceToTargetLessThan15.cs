using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HorizontalDistanceToTargetLessThan15", menuName = "FuzzyBrain/Conditions/HorizontalDistanceToTargetLessThan15")]
    public class HorizontalDistanceToTargetLessThan15 : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.HorizontalDistance() < 15f;
            return inverted ? !result : result;
        }
    }
}