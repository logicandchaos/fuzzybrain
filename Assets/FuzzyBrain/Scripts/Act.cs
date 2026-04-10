using UnityEngine;
using UnityEngine.Events;

namespace FuzzyBrain
{
    /// <summary>
    /// A single rule in an actor's activity list.
    /// When all conditions pass, onFire is invoked.
    /// Wire onFire to any method on any component attached to the actor's GameObject.
    /// Create via Assets > Create > DynamicBehaviour > Act.
    /// </summary>
    [CreateAssetMenu(fileName = "New Act", menuName = "DynamicBehaviour/Act")]
    public class Act : ScriptableObject
    {
        [Tooltip("Conditions evaluated with AND logic. All must pass for the act to fire.")]
        public Condition[] conditions = new Condition[0];

        [Tooltip("Invoked when all conditions pass. Wire to methods on your components.")]
        public UnityEvent onFire;

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

        /// <summary>Invokes onFire and resets idle time if resetIdle is true.</summary>
        public void PerformAct(ActContext ctx)
        {
            onFire?.Invoke();
            if (resetIdle) ctx.Actor.ResetIdle();
        }
    }
}
