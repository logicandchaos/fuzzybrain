namespace FuzzyBrain
{
    /// <summary>
    /// Opt-in interface for conditions that perform spatial queries.
    /// Implement this on a Condition to have ActorEditor draw its query volume
    /// in the Scene view via OnDrawGizmosSelected.
    ///
    /// Color convention:
    ///   Grey  — edit mode (no evaluation).
    ///   Green — play mode, last query returned true.
    ///   Red   — play mode, last query returned false.
    /// </summary>
    public interface IGizmoDrawable
    {
        /// <summary>
        /// Draw this condition's spatial query using UnityEngine.Gizmos.
        /// ctx.Get<T>() provides cached component access without allocation.
        /// Perform the query fresh inside this method for accurate color feedback.
        /// </summary>
        void DrawGizmo(ActContext ctx);
    }
}
