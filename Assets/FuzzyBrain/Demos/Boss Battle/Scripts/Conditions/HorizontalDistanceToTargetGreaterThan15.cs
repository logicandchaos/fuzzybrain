using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HorizontalDistanceToTargetGreaterThan15", menuName = "FuzzyBrain/Conditions/HorizontalDistanceToTargetGreaterThan15")]
    public class HorizontalDistanceToTargetGreaterThan15 : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.HorizontalDistance() > 15f;
            return inverted ? !result : result;
        }
    }
}