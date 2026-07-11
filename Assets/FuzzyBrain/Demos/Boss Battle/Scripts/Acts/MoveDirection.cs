using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "MoveDirection", menuName = "FuzzyBrain/Acts/MoveDirection")]
    public class MoveDirection : Act
    {
        public int direction;
        public override void PerformAct(ActContext ctx)
        {
            ctx.Get<CharacterAbilities>().MoveHorizontal(direction);
        }
    }
}
