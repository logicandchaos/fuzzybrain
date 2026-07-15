using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Custom inspector for Actor.
    /// Warns when no FuzzyBrainManager is assigned to the Actor.
    /// Adds an "Open FuzzyBrain Editor" button.
    /// Implements OnDrawGizmosSelected to call DrawGizmo on every condition
    /// in the active list that implements IGizmoDrawable.
    /// </summary>
    [CustomEditor(typeof(Actor))]
    public class ActorEditor : UnityEditor.Editor
    {
        private Dictionary<Type, Component> _gizmoComponentCache;
        private Actor                       _gizmoCachedActor;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Actor actor = (Actor)target;

            // ── Manager check ─────────────────────────────────────────────────────
            SerializedProperty managerProp = serializedObject.FindProperty("_manager");
            bool managerAssigned = managerProp != null && managerProp.objectReferenceValue != null;

            if (!managerAssigned)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(
                    "No FuzzyBrainManager assigned. This Actor will not evaluate.\n" +
                    "Assign a ContinuousFuzzyBrainManager or StepFuzzyBrainManager in the Manager field above.",
                    MessageType.Error);

                FuzzyBrainManager existingManager = FindFirstObjectByType<FuzzyBrainManager>();

                if (existingManager != null)
                {
                    if (GUILayout.Button($"Assign \"{existingManager.name}\" to this Actor", GUILayout.Height(26f)))
                    {
                        managerProp.objectReferenceValue = existingManager;
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    if (GUILayout.Button("Add Continuous Manager to Scene", GUILayout.Height(26f)))
                    {
                        var go = new GameObject("FuzzyBrainManager");
                        var mgr = go.AddComponent<ContinuousFuzzyBrainManager>();
                        Undo.RegisterCreatedObjectUndo(go, "Add Continuous FuzzyBrain Manager");
                        managerProp.objectReferenceValue = mgr;
                        serializedObject.ApplyModifiedProperties();
                        Selection.activeGameObject = go;
                    }
                }

                EditorGUILayout.Space(2f);
            }

            // ── Editor window button ──────────────────────────────────────────────
            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Open FuzzyBrain Editor", GUILayout.Height(28f)))
                FuzzyBrainWindow.Open();
        }

        private void OnDrawGizmosSelected()
        {
            Actor actor = (Actor)target;

            SerializedProperty activitiesProp = serializedObject.FindProperty("acts");
            if (activitiesProp == null) return;

            ScriptableActList list = activitiesProp.objectReferenceValue as ScriptableActList;
            if (list == null) return;

            // Rebuild the cache only when the actor reference changes (e.g. multi-selection switch).
            // This avoids calling GetComponents + reflection on every Scene view repaint.
            if (_gizmoComponentCache == null || _gizmoCachedActor != actor)
            {
                _gizmoCachedActor    = actor;
                _gizmoComponentCache = ActContext.BuildComponentCache(actor);
            }

            var tempConditionCache = new Dictionary<Condition, bool>();
            ActContext ctx = new ActContext(actor, _gizmoComponentCache, tempConditionCache);

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
