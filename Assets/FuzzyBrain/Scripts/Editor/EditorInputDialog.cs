using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>Modal single-field text input dialog for the Unity editor.</summary>
    public class EditorInputDialog : EditorWindow
    {
        private string _label;
        private string _value;
        private bool   _showFolderPicker;
        private string _folder;

        private static string _result;
        private static string _resultFolder;

        /// <summary>Shows a blocking input dialog and returns the entered string, or null if cancelled.</summary>
        public static string Show(string title, string label, string defaultValue = "")
        {
            _result = null;
            var window = CreateInstance<EditorInputDialog>();
            window.titleContent    = new GUIContent(title);
            window._label          = label;
            window._value          = defaultValue;
            window._showFolderPicker = false;
            window.minSize = window.maxSize = new Vector2(340f, 90f);
            window.ShowModalUtility();
            return _result;
        }

        /// <summary>
        /// Shows a blocking input dialog with an additional folder picker.
        /// Returns the entered name, or null if cancelled.
        /// The chosen folder is written to <paramref name="selectedFolder"/>.
        /// </summary>
        public static string ShowWithFolder(
            string title,
            string label,
            string defaultValue,
            string defaultFolder,
            out string selectedFolder)
        {
            _result       = null;
            _resultFolder = defaultFolder;

            var window = CreateInstance<EditorInputDialog>();
            window.titleContent      = new GUIContent(title);
            window._label            = label;
            window._value            = defaultValue;
            window._showFolderPicker = true;
            window._folder           = defaultFolder;
            window.minSize = window.maxSize = new Vector2(380f, 130f);
            window.ShowModalUtility();

            selectedFolder = _resultFolder;
            return _result;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(_label);

            GUI.SetNextControlName("inputField");
            _value = EditorGUILayout.TextField(_value);
            EditorGUI.FocusTextInControl("inputField");

            if (_showFolderPicker)
            {
                EditorGUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Folder", GUILayout.Width(46f));
                    _folder = EditorGUILayout.TextField(_folder);
                    if (GUILayout.Button("…", GUILayout.Width(26f)))
                    {
                        string chosen = EditorUtility.OpenFolderPanel(
                            "Select folder", _folder, "");

                        // Convert absolute OS path to a project-relative Assets/… path.
                        if (!string.IsNullOrEmpty(chosen) && chosen.StartsWith(Application.dataPath))
                            _folder = "Assets" + chosen.Substring(Application.dataPath.Length);
                    }
                }
            }

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool pressedReturn = Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Return;

                if (GUILayout.Button("Create") || pressedReturn)
                {
                    _result       = _value;
                    _resultFolder = _folder;
                    Close();
                }
                if (GUILayout.Button("Cancel"))
                    Close();
            }
        }
    }
}
