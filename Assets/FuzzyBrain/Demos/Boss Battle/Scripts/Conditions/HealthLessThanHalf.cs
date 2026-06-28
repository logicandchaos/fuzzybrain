using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HealthLessThanHalf", menuName = "FuzzyBrain/Conditions/HealthLessThanHalf")]
    public class HealthLessThanHalf : Condition<CharacterAbilities>
    {
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.health < 50;
            return inverted ? !result : result;
        }
    }
}
