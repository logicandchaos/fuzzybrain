using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyShootAct", menuName = "FuzzyBrain/Acts/EnemyShootAct")]
    public class EnemyShootAct : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var component = ctx.Get<CharacterAbilities>();
            if (component == null) return;
            component.Shoot();
        }
    }
}
