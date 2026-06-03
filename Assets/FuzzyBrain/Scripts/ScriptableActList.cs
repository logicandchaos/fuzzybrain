using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// ScriptableObject asset holding a prioritised list of Acts for an Actor.
    /// Multiple lists can be authored per actor and swapped at runtime.
    /// Acts are automatically sorted by descending condition count (specificity-first).
    /// Create via Assets > Create > FuzzyBrain > Act List.
    /// </summary>
    [CreateAssetMenu(fileName = "New Act List", menuName = "FuzzyBrain/Act List")]
    public class ScriptableActList : ScriptableObject
    {
        public List<Act> list = new List<Act>();

        private bool _isDirty = true;

        /// <summary>Flags the list for re-sorting on the next SortIfDirty call.</summary>
        public void MarkDirty() => _isDirty = true;

        /// <summary>
        /// Sorts acts by descending condition count if the list has been marked dirty.
        /// Called automatically by Actor.EnableActor and when the list asset changes.
        /// </summary>
        public void SortActs()
        {
            list.Sort((a, b) =>
            {
                int aCount = a != null ? a.conditions.Length : 0;
                int bCount = b != null ? b.conditions.Length : 0;
                return bCount.CompareTo(aCount);
            });
            _isDirty = false;
        }

        /// <summary>Sorts only if MarkDirty has been called since the last sort.</summary>
        public void SortIfDirty()
        {
            if (_isDirty) SortActs();
        }

        /// <summary>
        /// Adds an act to the list and marks it dirty for re-sorting.
        /// Call Actor.Refresh() after all modifications are complete.
        /// </summary>
        /// <param name="act">The act to add. Null entries are ignored.</param>
        public void Add(Act act)
        {
            if (act == null) return;
            list.Add(act);
            MarkDirty();
        }

        /// <summary>
        /// Removes an act from the list and marks it dirty for re-sorting.
        /// Call Actor.Refresh() after all modifications are complete.
        /// </summary>
        /// <param name="act">The act to remove.</param>
        /// <returns>True if the act was found and removed.</returns>
        public bool Remove(Act act)
        {
            bool removed = list.Remove(act);
            if (removed) MarkDirty();
            return removed;
        }

        /// <summary>
        /// Creates a new in-memory ScriptableActList instance with a shallow copy of this list's acts.
        /// Use this to give an Actor its own independent list at runtime without affecting other actors
        /// that share the original asset.
        /// Acts themselves are shared references — they are stateless and safe to share.
        /// </summary>
        /// <returns>A new ScriptableActList instance not tied to any asset on disk.</returns>
        public ScriptableActList Clone()
        {
            var clone = CreateInstance<ScriptableActList>();
            clone.list = new List<Act>(list);
            clone.MarkDirty();
            return clone;
        }

        private void OnValidate() => MarkDirty();
    }
}
