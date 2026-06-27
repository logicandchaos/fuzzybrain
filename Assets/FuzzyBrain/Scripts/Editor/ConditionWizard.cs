using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Three-tab wizard for creating condition types and assets.
    /// Tab 1 — Generate Script:   writes a Condition&lt;T&gt; subclass .cs file and opens it in the IDE.
    /// Tab 2 — Create Asset:      creates a ScriptableObject instance from an existing condition type.
    /// Tab 3 — Quick Condition:   generates a compiled Condition&lt;T&gt; with a field comparison baked into Verify.
    /// Open via Tools > FuzzyBrain > New Condition, or from the FuzzyBrainWindow.
    /// </summary>
    public class ConditionWizard : EditorWindow
    {
        private static readonly Regex ValidIdentifier = new Regex(@"^[A-Za-z][A-Za-z0-9_]*$");

        private const string LiteralIntControlName   = "FBLiteralInt";
        private const string LiteralFloatControlName = "FBLiteralFloat";

        // ── Tab 1 state ───────────────────────────────────────────────────────────

        private string _conditionName = "MyCondition";
        private int    _componentIndex;
        private string _scriptFolder;
        private string _namespace;
        private string _menuPath;
        private List<Type>   _componentTypes = new List<Type>();
        private List<string> _componentNames = new List<string>();

        // ── Tab 2 state ───────────────────────────────────────────────────────────

        private int                  _conditionTypeIndex;
        private string               _assetName   = "NewCondition";
        private string               _assetFolder;
        private List<Type>           _conditionTypes = new List<Type>();
        private List<string>         _conditionNames = new List<string>();
        private ScriptableObject     _previewInstance;
        private UnityEditor.Editor   _previewEditor;
        private Vector2              _previewScroll;

        // ── Tab 3 state ───────────────────────────────────────────────────────────

        private int              _quickComponentIndex;
        private List<MemberInfo> _quickMembers     = new List<MemberInfo>();
        private List<string>     _quickMemberNames = new List<string>();
        private int              _quickLeftIndex;
        private int              _quickOpIndex;
        private bool             _quickRHSIsField;
        private int              _quickRHSComponentIndex;
        private List<MemberInfo> _quickRHSMembers     = new List<MemberInfo>();
        private List<string>     _quickRHSMemberNames = new List<string>();
        private int              _quickRightIndex;
        private bool             _quickLiteralBool   = false;
        private int              _quickLiteralInt    = 0;
        private float            _quickLiteralFloat  = 0f;
        private string           _quickLiteralString = string.Empty;
        private string           _quickClassName     = "MyComparison";
        private string           _quickScriptFolder;

        private static readonly string[] AllOperators      = { "==", "!=", ">", "<", ">=", "<=" };
        private static readonly string[] BoolOnlyOperators = { "==", "!=" };

        private static readonly HashSet<Type> SupportedMemberTypes = new HashSet<Type>
        {
            typeof(bool), typeof(int), typeof(float), typeof(double), typeof(string)
        };

        // ── Shared ────────────────────────────────────────────────────────────────

        private int _activeTab;

        // ── Menu entry ────────────────────────────────────────────────────────────

        [MenuItem("Tools/FuzzyBrain/Condition Wizard", priority = 11)]
        public static void Open() => Open(0);

        /// <summary>Opens the wizard on the given tab index.</summary>
        public static void Open(int tab)
        {
            var window = GetWindow<ConditionWizard>("Condition Wizard");
            window.minSize = new Vector2(420f, 360f);
            window._activeTab = tab;
            window.Show();
            FuzzyBrainEditorUtils.SetWindowIcon(window);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            var settings       = FuzzyBrainSettings.GetOrCreate();
            _scriptFolder      = settings.conditionScriptsFolder;
            _assetFolder       = settings.conditionAssetsFolder;
            _quickScriptFolder = settings.conditionScriptsFolder;
            _namespace         = settings.defaultNamespace;

            PopulateComponentTypes();
            PopulateConditionTypes();
            RefreshMenuPath();
            RefreshPreviewInstance();

            if (_componentTypes.Count > 0)
            {
                RefreshQuickMemberList(_componentTypes[_quickComponentIndex]);
                RefreshQuickRHSMemberList(_componentTypes[_quickRHSComponentIndex]);
            }
        }

        private void OnDestroy()
        {
            var settings = FuzzyBrainSettings.GetOrCreate();
            settings.conditionScriptsFolder = _scriptFolder;
            settings.conditionAssetsFolder  = _assetFolder;
            settings.defaultNamespace       = _namespace;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            if (_previewEditor   != null) DestroyImmediate(_previewEditor);
            if (_previewInstance != null) DestroyImmediate(_previewInstance);
        }

        private void PopulateComponentTypes()
        {
            _componentTypes.Clear();
            _componentNames.Clear();

            var unityTypes   = new List<(string name, Type type)>();
            var projectTypes = new List<(string name, Type type)>();

            foreach (Type t in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (t.IsAbstract || t.Name.StartsWith("<")) continue;
                string assemblyName = t.Assembly.GetName().Name;
                bool isUnity = assemblyName.StartsWith("UnityEngine") ||
                               assemblyName.StartsWith("Unity.");

                if (isUnity) unityTypes.Add((t.Name, t));
                else         projectTypes.Add((t.Name, t));
            }

            unityTypes.Sort((a, b)   => string.Compare(a.name, b.name, StringComparison.Ordinal));
            projectTypes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            foreach (var (name, type) in unityTypes)
            {
                _componentTypes.Add(type);
                _componentNames.Add("Unity Components/" + name);
            }
            foreach (var (name, type) in projectTypes)
            {
                _componentTypes.Add(type);
                _componentNames.Add("Project Components/" + name);
            }
        }

        private void PopulateConditionTypes()
        {
            _conditionTypes.Clear();
            _conditionNames.Clear();

            foreach (Type t in TypeCache.GetTypesDerivedFrom<Condition>())
            {
                if (t.IsAbstract || t.IsGenericTypeDefinition) continue;
                _conditionTypes.Add(t);
                _conditionNames.Add(t.Name);
            }

            _conditionNames.Sort(StringComparer.Ordinal);
            _conditionTypes.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        private void RefreshMenuPath()
        {
            _menuPath = $"FuzzyBrain/Conditions/{_conditionName}";
        }

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            // After a domain reload the window survives but _previewEditor may wrap a
            // ScriptableObject whose script Unity now considers missing. Detect this by
            // checking whether the MonoScript reference is null — valid in-memory instances
            // always have a resolvable script, while post-reload missing-script objects do not.
            // NOTE: do NOT check editor type name ("GenericInspector") because that name is
            // also used for types with no custom editor, which would destroy valid previews
            // every frame and discard all unsaved property edits.
            if (_previewEditor != null && _previewInstance != null &&
                MonoScript.FromScriptableObject(_previewInstance) == null)
            {
                DestroyImmediate(_previewEditor);
                _previewEditor   = null;
                _previewInstance = null;
                RefreshPreviewInstance();
            }

            _activeTab = GUILayout.Toolbar(_activeTab,
                new[] { "Generate Script", "Create Asset", "Quick Condition" });

            EditorGUILayout.Space(8f);

            if (_activeTab == 0)
                DrawGenerateTab();
            else if (_activeTab == 1)
                DrawCreateAssetTab();
            else
                DrawQuickConditionTab();
        }

        // ── Tab 1: Generate Script ────────────────────────────────────────────────

        private void DrawGenerateTab()
        {
            EditorGUILayout.LabelField("New Condition Type", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();
            _conditionName = EditorGUILayout.TextField("Class Name", _conditionName);
            if (EditorGUI.EndChangeCheck())
                RefreshMenuPath();

            _componentIndex = EditorGUILayout.Popup("Component Type",
                _componentIndex, _componentNames.ToArray());

            _namespace = EditorGUILayout.TextField(
                new GUIContent("Namespace", "C# namespace for the generated class. Leave empty for global namespace."),
                _namespace);

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
            if (string.IsNullOrWhiteSpace(_conditionName))
                return "Class name cannot be empty.";
            if (!ValidIdentifier.IsMatch(_conditionName))
                return "Class name must be a valid C# identifier (letters, digits, underscores; start with a letter).";
            if (_componentTypes.Count == 0)
                return "No component types found. Make sure your project has compiled.";

            string path = Path.Combine(_scriptFolder, _conditionName + ".cs");
            if (File.Exists(path))
                return $"A file named '{_conditionName}.cs' already exists in the selected folder.";

            return null;
        }

        private void GenerateScript()
        {
            if (_componentTypes.Count == 0) return;
            Type   componentType = _componentTypes[_componentIndex];
            string typeName      = componentType.Name;

            string compNs    = componentType.Namespace;
            string compUsing = (!string.IsNullOrEmpty(compNs) && compNs != "UnityEngine")
                ? $"using {compNs};\n"
                : string.Empty;

            bool   hasNamespace = !string.IsNullOrWhiteSpace(_namespace);
            string indent       = hasNamespace ? "    " : string.Empty;

            string classBody =
$@"{indent}[CreateAssetMenu(fileName = ""{_conditionName}"", menuName = ""{_menuPath}"")]
{indent}public class {_conditionName} : Condition<{typeName}>
{indent}{{
{indent}    protected override bool Verify({typeName} component)
{indent}    {{
{indent}        // TODO: implement condition logic
{indent}        bool result = false;
{indent}        return inverted ? !result : result;
{indent}    }}
{indent}}}";

            string template = hasNamespace
                ? $"using UnityEngine;\nusing FuzzyBrain;\n{compUsing}\nnamespace {_namespace}\n{{\n{classBody}\n}}\n"
                : $"using UnityEngine;\nusing FuzzyBrain;\n{compUsing}\n{classBody}\n";
            if (!Directory.Exists(_scriptFolder))
                Directory.CreateDirectory(_scriptFolder);

            string filePath = Path.Combine(_scriptFolder, _conditionName + ".cs");
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
            Debug.Log($"[FuzzyBrain] Generated: {filePath}");
        }

        // ── Tab 2: Create Asset ───────────────────────────────────────────────────

        private void DrawCreateAssetTab()
        {
            EditorGUILayout.LabelField("Create Condition Asset", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (_conditionTypes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No concrete Condition types found. Generate a script first and let Unity compile.",
                    MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _conditionTypeIndex = EditorGUILayout.Popup("Condition Type",
                _conditionTypeIndex, _conditionNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                _assetName = _conditionTypes[_conditionTypeIndex].Name;
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

            // ── Properties preview ────────────────────────────────────────────────
            // Guard against Unity fake-null: after CreateAsset transfers ownership,
            // _previewInstance becomes a destroyed object that passes C# null checks.
            bool previewValid = !ReferenceEquals(_previewEditor, null)
                             && !ReferenceEquals(_previewInstance, null)
                             && _previewEditor != null
                             && _previewInstance != null;

            if (previewValid)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

                _previewScroll = EditorGUILayout.BeginScrollView(
                    _previewScroll, GUILayout.MaxHeight(200f));

                try
                {
                    _previewEditor.serializedObject.Update();
                    _previewEditor.OnInspectorGUI();
                    _previewEditor.serializedObject.ApplyModifiedProperties();
                }
                catch (Exception)
                {
                    // Absorb transient exceptions (e.g. SerializedObjectNotCreatableException
                    // during domain reload) so the scroll view End is always reached.
                    _previewEditor   = null;
                    _previewInstance = null;
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Create Asset", GUILayout.Height(28f)))
                CreateConditionAsset();
        }

        private void RefreshPreviewInstance()
        {
            if (_previewEditor != null)
            {
                DestroyImmediate(_previewEditor);
                _previewEditor = null;
            }
            if (_previewInstance != null)
            {
                DestroyImmediate(_previewInstance);
                _previewInstance = null;
            }

            if (_conditionTypes.Count == 0) return;

            // Clamp index in case the list changed since the last draw (e.g. after recompile).
            _conditionTypeIndex = Mathf.Clamp(_conditionTypeIndex, 0, _conditionTypes.Count - 1);

            _previewInstance = CreateInstance(_conditionTypes[_conditionTypeIndex]);
            if (_previewInstance == null) return;

            _previewInstance.name = _assetName;
            _previewEditor        = UnityEditor.Editor.CreateEditor(_previewInstance);
        }

        private void CreateConditionAsset()
        {
            if (_previewInstance == null) return;

            if (!Directory.Exists(_assetFolder))
                Directory.CreateDirectory(_assetFolder);

            _previewInstance.name = _assetName;

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(_assetFolder, _assetName + ".asset"));

            _previewEditor?.serializedObject?.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(_previewInstance, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_previewInstance);

            Debug.Log($"[FuzzyBrain] Created condition asset: {assetPath}");

            // Null both references explicitly before refreshing — CreateAsset transfers
            // ownership of _previewInstance to the AssetDatabase, making the in-memory
            // object invalid for further editor use. Clearing here prevents the stale
            // editor from calling OnInspectorGUI on a destroyed serialized object.
            _previewInstance = null;
            _previewEditor   = null;
            PopulateConditionTypes();
            RefreshPreviewInstance();
        }

        // ── Tab 3: Quick Condition ────────────────────────────────────────────────

        private void RefreshQuickMemberList(Type componentType)
        {
            _quickMembers.Clear();
            _quickMemberNames.Clear();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (FieldInfo fi in componentType.GetFields(flags))
            {
                if (fi.Name.StartsWith("<") || fi.Name.StartsWith("m_")) continue;
                if (!SupportedMemberTypes.Contains(fi.FieldType)) continue;
                _quickMembers.Add(fi);
                _quickMemberNames.Add(fi.Name);
            }

            foreach (PropertyInfo pi in componentType.GetProperties(flags))
            {
                if (!pi.CanRead) continue;
                if (pi.Name.StartsWith("<") || pi.Name.StartsWith("m_")) continue;
                if (!SupportedMemberTypes.Contains(pi.PropertyType)) continue;
                _quickMembers.Add(pi);
                _quickMemberNames.Add(pi.Name);
            }

            foreach (MethodInfo mi in componentType.GetMethods(flags))
            {
                if (mi.IsSpecialName) continue;
                if (mi.GetParameters().Length != 0) continue;
                if (!SupportedMemberTypes.Contains(mi.ReturnType)) continue;
                _quickMembers.Add(mi);
                _quickMemberNames.Add(mi.Name);
            }

            _quickLeftIndex  = 0;
            _quickRightIndex = 0;
            _quickOpIndex    = 0;
        }

        private void RefreshQuickRHSMemberList(Type componentType)
        {
            _quickRHSMembers.Clear();
            _quickRHSMemberNames.Clear();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (FieldInfo fi in componentType.GetFields(flags))
            {
                if (fi.Name.StartsWith("<") || fi.Name.StartsWith("m_")) continue;
                if (!SupportedMemberTypes.Contains(fi.FieldType)) continue;
                _quickRHSMembers.Add(fi);
                _quickRHSMemberNames.Add(fi.Name);
            }

            foreach (PropertyInfo pi in componentType.GetProperties(flags))
            {
                if (!pi.CanRead) continue;
                if (pi.Name.StartsWith("<") || pi.Name.StartsWith("m_")) continue;
                if (!SupportedMemberTypes.Contains(pi.PropertyType)) continue;
                _quickRHSMembers.Add(pi);
                _quickRHSMemberNames.Add(pi.Name);
            }

            foreach (MethodInfo mi in componentType.GetMethods(flags))
            {
                if (mi.IsSpecialName) continue;
                if (mi.GetParameters().Length != 0) continue;
                if (!SupportedMemberTypes.Contains(mi.ReturnType)) continue;
                _quickRHSMembers.Add(mi);
                _quickRHSMemberNames.Add(mi.Name);
            }

            _quickRightIndex = 0;
        }

        private static Type GetMemberType(MemberInfo m) =>
            m is FieldInfo fi ? fi.FieldType :
            m is PropertyInfo pi ? pi.PropertyType :
            ((MethodInfo)m).ReturnType;

        private static string GetMemberAccess(MemberInfo m, string componentExpr) =>
            m is MethodInfo ? $"{componentExpr}.{m.Name}()" : $"{componentExpr}.{m.Name}";

        private static string[] GetOperatorsForMember(MemberInfo m) =>
            GetMemberType(m) == typeof(bool) ? BoolOnlyOperators : AllOperators;

        /// <summary>
        /// Consumes any keydown event whose character is not valid for a numeric field,
        /// preventing it from reaching the text buffer of the focused control.
        /// Must be called before drawing the control, while the correct control is focused.
        /// </summary>
        private static void FilterNumericInput(bool allowDecimal)
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;

            char c = e.character;
            if (c == 0)    return;  // non-printable / special key (arrows, F-keys, etc.)
            if (c == '\b') return;  // backspace
            if (c == '\t' || c == '\n' || c == '\r') return;  // tab / confirm
            if (char.IsDigit(c)) return;
            if (c == '-')  return;
            if (allowDecimal && c == '.') return;

            e.Use();
        }

        private string DrawLiteralField(Type t)
        {
            if (t == typeof(bool))
                return "true";
            if (t == typeof(int))
            {
                if (GUI.GetNameOfFocusedControl() == LiteralIntControlName)
                    FilterNumericInput(false);
                GUI.SetNextControlName(LiteralIntControlName);
                _quickLiteralInt = EditorGUILayout.IntField("Value", _quickLiteralInt);
                return _quickLiteralInt.ToString();
            }
            if (t == typeof(float) || t == typeof(double))
            {
                if (GUI.GetNameOfFocusedControl() == LiteralFloatControlName)
                    FilterNumericInput(true);
                GUI.SetNextControlName(LiteralFloatControlName);
                _quickLiteralFloat = EditorGUILayout.FloatField("Value", _quickLiteralFloat);
                return $"{_quickLiteralFloat}f";
            }
            // string
            _quickLiteralString = EditorGUILayout.TextField("Value", _quickLiteralString);
            return $"\"{_quickLiteralString}\"";
        }

        private string GetCurrentLiteral(Type t)
        {
            if (t == typeof(bool))                        return "true";
            if (t == typeof(int))                         return _quickLiteralInt.ToString();
            if (t == typeof(float) || t == typeof(double)) return $"{_quickLiteralFloat}f";
            return $"\"{_quickLiteralString}\"";
        }

        private string BuildPreview()
        {
            if (_quickMembers.Count == 0) return "—";

            int        safeLeft   = Mathf.Clamp(_quickLeftIndex,  0, _quickMembers.Count - 1);
            int        safeRight  = Mathf.Clamp(_quickRightIndex, 0, _quickMembers.Count - 1);
            MemberInfo leftMember = _quickMembers[safeLeft];
            string[]   ops        = GetOperatorsForMember(leftMember);
            string     op         = ops[Mathf.Clamp(_quickOpIndex, 0, ops.Length - 1)];

            string lhs = GetMemberAccess(leftMember, "component");
            string rhs = _quickRHSIsField
                ? GetMemberAccess(_quickRHSMembers[Mathf.Clamp(_quickRightIndex, 0, _quickRHSMembers.Count - 1)], "rhsComponent")
                : GetCurrentLiteral(GetMemberType(leftMember));

            return $"{lhs} {op} {rhs}";
        }

        private string ValidateQuickScript()
        {
            if (string.IsNullOrWhiteSpace(_quickClassName))
                return "Class name cannot be empty.";
            if (!ValidIdentifier.IsMatch(_quickClassName))
                return "Class name must be a valid C# identifier.";
            if (_quickMembers.Count == 0)
                return "No supported members found on the selected component.";

            string path = Path.Combine(_quickScriptFolder, _quickClassName + ".cs");
            if (File.Exists(path))
                return $"'{_quickClassName}.cs' already exists in the output folder.";

            return null;
        }

        private void DrawQuickConditionTab()
        {
            EditorGUILayout.LabelField("Quick Condition Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (_componentTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No component types found. Ensure the project has compiled.", MessageType.Warning);
                return;
            }

            // Component picker
            EditorGUI.BeginChangeCheck();
            _quickComponentIndex = EditorGUILayout.Popup(
                "Component Type", _quickComponentIndex, _componentNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                RefreshQuickMemberList(_componentTypes[_quickComponentIndex]);
                _quickClassName = _componentTypes[_quickComponentIndex].Name + "Condition";
            }

            if (_quickMemberNames.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No supported public fields, properties, or methods found on this type (bool, int, float, double, string).",
                    MessageType.Info);
                return;
            }

            // Left-hand field
            EditorGUI.BeginChangeCheck();
            _quickLeftIndex = EditorGUILayout.Popup("Left Field", _quickLeftIndex, _quickMemberNames.ToArray());
            if (EditorGUI.EndChangeCheck())
                _quickOpIndex = 0;

            // Operator (filtered by field type)
            string[] ops = GetOperatorsForMember(_quickMembers[_quickLeftIndex]);
            _quickOpIndex = EditorGUILayout.Popup("Operator", _quickOpIndex, ops);

            // Right-hand side
            _quickRHSIsField = EditorGUILayout.Toggle("Use Field (RHS)", _quickRHSIsField);
            if (_quickRHSIsField)
            {
                // RHS component picker
                EditorGUI.BeginChangeCheck();
                _quickRHSComponentIndex = EditorGUILayout.Popup(
                    "RHS Component", _quickRHSComponentIndex, _componentNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                    RefreshQuickRHSMemberList(_componentTypes[_quickRHSComponentIndex]);

                // Only show fields whose type matches the LHS to prevent invalid comparisons
                Type lhsType = GetMemberType(_quickMembers[_quickLeftIndex]);
                var rhsFiltered = _quickRHSMembers
                    .Select((m, i) => (member: m, name: _quickRHSMemberNames[i], originalIndex: i))
                    .Where(x => GetMemberType(x.member) == lhsType)
                    .ToList();

                if (rhsFiltered.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        $"No fields of type '{lhsType.Name}' found on {_componentTypes[_quickRHSComponentIndex].Name}.",
                        MessageType.Info);
                }
                else
                {
                    string[] rhsNames    = rhsFiltered.Select(x => x.name).ToArray();
                    int      filteredIdx = rhsFiltered.FindIndex(x => x.originalIndex == _quickRightIndex);
                    if (filteredIdx < 0) filteredIdx = 0;

                    filteredIdx      = EditorGUILayout.Popup("Right Field", filteredIdx, rhsNames);
                    _quickRightIndex = rhsFiltered[filteredIdx].originalIndex;
                }
            }
            else
            {
                Type lhsType = GetMemberType(_quickMembers[_quickLeftIndex]);
                DrawLiteralField(lhsType);
            }

            // Separator
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2f);

            // Output
            _quickClassName = EditorGUILayout.TextField("Class Name", _quickClassName);
            using (new EditorGUILayout.HorizontalScope())
            {
                _quickScriptFolder = EditorGUILayout.TextField("Scripts Folder", _quickScriptFolder);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                {
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select Script Output Folder", _quickScriptFolder, "");
                    if (!string.IsNullOrEmpty(picked))
                        _quickScriptFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                }
            }

            // Preview
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox($"Verify: {BuildPreview()}", MessageType.None);
            EditorGUILayout.Space(4f);

            // Validation + Generate button
            string error = ValidateQuickScript();
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Generate Script", GUILayout.Height(28f)))
                GenerateQuickScript();

            GUI.enabled = true;
        }

        private void GenerateQuickScript()
        {
            Type   lhsType  = _componentTypes[_quickComponentIndex];
            string lhsName  = lhsType.Name;

            int        safeLeft  = Mathf.Clamp(_quickLeftIndex, 0, _quickMembers.Count - 1);
            string     leftName  = _quickMemberNames[safeLeft];
            MemberInfo leftMember = _quickMembers[safeLeft];
            string[]   ops        = GetOperatorsForMember(leftMember);
            string     op         = ops[Mathf.Clamp(_quickOpIndex, 0, ops.Length - 1)];

            string rhsExpr;
            string rhsFetch   = string.Empty;
            string rhsNsUsing = string.Empty;

            if (_quickRHSIsField && _quickRHSMembers.Count > 0)
            {
                Type   rhsType     = _componentTypes[_quickRHSComponentIndex];
                string rhsTypeName = rhsType.Name;
                int    safeRight   = Mathf.Clamp(_quickRightIndex, 0, _quickRHSMembers.Count - 1);
                string rightName   = _quickRHSMemberNames[safeRight];
                MemberInfo rightMember = _quickRHSMembers[safeRight];

                if (rhsType == lhsType)
                {
                    // Same component type — reuse the parameter directly, no extra fetch needed
                    rhsExpr  = GetMemberAccess(rightMember, "component");
                }
                else
                {
                    rhsExpr  = GetMemberAccess(rightMember, "rhs");
                    rhsFetch = $"        var rhs = component.gameObject.GetComponent<{rhsTypeName}>();\n" +
                               $"        if (rhs == null) return false;\n";
                }

                string rhsNs       = rhsType.Namespace;
                string lhsNsInner  = lhsType.Namespace;
                if (!string.IsNullOrEmpty(rhsNs) && rhsNs != lhsNsInner && rhsNs != "UnityEngine")
                    rhsNsUsing = $"using {rhsNs};\n";
            }
            else
            {
                rhsExpr = GetCurrentLiteral(GetMemberType(leftMember));
            }

            string lhsNs      = lhsType.Namespace;
            string lhsNsUsing = (!string.IsNullOrEmpty(lhsNs) && lhsNs != "UnityEngine")
                ? $"using {lhsNs};\n"
                : string.Empty;

            string leftAccess = GetMemberAccess(leftMember, "component");

            string template =
$@"using UnityEngine;
using FuzzyBrain;
{lhsNsUsing}{rhsNsUsing}
[CreateAssetMenu(fileName = ""{_quickClassName}"", menuName = ""FuzzyBrain/Conditions/{_quickClassName}"")]
public class {_quickClassName} : Condition<{lhsName}>
{{
    protected override bool Verify({lhsName} component)
    {{
{rhsFetch}        bool result = {leftAccess} {op} {rhsExpr};
        return inverted ? !result : result;
    }}
}}
";
            if (!Directory.Exists(_quickScriptFolder))
                Directory.CreateDirectory(_quickScriptFolder);

            string filePath = Path.Combine(_quickScriptFolder, _quickClassName + ".cs");
            File.WriteAllText(filePath, template);
            AssetDatabase.Refresh();

            string[] lines      = template.Split('\n');
            int      resultLine = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("bool result")) { resultLine = i + 1; break; }
            }

            string fullPath = Path.GetFullPath(filePath);
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, resultLine, 0);
            Debug.Log($"[FuzzyBrain] Generated: {filePath}");
        }
    }
}