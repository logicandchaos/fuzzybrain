using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "OnRightSide", menuName = "FuzzyBrain/Conditions/OnRightSide")]
    public class OnRightSide : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.transform.position.x > 10;
            return inverted ? !result : result;
        }
    }
}
