using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyAimAndShoot", menuName = "FuzzyBrain/Acts/EnemyAimAndShoot")]
    public class EnemyAimAndShoot : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var component1 = ctx.Get<EnemyAi>();
            if (component1 == null) return;
            component1.AimAtTarget();
            var component2 = ctx.Get<CharacterAbilities>();
            if (component2 == null) return;
            component2.Shoot();
        }
    }
}
