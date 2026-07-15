using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Adds a right-click Hierarchy menu item for creating a pre-wired Actor GameObject.
    /// "FuzzyBrain/Actor" — creates a GameObject with an Actor component.
    /// ActClock is added automatically by Actor on Awake.
    /// Add ActHistory manually to any Actor that needs combo support.
    /// Creates a FuzzyBrainManager in the scene if one does not exist.
    /// </summary>
    public static class FuzzyBrainActorFactory
    {
        private const int MenuPriority = 12;

        [MenuItem("GameObject/FuzzyBrain/Actor", false, MenuPriority)]
        private static void CreateActor(MenuCommand command)
        {
            FuzzyBrainManager mgr = EnsureFuzzyBrainManager();

            var go    = new GameObject("Actor");
            var actor = go.AddComponent<Actor>();

            // Wire the manager to the actor's serialized Manager field.
            if (mgr != null)
            {
                var so = new SerializedObject(actor);
                so.FindProperty("_manager").objectReferenceValue = mgr;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Parent to the selected object in the Hierarchy, if any.
            GameObject parent = command.context as GameObject;
            if (parent != null)
                go.transform.SetParent(parent.transform, false);

            Undo.RegisterCreatedObjectUndo(go, "Create Actor");
            Selection.activeGameObject = go;
        }

        /// <summary>
        /// Ensures a ContinuousFuzzyBrainManager is present in the scene.
        /// Returns the existing manager if one is found, otherwise creates one.
        /// </summary>
        private static FuzzyBrainManager EnsureFuzzyBrainManager()
        {
            var existing = Object.FindFirstObjectByType<FuzzyBrainManager>();
            if (existing != null) return existing;

            var managerGo = new GameObject("FuzzyBrainManager");
            var mgr       = managerGo.AddComponent<ContinuousFuzzyBrainManager>();
            Undo.RegisterCreatedObjectUndo(managerGo, "Create ContinuousFuzzyBrainManager");
            Debug.Log("[FuzzyBrain] No FuzzyBrainManager found in the scene — a ContinuousFuzzyBrainManager has been created.", managerGo);
            return mgr;
        }
    }
}
