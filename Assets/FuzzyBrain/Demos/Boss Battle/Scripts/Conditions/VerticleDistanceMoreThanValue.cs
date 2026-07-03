using UnityEngine;
using FuzzyBrain;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "VerticleDistanceMoreThanValue", menuName = "FuzzyBrain/Conditions/VerticleDistanceMoreThanValue")]
    public class VerticleDistanceMoreThanValue : Condition<EnemyAi>
    {
        [SerializeField] private float _value;
        protected override bool Verify(EnemyAi component)
        {
            bool result = component.VerticalDistance() > _value;
            return inverted ? !result : result;
        }
    }
}
