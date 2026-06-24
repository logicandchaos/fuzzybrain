using System;
using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// The FuzzyBrain evaluation host. Drop on any GameObject to give it behaviour.
    ///
    /// Actor manages the act list, evaluation loop, component caching,
    /// and the act lock cycle (OnStart → IsComplete / maxClockTime timeout).
    ///
    /// isIdle is reset to false at the start of each evaluation tick.
    /// Acts are responsible for setting it — the built-in IdleAct does this in PerformAct.
    ///
    /// Requires a FuzzyBrainManager in the scene to drive tick timing.
    /// If none is present when this Actor enables, one is created automatically
    /// with default settings (equivalent to per-frame evaluation).
    ///
    /// Optionally add an ActHistory component to enable combo sequencing.
    /// </summary>
    [AddComponentMenu("FuzzyBrain/Actor")]
    public class Actor : MonoBehaviour
    {
        [Header("Acts")]
        [SerializeField]
        private ScriptableActList acts;

        [Header("Actor State")]
        public bool isIdle;
        public bool isAlive = true;

        private Act _currentAct;

        private Dictionary<Type, Component> _componentCache;
        private Dictionary<Condition, bool> _conditionCache;
        private Dictionary<Act, float> _cooldownEndTimes;
        private float _lockStartTime;
        private ActHistory _actHistory;

        /// <summary>
        /// The act that fired most recently this session.
        /// Null until the first evaluation. Used by FuzzyBrainWindow for play-mode highlight.
        /// </summary>
        public Act LastFiredAct { get; private set; }

        /// <summary>
        /// The act currently locked. Null when no act is running.
        /// Readable by conditions and external scripts via ctx.Get&lt;Actor&gt;().CurrentAct.
        /// </summary>
        public Act CurrentAct => _currentAct;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            _componentCache = ActContext.BuildComponentCache(this);
            _conditionCache = new Dictionary<Condition, bool>();
            _cooldownEndTimes = new Dictionary<Act, float>();
            _actHistory = GetComponent<ActHistory>();
        }

        private void OnEnable()
        {
            EnsureManager();
            FuzzyBrainManager.Instance.Register(this);
            EnableActor();
        }

        /// <summary>
        /// Ensures a FuzzyBrainManager exists in the scene.
        /// If none is found, creates one automatically with default settings.
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
        /// Runs one evaluation pass.
        /// Called by FuzzyBrainManager on its tick schedule.
        ///
        /// If an act is currently locked, polls IsComplete and the ActClock timeout.
        /// If unlocked (or immediately unlocked this tick), evaluates the act list and
        /// fires the first act whose conditions all pass.
        ///
        /// isIdle is reset to false before evaluation each tick. Acts that represent an idle
        /// state should set ctx.Actor.isIdle = true in their PerformAct implementation.
        /// </summary>
        public void ActorUpdate()
        {
            if (acts == null) return;

            _conditionCache.Clear();
            ActContext ctx = new ActContext(this, _componentCache, _conditionCache);

            // ── Locked phase ──────────────────────────────────────────────────────
            if (_currentAct != null)
            {
                bool done = _currentAct.IsComplete(ctx);
                bool timeout = _currentAct.maxLockTime > 0f && Time.time >= _lockStartTime + _currentAct.maxLockTime;

                if (!done && !timeout) return;

                // Unlock: record in history before clearing.
                _actHistory?.RecordAct(_currentAct);
                _currentAct = null;
            }

            // ── Normal evaluation ─────────────────────────────────────────────────
            isIdle = false;

            foreach (Act act in acts.list)
            {
                if (act == null) continue;

                if (act.cooldown > 0f && _cooldownEndTimes.TryGetValue(act, out float readyAt) && Time.time < readyAt)
                    continue;

                if (!act.CheckConditions(ctx)) continue;

                act.OnStart(ctx);
                act.PerformAct(ctx);
                LastFiredAct = act;

                if (act.cooldown > 0f)
                    _cooldownEndTimes[act] = Time.time + act.cooldown;

                // Lock only if the act declares it is not yet complete.
                if (!act.IsComplete(ctx))
                {
                    _currentAct = act;
                    _lockStartTime = Time.time;
                }

                return;
            }
        }

        // ── Actor lifecycle ───────────────────────────────────────────────────────

        /// <summary>Sorts the act list and resets actor state. Called automatically on enable.</summary>
        public void EnableActor()
        {
            if (acts != null)
                acts.SortActs();

            ResetActor();
        }

        /// <summary>
        /// Rebuilds the component cache and re-sorts the current act list without touching actor state.
        /// Call this after adding or removing acts, assigning a new list, or modifying components.
        /// Safe to call mid-game; takes effect on the next ActorUpdate tick.
        /// </summary>
        public void Refresh()
        {
            _componentCache = ActContext.BuildComponentCache(this);
            _actHistory = GetComponent<ActHistory>();

            if (acts != null)
                acts.SortIfDirty();
        }

        /// <summary>
        /// Assigns a new act list and immediately refreshes the actor.
        /// Does not reset actor state — call ResetActor() explicitly if needed.
        /// </summary>
        /// <param name="newList">The act list to assign. Pass null to clear the current list.</param>
        public void SetActList(ScriptableActList newList)
        {
            acts = newList;
            Refresh();
        }

        /// <summary>Resets engine state to initial values.</summary>
        public void ResetActor()
        {
            isAlive = true;
            isIdle = false;
            _currentAct = null;
            _lockStartTime = 0f;
            _cooldownEndTimes.Clear();
        }

        // ── Public state methods ──────────────────────────────────────────────────

        /// <summary>Sets isAlive to false and deactivates the GameObject.</summary>
        public void Die()
        {
            isAlive = false;
            gameObject.SetActive(false);
        }
    }
}
