using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "DashAct", menuName = "FuzzyBrain/Acts/DashAct")]
    public class DashAct : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null) return;
            abilities.Dash();
        }
    }
}
