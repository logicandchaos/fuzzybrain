using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

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

        public string conditionScriptsFolder      = "Assets/FuzzyBrain/Scripts/ActorConditions";
        public string quickConditionScriptsFolder = "Assets/FuzzyBrain/Scripts/ActorConditions";
        public string conditionAssetsFolder       = "Assets/FuzzyBrain/Data/Conditions";
        public string actScriptsFolder       = "Assets/FuzzyBrain/Scripts/Acts";
        public string actAssetsFolder        = "Assets/FuzzyBrain/Data/Acts";
        [FormerlySerializedAs("ActListFolder")]
        public string actListFolder          = "Assets/FuzzyBrain/Data/ActLists";

        [Tooltip("Default C# namespace written into generated Act and Condition scripts. Leave empty for the global namespace.")]
        public string defaultNamespace       = string.Empty;

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
                    EditorGUILayout.PropertyField(so.FindProperty("actScriptsFolder"),
                        new GUIContent("Act Scripts"));
                    EditorGUILayout.PropertyField(so.FindProperty("actAssetsFolder"),
                        new GUIContent("Act Assets"));
                    EditorGUILayout.PropertyField(so.FindProperty("actListFolder"),
                        new GUIContent("Act List Assets"));

                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Code Generation", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f);
                    EditorGUILayout.PropertyField(so.FindProperty("defaultNamespace"),
                        new GUIContent("Default Namespace",
                            "Namespace written into generated Act and Condition scripts. Leave empty for global namespace."));

                    so.ApplyModifiedProperties();
                },
                keywords = new System.Collections.Generic.HashSet<string>
                    { "FuzzyBrain", "Actor", "Condition", "Act", "ActList" }
            };
        }
    }
}
