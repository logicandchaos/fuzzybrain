using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using FuzzyBrain;

namespace FuzzyBrain.Editor
{
    /// <summary>
    /// FuzzyBrain spreadsheet editor.
    /// Acts are rows. Conditions are pill chips. The UnityEvent is editable in the detail panel.
    /// Auto-focuses when an Actor is selected in the Hierarchy.
    /// Open via Tools > FuzzyBrain > Editor, or the button in the Actor inspector.
    /// </summary>
    public class FuzzyBrainWindow : EditorWindow
    {
        // ── Static access ─────────────────────────────────────────────────────────

        private static FuzzyBrainWindow _instance;

        /// <summary>True when the window is currently open.</summary>
        public static bool IsOpen => _instance != null;

        /// <summary>Appends an act to the currently displayed act list, if any.</summary>
        public static void TryAddActToCurrentList(Act act)
        {
            if (_instance == null || _instance._actListSO == null) return;
            _instance._actListSO.Update();
            SerializedProperty listProp = _instance._actListSO.FindProperty("list");
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = act;
            _instance._actListSO.ApplyModifiedProperties();
            _instance.RebuildTable();
        }

        // ── Menu entries ──────────────────────────────────────────────────────────

        [MenuItem("Tools/FuzzyBrain/Editor", priority = 10)]
        public static void Open()
        {
            _instance = GetWindow<FuzzyBrainWindow>("FuzzyBrain");
            _instance.minSize = new Vector2(640f, 400f);
            _instance.Show();
            FuzzyBrainEditorUtils.SetWindowIcon(_instance);
        }

        // ── State ─────────────────────────────────────────────────────────────────

        private Actor              _actor;
        private SerializedObject   _actorSO;
        private SerializedObject   _actListSO;
        private SerializedObject   _actSO;
        private ScriptableActList  _actList;

        private IMGUIContainer     _listContainer;
        private ReorderableList    _reorderableList;
        private VisualElement       _detailContent;
        private Label               _actorLabel;
        private Label               _activeActLabel;
        private Button              _addActBtn;
        private int                 _selectedIndex = -1;
        private Act                 _lastHighlightedAct;

        // ── USS class names ───────────────────────────────────────────────────────

        private const string UssChip        = "db-chip";
        private const string UssRowActive   = "db-row--active";
        private const string UssDivider     = "db-section-divider";
        private const string UssDetailLabel = "db-detail-label";

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _instance = this;
            Selection.selectionChanged             += OnSelectionChanged;
            EditorApplication.update               += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged     += RefreshToolbar;
            Undo.undoRedoPerformed                 += OnUndoRedo;
            BuildUI();
            // Force the initial detail state independently of the selection guard,
            // so the about panel shows immediately when the window first opens.
            ShowEmptyDetail();
            OnSelectionChanged();
        }

