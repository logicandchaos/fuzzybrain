using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    /// <summary>
    /// Condition: passes if the given Input System action was released within the current tick
    /// interval window. Uses InputBuffer to cache the release timestamp so releases are never
    /// missed when the tick interval is greater than zero.
    /// Requires an InputBuffer component on the Actor's GameObject.
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonReleasedThisFrameCondition", menuName = "FuzzyBrain/Conditions/ButtonReleasedThisFrameCondition")]
    public class ButtonReleasedThisFrameCondition : Condition<InputBuffer>
    {
        [Tooltip("The Input System action to check.")]
        [SerializeField] private InputActionReference action;

        [Tooltip("If true, clears the cached release after reading it so it cannot carry over to the next tick.")]
        [SerializeField] private bool consumeOnUse = true;

        protected override bool Verify(InputBuffer buffer)
        {
            if (action?.action == null) return inverted;

            bool result = buffer.WasReleased(action.action);
            if (result && consumeOnUse)
                buffer.ConsumeRelease(action.action);
            return inverted ? !result : result;
        }
    }
}
