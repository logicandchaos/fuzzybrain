using System;
using System.Collections.Generic;
using UnityEngine;

namespace FuzzyBrain
{
    /// <summary>
    /// Per-tick data carrier passed to every Act.CheckConditions, Act.PerformAct,
    /// and IGizmoDrawable.DrawGizmo call. Built once per ActorUpdate — zero allocation.
    /// readonly struct: the dictionary references are immutable but their contents are mutable,
    /// so the per-tick condition cache writes through correctly.
    /// </summary>
    public readonly struct ActContext
    {
        public readonly Actor Actor;
        public readonly GameObject GameObject;
        public readonly Transform Transform;

        private readonly Dictionary<Type, Component> _componentCache;
        private readonly Dictionary<Condition, bool> _conditionCache;

        /// <summary>Returns the cached component of type T, or null if not present on the actor.</summary>
        public T Get<T>() where T : Component
        {
            _componentCache.TryGetValue(typeof(T), out Component c);
            return c as T;
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
                          Dictionary<Condition, bool> conditionCache)
        {
            Actor           = actor;
            GameObject      = actor.gameObject;
            Transform       = actor.transform;
            _componentCache = componentCache;
            _conditionCache = conditionCache;
        }
    }
}
