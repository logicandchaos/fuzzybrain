using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "HorizontalDistanceMoreThanValue", menuName = "FuzzyBrain/Conditions/HorizontalDistanceMoreThanValue")]
    public class HorizontalDistanceMoreThanValue : Condition<EnemyAi>
    {
        [SerializeField] private float _value;
        protected override bool Verify(EnemyAi component)
        {
            // TODO: implement condition logic
            bool result = component.HorizontalDistanceToTarget() > _value;
            return inverted ? !result : result;
        }
    }
}
