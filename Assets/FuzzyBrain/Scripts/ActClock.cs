using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Per-actor timer that tracks when the current locked act started.
    /// Added automatically by Actor on Awake — do not add this component manually.
    /// Hidden from the Add Component menu.
    /// </summary>
    [AddComponentMenu("")]
    public class ActClock : MonoBehaviour
    {
        private float _startTime;

        /// <summary>Records the start of a new act lock. Call once when an act is first locked.</summary>
        public void RecordStart()
        {
            _startTime = Time.time;
        }

        /// <summary>
        /// Returns true when maxClockTime > 0 and the elapsed time since RecordStart exceeds it.
        /// Always returns false when maxClockTime is 0 (no timeout configured).
        /// </summary>
        public bool HasTimedOut(float maxClockTime)
        {
            return maxClockTime > 0f && Time.time >= _startTime + maxClockTime;
        }
    }
}
