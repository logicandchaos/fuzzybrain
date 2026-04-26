using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Abstract base for all act types. Subclass once per behaviour, override PerformAct,
    /// and create ScriptableObject instances via the Act Wizard.
    /// Actor handles setCanAct, resetTime, and resetIdle after PerformAct returns.
    /// </summary>
    public abstract class Act : ScriptableObject
    {
        [Tooltip("Conditions evaluated with AND logic. All must pass for the act to fire.")]
        public Condition[] conditions = new Condition[0];

        [Tooltip("When true, blocks act evaluation for resetTime seconds after this act fires.")]
        public bool setCanAct;

        [Tooltip("Seconds to block act evaluation after this act fires. Requires setCanAct.")]
        public float resetTime;

        [Tooltip("When true, calls Actor.ResetIdle() when this act fires.")]
        public bool resetIdle;

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
        /// Implement the behaviour this act performs when all conditions pass.
        /// Use ctx.Get&lt;T&gt;() to access components cached on the Actor's GameObject.
        /// Always null-check the result — the component may not be present.
        /// </summary>
        public abstract void PerformAct(ActContext ctx);
    }
}
