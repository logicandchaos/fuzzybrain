using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "JumpAct", menuName = "FuzzyBrain/Acts/JumpAct")]
    public class JumpAct : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null) return;
            abilities.Jump();
        }
    }
}
