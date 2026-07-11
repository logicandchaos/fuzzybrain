using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "Critical", menuName = "FuzzyBrain/Conditions/Critical")]
    public class Critical : Condition<CharacterAbilities>
    {
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.health < 25;
            return inverted ? !result : result;
        }
    }
}
