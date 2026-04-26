using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// The FuzzyBrain evaluation engine. Drop on any GameObject — no subclassing required.
    ///
    /// FSM mode (isFuSM = false, default): stops on the first matching act each tick.
    /// FuSM mode (isFuSM = true): evaluates all acts and fires every match.
    ///
    /// Requires a FuzzyBrainManager in the scene to drive tick timing.
    /// If none is present when this Actor enables, one is created automatically
    /// with default settings (equivalent to per-frame evaluation).
    /// </summary>
    [AddComponentMenu("DynamicBehaviour/Actor")]
    public class Actor : MonoBehaviour
    {
        [Header("Activities")]
        [SerializeField]
        private ScriptableActList activities;

        [Header("Evaluation Mode")]
        [Tooltip("False (FSM): stop at the first matching act. True (FuSM): fire all matching acts.")]
        public bool isFuSM;

        [Header("Actor State")]
        public bool canAct = true;
        public bool isIdle;
        public bool isAlive = true;

        [SerializeField]
        private float idleDelay = 1f;

        /// <summary>Seconds of idle accumulation before isIdle is set. Exposed internally for test access.</summary>
        internal float IdleDelay
        {
            get => idleDelay;
            set => idleDelay = value;
        }

        private float _idleTime;
        private Dictionary<Type, Component> _componentCache;
        private Dictionary<Condition, bool> _conditionCache;

        /// <summary>
        /// The act that fired most recently this session.
        /// Null until the first evaluation. Used by DynamicBehaviourWindow for play-mode highlight.
        /// </summary>
        public Act LastFiredAct { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _componentCache = ActContext.BuildComponentCache(this);
            _conditionCache = new Dictionary<Condition, bool>();
        }

        private void OnEnable()
        {
            EnsureManager();
            FuzzyBrainManager.Instance.Register(this);
            EnableActor();
        }

        /// <summary>
        /// Ensures a FuzzyBrainManager exists in the scene.
        /// If none is found, creates one automatically with default settings,
        /// matching the same behaviour as per-frame self-ticking.
        /// </summary>
        private static void EnsureManager()
        {
            if (FuzzyBrainManager.Instance != null) return;

            var go = new GameObject("FuzzyBrainManager");
            go.AddComponent<FuzzyBrainManager>();
            Debug.Log("[FuzzyBrain] No FuzzyBrainManager found in the scene — one was created automatically.", go);
        }

        private void OnDisable()
        {
            if (FuzzyBrainManager.Instance != null)
                FuzzyBrainManager.Instance.Unregister(this);
        }

        // ── Evaluation loop ───────────────────────────────────────────────────────

        /// <summary>
        /// Runs one evaluation pass over the act list.
        /// Called by FuzzyBrainManager on its tick schedule.
        /// FSM mode: stops after the first matching act.
        /// FuSM mode: evaluates all acts and fires every match.
        /// Each unique condition SO is evaluated at most once per call.
        /// </summary>
        public void ActorUpdate()
        {
            if (activities == null) return;

            _conditionCache.Clear();
            ActContext ctx = new ActContext(this, _componentCache, _conditionCache);

            foreach (Act activity in activities.list)
            {
                if (activity == null) continue;

                if (activity.CheckConditions(ctx))
                {
                    activity.PerformAct(ctx);
                    LastFiredAct = activity;

                    if (activity.resetIdle) ResetIdle();

                    if (activity.setCanAct)
                    {
                        canAct = false;
                        StartCoroutine(ResetCanAct(activity.resetTime));
                    }

                    if (!isFuSM) return;
                }
            }
        }

        // ── Actor lifecycle ───────────────────────────────────────────────────────

        /// <summary>Sorts the act list and resets actor state. Called automatically on enable.</summary>
        public void EnableActor()
        {
            if (activities != null)
                activities.SortActivities();

            ResetActor();
        }

        /// <summary>Resets engine state to initial values.</summary>
        public void ResetActor()
        {
            isAlive = true;
            canAct  = true;
            _idleTime = 0f;
            isIdle  = false;
        }

        // ── Public state methods ──────────────────────────────────────────────────

        /// <summary>Sets isAlive to false and deactivates the GameObject.</summary>
        public void Die()
        {
            isAlive = false;
            gameObject.SetActive(false);
        }

        /// <summary>Resets the idle timer and clears the isIdle flag.</summary>
        public void ResetIdle()
        {
            _idleTime = 0f;
            isIdle    = false;
        }

        /// <summary>
        /// Accumulates idle time each call and sets isIdle once idleDelay is reached.
        /// Wire to onFire on a low-priority fallback act to detect inactivity.
        /// </summary>
        public void AddIdleTime()
        {
            _idleTime += Time.deltaTime;
            if (_idleTime > idleDelay)
            {
                isIdle    = true;
                _idleTime = 0f;
            }
        }

        // ── Cooldown ──────────────────────────────────────────────────────────────

        /// <summary>Restores canAct after the given delay. Started automatically when setCanAct is true.</summary>
        public void StartResetCanAct(float delay) => StartCoroutine(ResetCanAct(delay));

        private IEnumerator ResetCanAct(float delay)
        {
            yield return new WaitForSeconds(delay);
            canAct = true;
        }
    }
}
