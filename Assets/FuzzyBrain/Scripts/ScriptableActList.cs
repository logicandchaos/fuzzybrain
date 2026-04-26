using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// ScriptableObject asset holding a prioritised list of Acts for an Actor.
    /// Multiple lists can be authored per actor and swapped at runtime.
    /// Acts are automatically sorted by descending condition count (specificity-first).
    /// Create via Assets > Create > DynamicBehaviour > Act List.
    /// </summary>
    [CreateAssetMenu(fileName = "New Act List", menuName = "DynamicBehaviour/Act List")]
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
        public void SortActivities()
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
            if (_isDirty) SortActivities();
        }

        private void OnValidate() => MarkDirty();
    }
}
