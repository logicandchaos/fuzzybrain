using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "Rigidbody2DCondition1", menuName = "FuzzyBrain/Conditions/Rigidbody2DCondition1")]
public class Rigidbody2DCondition1 : Condition<Rigidbody2D>
{
    protected override bool Verify(Rigidbody2D component)
    {
        bool result = component.rotation == 0f;
        return inverted ? !result : result;
    }
}
