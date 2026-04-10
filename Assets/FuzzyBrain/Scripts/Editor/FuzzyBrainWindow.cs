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

        /// <summary>Appends an act to the currently displayed activity list, if any.</summary>
        public static void TryAddActToCurrentList(Act act)
        {
            if (_instance == null || _instance._activityListSO == null) return;
            _instance._activityListSO.Update();
            SerializedProperty listProp = _instance._activityListSO.FindProperty("list");
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = act;
            _instance._activityListSO.ApplyModifiedProperties();
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
        private SerializedObject   _activityListSO;
        private ScriptableActivityList _activityList;

        private IMGUIContainer     _listContainer;
        private ReorderableList    _reorderableList;
        private VisualElement       _detailContent;
        private Label               _actorLabel;
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
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update  += OnEditorUpdate;
            BuildUI();
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            _instance = null;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update  -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || _actor == null) return;
            if (_actor.LastFiredAct == _lastHighlightedAct) return;
            _lastHighlightedAct = _actor.LastFiredAct;
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
            LoadActivityList();
            RefreshToolbar();
            RebuildTable();
            ShowEmptyDetail();
        }

        private void LoadActivityList()
        {
            _activityList    = null;
            _activityListSO  = null;
            _selectedIndex   = -1;

            if (_actorSO == null) return;

            SerializedProperty prop = _actorSO.FindProperty("activities");
            if (prop == null) return;

            _activityList = prop.objectReferenceValue as ScriptableActivityList;
            if (_activityList != null)
            {
                _activityListSO = new SerializedObject(_activityList);
                BuildReorderableList();
            }
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

            // FSM / FuSM toggle
            var modeToggle = new ToolbarToggle { text = "FuSM", name = "mode-toggle" };
            modeToggle.style.marginRight = 6f;
            modeToggle.RegisterValueChangedCallback(evt =>
            {
                if (_actorSO == null) return;
                _actorSO.Update();
                _actorSO.FindProperty("isFuSM").boolValue = evt.newValue;
                _actorSO.ApplyModifiedProperties();
                modeToggle.text = evt.newValue ? "FuSM" : "FSM";
            });
            toolbar.Add(modeToggle);

            // Activity list field
            var listField = new ObjectField("Activity List")
            {
                objectType = typeof(ScriptableActivityList),
                name = "activity-list-field"
            };
            listField.style.flexGrow = 1f;
            listField.style.marginLeft = 4f;
            listField.RegisterValueChangedCallback(evt =>
            {
                if (_actorSO == null) return;
                _actorSO.Update();
                _actorSO.FindProperty("activities").objectReferenceValue = evt.newValue;
                _actorSO.ApplyModifiedProperties();
                LoadActivityList();
                RebuildTable();
                ShowEmptyDetail();
            });
            toolbar.Add(listField);

            var newListBtn = new ToolbarButton(CreateNewActivityList) { text = "+ New List" };
            toolbar.Add(newListBtn);

            rootVisualElement.Add(toolbar);

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
            newActBtn.style.marginBottom = 6f;
            newActBtn.style.marginLeft   = 4f;
            newActBtn.style.marginRight  = 4f;
            leftPane.Add(newActBtn);

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
            if (_activityListSO == null)
            {
                _reorderableList = null;
                return;
            }

            SerializedProperty listProp = _activityListSO.FindProperty("list");
            _reorderableList = new ReorderableList(_activityListSO, listProp,
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

                float prefixWidth = EditorStyles.label.CalcSize(new GUIContent(prefix)).x;
                Rect prefixRect = new Rect(rect.x, rect.y, prefixWidth, rect.height);
                Rect fieldRect  = new Rect(rect.x + prefixWidth, rect.y,
                    rect.width - prefixWidth, rect.height);

                EditorGUI.LabelField(prefixRect, prefix);
                EditorGUI.BeginChangeCheck();
                EditorGUI.ObjectField(fieldRect, element, typeof(Act), GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    _activityListSO.ApplyModifiedProperties();
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
                _activityListSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(_activityList);
                if (_selectedIndex >= 0)
                    ShowActDetail(_selectedIndex);
            };

            _reorderableList.onAddCallback = rl =>
            {
                _activityListSO.Update();
                listProp.arraySize++;
                _activityListSO.ApplyModifiedProperties();
                rl.index = listProp.arraySize - 1;
            };

            _reorderableList.onRemoveCallback = rl =>
            {
                _activityListSO.Update();
                // ObjectField arrays need DeleteArrayElementAtIndex called twice
                // if the element is non-null, to actually remove the slot.
                SerializedProperty el = listProp.GetArrayElementAtIndex(rl.index);
                if (el.objectReferenceValue != null)
                    listProp.DeleteArrayElementAtIndex(rl.index);
                listProp.DeleteArrayElementAtIndex(rl.index);
                _activityListSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(_activityList);
                _selectedIndex = -1;
                ShowEmptyDetail();
            };
        }

        private void DrawReorderableList()
        {
            if (_activityListSO == null)
            {
                EditorGUILayout.HelpBox("No Activity List assigned.", MessageType.Info);
                return;
            }

            _activityListSO.Update();
            _reorderableList?.DoLayoutList();
            _activityListSO.ApplyModifiedProperties();
        }

        private int  GetActCount() => _activityList != null ? _activityList.list.Count : 0;
        private Act  GetActAt(int index)
        {
            if (_activityList == null || index < 0 || index >= _activityList.list.Count) return null;
            return _activityList.list[index];
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

            var modeToggle = rootVisualElement.Q<ToolbarToggle>("mode-toggle");
            if (modeToggle != null && _actor != null)
            {
                modeToggle.SetValueWithoutNotify(_actor.isFuSM);
                modeToggle.text = _actor.isFuSM ? "FuSM" : "FSM";
            }

            var listField = rootVisualElement.Q<ObjectField>("activity-list-field");
            if (listField != null)
                listField.SetValueWithoutNotify(_activityList);
        }

        private void RefreshRowHighlight()
        {
            _listContainer?.MarkDirtyRepaint();
        }

        // ── Detail panel ──────────────────────────────────────────────────────────

        private void ShowEmptyDetail()
        {
            _detailContent?.Clear();
            if (_activityList == null) return;
            var hint = new Label("Select an act to edit.");
            hint.style.marginTop = 12f;
            hint.style.color = new Color(0.6f, 0.6f, 0.6f);
            _detailContent?.Add(hint);
        }

        private void ShowActDetail(int index)
        {
            _detailContent.Clear();
            Act act = GetActAt(index);
            if (act == null || _activityListSO == null) return;

            _activityListSO.Update();
            SerializedProperty actProp = _activityListSO
                .FindProperty("list")
                .GetArrayElementAtIndex(index);

            var actSO = new SerializedObject(act);

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
            _detailContent.Add(new PropertyField(actSO.FindProperty("setCanAct"), "Set Can Act"));
            _detailContent.Add(new PropertyField(actSO.FindProperty("resetTime"), "Reset Time (s)"));
            _detailContent.Add(new PropertyField(actSO.FindProperty("resetIdle"), "Reset Idle"));

            AddDivider();

            // Conditions header
            var condLabel = new Label("Conditions") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _detailContent.Add(condLabel);

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

            AddDivider();

            // onFire event
            var eventLabel = new Label("On Fire") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _detailContent.Add(eventLabel);
            _detailContent.Add(new PropertyField(actSO.FindProperty("onFire")));

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

                if (cond != null)
                {
                    var invertedToggle = new Toggle("inv") { value = cond.inverted };
                    invertedToggle.style.marginRight = 4f;
                    invertedToggle.RegisterValueChangedCallback(evt =>
                    {
                        cond.inverted = evt.newValue;
                        EditorUtility.SetDirty(cond);
                    });
                    row.Add(invertedToggle);
                }

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

        /// <summary>Sorts the current activity list descending by condition count.</summary>
        private void SortListByConditionCount()
        {
            if (_activityList == null || _activityListSO == null) return;

            _activityList.list.Sort((a, b) =>
            {
                int ca = a != null && a.conditions != null ? a.conditions.Length : 0;
                int cb = b != null && b.conditions != null ? b.conditions.Length : 0;
                return cb.CompareTo(ca);
            });

            EditorUtility.SetDirty(_activityList);
            AssetDatabase.SaveAssets();
            RebuildTable();
        }

        private void CreateNewActivityList()
        {
            if (_actorSO == null)
            {
                EditorUtility.DisplayDialog(
                    "No Actor Selected",
                    "Select an Actor in the Hierarchy before creating a new Activity List.",
                    "OK");
                return;
            }

            string listName = EditorInputDialog.Show(
                "New Activity List", "Activity list name:", "New Activity List");

            if (string.IsNullOrWhiteSpace(listName)) return;

            var    settings = FuzzyBrainSettings.GetOrCreate();
            string folder   = settings.activityListFolder;

            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(folder, listName + ".asset"));

            var newList  = CreateInstance<ScriptableActivityList>();
            newList.name = listName;
            AssetDatabase.CreateAsset(newList, assetPath);
            AssetDatabase.SaveAssets();

            _actorSO.Update();
            _actorSO.FindProperty("activities").objectReferenceValue = newList;
            _actorSO.ApplyModifiedProperties();

            LoadActivityList();
            RefreshToolbar();
            RebuildTable();
            ShowEmptyDetail();

            EditorGUIUtility.PingObject(newList);
            Debug.Log($"[FuzzyBrain] Created activity list: {assetPath}");
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
    }
}
