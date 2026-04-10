using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>Modal single-field text input dialog for the Unity editor.</summary>
    public class EditorInputDialog : EditorWindow
    {
        private string _label;
        private string _value;

        private static string _result;

        /// <summary>Shows a blocking input dialog and returns the entered string, or null if cancelled.</summary>
        public static string Show(string title, string label, string defaultValue = "")
        {
            _result = null;
            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window._label = label;
            window._value = defaultValue;
            window.minSize = window.maxSize = new Vector2(340f, 90f);
            window.ShowModalUtility();
            return _result;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(_label);

            GUI.SetNextControlName("inputField");
            _value = EditorGUILayout.TextField(_value);
            EditorGUI.FocusTextInControl("inputField");

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool pressedReturn = Event.current.type == EventType.KeyDown
                    && Event.current.keyCode == KeyCode.Return;

                if (GUILayout.Button("Create") || pressedReturn)
                {
                    _result = _value;
                    Close();
                }
                if (GUILayout.Button("Cancel"))
                    Close();
            }
        }
    }
}
