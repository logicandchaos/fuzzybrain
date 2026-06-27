using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "IsMovingDirection", menuName = "FuzzyBrain/Conditions/IsMovingDirection")]
    public class IsMovingDirection : Condition<Rigidbody2D>
    {
        public enum Direction
        {
            Left,
            Right
        }
        public Direction direction;
        protected override bool Verify(Rigidbody2D component)
        {
            bool result = false;
            if (direction == Direction.Left)
            {
                if (component.linearVelocityX < 0)
                    result = true;
            }
            else
            {
                if (component.linearVelocityX > 0)
                    result = true;
            }
            return inverted ? !result : result;
        }
    }
}
