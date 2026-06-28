using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "OnLeftSide", menuName = "FuzzyBrain/Conditions/OnLeftSide")]
    public class OnLeftSide : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.transform.position.x < -10;
            return inverted ? !result : result;
        }
    }
}
