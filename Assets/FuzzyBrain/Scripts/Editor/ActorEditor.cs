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

            // ── Manager check ─────────────────────────────────────────────────────
            bool hasManager = FindFirstObjectByType<FuzzyBrainManager>() != null;
            if (!hasManager)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox(
                    "No FuzzyBrainManager found in this scene.\n" +
                    "One will be created automatically at runtime, but it is recommended to add one manually so you can configure bucket count and tick interval.",
                    MessageType.Warning);

                if (GUILayout.Button("Add FuzzyBrainManager to Scene", GUILayout.Height(26f)))
                {
                    var go = new GameObject("FuzzyBrainManager");
                    go.AddComponent<FuzzyBrainManager>();
                    Undo.RegisterCreatedObjectUndo(go, "Add FuzzyBrainManager");
                    Selection.activeGameObject = go;
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

            SerializedProperty activitiesProp = serializedObject.FindProperty("activities");
            if (activitiesProp == null) return;

            ScriptableActList list = activitiesProp.objectReferenceValue as ScriptableActList;
            if (list == null) return;

            // Build a temporary component cache — _componentCache is private, so we rebuild here.
            // This runs only when the actor is selected in the editor, not on the hot path.
            var tempComponentCache = ActContext.BuildComponentCache(actor);

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
