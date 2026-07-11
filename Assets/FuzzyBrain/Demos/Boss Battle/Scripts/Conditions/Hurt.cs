using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "Hurt", menuName = "FuzzyBrain/Conditions/Hurt")]
    public class Hurt : Condition<CharacterAbilities>
    {
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.health < 50;
            return inverted ? !result : result;
        }
    }
}
