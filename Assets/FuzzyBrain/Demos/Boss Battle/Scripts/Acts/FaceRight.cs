using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "FaceRight", menuName = "FuzzyBrain/Acts/FaceRight")]
    public class FaceRight : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            CharacterAbilities abilities = ctx.Get<CharacterAbilities>();
            abilities.FacingDirection = 1;
            abilities.Aim(0);
        }
    }
}
