using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyDashAway", menuName = "FuzzyBrain/Acts/EnemyDashAway")]
    public class EnemyDashAway : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var component = ctx.Get<CharacterAbilities>();
            if (component == null) return;
            component.Dash();
        }
    }
}
