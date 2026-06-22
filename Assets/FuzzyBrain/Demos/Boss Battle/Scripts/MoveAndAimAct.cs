using FuzzyBrain;
using UnityEngine;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "MoveAndAimAct", menuName = "FuzzyBrain/Acts/MoveAndAimAct")]
    public class MoveAndAimAct : Act
    {
        [Tooltip("Horizontal direction: 1 = right, -1 = left, 0 = stationary.")]
        [SerializeField] private float moveDirection;

        [Tooltip("Aim angle relative to the character's facing direction in degrees.")]
        [SerializeField] private float aimAngle;

        public override void PerformAct(ActContext ctx)
        {
            var abilities = ctx.Get<CharacterAbilities>();
            if (abilities == null) return;

            if (moveDirection != 0f)
                abilities.MoveHorizontal(moveDirection);

            abilities.Aim(aimAngle);
        }
    }
}
