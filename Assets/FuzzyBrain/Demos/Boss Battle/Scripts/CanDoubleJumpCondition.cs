using UnityEngine;
using FuzzyBrain;

[CreateAssetMenu(fileName = "CanDoubleJumpCondition", menuName = "FuzzyBrain/Conditions/CanDoubleJumpCondition")]
public class CanDoubleJumpCondition : Condition<CharacterAbilities>
{
    protected override bool Verify(CharacterAbilities component)
    {
        bool result = component.CanDoubleJump == true;
        return inverted ? !result : result;
    }
}
