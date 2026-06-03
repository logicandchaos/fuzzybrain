using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Marks a serialized field as read-only in the Inspector.
    /// The field is visible for debugging but cannot be edited.
    /// Pair with ReadOnlyDrawer in the Editor assembly.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}
