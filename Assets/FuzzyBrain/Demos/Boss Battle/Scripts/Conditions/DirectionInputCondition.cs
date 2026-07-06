using UnityEngine;
using FuzzyBrain;
using UnityEngine.InputSystem;

namespace BossBattleDemo
{
    /// <summary>
    /// Condition: reads a 2D axis action and compares one axis against a threshold.
    /// Because axis values are continuous state (not single-frame events), this condition
    /// reads the current value directly and does not rely on InputBuffer timing.
    /// Requires an InputBuffer component on the Actor's GameObject.
    /// </summary>
    [CreateAssetMenu(fileName = "DirectionInputCondition", menuName = "FuzzyBrain/Conditions/DirectionInputCondition")]
    public class DirectionInputCondition : Condition<InputBuffer>
    {
        [Tooltip("The 2D axis Input System action to read.")]
        [SerializeField] private InputActionReference action;

        [Tooltip("True = read the X axis, False = read the Y axis.")]
        [SerializeField] private bool useXAxis = true;

        [Tooltip("True = axis > threshold, False = axis < threshold.")]
        [SerializeField] private bool greaterThan = true;

        [Tooltip("Comparison threshold. Use 0 to detect any positive or negative input.")]
        [SerializeField] private float threshold;

        protected override bool Verify(InputBuffer buffer)
        {
            if (action?.action == null) return inverted;

            Vector2 value = action.action.ReadValue<Vector2>();
            float axis    = useXAxis ? value.x : value.y;
            bool result   = greaterThan ? axis > threshold : axis < threshold;
            return inverted ? !result : result;
        }
    }
}
