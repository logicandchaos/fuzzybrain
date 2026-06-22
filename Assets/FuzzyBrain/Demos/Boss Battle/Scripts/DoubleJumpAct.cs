using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "DoubleJumpAct", menuName = "FuzzyBrain/Acts/DoubleJumpAct")]
    public class DoubleJumpAct : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null) return;
            abilities.DoubleJump();
        }
    }
}
