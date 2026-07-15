using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Drives registered Actors only when Evaluate() is called explicitly.
    ///
    /// Suitable for turn-based systems, dialogue trees, or any context where evaluation
    /// should be triggered by game logic rather than the Unity Update loop.
    /// Call Evaluate() from your turn manager, input handler, or dialogue system.
    /// </summary>
    [AddComponentMenu("FuzzyBrain/Step FuzzyBrain Manager")]
    [DefaultExecutionOrder(-1000)]
    public class StepFuzzyBrainManager : FuzzyBrainManager
    {
        /// <summary>
        /// Evaluates all registered Actors immediately.
        /// Call this from your turn manager, input handler, or any game logic that drives evaluation.
        /// </summary>
        public void Evaluate()
        {
            TickAll();
        }
    }
}
