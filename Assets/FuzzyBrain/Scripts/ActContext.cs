using System;
using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Per-tick data carrier passed to every Act.CheckConditions and Act.PerformAct call.
    /// Built once per ActorUpdate — zero allocation. When isValidating is true, Get&lt;T&gt;()
    /// logs warnings for missing components but returns null for all components, preventing
    /// act behaviour from executing during validation.
    /// </summary>
    public readonly struct ActContext
    {
        public readonly Actor     Actor;
        public readonly GameObject GameObject;
        public readonly Transform  Transform;

        private readonly Dictionary<Type, Component> _componentCache;
        private readonly Dictionary<Condition, bool> _conditionCache;
        private readonly bool                        _isValidating;

        /// <summary>
        /// Returns the cached component of type T, or null if not present on the actor.
        /// Logs a warning when the component is missing (runtime mode only).
        /// In validation mode, always returns null to suppress act behaviour.
        /// </summary>
        public T Get<T>() where T : Component
        {
            if (!_componentCache.TryGetValue(typeof(T), out Component c))
            {
                if (!_isValidating)
                    Debug.LogWarning(
                        $"[FuzzyBrain] {Actor.name}: act requested {typeof(T).Name} " +
                        $"but it is not present on this GameObject.",
                        Actor);
                return null;
            }

            return _isValidating ? null : c as T;
        }

        /// <summary>
        /// Evaluates a condition using the per-tick cache. If the same condition SO
        /// appears in multiple acts, Verify() runs only once per tick.
        /// Returns false if the required component is not present on the actor.
        /// </summary>
        public bool Evaluate(Condition condition)
        {
            if (_conditionCache.TryGetValue(condition, out bool cached))
                return cached;

            if (!_componentCache.TryGetValue(condition.RequiredType, out Component comp))
                return _conditionCache[condition] = false;

            return _conditionCache[condition] = condition.Verify(comp);
        }

        public ActContext(Actor actor,
                          Dictionary<Type, Component> componentCache,
                          Dictionary<Condition, bool> conditionCache,
                          bool isValidating = false)
        {
            Actor           = actor;
            GameObject      = actor.gameObject;
            Transform       = actor.transform;
            _componentCache = componentCache;
            _conditionCache = conditionCache;
            _isValidating   = isValidating;
        }

        /// <summary>
        /// Builds a component cache from all components on the given actor's GameObject.
        /// Each component is registered under its concrete type and every base type up to
        /// Component, so lookups by base class (e.g. Collider2D) resolve correctly even
        /// when only a derived type (e.g. BoxCollider2D) is attached.
        /// </summary>
        public static Dictionary<Type, Component> BuildComponentCache(Actor actor)
        {
            var cache = new Dictionary<Type, Component>();
            foreach (Component c in actor.GetComponents<Component>())
            {
                Type t = c.GetType();
                while (t != null && typeof(Component).IsAssignableFrom(t))
                {
                    if (!cache.ContainsKey(t))
                        cache[t] = c;
                    t = t.BaseType;
                }
            }
            return cache;
        }

        /// <summary>
        /// Creates an ActContext for validation. Get&lt;T&gt;() logs warnings for missing
        /// components and returns null for all components, preventing act behaviour from executing.
        /// </summary>
        public static ActContext ForValidation(Actor actor, Dictionary<Type, Component> componentCache)
        {
            return new ActContext(actor, componentCache, new Dictionary<Condition, bool>(), isValidating: true);
        }
    }
}
