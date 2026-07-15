using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Abstract base class that drives registered Actors through a bucket-based evaluation schedule.
    ///
    /// Actors register with their assigned FuzzyBrainManager on enable and are distributed across
    /// stagger buckets to spread evaluation cost evenly.
    ///
    /// Subclasses define when evaluation happens:
    ///   ContinuousFuzzyBrainManager — evaluates automatically on Unity's Update loop.
    ///   StepFuzzyBrainManager       — evaluates only when Evaluate() is called explicitly.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public abstract class FuzzyBrainManager : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField, Tooltip("Enable act logging to the FuzzyBrain Log window (Editor only).")]
        private bool _enableActLogging = false;

        /// <summary>Gets or sets act logging. Fires OnActLoggingChanged so the FuzzyBrain Log window stays in sync.</summary>
        public bool EnableActLogging
        {
            get => _enableActLogging;
            set
            {
                _enableActLogging = value;
                OnActLoggingChanged?.Invoke(value);
            }
        }

        /// <summary>Fires when _enableActLogging changes. FuzzyBrainLog subscribes to keep IsRecording in sync.</summary>
        public static System.Action<bool> OnActLoggingChanged;

        /// <summary>Fires each time an actor ticks. FuzzyBrainLog subscribes to record entries.</summary>
        public static System.Action<string, string> OnActRecorded;
#endif

        [SerializeField, Min(1), Tooltip("Number of stagger buckets. Higher values spread evaluation cost more evenly across frames.")]
        private int bucketCount = 4;

        /// <summary>
        /// The interval between evaluations in seconds.
        /// Returns 0 for managers that do not use time-based scheduling.
        /// </summary>
        public virtual float TickInterval => 0f;

        private List<Actor>[] _buckets;
        private float[] _bucketNextTick;
        private int _registrationCounter;
        private readonly Dictionary<Actor, int> _actorBucket = new Dictionary<Actor, int>();

        // ── Bucket count and next-tick arrays exposed to subclasses ──────────────

        /// <summary>The number of stagger buckets currently configured.</summary>
        protected int BucketCount => bucketCount;

        /// <summary>Per-bucket scheduled next-tick times. Subclasses use this to implement time-based scheduling.</summary>
        protected float[] BucketNextTick => _bucketNextTick;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            InitBuckets();
#if UNITY_EDITOR
            OnActLoggingChanged?.Invoke(_enableActLogging);
#endif
        }

        // ── Bucket initialisation ─────────────────────────────────────────────────

        /// <summary>Allocates and clears the bucket arrays. Called automatically in Awake.</summary>
        protected void InitBuckets()
        {
            bucketCount = Mathf.Max(1, bucketCount);

            _buckets        = new List<Actor>[bucketCount];
            _bucketNextTick = new float[bucketCount];
            _actorBucket.Clear();

            for (int i = 0; i < bucketCount; i++)
                _buckets[i] = new List<Actor>();
        }

        // ── Registration ──────────────────────────────────────────────────────────

        /// <summary>
        /// Registers an Actor into the next available bucket.
        /// Called automatically by Actor.OnEnable.
        /// </summary>
        public void Register(Actor actor)
        {
            int bucket = _registrationCounter++ % bucketCount;
            _buckets[bucket].Add(actor);
            _actorBucket[actor] = bucket;
        }

        /// <summary>
        /// Removes an Actor from its bucket.
        /// Called automatically by Actor.OnDisable.
        /// </summary>
        public void Unregister(Actor actor)
        {
            if (!_actorBucket.TryGetValue(actor, out int bucket)) return;
            _buckets[bucket].Remove(actor);
            _actorBucket.Remove(actor);
        }

        // ── Tick ──────────────────────────────────────────────────────────────────

        /// <summary>Evaluates all Actors in a single bucket.</summary>
        protected void TickBucket(int b)
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
#if UNITY_EDITOR
                OnActRecorded?.Invoke(actor.name, actor.LastFiredAct?.name ?? "(none)");
#endif
            }
        }

        /// <summary>Evaluates all Actors across all buckets immediately.</summary>
        protected void TickAll()
        {
            for (int b = 0; b < _buckets.Length; b++)
                TickBucket(b);
        }
    }
}
