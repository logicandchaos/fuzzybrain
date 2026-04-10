using System;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Generic abstract base for user-defined condition types.
    /// Subclass this once per condition type, specifying the component T it operates on.
    /// Create as many ScriptableObject instances of your subclass as needed.
    ///
    /// Example:
    ///   public class IsFalling : Condition<Rigidbody2D>
    ///   {
    ///       protected override bool Verify(Rigidbody2D rb)
    ///       {
    ///           bool result = rb.linearVelocity.y < -1f;
    ///           return inverted ? !result : result;
    ///       }
    ///   }
    ///
    /// File is named Condition_T.cs because the filesystem cannot store angle brackets.
    /// The class name Condition<T> is valid C# and compiles without issues.
    /// </summary>
    public abstract class Condition<T> : Condition where T : Component
    {
        /// <summary>Resolved automatically from the generic type parameter. Do not override.</summary>
        public sealed override Type RequiredType => typeof(T);

        /// <summary>Casts the resolved component and delegates to Verify(T). Do not override.</summary>
        internal sealed override bool Verify(Component component) => Verify((T)component);

        /// <summary>
        /// Implement your condition logic here.
        /// Apply inverted before returning: return inverted ? !result : result;
        /// </summary>
        protected abstract bool Verify(T component);
    }
}
