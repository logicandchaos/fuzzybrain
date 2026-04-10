using System;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Non-generic abstract base for all DynamicBehaviour conditions.
    /// Holds the inverted flag and the two abstract members Act.CheckConditions needs
    /// without knowing the generic type parameter T.
    /// Do not subclass this directly — subclass Condition<T> instead.
    /// </summary>
    public abstract class Condition : ScriptableObject
    {
        [Tooltip("When true, the condition must evaluate to false for the act to fire.")]
        public bool inverted;

        /// <summary>The component type this condition requires from the actor's cache.</summary>
        public abstract Type RequiredType { get; }

        /// <summary>
        /// Called by ActContext.Evaluate with the already-resolved component.
        /// Do not call directly. Do not override in user subclasses — override Verify(T) in Condition<T>.
        /// </summary>
        internal abstract bool Verify(Component component);
    }
}
