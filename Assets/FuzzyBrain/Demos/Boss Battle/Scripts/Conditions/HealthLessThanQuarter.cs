using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HealthLessThanQuarter", menuName = "FuzzyBrain/Conditions/HealthLessThanQuarter")]
    public class HealthLessThanQuarter : Condition<CharacterAbilities>
    {
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.health < 25;
            return inverted ? !result : result;
        }
    }
}
