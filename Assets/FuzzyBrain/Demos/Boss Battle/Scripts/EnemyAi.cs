using UnityEngine;

namespace BossBattleDemo
{
    public class EnemyAi : MonoBehaviour
    {
        public Transform target;
        public float PositionX() { return transform.position.x; }
        public float PositionY() { return transform.position.y; }

        public enum AimAngle
        {
            Straight = 0,
            DiagonalUp = 45,
            DiagonalDown = -45
        }

        private CharacterAbilities _abilities;

        private void Awake()
        {
            _abilities = GetComponent<CharacterAbilities>();
            _abilities.FacingDirection = -1;
        }

        // 2D magnitude to Target
        public float DistanceToTarget()
        {
            if (target == null)
                return 0f;
            return Vector2.Distance(transform.position, target.position);
        }

        // Horizontal distance from target
        public float HorizontalDistanceToTarget()
        {
            if (target == null)
                return 0f;
            return Mathf.Abs(transform.position.x - target.position.x);
        }

        // Vertical distance from target
        public float VerticalDistanceToTarget()
        {
            if (target == null)
                return 0f;
            return Mathf.Abs(transform.position.y - target.position.y);
        }

        public bool TargetAbove()
        {
            if (target == null)
                return false;
            return target.position.y > transform.position.y;
        }

        public bool TargetBelow()
        {
            if (target == null)
                return false;
            return target.position.y < transform.position.y;
        }

        public int DirectionToTarget()
        {
            if (target == null)
                return 0;
            return target.position.x < transform.position.x ? -1 : 1;
        }

        public void AimAtTarget()
        {
            if (target == null)
                return;
            float angle = 0;
            if (HorizontalDistanceToTarget() > 12)
            {
                angle = (float)AimAngle.Straight;
            }
            else
            {
                if (VerticalDistanceToTarget() > 3)
                    angle = (float)AimAngle.Straight;
                else
                {
                    if (TargetAbove())
                        angle = (float)AimAngle.DiagonalUp;
                    else
                        angle = (float)AimAngle.DiagonalDown;
                }
            }
            _abilities.FacingDirection = DirectionToTarget();
            _abilities.Aim(angle);
        }
    }
}