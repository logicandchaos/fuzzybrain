using UnityEngine;
using FuzzyBrain;
using UnityEngine.InputSystem;

namespace BossBattleDemo
{
    [CreateAssetMenu(fileName = "DirectionInputCondition", menuName = "FuzzyBrain/Conditions/DirectionInputCondition")]
    public class DirectionInputCondition : Condition<PlayerInput>
    {
        [Tooltip("Name of the 2D axis action in the Input Action Asset.")]
        [SerializeField] private string actionName = "Move";

        [Tooltip("True = read the X axis, False = read the Y axis.")]
        [SerializeField] private bool useXAxis = true;

        [Tooltip("True = axis > threshold, False = axis < threshold.")]
        [SerializeField] private bool greaterThan = true;

        [Tooltip("Comparison threshold. Use 0 to detect any positive or negative input.")]
        [SerializeField] private float threshold;

        protected override bool Verify(PlayerInput component)
        {
            var action = component.actions[actionName];
            if (action == null) return inverted;

            Vector2 value = action.ReadValue<Vector2>();
            float axis = useXAxis ? value.x : value.y;
            bool result = greaterThan ? axis > threshold : axis < threshold;
            return inverted ? !result : result;
        }
    }
}
