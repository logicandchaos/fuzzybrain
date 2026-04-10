using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Wizard for creating Act and ScriptableActivityList ScriptableObject assets.
    /// When creating an Act, conditions can be added before the asset is saved.
    /// Open via Tools > FuzzyBrain > New Act, or from the FuzzyBrainWindow.
    /// </summary>
    public class ActWizard : EditorWindow
    {
        private enum AssetType { Act, ActivityList }

        private AssetType      _assetType = AssetType.Act;
        private string         _assetName = "New Act";
        private string         _outputFolder;
        private bool           _addToCurrentList;

        // Conditions staged before asset creation
        private readonly List<Condition> _stagedConditions = new List<Condition>();
        private Condition                _conditionToAdd;

        private Vector2 _conditionScrollPos;

        [MenuItem("Tools/FuzzyBrain/New Act", priority = 12)]
        public static void Open()
        {
            var window = GetWindow<ActWizard>("Act Wizard");
            window.minSize = new Vector2(400f, 320f);
            window.Show();
            FuzzyBrainEditorUtils.SetWindowIcon(window);
        }

        private void OnEnable()
        {
            RefreshFolderFromSettings();
        }

        private void RefreshFolderFromSettings()
        {
            var settings = FuzzyBrainSettings.GetOrCreate();
            _outputFolder = _assetType == AssetType.Act
                ? settings.actAssetsFolder
                : settings.activityListFolder;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create FuzzyBrain Asset", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            _assetType = (AssetType)EditorGUILayout.EnumPopup("Type", _assetType);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshFolderFromSettings();
                _stagedConditions.Clear();
            }

            _assetName = EditorGUILayout.TextField("Asset Name", _assetName);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _outputFolder = EditorGUILayout.TextField("Folder", _outputFolder);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                {
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select Output Folder", _outputFolder, "");
                    if (!string.IsNullOrEmpty(picked))
                        _outputFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                }
            }

            // Condition staging — only visible when creating an Act
            if (_assetType == AssetType.Act)
            {
                EditorGUILayout.Space(6f);
                DrawConditionSection();
            }

            // Add to current list toggle — only when the editor window is open
            if (_assetType == AssetType.Act && FuzzyBrainWindow.IsOpen)
            {
                EditorGUILayout.Space(4f);
                _addToCurrentList = EditorGUILayout.Toggle(
                    "Add to Current Activity List", _addToCurrentList);
            }

            EditorGUILayout.Space(8f);

            if (string.IsNullOrWhiteSpace(_assetName))
            {
                EditorGUILayout.HelpBox("Asset name cannot be empty.", MessageType.Warning);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Create", GUILayout.Height(28f)))
                CreateAsset();

            GUI.enabled = true;
        }

        // ── Condition section ─────────────────────────────────────────────────────

        private void DrawConditionSection()
        {
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);

            // Staged condition list
            if (_stagedConditions.Count > 0)
            {
                _conditionScrollPos = EditorGUILayout.BeginScrollView(
                    _conditionScrollPos, GUILayout.MaxHeight(120f));

                for (int i = 0; i < _stagedConditions.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Condition cond = _stagedConditions[i];
                        EditorGUILayout.ObjectField(cond, typeof(Condition), false);

                        // Inverted toggle
                        if (cond != null)
                        {
                            EditorGUI.BeginChangeCheck();
                            bool inv = EditorGUILayout.Toggle(
                                new GUIContent("inv", "Invert this condition"),
                                cond.inverted, GUILayout.Width(40f));
                            if (EditorGUI.EndChangeCheck())
                            {
                                cond.inverted = inv;
                                EditorUtility.SetDirty(cond);
                            }
                        }

                        // Remove button
                        if (GUILayout.Button("×", GUILayout.Width(22f)))
                        {
                            _stagedConditions.RemoveAt(i);
                            GUIUtility.ExitGUI();
                            return;
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No conditions added yet. Drag a Condition asset below or use the Condition Wizard.",
                    MessageType.None);
            }

            // Add condition row
            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _conditionToAdd = (Condition)EditorGUILayout.ObjectField(
                    "Add Condition", _conditionToAdd, typeof(Condition), false);
                if (EditorGUI.EndChangeCheck() && _conditionToAdd != null)
                {
                    _stagedConditions.Add(_conditionToAdd);
                    _conditionToAdd = null;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Condition Wizard", GUILayout.Width(130f)))
                    ConditionWizard.Open(1);
            }
        }

        // ── Asset creation ────────────────────────────────────────────────────────

        private void CreateAsset()
        {
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(_outputFolder, _assetName + ".asset"));

            ScriptableObject instance;
            if (_assetType == AssetType.Act)
            {
                Act act = CreateInstance<Act>();
                act.name = _assetName;

                // Write staged conditions into the asset before saving
                act.conditions = _stagedConditions.ToArray();

                AssetDatabase.CreateAsset(act, assetPath);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(act);

                if (_addToCurrentList)
                    FuzzyBrainWindow.TryAddActToCurrentList(act);

                instance = act;
            }
            else
            {
                instance = CreateInstance<ScriptableActivityList>();
                instance.name = _assetName;
                AssetDatabase.CreateAsset(instance, assetPath);
                AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(instance);
            }

            Debug.Log($"[FuzzyBrain] Created asset: {assetPath}");
            Close();
        }
    }
}
