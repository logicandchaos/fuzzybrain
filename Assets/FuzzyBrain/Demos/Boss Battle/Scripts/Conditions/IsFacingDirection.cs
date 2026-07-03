using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "IsFacingDirection", menuName = "FuzzyBrain/Conditions/IsFacingDirection")]
    public class IsFacingDirection : Condition<CharacterAbilities>
    {
        [SerializeField] private int direction;
        protected override bool Verify(CharacterAbilities component)
        {
            bool result = component.FacingDirection == direction;
            return inverted ? !result : result;
        }
    }
}
