using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Adds FuzzyBrain script templates to the Project window right-click Create menu.
    /// Items appear under Assets > Create > FuzzyBrain.
    /// Uses the same script templates as the Act Wizard and Condition Wizard Generate Script tabs.
    /// </summary>
    internal static class FuzzyBrainProjectMenuItems
    {
        [MenuItem("Assets/Create/FuzzyBrain/Act Script", priority = 81)]
        private static void CreateActScript()
        {
            string folder    = GetSelectedFolderPath();
            var    settings  = FuzzyBrainSettings.GetOrCreate();

            string className = EditorInputDialog.Show("New Act Script", "Class Name", "MyAct");
            if (string.IsNullOrWhiteSpace(className)) return;

            string filePath = Path.Combine(folder, className + ".cs");
            if (File.Exists(filePath))
            {
                EditorUtility.DisplayDialog(
                    "File Already Exists",
                    $"'{className}.cs' already exists in:\n{folder}",
                    "OK");
                return;
            }

            WriteActScript(className, folder, settings.defaultNamespace);
        }

        [MenuItem("Assets/Create/FuzzyBrain/Condition Script", priority = 82)]
        private static void CreateConditionScript()
        {
            string folder = GetSelectedFolderPath();
            CreateConditionScriptDialog.Show(folder);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>Returns the folder path of the currently selected asset or folder.</summary>
        private static string GetSelectedFolderPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
                return "Assets";

            return Directory.Exists(path)
                ? path
                : Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
        }

        // ── Template writers ──────────────────────────────────────────────────────

        /// <summary>Writes an Act subclass script to disk using the same template as the Act Wizard.</summary>
        internal static void WriteActScript(string className, string folder, string ns)
        {
            string menuPath      = $"FuzzyBrain/Acts/{className}";
            bool   hasNamespace  = !string.IsNullOrWhiteSpace(ns);
            string indent        = hasNamespace ? "    " : string.Empty;

            string classBody =
$@"{indent}[CreateAssetMenu(fileName = ""{className}"", menuName = ""{menuPath}"")]
{indent}public class {className} : Act
{indent}{{
{indent}    public override void PerformAct(ActContext ctx)
{indent}    {{
{indent}        // TODO: implement act behaviour
{indent}    }}
{indent}}}";

            string template = hasNamespace
                ? $"using FuzzyBrain;\nusing UnityEngine;\n\nnamespace {ns}\n{{\n{classBody}\n}}\n"
                : $"using FuzzyBrain;\nusing UnityEngine;\n\n{classBody}\n";

            WriteAndOpen(template, folder, className);
            Debug.Log($"[FuzzyBrain] Generated act script: {Path.Combine(folder, className + ".cs")}");
        }

        /// <summary>Writes a Condition&lt;T&gt; subclass script to disk using the same template as the Condition Wizard.</summary>
        internal static void WriteConditionScript(
            string className, Type componentType, string folder, string ns)
        {
            string componentTypeName = componentType.Name;
            string menuPath     = $"FuzzyBrain/Conditions/{className}";
            bool   hasNamespace = !string.IsNullOrWhiteSpace(ns);
            string indent       = hasNamespace ? "    " : string.Empty;

            string classBody =
$@"{indent}[CreateAssetMenu(fileName = ""{className}"", menuName = ""{menuPath}"")]
{indent}public class {className} : Condition<{componentTypeName}>
{indent}{{
{indent}    protected override bool Verify({componentTypeName} component)
{indent}    {{
{indent}        // TODO: implement condition logic
{indent}        bool result = false;
{indent}        return inverted ? !result : result;
{indent}    }}
{indent}}}";

            string compNs    = componentType.Namespace;
            string compUsing = (!string.IsNullOrEmpty(compNs) && compNs != "UnityEngine")
                ? $"using {compNs};\n"
                : string.Empty;

            string template = hasNamespace
                ? $"using UnityEngine;\nusing FuzzyBrain;\n{compUsing}\nnamespace {ns}\n{{\n{classBody}\n}}\n"
                : $"using UnityEngine;\nusing FuzzyBrain;\n{compUsing}\n{classBody}\n";

            WriteAndOpen(template, folder, className);
            Debug.Log($"[FuzzyBrain] Generated condition script: {Path.Combine(folder, className + ".cs")}");
        }

        private static void WriteAndOpen(string template, string folder, string className)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, className + ".cs");
            File.WriteAllText(filePath, template);
            AssetDatabase.Refresh();

            // Open the new file at the TODO line, matching wizard behaviour.
            string[] lines    = template.Split('\n');
            int      todoLine = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("TODO")) { todoLine = i + 1; break; }
            }

            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                Path.GetFullPath(filePath), todoLine, 0);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Modal dialog — collects class name + component type for Condition scripts.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Modal EditorWindow that prompts for a class name and component type,
    /// then delegates to <see cref="FuzzyBrainProjectMenuItems.WriteConditionScript"/>.
    /// </summary>
    internal class CreateConditionScriptDialog : EditorWindow
    {
        private static readonly Regex ValidIdentifier =
            new Regex(@"^[A-Za-z][A-Za-z0-9_]*$");

        private string       _outputFolder;
        private string       _className = "MyCondition";
        private int          _componentIndex;
        private List<Type>   _componentTypes = new List<Type>();
        private List<string> _componentNames = new List<string>();

        private const float WindowWidth  = 400f;
        private const float WindowHeight = 190f;

        /// <summary>Opens the dialog as a modal utility window.</summary>
        public static void Show(string outputFolder)
        {
            var window = CreateInstance<CreateConditionScriptDialog>();
            window.titleContent  = new GUIContent("New Condition Script");
            window._outputFolder = outputFolder;
            window.minSize       = window.maxSize = new Vector2(WindowWidth, WindowHeight);
            window.PopulateComponentTypes();
            FuzzyBrainEditorUtils.SetWindowIcon(window);
            window.ShowModalUtility();
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
                string asmName  = t.Assembly.GetName().Name;
                bool   isUnity  = asmName.StartsWith("UnityEngine") || asmName.StartsWith("Unity.");

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

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);

            GUI.SetNextControlName("classNameField");
            _className = EditorGUILayout.TextField("Class Name", _className);
            EditorGUI.FocusTextInControl("classNameField");

            EditorGUILayout.Space(2f);
            _componentIndex = EditorGUILayout.Popup(
                "Component Type", _componentIndex, _componentNames.ToArray());

            EditorGUILayout.Space(8f);

            string error = GetValidationError();
            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Warning);

            EditorGUILayout.Space(6f);

            bool canCreate     = string.IsNullOrEmpty(error);
            bool pressedReturn = Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Return;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!canCreate))
                {
                    if (GUILayout.Button("Create", GUILayout.Height(26f)) ||
                        (pressedReturn && canCreate))
                    {
                        var settings = FuzzyBrainSettings.GetOrCreate();
                        FuzzyBrainProjectMenuItems.WriteConditionScript(
                            _className, _componentTypes[_componentIndex], _outputFolder, settings.defaultNamespace);
                        Close();
                    }
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(26f)))
                    Close();
            }
        }

        private string GetValidationError()
        {
            if (string.IsNullOrWhiteSpace(_className))
                return "Class name cannot be empty.";
            if (!ValidIdentifier.IsMatch(_className))
                return "Class name must be a valid C# identifier (letters, digits, underscores; must start with a letter).";
            if (_componentTypes.Count == 0)
                return "No component types found. Make sure your project has compiled.";

            string filePath = Path.Combine(_outputFolder, _className + ".cs");
            if (File.Exists(filePath))
                return $"'{_className}.cs' already exists in the selected folder.";

            return null;
        }
    }
}
