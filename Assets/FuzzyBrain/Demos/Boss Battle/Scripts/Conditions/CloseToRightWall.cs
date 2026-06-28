using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "CloseToRightWall", menuName = "FuzzyBrain/Conditions/CloseToRightWall")]
    public class CloseToRightWall : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.transform.position.x > 10;
            return inverted ? !result : result;
        }
    }
}
