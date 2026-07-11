using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "IsFacingLeft", menuName = "FuzzyBrain/Conditions/IsFacingLeft")]
    public class IsFacingLeft : Condition<CharacterAbilities>
    {
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.FacingDirection == -1f;
            return inverted ? !result : result;
        }
    }
}
