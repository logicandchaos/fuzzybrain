using FuzzyBrain;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "RandomMove", menuName = "FuzzyBrain/Acts/RandomMove")]
    public class RandomMove : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            var abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null) return;

            int randomRoll = Random.Range(-1, 2);
            if (randomRoll != 0)
            {
                abilities.MoveHorizontal(randomRoll);
            }
        }
    }
}
