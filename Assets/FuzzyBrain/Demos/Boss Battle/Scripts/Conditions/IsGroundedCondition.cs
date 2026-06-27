using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "IsGroundedCondition", menuName = "FuzzyBrain/Conditions/IsGroundedCondition")]
public class IsGroundedCondition : Condition<CharacterAbilities>
{
    protected override bool Verify(CharacterAbilities component)
    {
        bool result = component.IsGrounded == true;
        return inverted ? !result : result;
    }
}
