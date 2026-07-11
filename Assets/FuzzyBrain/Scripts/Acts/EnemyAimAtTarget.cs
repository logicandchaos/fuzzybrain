using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyAimAtTarget", menuName = "FuzzyBrain/Acts/EnemyAimAtTarget")]
    public class EnemyAimAtTarget : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var component = ctx.Get<EnemyAi>();
            if (component == null) return;
            component.AimAtTarget();
        }
    }
}
