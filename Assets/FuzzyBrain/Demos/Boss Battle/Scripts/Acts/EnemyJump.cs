using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyJump", menuName = "FuzzyBrain/Acts/EnemyJump")]
    public class EnemyJump : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            ctx.Get<CharacterAbilities>().Jump();
        }
    }
}