        private void OnFocus()
        {
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            _instance = null;
            Selection.selectionChanged             -= OnSelectionChanged;
            EditorApplication.update               -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged     -= RefreshToolbar;
            Undo.undoRedoPerformed                 -= OnUndoRedo;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode ||
                state == PlayModeStateChange.EnteredEditMode)
            {
                OnSelectionChanged();
            }
        }

        private void OnUndoRedo()
        {
            // Serialized data was rolled back by undo/redo. Refresh the SerializedObjects
            // so they reflect the restored state, then redraw both panels.
            _actListSO?.Update();
            _actSO?.Update();
            RebuildTable();
            if (_selectedIndex >= 0)
                ShowActDetail(_selectedIndex);
            else
                ShowEmptyDetail();
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || _actor == null)
            {
                if (_activeActLabel != null)
                    _activeActLabel.style.display = DisplayStyle.None;
                return;
            }

            if (_actor.LastFiredAct == _lastHighlightedAct) return;

            _lastHighlightedAct = _actor.LastFiredAct;

            if (_activeActLabel != null)
            {
                _activeActLabel.text          = _lastHighlightedAct != null
                    ? $"▶  {_lastHighlightedAct.name}"
                    : "▶  None";
                _activeActLabel.style.display = DisplayStyle.Flex;
            }

            RefreshRowHighlight();
            Repaint();
        }

        // ── Actor selection ───────────────────────────────────────────────────────

        private void OnSelectionChanged()
        {
            Actor selected = null;
            if (Selection.activeGameObject != null)
                selected = Selection.activeGameObject.GetComponent<Actor>();

            if (selected == _actor) return;
            _actor = selected;
            _actorSO = _actor != null ? new SerializedObject(_actor) : null;
            LoadActList();
            RefreshToolbar();
            RebuildTable();
            ShowEmptyDetail();
        }

        private void LoadActList()
        {
            _actList    = null;
            _actListSO  = null;
            _selectedIndex   = -1;

            if (_actorSO == null) return;

            SerializedProperty prop = _actorSO.FindProperty("acts");
            if (prop == null) return;

            _actList = prop.objectReferenceValue as ScriptableActList;
            if (_actList != null)
            {
                _actListSO = new SerializedObject(_actList);
                BuildReorderableList();
            }
        }

        private void ClearDestroyedActList()
        {
            _actList       = null;
            _actListSO     = null;
            _reorderableList = null;
            _selectedIndex = -1;

            if (_actorSO != null)
            {
                _actorSO.Update();
                _actorSO.FindProperty("acts").objectReferenceValue = null;
                _actorSO.ApplyModifiedProperties();
            }

            RefreshToolbar();
            ShowEmptyDetail();
        }

        // ── Build UI ──────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            rootVisualElement.Clear();
            InjectUSS();

            // Toolbar
            var toolbar = new Toolbar();
            _actorLabel = new Label("No Actor selected") { name = "actor-label" };
            _actorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _actorLabel.style.alignSelf = Align.Center;
            _actorLabel.style.marginLeft = 6f;
            _actorLabel.style.marginRight = 10f;
            toolbar.Add(_actorLabel);

            // Act list field
            var listField = new ObjectField("Act List")
            {
                objectType = typeof(ScriptableActList),
                name = "act-list-field"
            };
            listField.style.flexGrow = 1f;
            listField.style.marginLeft = 4f;
            listField.RegisterValueChangedCallback(evt =>
            {
                if (_actorSO == null) return;
                _actorSO.Update();
                _actorSO.FindProperty("acts").objectReferenceValue = evt.newValue;
                _actorSO.ApplyModifiedProperties();
                LoadActList();
                RebuildTable();
                ShowEmptyDetail();
            });
            toolbar.Add(listField);

            var newListBtn = new ToolbarButton(CreateNewActList) { text = "+ New List" };
            toolbar.Add(newListBtn);

            rootVisualElement.Add(toolbar);

            // Play-mode active act status bar
            _activeActLabel = new Label("") { name = "active-act-label" };
            _activeActLabel.style.paddingLeft      = 8f;
            _activeActLabel.style.paddingRight     = 8f;
            _activeActLabel.style.paddingTop       = 3f;
            _activeActLabel.style.paddingBottom    = 3f;
            _activeActLabel.style.backgroundColor  = new Color(0.15f, 0.4f, 0.15f, 0.85f);
            _activeActLabel.style.display          = DisplayStyle.None;
            rootVisualElement.Add(_activeActLabel);

            // Split view
            var splitView = new TwoPaneSplitView(0, 320f, TwoPaneSplitViewOrientation.Horizontal);

            // Left pane — inspector-style reorderable act list
            var leftPane = new VisualElement();
            leftPane.style.flexDirection = FlexDirection.Column;

            _listContainer = new IMGUIContainer(DrawReorderableList);
            _listContainer.style.flexGrow = 1f;
            leftPane.Add(_listContainer);

            // Sort button
            var sortBtn = new Button(SortListByConditionCount) { text = "Sort by Conditions ↓" };
            sortBtn.style.marginTop    = 4f;
            sortBtn.style.marginLeft   = 4f;
            sortBtn.style.marginRight  = 4f;
            sortBtn.style.marginBottom = 2f;
            leftPane.Add(sortBtn);

            // New Act — opens the wizard
            var newActBtn = new Button(() => ActWizard.Open()) { text = "+ New Act" };
            newActBtn.style.marginTop    = 2f;
            newActBtn.style.marginBottom = 2f;
            newActBtn.style.marginLeft   = 4f;
            newActBtn.style.marginRight  = 4f;
            leftPane.Add(newActBtn);

            // Validate act list
            var validateBtn = new Button(ValidateActList) { text = "Validate Act List" };
            validateBtn.style.marginTop    = 2f;
            validateBtn.style.marginBottom = 6f;
            validateBtn.style.marginLeft   = 4f;
            validateBtn.style.marginRight  = 4f;
            leftPane.Add(validateBtn);

            splitView.Add(leftPane);

            // Right pane — detail panel
            var rightPane = new ScrollView();
            rightPane.style.paddingLeft  = 8f;
            rightPane.style.paddingRight = 8f;
            _detailContent = rightPane.contentContainer;
            splitView.Add(rightPane);

            rootVisualElement.Add(splitView);
        }

        private void InjectUSS()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
        }

        // ── Reorderable list ──────────────────────────────────────────────────────

        private void BuildReorderableList()
        {
            if (_actListSO == null)
            {
                _reorderableList = null;
                return;
            }

            SerializedProperty listProp = _actListSO.FindProperty("list");
            _reorderableList = new ReorderableList(_actListSO, listProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _reorderableList.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Acts");

            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                rect.y      += 2f;
                rect.height  = EditorGUIUtility.singleLineHeight;
                SerializedProperty element = listProp.GetArrayElementAtIndex(index);

                // Condition count prefix
                Act act = element.objectReferenceValue as Act;
                int condCount = act != null && act.conditions != null ? act.conditions.Length : 0;
                string prefix = $"[{condCount}] ";

                // Highlight the currently firing act during play mode
                if (Application.isPlaying && act != null && act == _lastHighlightedAct)
                    EditorGUI.DrawRect(
                        new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f),
                        new Color(0.2f, 0.7f, 0.2f, 0.25f));

                float prefixWidth = EditorStyles.label.CalcSize(new GUIContent(prefix)).x;
                Rect prefixRect = new Rect(rect.x, rect.y, prefixWidth, rect.height);
                Rect fieldRect  = new Rect(rect.x + prefixWidth, rect.y,
                    rect.width - prefixWidth, rect.height);

                EditorGUI.LabelField(prefixRect, prefix);
                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(fieldRect, element, typeof(Act), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    _actListSO.ApplyModifiedProperties();
                    _listContainer?.MarkDirtyRepaint();
                }
            };

            _reorderableList.onSelectCallback = rl =>
            {
                _selectedIndex = rl.index;
                ShowActDetail(_selectedIndex);
            };

            _reorderableList.onReorderCallback = _ =>
            {
                _actListSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(_actList);
                if (_selectedIndex >= 0)
                    ShowActDetail(_selectedIndex);
            };

            _reorderableList.onAddCallback = rl =>
            {
                _actListSO.Update();
                listProp.arraySize++;
                _actListSO.ApplyModifiedProperties();
                rl.index = listProp.arraySize - 1;
            };

            _reorderableList.onRemoveCallback = rl =>
            {
                _actListSO.Update();
                // Re-fetch after Update() — the captured listProp reference may be stale.
                SerializedProperty freshList = _actListSO.FindProperty("list");
                if (rl.index < 0 || rl.index >= freshList.arraySize) return;
                freshList.DeleteArrayElementAtIndex(rl.index);
                _actListSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(_actList);
                _selectedIndex = -1;
                ShowEmptyDetail();
            };
        }

        private void DrawReorderableList()
        {
            if (_actListSO == null)
            {
                EditorGUILayout.HelpBox("No Act List assigned.", MessageType.Info);
                return;
            }

            // The asset may have been deleted from the project while the window was open.
            if (_actListSO.targetObject == null)
            {
                ClearDestroyedActList();
                return;
            }

            _actListSO.Update();
            _reorderableList?.DoLayoutList();
            _actListSO.ApplyModifiedProperties();
        }

        private int  GetActCount() => _actList != null ? _actList.list.Count : 0;
        private Act  GetActAt(int index)
        {
            if (_actList == null || index < 0 || index >= _actList.list.Count) return null;
            return _actList.list[index];
        }

        private void RebuildTable()
        {
            BuildReorderableList();
            _listContainer?.MarkDirtyRepaint();
        }

        private void RefreshToolbar()
        {
            if (_actorLabel == null) return;
            _actorLabel.text = _actor != null ? $"Actor: {_actor.name}" : "No Actor selected";

            var listField = rootVisualElement.Q<ObjectField>("act-list-field");
            if (listField != null)
                listField.SetValueWithoutNotify(_actList);
        }

        private void RefreshRowHighlight()
        {
            _listContainer?.MarkDirtyRepaint();
        }

        // ── Detail panel ──────────────────────────────────────────────────────────

        private void ShowEmptyDetail()
        {
            _detailContent?.Clear();

            // No Actor selected — show the full about panel in the right pane.
            if (_actor == null)
            {
                _detailContent?.Add(FuzzyBrainAboutPanel.Build());
                return;
            }

            // Actor selected but no act chosen yet.
            if (_actList == null) return;
            var hint = new Label("Select an act to edit.");
            hint.style.marginTop = 12f;
            hint.style.color = new Color(0.6f, 0.6f, 0.6f);
            _detailContent?.Add(hint);
        }

        private void ShowActDetail(int index)
        {
            _detailContent.Clear();
            Act act = GetActAt(index);
            if (act == null || _actListSO == null) return;

            _actListSO.Update();
            SerializedProperty actProp = _actListSO
                .FindProperty("list")
                .GetArrayElementAtIndex(index);

            var actSO = _actSO = new SerializedObject(act);

            // Act name
            var nameField = new TextField("Act Name") { value = act.name };
            nameField.RegisterValueChangedCallback(evt =>
            {
                act.name = evt.newValue;
                EditorUtility.SetDirty(act);
                _listContainer?.MarkDirtyRepaint();
            });
            _detailContent.Add(nameField);

            AddDivider();

            // Flags
            _detailContent.Add(new PropertyField(actSO.FindProperty("maxClockTime"), "Max Clock Time (s)"));

            AddDivider();

            // Conditions header
            var condLabel = new Label("Conditions") { style = { unityFontStyleAndWeight = FontStyle.Bold } };            _detailContent.Add(condLabel);

            SerializedProperty condsProp = actSO.FindProperty("conditions");
            RebuildConditionList(act, actSO, condsProp);

            // Add condition field
            var addCondRow = new VisualElement();
            addCondRow.style.flexDirection = FlexDirection.Row;
            addCondRow.style.marginTop = 4f;

            var condPicker = new ObjectField("Add Condition") { objectType = typeof(Condition) };
            condPicker.style.flexGrow = 1f;
            condPicker.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null) return;
                actSO.Update();
                condsProp.arraySize++;
                condsProp.GetArrayElementAtIndex(condsProp.arraySize - 1)
                    .objectReferenceValue = evt.newValue;
                actSO.ApplyModifiedProperties();
                condPicker.SetValueWithoutNotify(null);
                ShowActDetail(index);
                _listContainer?.MarkDirtyRepaint();
            });
            addCondRow.Add(condPicker);
            _detailContent.Add(addCondRow);

            // Wizard buttons
            var wizardRow = new VisualElement();
            wizardRow.style.flexDirection = FlexDirection.Row;
            wizardRow.style.marginTop = 4f;

            var genBtn = new Button(() => ConditionWizard.Open(0))
                { text = "New Condition Type", style = { flexGrow = 1f } };
            var assetBtn = new Button(() => ConditionWizard.Open(1))
                { text = "Create Condition Asset", style = { flexGrow = 1f } };

            wizardRow.Add(genBtn);
            wizardRow.Add(assetBtn);
            _detailContent.Add(wizardRow);

            _detailContent.Bind(actSO);
        }

        private void RebuildConditionList(Act act, SerializedObject actSO, SerializedProperty condsProp)
        {
            for (int i = 0; i < condsProp.arraySize; i++)
            {
                int capturedIndex = i;
                SerializedProperty entry = condsProp.GetArrayElementAtIndex(i);
                Condition cond = entry.objectReferenceValue as Condition;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2f;

                var nameLabel = new Label(cond != null ? cond.name : "(missing)")
                {
                    style = { flexGrow = 1f }
                };
                row.Add(nameLabel);

                var removeBtn = new Button(() =>
                {
                    actSO.Update();
                    condsProp.DeleteArrayElementAtIndex(capturedIndex);
                    actSO.ApplyModifiedProperties();
                    ShowActDetail(_selectedIndex);
                    _listContainer?.MarkDirtyRepaint();
                }) { text = "×" };
                removeBtn.style.width = 20f;
                row.Add(removeBtn);

                _detailContent.Add(row);
            }
        }

        /// <summary>Sorts the current act list descending by condition count.</summary>
        private void SortListByConditionCount()
        {
            if (_actList == null || _actListSO == null) return;

            Undo.RecordObject(_actList, "Sort Act List");
            _actList.list.Sort((a, b) =>
            {
                int ca = a != null && a.conditions != null ? a.conditions.Length : 0;
                int cb = b != null && b.conditions != null ? b.conditions.Length : 0;
                return cb.CompareTo(ca);
            });

            EditorUtility.SetDirty(_actList);
            AssetDatabase.SaveAssets();
            _actListSO.Update();
            RebuildTable();
        }

        private void CreateNewActList()
        {
            if (_actorSO == null)
            {
                EditorUtility.DisplayDialog(
                    "No Actor Selected",
                    "Select an Actor in the Hierarchy before creating a new Act List.",
                    "OK");
                return;
            }

            var    settings       = FuzzyBrainSettings.GetOrCreate();
            string defaultFolder  = settings.actListFolder;

            string listName = EditorInputDialog.ShowWithFolder(
                "New Act List", "Act list name:", "New Act List", defaultFolder, out string folder);

            if (string.IsNullOrWhiteSpace(listName)) return;

            if (string.IsNullOrWhiteSpace(folder))
                folder = defaultFolder;

            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(folder, listName + ".asset"));

            var newList  = CreateInstance<ScriptableActList>();
            newList.name = listName;
            AssetDatabase.CreateAsset(newList, assetPath);

            AssetDatabase.SaveAssets();

            _actorSO.Update();
            _actorSO.FindProperty("acts").objectReferenceValue = newList;
            _actorSO.ApplyModifiedProperties();

            LoadActList();
            RefreshToolbar();
            RebuildTable();
            ShowEmptyDetail();

            EditorGUIUtility.PingObject(newList);
            Debug.Log($"[FuzzyBrain] Created act list: {assetPath}");
        }

        private void AddDivider()
        {
            var divider = new VisualElement();
            divider.style.height = 1f;
            divider.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            divider.style.marginTop    = 6f;
            divider.style.marginBottom = 6f;
            _detailContent.Add(divider);
        }

        // ── Validation ────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs a dry-run validation pass over the current act list.
        /// Conditions are checked via RequiredType against the actor's components.
        /// </summary>
        private void ValidateActList()
        {
            if (_actor == null || _actList == null)
            {
                Debug.LogWarning("[FuzzyBrain] Validate: no Actor or Act List selected.");
                return;
            }

            Debug.Log($"[FuzzyBrain] Validating act list '{_actList.name}' on '{_actor.name}'...", _actor);

            var cache = ActContext.BuildComponentCache(_actor);

            ActContext ctx = ActContext.ForValidation(_actor, cache);

            foreach (Act act in _actList.list)
            {
                if (act == null) continue;

                // Validate conditions via their declared RequiredType
                if (act.conditions != null)
                {
                    foreach (Condition cond in act.conditions)
                    {
                        if (cond == null) continue;
                        if (!cache.ContainsKey(cond.RequiredType))
                            Debug.LogWarning(
                                $"[FuzzyBrain] '{act.name}' — condition '{cond.name}' requires " +
                                $"{cond.RequiredType.Name} which is missing on '{_actor.name}'.", _actor);
                    }
                }
            }

            Debug.Log($"[FuzzyBrain] Validation complete. Check above for any warnings.", _actor);
        }
    }
}
