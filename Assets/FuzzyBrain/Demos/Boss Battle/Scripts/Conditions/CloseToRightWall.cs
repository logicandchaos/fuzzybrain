using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "CloseToRightWall", menuName = "FuzzyBrain/Conditions/CloseToRightWall")]
    public class CloseToRightWall : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.PositionX() > 25f;
            return inverted ? !result : result;
        }
    }
}
