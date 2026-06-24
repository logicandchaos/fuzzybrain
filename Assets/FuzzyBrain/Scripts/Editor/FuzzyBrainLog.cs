#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// Static ring buffer that records act firing events for FuzzyBrainLogWindow.
    /// Populated by FuzzyBrainManager during play mode via static delegates.
    /// Editor-only; stripped from builds.
    /// </summary>
    [InitializeOnLoad]
    public static class FuzzyBrainLog
    {
        public struct LogEntry
        {
            public float  time;
            public string actorName;
            public string actName;
        }

        private const int MaxEntries = 500;
        private static readonly List<LogEntry> _entries = new List<LogEntry>(MaxEntries);

        /// <summary>When false, Record() is a no-op.</summary>
        public static bool IsRecording { get; set; } = false;

        /// <summary>Fires after each successful Record() call. Subscribe to repaint the log window.</summary>
        public static event Action OnEntryAdded;

        /// <summary>Read-only view of all recorded entries.</summary>
        public static IReadOnlyList<LogEntry> Entries => _entries;

        /// <summary>When true, the buffer is cleared when entering play mode.</summary>
        public static bool ClearOnPlay { get; set; } = false;

        static FuzzyBrainLog()
        {
            FuzzyBrainManager.OnActLoggingChanged += enabled => IsRecording = enabled;
            FuzzyBrainManager.OnActRecorded       += Record;

            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && ClearOnPlay)
                    Clear();
            };
        }

        /// <summary>
        /// Records an act firing event. No-op when IsRecording is false.
        /// Skips consecutive duplicate actor+act pairs to reduce noise.
        /// </summary>
        public static void Record(string actorName, string actName)
        {
            if (!IsRecording) return;

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].actorName != actorName) continue;
                if (_entries[i].actName == actName) return; // unchanged — skip
                break;
            }

            if (_entries.Count >= MaxEntries)
                _entries.RemoveAt(0);

            _entries.Add(new LogEntry
            {
                time      = Time.time,
                actorName = actorName,
                actName   = actName
            });

            OnEntryAdded?.Invoke();
        }

        /// <summary>Empties the log buffer.</summary>
        public static void Clear() => _entries.Clear();
    }
}
#endif
