using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HorizontalDistanceLessThanValue", menuName = "FuzzyBrain/Conditions/HorizontalDistanceLessThanValue")]
    public class HorizontalDistanceLessThanValue : Condition<EnemyAi>
    {
        [SerializeField] private float _value;
        protected override bool Verify(EnemyAi component)
        {
            // TODO: implement condition logic
            bool result = component.HorizontalDistanceToTarget() < _value;
            return inverted ? !result : result;
        }
    }
}
