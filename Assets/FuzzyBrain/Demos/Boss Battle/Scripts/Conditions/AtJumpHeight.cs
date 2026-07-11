using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "AtJumpHeight", menuName = "FuzzyBrain/Conditions/AtJumpHeight")]
    public class AtJumpHeight : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.PositionY() > 6f;
            return inverted ? !result : result;
        }
    }
}
