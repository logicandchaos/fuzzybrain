using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "EnemyDashUnder", menuName = "FuzzyBrain/Acts/EnemyDashUnder")]
    public class EnemyDashUnder : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var component = ctx.Get<CharacterAbilities>();
            if (component == null) return;
            component.Dash();
        }
    }
}
