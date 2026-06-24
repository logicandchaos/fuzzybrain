using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    [CreateAssetMenu(fileName = "ButtonPressedThisFrameCondition", menuName = "FuzzyBrain/Conditions/ButtonPressedThisFrameCondition")]
    public class ButtonPressedThisFrameCondition : Condition<PlayerInput>
    {
        [Tooltip("Name of the button action in the Input Action Asset.")]
        [SerializeField] private string actionName = "Jump";

        protected override bool Verify(PlayerInput component)
        {
            var action = component.actions[actionName];
            if (action == null) return inverted;

            bool result = action.WasPressedThisFrame();
            return inverted ? !result : result;
        }
    }
}
