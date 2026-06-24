using FuzzyBrain;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "PlayerIsStopped", menuName = "FuzzyBrain/Conditions/PlayerIsStopped")]
    public class PlayerIsStopped : Condition<Rigidbody2D>
    {
        protected override bool Verify(Rigidbody2D component)
        {
            bool result = Mathf.Abs(component.linearVelocityX) < 0.0001f;
            return inverted ? !result : result;
        }
    }
}
