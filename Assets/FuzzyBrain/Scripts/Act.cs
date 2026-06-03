using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Abstract base for all act types. Subclass once per behaviour, create ScriptableObject
    /// instances via the Act Wizard, and assign them to a ScriptableActList on an Actor.
    ///
    /// Lifecycle per match:
    ///   1. CheckConditions — all conditions must pass.
    ///   2. OnStart         — called once on the first matching tick.
    ///   3. PerformAct      — called on the first tick (and every subsequent tick if locked).
    ///   4. IsComplete      — polled each tick while locked. Return true to unlock.
    ///      If IsComplete never returns true, maxClockTime forces an unlock as a safety net.
    ///
    /// Default behaviour (no overrides): fire-and-forget — PerformAct runs once, IsComplete
    /// returns true immediately, no lock is set. Fully backwards-compatible.
    /// </summary>
    public abstract class Act : ScriptableObject
    {
        [Tooltip("Conditions evaluated with AND logic. All must pass for the act to fire.")]
        public Condition[] conditions = new Condition[0];

        [Tooltip("Maximum time in seconds the ActClock is allowed to run before forcing unlock. " +
                 "0 = no timeout. Acts that override IsComplete should always set this as a safety net.")]
        public float maxClockTime;

        /// <summary>
        /// Checks all conditions via the per-tick ActContext cache.
        /// Each unique condition SO is evaluated at most once per tick.
        /// Returns true only if every non-null condition passes.
        /// </summary>
        public bool CheckConditions(ActContext ctx)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] == null) continue;
                if (!ctx.Evaluate(conditions[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Called once when this act is first selected by the evaluator.
        /// Override to trigger animations, sounds, or other one-shot setup.
        /// </summary>
        public virtual void OnStart(ActContext ctx) { }

        /// <summary>
        /// Called each tick while this act is locked. Return true when the act is finished.
        /// The default implementation returns true immediately (fire-and-forget).
        /// Override to keep the actor locked until custom completion logic is satisfied.
        /// Always set maxClockTime when overriding to avoid permanent locks.
        /// </summary>
        public virtual bool IsComplete(ActContext ctx) => true;

        /// <summary>
        /// Implement the behaviour this act performs when all conditions pass.
        /// Use ctx.Get&lt;T&gt;() to access components cached on the Actor's GameObject.
        /// Always null-check the result — the component may not be present.
        /// </summary>
        public abstract void PerformAct(ActContext ctx);
    }
}
