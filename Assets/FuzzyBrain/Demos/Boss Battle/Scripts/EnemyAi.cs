using UnityEngine;

public class EnemyAi : MonoBehaviour
{
    public Transform target;

    public enum AimAngle
    {
        Straight = 0,
        DiagonalUp = 45,
        DiagonalDown = -45,
        Up = 90,
    }

    private CharacterAbilities _abilities;

    private void Awake()
    {
        _abilities = GetComponent<CharacterAbilities>();
    }

    // 2D magnitude to Target
    public float DistanceToTarget()
    {
        if (target == null)
            return 0f;
        return Vector2.Distance(transform.position, target.position);
    }

    // Horizontal distance from target
    public float HorizontalDistance()
    {
        if (target == null)
            return 0f;
        return Mathf.Abs(transform.position.x - target.position.x);
    }

    // Vertical distance from target
    public float VerticalDistance()
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
}
