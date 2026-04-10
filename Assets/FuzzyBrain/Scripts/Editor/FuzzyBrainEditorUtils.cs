using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    public static class FuzzyBrainEditorUtils
    {
        private const string IconPath = "Assets/FuzzyBrain/Editor/icon/FuzzyBrainIcon32x32.png";

        /// <summary>Loads the FuzzyBrain icon and applies it to the given window's titleContent.</summary>
        public static void SetWindowIcon(EditorWindow window)
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;
            window.titleContent = new GUIContent(window.titleContent.text, icon);
        }
    }
}
