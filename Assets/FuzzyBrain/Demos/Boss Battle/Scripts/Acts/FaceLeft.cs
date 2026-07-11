using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "FaceLeft", menuName = "FuzzyBrain/Acts/FaceLeft")]
    public class FaceLeft : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            CharacterAbilities abilities = ctx.Get<CharacterAbilities>();
            abilities.FacingDirection = -1;
            abilities.Aim(0);
        }
    }
}
