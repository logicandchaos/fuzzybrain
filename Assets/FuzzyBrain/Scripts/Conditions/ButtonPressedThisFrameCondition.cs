using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    /// <summary>
    /// Condition: passes if the given Input System action was pressed within the current tick
    /// interval window. Uses InputBuffer to cache the press timestamp so presses are never
    /// missed when the tick interval is greater than zero.
    /// Requires an InputBuffer component on the Actor's GameObject.
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonPressedThisFrameCondition", menuName = "FuzzyBrain/Conditions/ButtonPressedThisFrameCondition")]
    public class ButtonPressedThisFrameCondition : Condition<InputBuffer>
    {
        [Tooltip("The Input System action to check.")]
        [SerializeField] private InputActionReference action;

        [Tooltip("If true, clears the cached press after reading it so it cannot carry over to the next tick.")]
        [SerializeField] private bool consumeOnUse = true;

        protected override bool Verify(InputBuffer buffer)
        {
            if (action?.action == null) return inverted;

            bool result = buffer.WasPressed(action.action);
            if (result && consumeOnUse)
                buffer.ConsumePress(action.action);
            return inverted ? !result : result;
        }
    }
}
