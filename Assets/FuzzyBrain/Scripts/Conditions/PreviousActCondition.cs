using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Condition: passes when the act at the specified history offset in ActHistory
    /// matches the expected act asset. Requires an ActHistory component on the Actor.
    ///
    /// Use this to build combo sequences:
    ///   - historyOffset 0  checks the most recently completed act.
    ///   - historyOffset 1  checks the act before that.
    ///
    /// Example — three-step combo A → B → C:
    ///   Act C has two PreviousActConditions:
    ///     offset 0 → Act B   (most recent)
    ///     offset 1 → Act A   (one before)
    /// </summary>
    [CreateAssetMenu(fileName = "PreviousAct", menuName = "FuzzyBrain/Conditions/PreviousAct")]
    public class PreviousActCondition : Condition<ActHistory>
    {
        [Tooltip("The act that must appear at the specified history offset.")]
        [SerializeField]
        private Act expectedAct;

        [Tooltip("0 = most recently completed act, 1 = one before that, etc.")]
        [SerializeField, Min(0)]
        private int historyOffset = 0;

        /// <summary>
        /// Returns true when ActHistory.Matches(expectedAct, historyOffset) is satisfied.
        /// Applies inverted before returning.
        /// </summary>
        protected override bool Verify(ActHistory history)
        {
            bool match = history.Matches(expectedAct, historyOffset);
            return inverted ? !match : match;
        }
    }
}
