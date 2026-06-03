using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Built-in idle act. Assign no conditions so it sorts last and fires whenever
    /// no other act's conditions are met.
    ///
    /// Sets Actor.isIdle = true in PerformAct. Actor resets isIdle to false at the
    /// start of each tick, so isIdle is true only on ticks where this act (or a custom
    /// subclass) fires.
    ///
    /// Subclass IdleAct to add idle behaviour such as animations or sounds while
    /// keeping the automatic isIdle management.
    /// </summary>
    [CreateAssetMenu(fileName = "Idle", menuName = "FuzzyBrain/Acts/Idle")]
    public class IdleAct : Act
    {
        /// <summary>Sets Actor.isIdle = true. Override in a subclass to add idle behaviour — call base.PerformAct(ctx) to preserve the flag.</summary>
        public override void PerformAct(ActContext ctx)
        {
            ctx.Actor.isIdle = true;
        }
    }
}
