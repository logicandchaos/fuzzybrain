#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Play-mode act log. Displays a scrollable, colour-coded feed of act firing events
    /// recorded by FuzzyBrainManager. Open via Tools > FuzzyBrain > Log.
    /// </summary>
    public class FuzzyBrainLogWindow : EditorWindow
    {
        [MenuItem("Tools/FuzzyBrain/Log", priority = 13)]
        public static void Open()
        {
            var window = GetWindow<FuzzyBrainLogWindow>("FuzzyBrain Log");
            window.minSize = new Vector2(400f, 200f);
            window.Show();
            FuzzyBrainEditorUtils.SetWindowIcon(window);
        }

        private Vector2 _scroll;
        private string  _actorFilter  = string.Empty;
        private bool    _autoScroll   = true;
        private bool    _userScrolled;

        private const string PrefClearOnPlay = "FuzzyBrain.Log.ClearOnPlay";

        private static readonly Color[] ActorPalette =
        {
            new Color(0.40f, 0.70f, 1.00f),
            new Color(0.40f, 0.90f, 0.60f),
            new Color(1.00f, 0.75f, 0.30f),
            new Color(0.85f, 0.45f, 0.85f),
            new Color(0.40f, 0.90f, 0.90f),
            new Color(1.00f, 0.55f, 0.55f),
        };

        private void OnEnable()
        {
            FuzzyBrainLog.OnEntryAdded += OnEntryAdded;
            FuzzyBrainLog.ClearOnPlay   = EditorPrefs.GetBool(PrefClearOnPlay, false);
        }

        private void OnDisable() => FuzzyBrainLog.OnEntryAdded -= OnEntryAdded;

        private void OnEntryAdded()
        {
            if (_autoScroll && !_userScrolled)
                _scroll.y = float.MaxValue;
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawLog();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                bool wasRecording = FuzzyBrainLog.IsRecording;
                bool nowRecording = GUILayout.Toggle(
                    wasRecording, "● Record", EditorStyles.toolbarButton, GUILayout.Width(72f));

                if (nowRecording != wasRecording)
                {
                    FuzzyBrainLog.IsRecording = nowRecording;
                    if (FuzzyBrainManager.Instance != null)
                        FuzzyBrainManager.Instance.EnableActLogging = nowRecording;
                }

                GUILayout.Label("Filter:", EditorStyles.toolbarButton, GUILayout.Width(42f));
                _actorFilter = EditorGUILayout.TextField(
                    _actorFilter, EditorStyles.toolbarTextField, GUILayout.Width(120f));

                _autoScroll = GUILayout.Toggle(
                    _autoScroll, "Auto-scroll", EditorStyles.toolbarButton, GUILayout.Width(82f));

                bool wasClearOnPlay = FuzzyBrainLog.ClearOnPlay;
                bool nowClearOnPlay = GUILayout.Toggle(
                    wasClearOnPlay, "Clear on Play", EditorStyles.toolbarButton, GUILayout.Width(90f));
                if (nowClearOnPlay != wasClearOnPlay)
                {
                    FuzzyBrainLog.ClearOnPlay = nowClearOnPlay;
                    EditorPrefs.SetBool(PrefClearOnPlay, nowClearOnPlay);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    SaveLogToFile();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                {
                    FuzzyBrainLog.Clear();
                    Repaint();
                }
            }
        }

        private void SaveLogToFile()
        {
            IReadOnlyList<FuzzyBrainLog.LogEntry> entries = FuzzyBrainLog.Entries;
            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("FuzzyBrain Log", "No entries to save.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Save FuzzyBrain Log", "", "FuzzyBrainLog", "txt");
            if (string.IsNullOrEmpty(path)) return;

            bool hasFilter = !string.IsNullOrWhiteSpace(_actorFilter);
            var  sb        = new StringBuilder();
            foreach (FuzzyBrainLog.LogEntry e in entries)
            {
                if (hasFilter && !e.actorName.Contains(_actorFilter)) continue;
                sb.AppendLine($"[{e.time:F2}]  {e.actorName}  →  {e.actName}");
            }

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[FuzzyBrain] Log saved to: {path}");
        }

        private void DrawLog()
        {
            IReadOnlyList<FuzzyBrainLog.LogEntry> entries = FuzzyBrainLog.Entries;
            bool hasFilter = !string.IsNullOrWhiteSpace(_actorFilter);

            float prevY = _scroll.y;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < entries.Count; i++)
            {
                FuzzyBrainLog.LogEntry e = entries[i];
                if (hasFilter && !e.actorName.Contains(_actorFilter)) continue;

                Color col = ActorPalette[
                    Mathf.Abs(e.actorName.GetHashCode()) % ActorPalette.Length];

                Color prev = GUI.contentColor;
                GUI.contentColor = col;
                EditorGUILayout.LabelField(
                    $"[{e.time:F2}]  {e.actorName}  →  {e.actName}",
                    EditorStyles.miniLabel);
                GUI.contentColor = prev;
            }

            EditorGUILayout.EndScrollView();

            if (Mathf.Abs(_scroll.y - prevY) > 1f)
                _userScrolled = true;
            if (_autoScroll && _userScrolled && _scroll.y >= float.MaxValue - 1f)
                _userScrolled = false;
        }
    }
}
#endif
