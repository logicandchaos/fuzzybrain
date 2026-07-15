using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Drives registered Actors automatically on Unity's Update loop with staggered tick buckets.
    ///
    /// Actors are distributed across bucketCount buckets. Each bucket fires every tickInterval
    /// seconds but offset in time, spreading evaluation cost evenly across frames.
    /// Default settings (bucketCount=1, tickInterval=0) evaluate all actors every frame.
    /// </summary>
    [AddComponentMenu("FuzzyBrain/Continuous FuzzyBrain Manager")]
    [DefaultExecutionOrder(-1000)]
    public class ContinuousFuzzyBrainManager : FuzzyBrainManager
    {
        [SerializeField, Min(0f), Tooltip("Seconds between full condition evaluations per actor. Set to 0 to evaluate every frame.")]
        private float tickInterval = 0.1f;

        /// <summary>Seconds between full condition evaluations per actor. Matches the Inspector field.</summary>
        public override float TickInterval => tickInterval;

        protected override void Awake()
        {
            tickInterval = Mathf.Max(0f, tickInterval);
            base.Awake();

            // Stagger each bucket across the tick interval so evaluation cost is spread evenly.
            float offset = tickInterval / BucketCount;
            for (int i = 0; i < BucketCount; i++)
                BucketNextTick[i] = Time.time + tickInterval + i * offset;
        }

        private void Update()
        {
            for (int b = 0; b < BucketCount; b++)
            {
                if (Time.time < BucketNextTick[b]) continue;
                BucketNextTick[b] = Time.time + tickInterval;
                TickBucket(b);
            }
        }
    }
}
