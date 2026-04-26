using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "Rigidbody2DCondition", menuName = "FuzzyBrain/Conditions/Rigidbody2DCondition")]
public class Rigidbody2DCondition : Condition<Rigidbody2D>
{
    protected override bool Verify(Rigidbody2D component)
    {
        bool result = component.angularVelocity == 0f;
        return inverted ? !result : result;
    }
}
