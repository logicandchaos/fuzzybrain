using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Optional singleton that drives all registered Actors with staggered tick buckets.
    /// Place one in the scene to take over tick control from individual Actors.
    ///
    /// Stagger: actors are distributed across bucketCount buckets. Each bucket fires every
    /// tickInterval seconds but offset in time, spreading evaluation cost evenly across frames.
    ///
    /// Without a manager in the scene, each Actor self-ticks via its own Update() every frame.
    /// Both modes produce identical behaviour — the manager just controls timing and batches ticks.
    /// </summary>
    [AddComponentMenu("DynamicBehaviour/DynamicBehaviour Manager")]
    public class FuzzyBrainManager : MonoBehaviour
    {
        public static FuzzyBrainManager Instance { get; private set; }

        [SerializeField, Tooltip("Number of stagger buckets. Higher values spread evaluation cost more evenly across frames.")]
        private int bucketCount = 4;

        [SerializeField, Tooltip("Seconds between full condition evaluations per actor.")]
        private float tickInterval = 0.1f;

        private List<Actor>[] _buckets;
        private float[] _bucketNextTick;
        private int _registrationCounter;

        // ── Singleton lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitBuckets();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Bucket initialisation ─────────────────────────────────────────────────

        private void InitBuckets()
        {
            _buckets       = new List<Actor>[bucketCount];
            _bucketNextTick = new float[bucketCount];

            float offset = tickInterval / bucketCount;
            for (int i = 0; i < bucketCount; i++)
            {
                _buckets[i]       = new List<Actor>();
                _bucketNextTick[i] = Time.time + i * offset;
            }
        }

        // ── Registration ──────────────────────────────────────────────────────────

        /// <summary>
        /// Registers an Actor into the next available bucket.
        /// Sets IsManagedExternally = true so the Actor skips self-ticking.
        /// Called automatically by Actor.OnEnable when a manager is present.
        /// </summary>
        public void Register(Actor actor)
        {
            int bucket = _registrationCounter++ % bucketCount;
            _buckets[bucket].Add(actor);
            actor.IsManagedExternally = true;
        }

        /// <summary>
        /// Removes an Actor from all buckets.
        /// Sets IsManagedExternally = false so the Actor resumes self-ticking if re-enabled.
        /// Called automatically by Actor.OnDisable.
        /// </summary>
        public void Unregister(Actor actor)
        {
            for (int b = 0; b < bucketCount; b++)
                _buckets[b].Remove(actor);

            actor.IsManagedExternally = false;
        }

        // ── Tick loop ─────────────────────────────────────────────────────────────

        private void Update()
        {
            for (int b = 0; b < bucketCount; b++)
            {
                if (Time.time < _bucketNextTick[b]) continue;
                _bucketNextTick[b] = Time.time + tickInterval;
                TickBucket(b);
            }
        }

        private void TickBucket(int b)
        {
            List<Actor> bucket = _buckets[b];
            for (int i = bucket.Count - 1; i >= 0; i--)
            {
                Actor actor = bucket[i];
                if (actor == null || !actor.isActiveAndEnabled)
                {
                    bucket.RemoveAt(i);
                    continue;
                }
                actor.ActorUpdate();
            }
        }
    }
}
