using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Custom inspector for Actor.
    /// Adds an "Open FuzzyBrain Editor" button.
    /// Implements OnDrawGizmosSelected to call DrawGizmo on every condition
    /// in the active list that implements IGizmoDrawable.
    /// </summary>
    [CustomEditor(typeof(Actor))]
    public class ActorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Open FuzzyBrain Editor", GUILayout.Height(28f)))
                FuzzyBrainWindow.Open();
        }

        private void OnDrawGizmosSelected()
        {
            Actor actor = (Actor)target;

            SerializedProperty activitiesProp = serializedObject.FindProperty("activities");
            if (activitiesProp == null) return;

            ScriptableActivityList list = activitiesProp.objectReferenceValue as ScriptableActivityList;
            if (list == null) return;

            // Build a temporary component cache — _componentCache is private, so we rebuild here.
            // This runs only when the actor is selected in the editor, not on the hot path.
            var tempComponentCache = new Dictionary<Type, Component>();
            foreach (Component c in actor.GetComponents<Component>())
                tempComponentCache[c.GetType()] = c;

            var tempConditionCache = new Dictionary<Condition, bool>();
            ActContext ctx = new ActContext(actor, tempComponentCache, tempConditionCache);

            foreach (Act act in list.list)
            {
                if (act == null || act.conditions == null) continue;
                foreach (Condition condition in act.conditions)
                {
                    if (condition is IGizmoDrawable drawable)
                        drawable.DrawGizmo(ctx);
                }
            }
        }
    }
}
