using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "JustUsed", menuName = "FuzzyBrain/Conditions/JustUsed")]
    public class JustUsed : Condition<ActHistory>
    {
        public Act act;
        protected override bool Verify(ActHistory component)
        {
            bool result = component.Matches(act, 0);
            return inverted ? !result : result;
        }
    }
}
