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
            EnsureFuzzyBrainManager();

            var go = new GameObject("Actor");
            go.AddComponent<Actor>();

            // Parent to the selected object in the Hierarchy, if any.
            GameObject parent = command.context as GameObject;
            if (parent != null)
                go.transform.SetParent(parent.transform, false);

            Undo.RegisterCreatedObjectUndo(go, "Create Actor");
            Selection.activeGameObject = go;
        }

        /// <summary>
        /// Ensures a FuzzyBrainManager is present in the scene.
        /// If one is created, it is registered with Undo and a console message is logged.
        /// </summary>
        private static void EnsureFuzzyBrainManager()
        {
            if (Object.FindFirstObjectByType<FuzzyBrainManager>() != null) return;

            var managerGo = new GameObject("FuzzyBrainManager");
            managerGo.AddComponent<FuzzyBrainManager>();
            Undo.RegisterCreatedObjectUndo(managerGo, "Create FuzzyBrainManager");
            Debug.Log("[FuzzyBrain] FuzzyBrainManager was not found in the scene — one has been created.", managerGo);
        }
    }
}
