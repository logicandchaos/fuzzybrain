using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyDoubleJumpAct", menuName = "FuzzyBrain/Acts/EnemyDoubleJumpAct")]
    public class EnemyDoubleJumpAct : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var component = ctx.Get<CharacterAbilities>();
            if (component == null) return;
            component.DoubleJump();
        }
    }
}
