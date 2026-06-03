using UnityEditor;
using UnityEngine;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Detects newly created ScriptableActList assets and injects the built-in
    /// Idle act automatically, so every new act list starts with a guaranteed
    /// resting state.
    ///
    /// Injection is skipped when:
    ///   - The list already contains a zero-condition act (custom idle or a reimport).
    ///   - The Idle.asset cannot be found at its expected path.
    /// </summary>
    public class ActListPostprocessor : AssetPostprocessor
    {
        internal const string IdleActPath = "Assets/FuzzyBrain/Data/Acts/Idle.asset";

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                // Only process ScriptableActList assets.
                var actList = AssetDatabase.LoadAssetAtPath<ScriptableActList>(path);
                if (actList == null) continue;

                // Skip lists that already have a zero-condition act (idle or custom).
                if (HasZeroConditionAct(actList)) continue;

                var idleAct = AssetDatabase.LoadAssetAtPath<Act>(IdleActPath);
                if (idleAct == null)
                {
                    Debug.LogWarning(
                        $"[FuzzyBrain] Could not inject idle act — Idle.asset not found at '{IdleActPath}'.",
                        actList);
                    continue;
                }

                actList.list.Add(idleAct);
                EditorUtility.SetDirty(actList);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>Returns true if the list already contains at least one act with no conditions.</summary>
        private static bool HasZeroConditionAct(ScriptableActList actList)
        {
            foreach (Act act in actList.list)
            {
                if (act != null && act.conditions.Length == 0)
                    return true;
            }
            return false;
        }
    }
}
