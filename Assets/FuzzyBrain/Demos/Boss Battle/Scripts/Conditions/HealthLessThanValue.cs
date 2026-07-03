using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HealthLessThanValue", menuName = "FuzzyBrain/Conditions/HealthLessThanValue")]
    public class HealthLessThanValue : Condition<CharacterAbilities>
    {
        [SerializeField] private float _value;
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.health < _value;
            return inverted ? !result : result;
        }
    }
}
