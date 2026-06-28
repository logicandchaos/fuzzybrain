using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "CloseToLeftWall", menuName = "FuzzyBrain/Conditions/CloseToLeftWall")]
    public class CloseToLeftWall : Condition<EnemyAi>
    {
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.transform.position.x < -20;
            return inverted ? !result : result;
        }
    }
}
