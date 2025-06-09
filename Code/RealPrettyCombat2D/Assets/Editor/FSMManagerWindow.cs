using Assets._1._Scripts;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class FSMManagerWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset VisualTreeAsset;

    private FSMBase fsm;
    private string basePath;
    private FSMState curState;
    private FSMTransition curTransition;
    private VisualElement curTab;

    [MenuItem("Tool/FSMManager")]
    public static void ShowWindow()
    {
        var window = GetWindow<FSMManagerWindow>();
        window.Show();
    }

    private void CreateGUI()
    {
        VisualTreeAsset.CloneTree(rootVisualElement);

        var target = rootVisualElement.Q<ObjectField>();
        target.RegisterValueChangedCallback(OnTargetChange);

        var stateList = rootVisualElement.Q<ListView>("StateList");
        stateList.selectionChanged += (list) =>
        {
            if (list == null || list.Count() == 0)
            {
                SetCurState(null);
                return;
            }
            SetCurState(list.First() as FSMState);
        };

        var inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        var outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        inTransitionList.selectionChanged += (list) =>
        {
            if (list == null || list.Count() == 0)
            {
                SetCurTranstition(null);
                return;
            }
            outTransitionList.ClearSelection();
            SetCurTranstition(list.First() as FSMTransition);
        };
        outTransitionList.selectionChanged += (list) =>
        {
            if (list == null || list.Count() == 0)
            {
                SetCurTranstition(null);
                return;
            }
            inTransitionList.ClearSelection();
            SetCurTranstition(list.First() as FSMTransition);
        };
        var addInTransBtn = rootVisualElement.Q<Button>("AddInTrans");
        addInTransBtn.clicked += () =>
        {
            if (fsm == null) return;

            var nTrans = ScriptableObject.CreateInstance<FSMTransition>();
            nTrans.To = curState.UniqueName;
            nTrans.From = new string[] { "Default State" };
            if (!Directory.Exists($"{basePath}/Transitions"))
            {
                Directory.CreateDirectory($"{basePath}/Transitions");
            }
            AssetDatabase.CreateAsset(nTrans, $"{basePath}/Transitions/{System.Guid.NewGuid()}.asset");
            fsm.Transitions.Add(nTrans);
            UpdateTransitionList();
            inTransitionList.SetSelection(inTransitionList.itemsSource.IndexOf(nTrans));
        };
        var removeInTransBtn = rootVisualElement.Q<Button>("RemoveInTrans");
        removeInTransBtn.clicked += () =>
        {
            if (fsm == null || curState == null || curTransition == null) return;

            fsm.Transitions.Remove(curTransition);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(curTransition));
            UpdateTransitionList();
            inTransitionList.ClearSelection();
        };
        var addOutTransBtn = rootVisualElement.Q<Button>("AddOutTrans");
        addOutTransBtn.clicked += () =>
        {
            if (fsm == null) return;

            var nTrans = ScriptableObject.CreateInstance<FSMTransition>();
            nTrans.From = new string[] { curState.UniqueName };
            nTrans.To = "Default State";
            if (!Directory.Exists($"{basePath}/Transitions"))
            {
                Directory.CreateDirectory($"{basePath}/Transitions");
            }
            AssetDatabase.CreateAsset(nTrans, $"{basePath}/Transitions/{System.Guid.NewGuid()}.asset");
            fsm.Transitions.Add(nTrans);
            UpdateTransitionList();
            outTransitionList.SetSelection(outTransitionList.itemsSource.IndexOf(nTrans));
        };
        var removeOutTransBtn = rootVisualElement.Q<Button>("RemoveOutTrans");
        removeOutTransBtn.clicked += () =>
        {
            if (fsm == null || curState == null || curTransition == null) return;

            fsm.Transitions.Remove(curTransition);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(curTransition));
            UpdateTransitionList();
            outTransitionList.ClearSelection();
        };

        var addState = rootVisualElement.Q<Button>("Add");
        addState.clicked += () =>
        {
            if (fsm == null) return;

            var nState = ScriptableObject.CreateInstance<FSMState>();
            nState.UniqueName = $"New State";
            if (!Directory.Exists($"{basePath}/States"))
            {
                Directory.CreateDirectory($"{basePath}/States");
            }
            AssetDatabase.CreateAsset(nState, $"{basePath}/States/{System.Guid.NewGuid()}.asset");
            fsm.States.Add(nState);
            stateList.Rebuild();
            stateList.SetSelection(fsm.States.IndexOf(nState));
        };
        var removeState = rootVisualElement.Q<Button>("Remove");
        removeState.clicked += () =>
        {
            if (fsm == null || curState == null) return;

            fsm.States.Remove(curState);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(curState));
            stateList.Rebuild();
        };
        var uniqueNameField = rootVisualElement.Q<TextField>("UniqueNameField");
        uniqueNameField.RegisterValueChangedCallback(OnUniqueNameFieldChange);

        var stateTab = rootVisualElement.Q<VisualElement>("StateTabButton");
        stateTab.RegisterCallback<ClickEvent>(OnStateTabClick);
        var transitionTab = rootVisualElement.Q<VisualElement>("TransitionTabButton");
        transitionTab.RegisterCallback<ClickEvent>(OnTransitionTabClick);
        rootVisualElement.Q<VisualElement>("StateTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        rootVisualElement.Q<VisualElement>("TransitionTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

        SetCurState(null);
        SetCurTranstition(null);
        SetTab("StateTab");
    }

    private void OnStateTabClick(ClickEvent e)
    {
        SetTab("StateTab");
    }

    private void OnTransitionTabClick(ClickEvent e)
    {
        SetTab("TransitionTab");
    }

    private void SetTab(string tabName)
    {
        if(curTab != null)
        {
            curTab.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        curTab = rootVisualElement.Q<VisualElement>(tabName);
        curTab.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
    }

    private void OnUniqueNameFieldChange(ChangeEvent<string> evt)
    {
        var transitionbaselable = rootVisualElement.Q<Label>("CurStateName");
        transitionbaselable.text = evt.newValue;

        var stateList = rootVisualElement.Q<ListView>("StateList");
        stateList.Rebuild();
    }

    private void SetCurState(FSMState state)
    {
        curState = state;
        var inspector = rootVisualElement.Q<VisualElement>("InspectorCol");
        var transitionbaselable = rootVisualElement.Q<Label>("CurStateName");
        var stateList = rootVisualElement.Q<ListView>("StateList");
        var inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        var outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        inTransitionList.ClearSelection();
        outTransitionList.ClearSelection();
        if (curState == null)
        {
            inspector.visible = false;
            stateList.ClearSelection();
            SetCurTranstition(null);
            return;
        }
        inspector.visible = true;
        var serializedState = new SerializedObject(curState);
        inspector.Bind(serializedState);
        transitionbaselable.text = curState.UniqueName;
        SetCurTranstition(null);

        UpdateTransitionList();
    }

    private void UpdateTransitionList()
    {
        if(curState == null) return;

        List<FSMTransition> inT = new List<FSMTransition>();
        List<FSMTransition> outT = new List<FSMTransition>();
        foreach (var t in fsm.Transitions)
        {
            if (t.From.Contains(curState.UniqueName))
            {
                outT.Add(t);
            }
            else if (t.To.Equals(curState.UniqueName))
            {
                inT.Add(t);
            }
        }

        var inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        var outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        inTransitionList.itemsSource = inT;
        outTransitionList.itemsSource = outT;
    }

    private void SetCurTranstition(FSMTransition transtition)
    {
        curTransition = transtition;
        var inspector = rootVisualElement.Q<VisualElement>("TransitionInfo");
        inspector.Clear();
        if (curTransition == null)
        {
            return;
        }

        var obj = new SerializedObject(curTransition);
        InspectorElement.FillDefaultInspector(inspector, obj, null);
        bool IsOutTrans = transtition.From.Contains(curState.UniqueName);
        SetPropertyFieldBindings(inspector, obj, IsOutTrans ? "From" : "To");
    }

    private void SetPropertyFieldBindings(VisualElement container, SerializedObject serializedObject, string omitField)
    {
        var propertyFields = container.Query<PropertyField>().ToList();
        foreach (var propertyField in propertyFields)
        {
            propertyField.Bind(serializedObject);
            propertyField.RegisterValueChangeCallback((e) => UpdateTransitionList());
            if (propertyField.name.Equals($"PropertyField:{omitField}") || propertyField.name.Equals($"PropertyField:Script"))
            {
                propertyField.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            }
        }
    }

    private void OnTargetChange(ChangeEvent<Object> evt)
    {
        fsm = evt.newValue as FSMBase;
        var stateList = rootVisualElement.Q<ListView>("StateList");
        var inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        var outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        if (evt.newValue.IsUnityNull())
        {
            fsm = null;
            basePath = "Assets";
            SetCurState(null);
            SetCurTranstition(null);
            stateList.itemsSource = null;
            stateList.Rebuild();
            inTransitionList.itemsSource = null;
            outTransitionList.itemsSource = null;
            inTransitionList.Rebuild();
            outTransitionList.Rebuild();
            return;
        }

        basePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(fsm));
        stateList.bindItem = (e, i) =>
        {
            e.Q<Label>().text = $"{(stateList.itemsSource[i] as FSMState).UniqueName}";
        };
        stateList.itemsSource = fsm.States;
        stateList.makeItem = () => stateList.itemTemplate.Instantiate();
        inTransitionList.bindItem = (e, i) =>
        {
            e.Q<Label>().text = $"From {string.Join(", ", (inTransitionList.itemsSource[i] as FSMTransition).From)}";
        };
        inTransitionList.makeItem = () => inTransitionList.itemTemplate.Instantiate();
        outTransitionList.bindItem = (e, i) =>
        {
            e.Q<Label>().text = $"To {(outTransitionList.itemsSource[i] as FSMTransition).To}";
        };
        inTransitionList.makeItem = () => outTransitionList.itemTemplate.Instantiate();
        SetCurState(null);
        SetCurTranstition(null);
    }
}
