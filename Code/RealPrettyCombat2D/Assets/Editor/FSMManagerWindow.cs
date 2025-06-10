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
    private List<VisualElement> TabButtonList = new List<VisualElement>();

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
        var targetFSMSelector = rootVisualElement.Q<ObjectField>("FSM");
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
        var fsmTab = rootVisualElement.Q<VisualElement>("FSMTabButton");
        fsmTab.RegisterCallback<ClickEvent>((e) => SetTab("FSM"));
        fsmTab.style.opacity = 0.5f;
        TabButtonList.Add(fsmTab);
        var stateTab = rootVisualElement.Q<VisualElement>("StateTabButton");
        stateTab.RegisterCallback<ClickEvent>((e) => SetTab("State"));
        stateTab.style.opacity = 0.5f;
        TabButtonList.Add(stateTab);
        var transitionTab = rootVisualElement.Q<VisualElement>("TransitionTabButton");
        transitionTab.RegisterCallback<ClickEvent>((e) => SetTab("Transition"));
        transitionTab.style.opacity = 0.5f;
        TabButtonList.Add(transitionTab);
        var varTab = rootVisualElement.Q<VisualElement>("VariablesTabButton");
        varTab.RegisterCallback<ClickEvent>((e) => SetTab("Variables"));
        varTab.style.opacity = 0.5f;
        TabButtonList.Add(varTab);
        rootVisualElement.Q<VisualElement>("FSMTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        rootVisualElement.Q<VisualElement>("StateTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        rootVisualElement.Q<VisualElement>("TransitionTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        rootVisualElement.Q<VisualElement>("VariablesTab").style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

        //초기화
        SetTab("FSM");
        SetCurFSM(null);

    } 
    #endregion


    #region FSM
    private void OnTargetFSMChange(ChangeEvent<Object> evt)
    {
        SetCurFSM(evt.newValue as FSMBase);
    }

    private void SetCurFSM(FSMBase value)
    {
        fsm = value;
        var stateList = rootVisualElement.Q<ListView>("StateList");
        var inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        var outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        var fsmTab = rootVisualElement.Q<VisualElement>($"FSMTab");
        var inspector = rootVisualElement.Q<VisualElement>($"InspectorCol");
        if (value.IsUnityNull())
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
            inspector.visible = false;
            return;
        }

        inspector.visible = true;
        basePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(fsm));
        stateList.bindItem = (e, i) =>
        {
            e.Q<Label>().text = $"{(stateList.itemsSource[i] as FSMState).UniqueName}";
        };
        stateList.itemsSource = fsm.States;
        stateList.makeItem = () => stateList.itemTemplate.Instantiate();
        inTransitionList.bindItem = (e, i) =>
        {
            var item = inTransitionList.itemsSource[i] as FSMTransition;
            if (item.From.Equals(curState)) e.Q<Label>().text = $"From Self";
            else e.Q<Label>().text = $"From {item.From.UniqueName}";
        };
        inTransitionList.makeItem = () => inTransitionList.itemTemplate.Instantiate();
        outTransitionList.bindItem = (e, i) =>
        {
            var item = outTransitionList.itemsSource[i] as FSMTransition;
            if (item.To.Equals(curState)) e.Q<Label>().text = $"To Self";
            else e.Q<Label>().text = $"To {item.To.UniqueName}";
        };
        inTransitionList.makeItem = () => outTransitionList.itemTemplate.Instantiate();
        SetCurState(null);
        SetCurTranstition(null);
        fsmTab.Bind(new SerializedObject(fsm));
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
        var transitionbaselable = rootVisualElement.Q<Label>("CurStateName");
        inTransitionList.ClearSelection();
        outTransitionList.ClearSelection();
        if (curState == null)
        {
            stateList.ClearSelection();
            SetCurTranstition(null);
            if(curTab.name.StartsWith("State") || curTab.name.StartsWith("Transition")) SetTab("FSM");
            return;
        }
        var stateTab = rootVisualElement.Q<VisualElement>($"StateTab");
        var serializedState = new SerializedObject(curState);
        stateTab.Bind(serializedState);
        transitionbaselable.text = curState.UniqueName;
        SetCurTranstition(null);
        UpdateTransitionList();
        if (!curTab.name.StartsWith("State") && !curTab.name.StartsWith("Transition")) SetTab("State");
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

        if (!isInTrans) inTransitionList.ClearSelection();
        if (isInTrans) outTransitionList.ClearSelection();

        SetCurTranstition(list.First() as FSMTransition);
    }

    private void AddTransition(bool isInTrans)
    {
        if (fsm == null) return;

        var nTrans = ScriptableObject.CreateInstance<FSMTransition>();
        nTrans.From = isInTrans ? fsm.BaseState : curState;
        nTrans.To = isInTrans ? curState : fsm.BaseState;

        if (!Directory.Exists($"{basePath}/Transitions"))
        {
            Directory.CreateDirectory($"{basePath}/Transitions");
        }
        AssetDatabase.CreateAsset(nTrans, $"{basePath}/Transitions/{System.Guid.NewGuid()}.asset");

        fsm.Transitions.Add(nTrans);
        UpdateTransitionList();

        if(isInTrans) inTransitionList.SetSelection(inTransitionList.itemsSource.IndexOf(nTrans));
        else outTransitionList.SetSelection(outTransitionList.itemsSource.IndexOf(nTrans));
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
            if (t.To.Equals(curState))
            {
                inT.Add(t);
            }
        }

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
        bool IsSelfTrans = transtition.To.Equals(transtition.From);
        bool IsOutTrans = transtition.From.Equals(curState);
        SetPropertyFieldBindings(inspector, obj, IsSelfTrans ? "" : (IsOutTrans ? "From" : "To"));
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
    private void SetTab(string tabName)
    {
        if (curTab != null)
        {
            curTab.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        curTab = rootVisualElement.Q<VisualElement>($"{tabName}Tab");
        curTab.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        foreach(var btn in TabButtonList)
        {
            btn.style.opacity = btn.name.StartsWith(tabName) ? 1f : 0.5f;
        }
    } 
    #endregion
}
