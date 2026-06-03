using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Standalone About popup for FuzzyBrain.
    /// Open via Tools > FuzzyBrain > About.
    /// </summary>
    public class FuzzyBrainAboutWindow : EditorWindow
    {
        [MenuItem("Tools/FuzzyBrain/About", priority = 100)]
        public static void Open()
        {
            var window = GetWindow<FuzzyBrainAboutWindow>(
                utility: true,
                title: "About FuzzyBrain",
                focus: true);

            window.minSize = new Vector2(440f, 500f);
            window.maxSize = new Vector2(440f, 500f);
            FuzzyBrainEditorUtils.SetWindowIcon(window);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(FuzzyBrainAboutPanel.Build());
        }
    }
}
