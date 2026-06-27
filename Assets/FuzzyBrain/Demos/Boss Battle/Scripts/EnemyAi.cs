using UnityEngine;

public class EnemyAi : MonoBehaviour
{
    public Transform target;

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

    // Left or Right direction of target Right=1 Left=-1
    public int DirectionToTarget()
    {
        if (target == null)
            return 0;
        return target.position.x > transform.position.x ? 1 : -1;
    }

    /// <summary>
    /// Returns the angle in degrees from this enemy to the target.
    /// 0° = right, 90° = up, 180°/-180° = left, -90° = down.
    /// </summary>
    public float AngleToTarget()
    {
        if (target == null)
            return 0f;

        Vector2 direction = target.position - transform.position;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
}
