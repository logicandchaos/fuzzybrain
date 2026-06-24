using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "PlayerStop", menuName = "FuzzyBrain/Acts/PlayerStop")]
    public class PlayerStop : Act
    {
        public override void PerformAct(ActContext ctx)
        {
            ctx.Get<Rigidbody2D>().linearVelocityX = 0f;
        }
    }
}
