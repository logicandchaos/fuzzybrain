using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    [CreateAssetMenu(fileName = "ButtonPressedCondition", menuName = "FuzzyBrain/Conditions/ButtonPressedCondition")]
    public class ButtonPressedCondition : Condition<PlayerInput>
    {
        [Tooltip("Name of the button action in the Input Action Asset.")]
        [SerializeField] private string actionName = "Jump";

        protected override bool Verify(PlayerInput component)
        {
            var action = component.actions[actionName];
            if (action == null) return inverted;

            bool result = action.IsPressed();
            return inverted ? !result : result;
        }
    }
}
