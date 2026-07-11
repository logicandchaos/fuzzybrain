using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "SetSpeed", menuName = "FuzzyBrain/Acts/SetSpeed")]
    public class SetSpeed : Act
    {
        public float speed;
        public override void PerformAct(ActContext ctx)
        {
            CharacterAbilities abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null)
                return;
            abilities.Speed = speed;
        }
    }
}
