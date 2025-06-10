using Assets._1._Scripts;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
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

    private ListView stateList;
    private ListView inTransitionList;
    private ListView outTransitionList;

    //--------------------------------------------------------------

    #region Create And Initialize
    [MenuItem("Tool/FSMManager")]
    public static void ShowWindow()
    {
        var window = GetWindow<FSMManagerWindow>();
        window.Show();
    }

    private void CreateGUI()
    {
        //UI 생성
        VisualTreeAsset.CloneTree(rootVisualElement);


        //FSM 선택 이벤트 등록
        var targetFSMSelector = rootVisualElement.Q<ObjectField>();
        targetFSMSelector.RegisterValueChangedCallback(OnTargetFSMChange);


        //State 선택 이벤트 등록
        stateList = rootVisualElement.Q<ListView>("StateList");
        stateList.selectionChanged += OnSelectState;

        //State 추가&제거 이벤트 등록
        var addState = rootVisualElement.Q<Button>("Add");
        addState.clicked += AddState;
        var removeState = rootVisualElement.Q<Button>("Remove");
        removeState.clicked += RemoveState;
        var uniqueNameField = rootVisualElement.Q<TextField>("UniqueNameField");
        uniqueNameField.RegisterValueChangedCallback(OnUniqueNameFieldChange);


        //Transition 선택 이벤트 등록
        inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        inTransitionList.selectionChanged += (list) => OnSelectTransition(list, isInTrans: true);
        outTransitionList.selectionChanged += (list) => OnSelectTransition(list, isInTrans: false);

        //Transition 추가&제거 이벤트 등록
        var addInTransBtn = rootVisualElement.Q<Button>("AddInTrans");
        addInTransBtn.clicked += () => AddTransition(isInTrans: true);
        var removeInTransBtn = rootVisualElement.Q<Button>("RemoveInTrans");
        removeInTransBtn.clicked += RemoveTransition;
        var addOutTransBtn = rootVisualElement.Q<Button>("AddOutTrans");
        addOutTransBtn.clicked += () => AddTransition(isInTrans: false);
        var removeOutTransBtn = rootVisualElement.Q<Button>("RemoveOutTrans");
        removeOutTransBtn.clicked += RemoveTransition;

        //텝 선택 이벤트 등록
        var stateTab = rootVisualElement.Q<VisualElement>("StateTabButton");
        stateTab.RegisterCallback<ClickEvent>(OnStateTabClick);
        var transitionTab = rootVisualElement.Q<VisualElement>("TransitionTabButton");
        transitionTab.RegisterCallback<ClickEvent>(OnTransitionTabClick);
        rootVisualElement.Q<VisualElement>("StateTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        rootVisualElement.Q<VisualElement>("TransitionTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

        //초기화
        SetCurState(null);
        SetCurTranstition(null);
        SetTab("StateTab");
    } 
    #endregion


    #region FSM
    private void OnTargetFSMChange(ChangeEvent<Object> evt)
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
            e.Q<Label>().text = $"From {(outTransitionList.itemsSource[i] as FSMTransition).To.UniqueName}";
        };
        inTransitionList.makeItem = () => inTransitionList.itemTemplate.Instantiate();
        outTransitionList.bindItem = (e, i) =>
        {
            e.Q<Label>().text = $"To {(outTransitionList.itemsSource[i] as FSMTransition).To.UniqueName}";
        };
        inTransitionList.makeItem = () => outTransitionList.itemTemplate.Instantiate();
        SetCurState(null);
        SetCurTranstition(null);
    }
    #endregion

    #region State
    private void OnSelectState(IEnumerable<object> list)
    {
        if (list == null || list.Count() == 0)
        {
            SetCurState(null);
            return;
        }
        SetCurState(list.First() as FSMState);
    }

    private void AddState()
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
        if (fsm.BaseState == null) fsm.BaseState = nState;
    }

    private void RemoveState()
    {
        if (fsm == null || curState == null) return;

        fsm.States.Remove(curState);
        if (fsm.BaseState == curState) fsm.BaseState = null;
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(curState));
        stateList.Rebuild();
        if (fsm.States.Count() > 0) fsm.BaseState = fsm.States.First();
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


    //State의 이름을 변경했을때
    private void OnUniqueNameFieldChange(ChangeEvent<string> evt)
    {
        var transitionbaselable = rootVisualElement.Q<Label>("CurStateName");
        transitionbaselable.text = evt.newValue;

        var stateList = rootVisualElement.Q<ListView>("StateList");
        stateList.Rebuild();
    }
    #endregion

    #region Transition
    private void OnSelectTransition(IEnumerable<object> list, bool isInTrans)
    {
        if (list == null || list.Count() == 0)
        {
            SetCurTranstition(null);
            return;
        }

        if (!inTransitionList.selectedItem.Equals(curTransition)) inTransitionList.ClearSelection();
        if (!outTransitionList.selectedItem.Equals(curTransition)) outTransitionList.ClearSelection();

        SetCurTranstition(list.First() as FSMTransition);
    }

    private void AddTransition(bool isInTrans)
    {
        if (fsm == null) return;

        var nTrans = ScriptableObject.CreateInstance<FSMTransition>();
        nTrans.To = curState;
        nTrans.From = fsm.BaseState;
        if (!Directory.Exists($"{basePath}/Transitions"))
        {
            Directory.CreateDirectory($"{basePath}/Transitions");
        }
        AssetDatabase.CreateAsset(nTrans, $"{basePath}/Transitions/{System.Guid.NewGuid()}.asset");
        fsm.Transitions.Add(nTrans);
        UpdateTransitionList();
        inTransitionList.SetSelection(inTransitionList.itemsSource.IndexOf(nTrans));
    }

    private void RemoveTransition()
    {
        if (fsm == null || curState == null || curTransition == null) return;

        fsm.Transitions.Remove(curTransition);
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(curTransition));
        UpdateTransitionList();
        inTransitionList.ClearSelection();
    }

    private void UpdateTransitionList()
    {
        if (curState == null) return;

        List<FSMTransition> inT = new List<FSMTransition>();
        List<FSMTransition> outT = new List<FSMTransition>();
        foreach (var t in fsm.Transitions)
        {
            if (t.From.Equals(curState))
            {
                outT.Add(t);
            }
            else if (t.To.Equals(curState))
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
        bool IsOutTrans = transtition.From.Equals(curState);
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
    #endregion

    #region Tab
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
        if (curTab != null)
        {
            curTab.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        curTab = rootVisualElement.Q<VisualElement>(tabName);
        curTab.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
    } 
    #endregion
}
