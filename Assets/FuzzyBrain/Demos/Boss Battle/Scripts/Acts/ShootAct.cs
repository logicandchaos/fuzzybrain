using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "ShootAct", menuName = "FuzzyBrain/Acts/ShootAct")]
    public class ShootAct : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null) return;
            abilities.Shoot();
        }
    }
}
