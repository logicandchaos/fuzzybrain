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
    /// Three-tab wizard for creating Act types and assets.
    /// Tab 1 — Generate Script: writes an Act subclass .cs file and opens it in the IDE.
    /// Tab 2 — Create Asset:    creates a ScriptableObject instance from an existing Act type.
    /// Tab 3 — Quick Act:       generates a compiled Act with a component method call baked into PerformAct.
    /// Open via Tools > FuzzyBrain > New Act, or from the FuzzyBrainWindow.
    /// </summary>
    public class ActWizard : EditorWindow
    {
        private static readonly Regex ValidIdentifier = new Regex(@"^[A-Za-z][A-Za-z0-9_]*$");

        // ── Tab 1 state ───────────────────────────────────────────────────────────

        private string _className    = "MyAct";
        private string _scriptFolder;
        private string _namespace;
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

        // ── Tab 3 state (Quick Act) ───────────────────────────────────────────────

        private const string QuickActLiteralIntControlName   = "FBActLiteralInt";
        private const string QuickActLiteralFloatControlName = "FBActLiteralFloat";

        private static readonly HashSet<Type> ExcludedActMethodTypes = new HashSet<Type>
        {
            typeof(object),
            typeof(UnityEngine.Object),
            typeof(Component),
            typeof(Behaviour),
            typeof(MonoBehaviour)
        };

        private List<Type>       _quickActComponentTypes    = new List<Type>();
        private List<string>     _quickActComponentNames    = new List<string>();
        private int              _quickActComponentIndex;

        private List<MethodInfo> _quickActMethods           = new List<MethodInfo>();
        private List<string>     _quickActMethodNames       = new List<string>();
        private int              _quickActMethodIndex;

        // Argument state (1-param methods)
        private bool             _quickActArgRHSIsField;
        private int              _quickActArgRHSComponentIndex;
        private List<MemberInfo> _quickActArgRHSMembers     = new List<MemberInfo>();
        private List<string>     _quickActArgRHSMemberNames = new List<string>();
        private int              _quickActArgRHSMemberIndex;

        private bool             _quickActLiteralBool;
        private int              _quickActLiteralInt;
        private float            _quickActLiteralFloat;
        private string           _quickActLiteralString     = string.Empty;
        private int              _quickActLiteralEnumIndex;

        private string           _quickActClassName         = "MyAct";
        private string           _quickActScriptFolder;

        // ── Shared ────────────────────────────────────────────────────────────────

        private int _activeTab;

        // ── Menu entry ────────────────────────────────────────────────────────────

        [MenuItem("Tools/FuzzyBrain/Act Wizard", priority = 12)]
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
            _namespace    = settings.defaultNamespace;

            PopulateActTypes();
            RefreshMenuPath();
            RefreshPreviewInstance();

            _quickActScriptFolder = settings.quickActScriptsFolder;
            PopulateQuickActComponentTypes();
            if (_quickActComponentTypes.Count > 0)
            {
                RefreshQuickActMethodList();
                RefreshQuickActArgRHSMemberList(_quickActComponentTypes[_quickActArgRHSComponentIndex]);
            }
        }

        private void OnDestroy()
        {
            var settings = FuzzyBrainSettings.GetOrCreate();
            settings.actScriptsFolder      = _scriptFolder;
            settings.actAssetsFolder       = _assetFolder;
            settings.defaultNamespace      = _namespace;
            settings.quickActScriptsFolder = _quickActScriptFolder;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

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
            _activeTab = GUILayout.Toolbar(_activeTab, new[] { "Generate Script", "Create Asset", "Quick Act" });
            EditorGUILayout.Space(8f);

            if      (_activeTab == 0) DrawGenerateTab();
            else if (_activeTab == 1) DrawCreateAssetTab();
            else                      DrawQuickActTab();
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
            bool hasNamespace = !string.IsNullOrWhiteSpace(_namespace);
            string indent     = hasNamespace ? "    " : string.Empty;

            string classBody =
$@"{indent}[CreateAssetMenu(fileName = ""{_className}"", menuName = ""{_menuPath}"")]
{indent}public class {_className} : Act
{indent}{{
{indent}    public override void PerformAct(ActContext ctx)
{indent}    {{
{indent}        // TODO: implement act behaviour
{indent}    }}
{indent}}}";

            string template = hasNamespace
                ? $"using FuzzyBrain;\nusing UnityEngine;\n\nnamespace {_namespace}\n{{\n{classBody}\n}}\n"
                : $"using FuzzyBrain;\nusing UnityEngine;\n\n{classBody}\n";

            if (!Directory.Exists(_scriptFolder))
                Directory.CreateDirectory(_scriptFolder);

            string filePath = Path.Combine(_scriptFolder, _className + ".cs");
            File.WriteAllText(filePath, template);

            var settings = FuzzyBrainSettings.GetOrCreate();
            settings.actScriptsFolder = _scriptFolder;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

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

            // Recover preview instance if it was destroyed (e.g. after a domain reload).
            if (_previewInstance == null && _actTypes.Count > 0)
                RefreshPreviewInstance();

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
            if (_previewEditor != null && _previewInstance != null)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
                _previewScroll = EditorGUILayout.BeginScrollView(
                    _previewScroll, GUILayout.MaxHeight(200f));
                _previewEditor.serializedObject.Update();
                _previewEditor.OnInspectorGUI();
                _previewEditor.serializedObject.ApplyModifiedProperties();
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

            _previewEditor?.serializedObject?.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(_previewInstance, assetPath);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(_previewInstance);

            if (_addToCurrentList)
                FuzzyBrainWindow.TryAddActToCurrentList(_previewInstance as Act);

            Debug.Log($"[FuzzyBrain] Created act asset: {assetPath}");

            // Destroy the editor before nulling out — Editor inherits from UnityEngine.Object
            // and must be explicitly destroyed to avoid a leak.
            if (_previewEditor != null) { DestroyImmediate(_previewEditor); _previewEditor = null; }
            _previewInstance = null;
            RefreshPreviewInstance();
        }

        // ── Tab 3: Quick Act ──────────────────────────────────────────────────────

        private static bool IsQuickActArgSupported(Type t) =>
            t == typeof(bool)   || t == typeof(int)    || t == typeof(float) ||
            t == typeof(double) || t == typeof(string) || t.IsEnum;

        private static Type GetQuickActMemberType(MemberInfo m) =>
            m is FieldInfo fi    ? fi.FieldType :
            m is PropertyInfo pi ? pi.PropertyType :
            ((MethodInfo)m).ReturnType;

        private static string GetQuickActMemberAccess(MemberInfo m, string expr) =>
            m is MethodInfo ? $"{expr}.{m.Name}()" : $"{expr}.{m.Name}";

        private void PopulateQuickActComponentTypes()
        {
            _quickActComponentTypes.Clear();
            _quickActComponentNames.Clear();

            var unityTypes   = new List<(string name, Type type)>();
            var projectTypes = new List<(string name, Type type)>();

            foreach (Type t in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (t.IsAbstract || t.Name.StartsWith("<")) continue;
                string asm     = t.Assembly.GetName().Name;
                bool   isUnity = asm.StartsWith("UnityEngine") || asm.StartsWith("Unity.");
                if (isUnity) unityTypes.Add((t.Name, t));
                else         projectTypes.Add((t.Name, t));
            }

            unityTypes.Sort((a, b)   => string.Compare(a.name, b.name, StringComparison.Ordinal));
            projectTypes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            foreach (var (name, type) in unityTypes)
            {
                _quickActComponentTypes.Add(type);
                _quickActComponentNames.Add("Unity Components/" + name);
            }
            foreach (var (name, type) in projectTypes)
            {
                _quickActComponentTypes.Add(type);
                _quickActComponentNames.Add("Project Components/" + name);
            }
        }

        private void RefreshQuickActMethodList()
        {
            _quickActMethods.Clear();
            _quickActMethodNames.Clear();
            if (_quickActComponentTypes.Count == 0) return;

            Type componentType = _quickActComponentTypes[_quickActComponentIndex];
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (MethodInfo m in componentType.GetMethods(flags))
            {
                if (m.ReturnType != typeof(void)) continue;
                if (m.IsSpecialName) continue;
                if (ExcludedActMethodTypes.Contains(m.DeclaringType)) continue;

                ParameterInfo[] p = m.GetParameters();
                if (p.Length == 0)
                {
                    _quickActMethods.Add(m);
                    _quickActMethodNames.Add(m.Name + "()");
                }
                else if (p.Length == 1 && IsQuickActArgSupported(p[0].ParameterType))
                {
                    _quickActMethods.Add(m);
                    _quickActMethodNames.Add($"{m.Name}({p[0].ParameterType.Name})");
                }
            }

            _quickActMethodIndex = 0;
            UpdateQuickActClassName();
        }

        private void RefreshQuickActArgRHSMemberList(Type componentType)
        {
            _quickActArgRHSMembers.Clear();
            _quickActArgRHSMemberNames.Clear();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            foreach (FieldInfo fi in componentType.GetFields(flags))
            {
                if (fi.Name.StartsWith("<") || fi.Name.StartsWith("m_")) continue;
                if (!IsQuickActArgSupported(fi.FieldType)) continue;
                _quickActArgRHSMembers.Add(fi);
                _quickActArgRHSMemberNames.Add(fi.Name);
            }
            foreach (PropertyInfo pi in componentType.GetProperties(flags))
            {
                if (!pi.CanRead) continue;
                if (pi.Name.StartsWith("<") || pi.Name.StartsWith("m_")) continue;
                if (!IsQuickActArgSupported(pi.PropertyType)) continue;
                _quickActArgRHSMembers.Add(pi);
                _quickActArgRHSMemberNames.Add(pi.Name);
            }
            foreach (MethodInfo mi in componentType.GetMethods(flags))
            {
                if (mi.IsSpecialName) continue;
                if (mi.GetParameters().Length != 0) continue;
                if (!IsQuickActArgSupported(mi.ReturnType)) continue;
                _quickActArgRHSMembers.Add(mi);
                _quickActArgRHSMemberNames.Add(mi.Name);
            }

            _quickActArgRHSMemberIndex = 0;
        }

        private void UpdateQuickActClassName()
        {
            if (_quickActMethods.Count == 0) return;
            int safe = Mathf.Clamp(_quickActMethodIndex, 0, _quickActMethods.Count - 1);
            _quickActClassName = _quickActMethods[safe].Name + "Act";
        }

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
            if (c == 0 || c == '\b' || c == '\t' || c == '\n' || c == '\r') return;
            if (char.IsDigit(c) || c == '-') return;
            if (allowDecimal && c == '.') return;
            e.Use();
        }

        private string DrawQuickActLiteralField(Type t)
        {
            if (t == typeof(bool))
            {
                _quickActLiteralBool = EditorGUILayout.Toggle("Value", _quickActLiteralBool);
                return _quickActLiteralBool ? "true" : "false";
            }
            if (t == typeof(int))
            {
                if (GUI.GetNameOfFocusedControl() == QuickActLiteralIntControlName)
                    FilterNumericInput(false);
                GUI.SetNextControlName(QuickActLiteralIntControlName);
                _quickActLiteralInt = EditorGUILayout.IntField("Value", _quickActLiteralInt);
                return _quickActLiteralInt.ToString();
            }
            if (t == typeof(float) || t == typeof(double))
            {
                if (GUI.GetNameOfFocusedControl() == QuickActLiteralFloatControlName)
                    FilterNumericInput(true);
                GUI.SetNextControlName(QuickActLiteralFloatControlName);
                _quickActLiteralFloat = EditorGUILayout.FloatField("Value", _quickActLiteralFloat);
                return $"{_quickActLiteralFloat}f";
            }
            if (t.IsEnum)
            {
                string[] names = Enum.GetNames(t);
                if (names.Length == 0) return $"default({t.Name})";
                _quickActLiteralEnumIndex = Mathf.Clamp(_quickActLiteralEnumIndex, 0, names.Length - 1);
                _quickActLiteralEnumIndex = EditorGUILayout.Popup("Value", _quickActLiteralEnumIndex, names);
                return $"{t.Name}.{names[_quickActLiteralEnumIndex]}";
            }
            // string
            _quickActLiteralString = EditorGUILayout.TextField("Value", _quickActLiteralString);
            return $"\"{_quickActLiteralString}\"";
        }

        private string GetCurrentQuickActLiteral(Type t)
        {
            if (t == typeof(bool))                         return _quickActLiteralBool ? "true" : "false";
            if (t == typeof(int))                          return _quickActLiteralInt.ToString();
            if (t == typeof(float) || t == typeof(double)) return $"{_quickActLiteralFloat}f";
            if (t.IsEnum)
            {
                string[] names = Enum.GetNames(t);
                int safe = Mathf.Clamp(_quickActLiteralEnumIndex, 0, names.Length - 1);
                return $"{t.Name}.{names[safe]}";
            }
            return $"\"{_quickActLiteralString}\"";
        }

        private string BuildQuickActPreview()
        {
            if (_quickActMethods.Count == 0) return "—";

            MethodInfo      method   = _quickActMethods[_quickActMethodIndex];
            ParameterInfo[] pars     = method.GetParameters();
            string          typeName = _quickActComponentTypes[_quickActComponentIndex].Name;

            if (pars.Length == 0)
                return $"ctx.Get<{typeName}>().{method.Name}()";

            Type   paramType = pars[0].ParameterType;
            string argExpr;

            if (_quickActArgRHSIsField && _quickActArgRHSMembers.Count > 0)
            {
                var filtered = _quickActArgRHSMembers
                    .Select((m, i) => (member: m, idx: i))
                    .Where(x => GetQuickActMemberType(x.member) == paramType)
                    .ToList();

                if (filtered.Count == 0)
                {
                    argExpr = "?";
                }
                else
                {
                    int filteredIdx = filtered.FindIndex(x => x.idx == _quickActArgRHSMemberIndex);
                    if (filteredIdx < 0) filteredIdx = 0;
                    Type   rhsType = _quickActComponentTypes[_quickActArgRHSComponentIndex];
                    string srcExpr = rhsType == _quickActComponentTypes[_quickActComponentIndex]
                        ? "component"
                        : "rhs";
                    argExpr = GetQuickActMemberAccess(filtered[filteredIdx].member, srcExpr);
                }
            }
            else
            {
                argExpr = GetCurrentQuickActLiteral(paramType);
            }

            return $"ctx.Get<{typeName}>().{method.Name}({argExpr})";
        }

        private string ValidateQuickActScript()
        {
            if (string.IsNullOrWhiteSpace(_quickActClassName))
                return "Class name cannot be empty.";
            if (!ValidIdentifier.IsMatch(_quickActClassName))
                return "Class name must be a valid C# identifier.";
            if (_quickActMethods.Count == 0)
                return "No methods available on the selected component.";

            MethodInfo method = _quickActMethods[_quickActMethodIndex];
            if (method.GetParameters().Length == 1 && _quickActArgRHSIsField)
            {
                Type paramType = method.GetParameters()[0].ParameterType;
                if (!_quickActArgRHSMembers.Any(m => GetQuickActMemberType(m) == paramType))
                    return $"No members of type '{paramType.Name}' found on the selected source component.";
            }

            string path = Path.Combine(_quickActScriptFolder, _quickActClassName + ".cs");
            if (File.Exists(path))
                return $"'{_quickActClassName}.cs' already exists in the output folder.";

            return null;
        }

        private void DrawQuickActTab()
        {
            EditorGUILayout.LabelField("Quick Act Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            if (_quickActComponentTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No component types found. Ensure the project has compiled.", MessageType.Warning);
                return;
            }

            // Target component
            EditorGUI.BeginChangeCheck();
            _quickActComponentIndex = EditorGUILayout.Popup(
                "Component Type", _quickActComponentIndex, _quickActComponentNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                RefreshQuickActMethodList();
                RefreshQuickActArgRHSMemberList(_quickActComponentTypes[_quickActArgRHSComponentIndex]);
                _quickActClassName = _quickActComponentTypes[_quickActComponentIndex].Name + "Act";
            }

            if (_quickActMethods.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No public void methods with 0 or 1 supported parameter (bool, int, float, string, enum) found on this component.",
                    MessageType.Info);
                return;
            }

            // Method
            EditorGUI.BeginChangeCheck();
            _quickActMethodIndex = EditorGUILayout.Popup("Method", _quickActMethodIndex, _quickActMethodNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                UpdateQuickActClassName();
                _quickActArgRHSIsField    = false;
                _quickActLiteralInt       = 0;
                _quickActLiteralFloat     = 0f;
                _quickActLiteralBool      = false;
                _quickActLiteralString    = string.Empty;
                _quickActLiteralEnumIndex = 0;
            }

            // Argument UI — only shown for 1-param methods
            MethodInfo      currentMethod = _quickActMethods[_quickActMethodIndex];
            ParameterInfo[] methodParams  = currentMethod.GetParameters();

            if (methodParams.Length == 1)
            {
                ParameterInfo param     = methodParams[0];
                Type          paramType = param.ParameterType;

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField($"Argument  ·  {param.Name} : {paramType.Name}", EditorStyles.miniLabel);
                EditorGUILayout.Space(2f);

                EditorGUI.BeginChangeCheck();
                _quickActArgRHSIsField = EditorGUILayout.Toggle("Use Component Value", _quickActArgRHSIsField);
                if (EditorGUI.EndChangeCheck() && _quickActArgRHSIsField)
                    RefreshQuickActArgRHSMemberList(_quickActComponentTypes[_quickActArgRHSComponentIndex]);

                if (_quickActArgRHSIsField)
                {
                    EditorGUI.BeginChangeCheck();
                    _quickActArgRHSComponentIndex = EditorGUILayout.Popup(
                        "Source Component", _quickActArgRHSComponentIndex, _quickActComponentNames.ToArray());
                    if (EditorGUI.EndChangeCheck())
                        RefreshQuickActArgRHSMemberList(_quickActComponentTypes[_quickActArgRHSComponentIndex]);

                    var filtered = _quickActArgRHSMembers
                        .Select((m, i) => (member: m, name: _quickActArgRHSMemberNames[i], idx: i))
                        .Where(x => GetQuickActMemberType(x.member) == paramType)
                        .ToList();

                    if (filtered.Count == 0)
                    {
                        EditorGUILayout.HelpBox(
                            $"No members of type '{paramType.Name}' found on " +
                            $"{_quickActComponentTypes[_quickActArgRHSComponentIndex].Name}.",
                            MessageType.Info);
                    }
                    else
                    {
                        string[] rhsNames    = filtered.Select(x => x.name).ToArray();
                        int      filteredIdx = filtered.FindIndex(x => x.idx == _quickActArgRHSMemberIndex);
                        if (filteredIdx < 0) filteredIdx = 0;
                        filteredIdx                = EditorGUILayout.Popup("Source Member", filteredIdx, rhsNames);
                        _quickActArgRHSMemberIndex = filtered[filteredIdx].idx;
                    }
                }
                else
                {
                    DrawQuickActLiteralField(paramType);
                }
            }

            // Separator + output settings
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
            EditorGUILayout.Space(2f);

            _quickActClassName = EditorGUILayout.TextField("Class Name", _quickActClassName);
            _namespace = EditorGUILayout.TextField(
                new GUIContent("Namespace", "C# namespace for the generated class. Leave empty for global namespace."),
                _namespace);

            using (new EditorGUILayout.HorizontalScope())
            {
                _quickActScriptFolder = EditorGUILayout.TextField("Scripts Folder", _quickActScriptFolder);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                {
                    string picked = EditorUtility.OpenFolderPanel(
                        "Select Script Output Folder", _quickActScriptFolder, "");
                    if (!string.IsNullOrEmpty(picked))
                        _quickActScriptFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox($"PerformAct: {BuildQuickActPreview()}", MessageType.None);
            EditorGUILayout.Space(4f);

            string error = ValidateQuickActScript();
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
                GUI.enabled = false;
            }

            if (GUILayout.Button("Generate Script", GUILayout.Height(28f)))
                GenerateQuickActScript();

            GUI.enabled = true;
        }

        private void GenerateQuickActScript()
        {
            Type       targetType     = _quickActComponentTypes[_quickActComponentIndex];
            string     targetTypeName = targetType.Name;
            MethodInfo method         = _quickActMethods[_quickActMethodIndex];
            ParameterInfo[] pars      = method.GetParameters();

            bool   hasNamespace = !string.IsNullOrWhiteSpace(_namespace);
            string indent       = hasNamespace ? "    " : string.Empty;
            string bodyIndent   = indent + "        ";
            string menuPath     = $"FuzzyBrain/Acts/{_quickActClassName}";

            string argExpr    = string.Empty;
            string argFetch   = string.Empty;
            string extraUsing = string.Empty;

            if (pars.Length == 1)
            {
                Type paramType = pars[0].ParameterType;

                if (_quickActArgRHSIsField && _quickActArgRHSMembers.Count > 0)
                {
                    var filtered = _quickActArgRHSMembers
                        .Select((m, i) => (member: m, idx: i))
                        .Where(x => GetQuickActMemberType(x.member) == paramType)
                        .ToList();

                    int        filteredIdx = Mathf.Max(0, filtered.FindIndex(x => x.idx == _quickActArgRHSMemberIndex));
                    MemberInfo rhsMember   = filtered[filteredIdx].member;
                    Type       rhsType     = _quickActComponentTypes[_quickActArgRHSComponentIndex];

                    if (rhsType == targetType)
                    {
                        // Same component — reuse the already-fetched variable
                        argExpr = GetQuickActMemberAccess(rhsMember, "component");
                    }
                    else
                    {
                        string rhsTypeName = rhsType.Name;
                        argExpr  = GetQuickActMemberAccess(rhsMember, "rhs");
                        argFetch = $"\n{bodyIndent}var rhs = ctx.Get<{rhsTypeName}>();\n{bodyIndent}if (rhs == null) return;";

                        string rhsNs = rhsType.Namespace;
                        if (!string.IsNullOrEmpty(rhsNs) &&
                            rhsNs != targetType.Namespace &&
                            rhsNs != "UnityEngine" &&
                            rhsNs != "FuzzyBrain")
                            extraUsing = $"using {rhsNs};\n";
                    }
                }
                else
                {
                    argExpr = GetCurrentQuickActLiteral(paramType);

                    if (paramType.IsEnum)
                    {
                        string enumNs = paramType.Namespace;
                        if (!string.IsNullOrEmpty(enumNs) &&
                            enumNs != targetType.Namespace &&
                            enumNs != "UnityEngine" &&
                            enumNs != "FuzzyBrain")
                            extraUsing = $"using {enumNs};\n";
                    }
                }
            }

            string targetNs      = targetType.Namespace;
            string targetNsUsing = (!string.IsNullOrEmpty(targetNs) &&
                                    targetNs != "UnityEngine" &&
                                    targetNs != "FuzzyBrain")
                ? $"using {targetNs};\n"
                : string.Empty;

            string classBody =
$@"{indent}[CreateAssetMenu(fileName = ""{_quickActClassName}"", menuName = ""{menuPath}"")]
{indent}public class {_quickActClassName} : Act
{indent}{{
{indent}    public override void PerformAct(ActContext ctx)
{indent}    {{
{bodyIndent}var component = ctx.Get<{targetTypeName}>();
{bodyIndent}if (component == null) return;{argFetch}
{bodyIndent}component.{method.Name}({argExpr});
{indent}    }}
{indent}}}";

            string template = hasNamespace
                ? $"using FuzzyBrain;\nusing UnityEngine;\n{targetNsUsing}{extraUsing}\nnamespace {_namespace}\n{{\n{classBody}\n}}\n"
                : $"using FuzzyBrain;\nusing UnityEngine;\n{targetNsUsing}{extraUsing}\n{classBody}\n";

            if (!Directory.Exists(_quickActScriptFolder))
                Directory.CreateDirectory(_quickActScriptFolder);

            string filePath = Path.Combine(_quickActScriptFolder, _quickActClassName + ".cs");
            File.WriteAllText(filePath, template);

            var settings = FuzzyBrainSettings.GetOrCreate();
            settings.quickActScriptsFolder = _quickActScriptFolder;
            settings.defaultNamespace      = _namespace;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string[] lines    = template.Split('\n');
            int      callLine = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains($"component.{method.Name}(")) { callLine = i + 1; break; }
            }

            string fullPath = Path.GetFullPath(filePath);
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, callLine, 0);
            Debug.Log($"[FuzzyBrain] Generated quick act script: {filePath}");
        }
    }
}
