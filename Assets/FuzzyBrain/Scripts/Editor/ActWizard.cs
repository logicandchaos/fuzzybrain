using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Two-tab wizard for creating Act types and assets.
    /// Tab 1 — Generate Script: writes an Act subclass .cs file and opens it in the IDE.
    /// Tab 2 — Create Asset:    creates a ScriptableObject instance from an existing Act type.
    /// Open via Tools > FuzzyBrain > New Act, or from the FuzzyBrainWindow.
    /// </summary>
    public class ActWizard : EditorWindow
    {
        private static readonly Regex ValidIdentifier = new Regex(@"^[A-Za-z][A-Za-z0-9_]*$");

        // ── Tab 1 state ───────────────────────────────────────────────────────────

        private string _className    = "MyAct";
        private string _scriptFolder;
        private string _menuPath;

        // ── Tab 2 state ───────────────────────────────────────────────────────────

        private int                  _actTypeIndex;
        private string               _assetName   = "NewAct";
        private string               _assetFolder;
        private List<Type>           _actTypes    = new List<Type>();
        private List<string>         _actNames    = new List<string>();
        private ScriptableObject     _previewInstance;
        private UnityEditor.Editor   _previewEditor;
        private Vector2              _previewScroll;
        private bool                 _addToCurrentList;

        // ── Shared ────────────────────────────────────────────────────────────────

        private int _activeTab;

        // ── Menu entry ────────────────────────────────────────────────────────────

        [MenuItem("Tools/FuzzyBrain/New Act", priority = 12)]
        public static void Open() => Open(0);

        /// <summary>Opens the wizard on the given tab index.</summary>
        public static void Open(int tab)
        {
            var window = GetWindow<ActWizard>("Act Wizard");
            window.minSize  = new Vector2(420f, 320f);
            window._activeTab = tab;
            window.Show();
            FuzzyBrainEditorUtils.SetWindowIcon(window);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            var settings  = FuzzyBrainSettings.GetOrCreate();
            _scriptFolder = settings.actScriptsFolder;
            _assetFolder  = settings.actAssetsFolder;

            PopulateActTypes();
            RefreshMenuPath();
            RefreshPreviewInstance();
        }

        private void OnDestroy()
        {
            if (_previewEditor   != null) DestroyImmediate(_previewEditor);
            if (_previewInstance != null) DestroyImmediate(_previewInstance);
        }

        private void PopulateActTypes()
        {
            _actTypes.Clear();
            _actNames.Clear();

            foreach (Type t in TypeCache.GetTypesDerivedFrom<Act>())
            {
                if (t.IsAbstract || t.IsGenericTypeDefinition) continue;
                _actTypes.Add(t);
                _actNames.Add(t.Name);
            }

            _actTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            _actNames.Sort(StringComparer.Ordinal);
        }

        private void RefreshMenuPath()
        {
            _menuPath = $"FuzzyBrain/Acts/{_className}";
        }

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            _activeTab = GUILayout.Toolbar(_activeTab, new[] { "Generate Script", "Create Asset" });
            EditorGUILayout.Space(8f);

            if (_activeTab == 0)
                DrawGenerateTab();
            else
                DrawCreateAssetTab();
        }

        // ── Tab 1: Generate Script ────────────────────────────────────────────────

        private void DrawGenerateTab()
        {
            EditorGUILayout.LabelField("New Act Type", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            _className = EditorGUILayout.TextField("Class Name", _className);
            if (EditorGUI.EndChangeCheck())
                RefreshMenuPath();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _scriptFolder = EditorGUILayout.TextField("Scripts Folder", _scriptFolder);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                {
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select Script Output Folder", _scriptFolder, "");
                    if (!string.IsNullOrEmpty(picked))
                        _scriptFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                }
            }

            _menuPath = EditorGUILayout.TextField("Menu Path", _menuPath);

            EditorGUILayout.Space(8f);

            string error = ValidateScript();
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Generate Script", GUILayout.Height(28f)))
                GenerateScript();

            GUI.enabled = true;
        }

        private string ValidateScript()
        {
            if (string.IsNullOrWhiteSpace(_className))
                return "Class name cannot be empty.";
            if (!ValidIdentifier.IsMatch(_className))
                return "Class name must be a valid C# identifier (letters, digits, underscores; start with a letter).";

            string path = Path.Combine(_scriptFolder, _className + ".cs");
            if (File.Exists(path))
                return $"A file named '{_className}.cs' already exists in the selected folder.";

            return null;
        }

        private void GenerateScript()
        {
            string template =
$@"using FuzzyBrain;
using UnityEngine;

[CreateAssetMenu(fileName = ""{_className}"", menuName = ""{_menuPath}"")]
public class {_className} : Act
{{
    public override void PerformAct(ActContext ctx)
    {{
        // TODO: implement act behaviour
    }}
}}
";
            if (!Directory.Exists(_scriptFolder))
                Directory.CreateDirectory(_scriptFolder);

            string filePath = Path.Combine(_scriptFolder, _className + ".cs");
            File.WriteAllText(filePath, template);
            AssetDatabase.Refresh();

            string[] lines    = template.Split('\n');
            int      todoLine = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("TODO")) { todoLine = i + 1; break; }
            }

            string fullPath = Path.GetFullPath(filePath);
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, todoLine, 0);
            Debug.Log($"[FuzzyBrain] Generated act script: {filePath}");
        }

        // ── Tab 2: Create Asset ───────────────────────────────────────────────────

        private void DrawCreateAssetTab()
        {
            EditorGUILayout.LabelField("Create Act Asset", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (_actTypes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No concrete Act types found. Generate a script first and let Unity compile.",
                    MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _actTypeIndex = EditorGUILayout.Popup("Act Type", _actTypeIndex, _actNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                _assetName = _actTypes[_actTypeIndex].Name;
                RefreshPreviewInstance();
            }

            _assetName = EditorGUILayout.TextField("Asset Name", _assetName);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _assetFolder = EditorGUILayout.TextField("Assets Folder", _assetFolder);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                {
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select Asset Output Folder", _assetFolder, "");
                    if (!string.IsNullOrEmpty(picked))
                        _assetFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                }
            }

            if (FuzzyBrainWindow.IsOpen)
            {
                EditorGUILayout.Space(4f);
                _addToCurrentList = EditorGUILayout.Toggle(
                    "Add to Current Act List", _addToCurrentList);
            }

            // Properties preview
            if (_previewEditor != null)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
                _previewScroll = EditorGUILayout.BeginScrollView(
                    _previewScroll, GUILayout.MaxHeight(200f));
                _previewEditor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(8f);

            if (string.IsNullOrWhiteSpace(_assetName))
            {
                EditorGUILayout.HelpBox("Asset name cannot be empty.", MessageType.Warning);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Create Asset", GUILayout.Height(28f)))
                CreateActAsset();

            GUI.enabled = true;
        }

        private void RefreshPreviewInstance()
        {
            if (_previewEditor   != null) { DestroyImmediate(_previewEditor);   _previewEditor   = null; }
            if (_previewInstance != null) { DestroyImmediate(_previewInstance); _previewInstance = null; }

            if (_actTypes.Count == 0) return;

            _previewInstance      = CreateInstance(_actTypes[_actTypeIndex]);
            _previewInstance.name = _assetName;
            _previewEditor        = UnityEditor.Editor.CreateEditor(_previewInstance);
        }

        private void CreateActAsset()
        {
            if (_previewInstance == null) return;

            if (!Directory.Exists(_assetFolder))
                Directory.CreateDirectory(_assetFolder);

            _previewInstance.name = _assetName;

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(_assetFolder, _assetName + ".asset"));

            AssetDatabase.CreateAsset(_previewInstance, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_previewInstance);

            if (_addToCurrentList)
                FuzzyBrainWindow.TryAddActToCurrentList(_previewInstance as Act);

            Debug.Log($"[FuzzyBrain] Created act asset: {assetPath}");

            _previewInstance = null;
            _previewEditor   = null;
            RefreshPreviewInstance();
        }
    }
}
