using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "SpeedNotEqualValue", menuName = "FuzzyBrain/Conditions/SpeedNotEqualValue")]
    public class SpeedNotEqualValue : Condition<CharacterAbilities>
    {
        public float value;
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.Speed != value;
            return inverted ? !result : result;
        }
    }
}
