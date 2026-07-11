using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "IsFacingRight", menuName = "FuzzyBrain/Conditions/IsFacingRight")]
    public class IsFacingRight : Condition<CharacterAbilities>
    {
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.FacingDirection == 1f;
            return inverted ? !result : result;
        }
    }
}
