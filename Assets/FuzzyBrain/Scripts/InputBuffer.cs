using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FuzzyBrain
{
    /// <summary>
    /// Caches input presses so they remain visible to FuzzyBrain conditions across tick intervals.
    ///
    /// The Input System's started callback fires once per press, every frame. Without buffering,
    /// any Actor whose tick interval is greater than zero can miss a press that falls between ticks.
    /// InputBuffer stores a timestamp for each watched action and exposes WasPressed(), which
    /// returns true for the full duration of one tick interval after the press was detected.
    ///
    /// If a PlayerInput component is present on the same GameObject, InputBuffer automatically
    /// watches all actions in its default action map — no manual setup required.
    /// For custom input setups without PlayerInput, call Watch(action) manually.
    /// </summary>
    [AddComponentMenu("FuzzyBrain/Input Buffer")]
    public class InputBuffer : MonoBehaviour
    {
        private readonly Dictionary<InputAction, float> _lastPressedTime  = new();
        private readonly Dictionary<InputAction, float> _lastReleasedTime = new();
        private readonly List<InputAction>              _watched          = new();

        // ── Registration ──────────────────────────────────────────────────────────

        /// <summary>
        /// Begins buffering presses for the given action. Safe to call multiple times
        /// for the same action — duplicate registrations are ignored.
        /// </summary>
        public void Watch(InputAction action)
        {
            if (action == null || _watched.Contains(action)) return;
            _watched.Add(action);
            action.started  += OnActionStarted;
            action.canceled += OnActionCanceled;
        }

        /// <summary>
        /// Stops buffering presses for the given action and removes its cached timestamps.
        /// </summary>
        public void Unwatch(InputAction action)
        {
            if (action == null) return;
            action.started  -= OnActionStarted;
            action.canceled -= OnActionCanceled;
            _watched.Remove(action);
            _lastPressedTime.Remove(action);
            _lastReleasedTime.Remove(action);
        }

        // ── Query ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the action was pressed within the current tick interval window.
        /// The window is the Actor's manager TickInterval plus one deltaTime as a boundary grace.
        /// Always returns false if the action has not been registered with Watch().
        /// </summary>
        public bool WasPressed(InputAction action)
        {
            if (action == null) return false;
            if (!_lastPressedTime.TryGetValue(action, out float pressedAt)) return false;

            float window = (GetComponent<Actor>()?.Manager?.TickInterval ?? 0f) + Time.deltaTime;
            return Time.time - pressedAt <= window;
        }

        /// <summary>
        /// Returns true if the action was released within the current tick interval window.
        /// The window is the Actor's manager TickInterval plus one deltaTime as a boundary grace.
        /// Always returns false if the action has not been registered with Watch().
        /// </summary>
        public bool WasReleased(InputAction action)
        {
            if (action == null) return false;
            if (!_lastReleasedTime.TryGetValue(action, out float releasedAt)) return false;

            float window = (GetComponent<Actor>()?.Manager?.TickInterval ?? 0f) + Time.deltaTime;
            return Time.time - releasedAt <= window;
        }

        /// <summary>
        /// Clears the cached press timestamp for the given action so it cannot carry over
        /// into the next tick. Safe to call mid-tick — the FuzzyBrain condition cache means
        /// other conditions checking the same action this tick still see the original result.
        /// </summary>
        public void ConsumePress(InputAction action)
        {
            if (action != null)
                _lastPressedTime.Remove(action);
        }

        /// <summary>
        /// Clears the cached release timestamp for the given action so it cannot carry over
        /// into the next tick.
        /// </summary>
        public void ConsumeRelease(InputAction action)
        {
            if (action != null)
                _lastReleasedTime.Remove(action);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (!TryGetComponent(out PlayerInput playerInput)) return;

            InputActionMap map = playerInput.actions?.FindActionMap(playerInput.defaultActionMap);
            if (map == null) return;

            foreach (InputAction action in map.actions)
                Watch(action);
        }

        private void OnDisable()
        {
            foreach (InputAction action in _watched)
            {
                action.started  -= OnActionStarted;
                action.canceled -= OnActionCanceled;
            }

            _watched.Clear();
            _lastPressedTime.Clear();
            _lastReleasedTime.Clear();
        }

        private void OnActionStarted(InputAction.CallbackContext ctx)
        {
            _lastPressedTime[ctx.action] = Time.time;
        }

        private void OnActionCanceled(InputAction.CallbackContext ctx)
        {
            _lastReleasedTime[ctx.action] = Time.time;
        }
    }
}
