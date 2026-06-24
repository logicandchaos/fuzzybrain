using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    [CreateAssetMenu(fileName = "ButtonReleasedThisFrameCondition", menuName = "FuzzyBrain/Conditions/ButtonReleasedThisFrameCondition")]
    public class ButtonReleasedThisFrameCondition : Condition<PlayerInput>
    {
        [Tooltip("Name of the button action in the Input Action Asset.")]
        [SerializeField] private string actionName = "Jump";

        protected override bool Verify(PlayerInput component)
        {
            var action = component.actions[actionName];
            if (action == null) return inverted;

            bool result = action.WasReleasedThisFrame();
            return inverted ? !result : result;
        }
    }
}
