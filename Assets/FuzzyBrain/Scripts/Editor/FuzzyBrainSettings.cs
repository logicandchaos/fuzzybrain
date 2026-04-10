using System.IO;
using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Per-project settings asset for FuzzyBrain.
    /// Stores default folder paths used by the wizard windows.
    /// Configure via Edit > Project Settings > FuzzyBrain.
    /// </summary>
    public class FuzzyBrainSettings : ScriptableObject
    {
        private const string SettingsAssetPath =
            "Assets/FuzzyBrain/Editor/FuzzyBrainSettings.asset";

        public string conditionScriptsFolder = "Assets/FuzzyBrain/Scripts/ActorConditions";
        public string conditionAssetsFolder  = "Assets/FuzzyBrain/Data/Conditions";
        public string actAssetsFolder        = "Assets/FuzzyBrain/Data/Acts";
        public string activityListFolder     = "Assets/FuzzyBrain/Data/ActivityLists";

        // ── Load / create ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the existing settings asset, or creates and saves a new one.
        /// Safe to call on domain reload — always checks LoadAssetAtPath first.
        /// </summary>
        public static FuzzyBrainSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<FuzzyBrainSettings>(SettingsAssetPath);
            if (settings != null) return settings;

            if (Application.isPlaying) return CreateInstance<FuzzyBrainSettings>();

            string dir = Path.GetDirectoryName(SettingsAssetPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            settings = CreateInstance<FuzzyBrainSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        // ── Project Settings integration ──────────────────────────────────────────

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/FuzzyBrain", SettingsScope.Project)
            {
                label = "FuzzyBrain",
                activateHandler = (searchContext, rootElement) => { },
                guiHandler = searchContext =>
                {
                    var settings = GetOrCreate();
                    var so = new SerializedObject(settings);
                    so.Update();

                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Default Folder Paths", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f);

                    EditorGUILayout.PropertyField(so.FindProperty("conditionScriptsFolder"),
                        new GUIContent("Condition Scripts"));
                    EditorGUILayout.PropertyField(so.FindProperty("conditionAssetsFolder"),
                        new GUIContent("Condition Assets"));
                    EditorGUILayout.PropertyField(so.FindProperty("actAssetsFolder"),
                        new GUIContent("Act Assets"));
                    EditorGUILayout.PropertyField(so.FindProperty("activityListFolder"),
                        new GUIContent("Activity List Assets"));

                    so.ApplyModifiedProperties();
                },
                keywords = new System.Collections.Generic.HashSet<string>
                    { "FuzzyBrain", "Actor", "Condition", "Act", "ActivityList" }
            };
        }
    }
}
