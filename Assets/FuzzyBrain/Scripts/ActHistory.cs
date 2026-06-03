using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Optional per-actor component that maintains a fixed-depth ring buffer of recently
    /// completed acts. Add this component manually to any Actor that needs combo support.
    ///
    /// Actor calls RecordAct automatically whenever an act unlocks — no additional wiring needed.
    /// Read history via GetAct(offset) or Matches(act, offset) in custom conditions or acts.
    ///
    /// offset 0 = most recently completed act
    /// offset 1 = one before that
    /// ...and so on up to depth - 1
    /// </summary>
    [AddComponentMenu("FuzzyBrain/Act History")]
    public class ActHistory : MonoBehaviour
    {
        [Tooltip("Number of past acts to retain in the ring buffer.")]
        [SerializeField, Min(1)]
        private int depth = 4;

        private Act[] _buffer;
        private int   _head;
        private int   _count;

        [Tooltip("Most recently completed acts (read-only, for Inspector debugging).")]
        [SerializeField, ReadOnly]
        private Act[] _preview = new Act[0];

        private void Awake()
        {
            _buffer  = new Act[depth];
            _preview = new Act[depth];
            _head    = 0;
            _count   = 0;
        }

        /// <summary>
        /// Records a completed act into the ring buffer.
        /// Called automatically by Actor whenever an act unlocks.
        /// </summary>
        public void RecordAct(Act act)
        {
            if (act == null) return;

            _head          = (_head - 1 + depth) % depth;
            _buffer[_head] = act;
            if (_count < depth) _count++;

            RefreshPreview();
        }

        /// <summary>
        /// Returns the act at the given history offset.
        /// offset 0 = most recently completed, 1 = one before that.
        /// Returns null if the offset exceeds available history.
        /// </summary>
        public Act GetAct(int offset = 0)
        {
            if (offset < 0 || offset >= _count) return null;
            return _buffer[(_head + offset) % depth];
        }

        /// <summary>
        /// Returns true if the act at the given history offset matches the specified act asset.
        /// Returns false if the offset exceeds available history.
        /// </summary>
        public bool Matches(Act act, int offset = 0)
        {
            return GetAct(offset) == act;
        }

        private void RefreshPreview()
        {
            for (int i = 0; i < depth; i++)
                _preview[i] = i < _count ? _buffer[(_head + i) % depth] : null;
        }
    }
}
