using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    /// <summary>
    /// Condition: passes while the given Input System action is currently held (IsPressed).
    /// Use this for held-button behaviours such as "crouch while button is down".
    /// For single-tap detection use ButtonPressedThisFrameCondition instead.
    /// Requires an InputBuffer component on the Actor's GameObject.
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonPressedCondition", menuName = "FuzzyBrain/Conditions/ButtonPressedCondition")]
    public class ButtonPressedCondition : Condition<InputBuffer>
    {
        [Tooltip("The Input System action to check.")]
        [SerializeField] private InputActionReference action;

        protected override bool Verify(InputBuffer buffer)
        {
            if (action?.action == null) return inverted;

            bool result = action.action.IsPressed();
            return inverted ? !result : result;
        }
    }
}
