using Assets._1._Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VolumeComponent;

public class FSMManagerWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset VisualTreeAsset;

    private FSMBase fsm;
    private string basePath;
    private FSMState curState;
    private FSMTransition curTransition;
    private VisualElement curTab;
    private FSMBase.VarItem curVar;

    private ListView stateList;
    private ListView inTransitionList;
    private ListView outTransitionList;
    private ListView varList;
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

        //FSM 가져오기
        var targetFSMSelector = rootVisualElement.Q<ObjectField>("FSM");
        targetFSMSelector.RegisterValueChangedCallback(OnTargetFSMChange);

        //ShowAnimPreview();

        //State 리스트 생성
        stateList = rootVisualElement.Q<ListView>("StateList");
        stateList.selectionChanged += OnSelectState;
        stateList.bindItem = (e, i) =>
        {
            var s = (stateList.itemsSource[i] as FSMState);
            e.Q<Label>().text = $"{s.ModifiedName}";
        };
        stateList.makeItem = () => stateList.itemTemplate.Instantiate();

        //State 추가
        var addState = rootVisualElement.Q<Button>("Add");
        addState.clicked += AddState;
        var removeState = rootVisualElement.Q<Button>("Remove");
        removeState.clicked += RemoveState;
        var uniqueNameField = rootVisualElement.Q<TextField>("UniqueNameField");
        uniqueNameField.RegisterValueChangedCallback(OnUniqueNameFieldChange);


        //Transition 리스트 생성
        inTransitionList = rootVisualElement.Q<ListView>("InTransitionList");
        outTransitionList = rootVisualElement.Q<ListView>("OutTransitionList");
        inTransitionList.selectionChanged += (list) => OnSelectTransition(list, isInTrans: true);
        outTransitionList.selectionChanged += (list) => OnSelectTransition(list, isInTrans: false);
        inTransitionList.bindItem = (e, i) =>
        {
            var item = inTransitionList.itemsSource[i] as FSMTransition;
            if (item.From.Equals(curState)) e.Q<Label>().text = $"From Self";
            else e.Q<Label>().text = $"From {item.From.ModifiedName}";
        };
        inTransitionList.makeItem = inTransitionList.itemTemplate.Instantiate;
        outTransitionList.bindItem = (e, i) =>
        {
            var item = outTransitionList.itemsSource[i] as FSMTransition;
            if (item.To.Equals(curState)) e.Q<Label>().text = $"To Self";
            else e.Q<Label>().text = $"To {item.To.ModifiedName}";
        };
        inTransitionList.makeItem = outTransitionList.itemTemplate.Instantiate;

        //Transition 추가
        var addInTransBtn = rootVisualElement.Q<Button>("AddInTrans");
        addInTransBtn.clicked += () => AddTransition(isInTrans: true);
        var removeInTransBtn = rootVisualElement.Q<Button>("RemoveInTrans");
        removeInTransBtn.clicked += RemoveTransition;
        var addOutTransBtn = rootVisualElement.Q<Button>("AddOutTrans");
        addOutTransBtn.clicked += () => AddTransition(isInTrans: false);
        var removeOutTransBtn = rootVisualElement.Q<Button>("RemoveOutTrans");
        removeOutTransBtn.clicked += RemoveTransition;

        //VariableList 가져오기
        varList = rootVisualElement.Q<ListView>("VariableList");
        varList.selectionChanged += OnSelectVar;
        varList.bindItem = BindVar;
        varList.makeItem = varList.itemTemplate.Instantiate;

        //Variable 추가
        var addVarBtn = rootVisualElement.Q<Button>("AddVar");
        addVarBtn.clicked += AddVar;
        var removeVarBtn = rootVisualElement.Q<Button>("RemoveVar");
        removeVarBtn.clicked += RemoveVar;

        //탭 전환
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
    private void OnTargetFSMChange(ChangeEvent<UnityEngine.Object> evt)
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
            SetCurVar(null);
            stateList.itemsSource = null;
            stateList.Rebuild();
            inTransitionList.itemsSource = null;
            outTransitionList.itemsSource = null;
            inTransitionList.Rebuild();
            outTransitionList.Rebuild();
            inspector.visible = false;
            varList.itemsSource = null;
            varList.Rebuild();
            return;
        }

        stateList.itemsSource = fsm.States;
        varList.itemsSource = fsm.Variables;
        inspector.visible = true;
        basePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(fsm));
        SetCurState(null);
        SetCurTranstition(null);
        SetCurVar(null);
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
        AssetDatabase.CreateAsset(nState, $"{basePath}/States/{nState.UniqueName}_____{System.Guid.NewGuid()}.asset");
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
        var removeState = rootVisualElement.Q<Button>("Remove");
        inTransitionList.ClearSelection();
        outTransitionList.ClearSelection();
        if (curState == null)
        {
            removeState.enabledSelf = false;
            stateList.ClearSelection();
            SetCurTranstition(null);
            if(curTab.name.StartsWith("State") || curTab.name.StartsWith("Transition")) SetTab("FSM");
            return;
        }
        removeState.enabledSelf = true;
        var stateTab = rootVisualElement.Q<VisualElement>($"StateTab");
        var serializedState = new SerializedObject(curState);
        stateTab.Bind(serializedState);
        stateTab.Q<TextField>("UniqueNameField").RegisterCallback((FocusOutEvent e) =>
        {
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(curState), $"{curState.UniqueName}_____{System.Guid.NewGuid()}");
        });
        stateTab.Q<Toggle>().RegisterValueChangedCallback((e) =>
        {
            var transitionbaselable = rootVisualElement.Q<Label>("CurStateName");
            if (curState.MarkAsBlendingState) transitionbaselable.text = $"({curState.UniqueName})";
            else transitionbaselable.text = $"{curState.UniqueName}";

            var stateList = rootVisualElement.Q<ListView>("StateList");
            stateList.Rebuild();
        });
        transitionbaselable.text = $"{curState.ModifiedName}";
        SetCurTranstition(null);
        UpdateTransitionList();
        if (!curTab.name.StartsWith("State") && !curTab.name.StartsWith("Transition")) SetTab("State");
    }


    //State�� �̸��� ����������
    private void OnUniqueNameFieldChange(ChangeEvent<string> evt)
    {
        var transitionbaselable = rootVisualElement.Q<Label>("CurStateName");
        transitionbaselable.text = curState.MarkAsBlendingState ? $"({evt.newValue})" : $"{evt.newValue}";

        var stateList = rootVisualElement.Q<ListView>("StateList");
        stateList.Rebuild();
    }
    #endregion

    #region Transition
    private void OnSelectTransition(IEnumerable<object> list, bool isInTrans)
    {
        var removeInTransBtn = rootVisualElement.Q<Button>("RemoveInTrans");
        var removeOutTransBtn = rootVisualElement.Q<Button>("RemoveOutTrans");

        if (list == null || list.Count() == 0)
        {
            removeInTransBtn.enabledSelf = false;
            removeOutTransBtn.enabledSelf = false;
            SetCurTranstition(null);
            return;
        }

        if (!isInTrans) inTransitionList.ClearSelection();
        if (isInTrans) outTransitionList.ClearSelection();

        if (isInTrans) removeInTransBtn.enabledSelf = true;
        if (!isInTrans) removeOutTransBtn.enabledSelf = true;

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
        SetCurTranstition(null);
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
        inT = inT.OrderByDescending(t => t.priority).ToList();
        outT = outT.OrderByDescending(t => t.priority).ToList();

        inTransitionList.itemsSource = inT;
        outTransitionList.itemsSource = outT;

        if(inTransitionList.selectedItem != null)
        {
            inTransitionList.SetSelectionWithoutNotify(new int[] { inTransitionList.itemsSource.IndexOf(curTransition) });
        }
        else if (outTransitionList.selectedItem != null)
        {
            outTransitionList.SetSelectionWithoutNotify(new int[] { outTransitionList.itemsSource.IndexOf(curTransition) });
        }
    }

    private void SetCurTranstition(FSMTransition transtition)
    {
        curTransition = transtition;
        var inspector = rootVisualElement.Q<VisualElement>("TransitionInfo");
        if (curTransition == null)
        {
            inspector.style.visibility = new StyleEnum<Visibility>(Visibility.Hidden);
            return;
        }
        inspector.style.visibility = new StyleEnum<Visibility>(Visibility.Visible);

        var obj = new SerializedObject(curTransition);
        inspector.Bind(obj);
        
        bool IsSelfTrans = transtition.To.Equals(transtition.From);
        bool IsOutTrans = transtition.From.Equals(curState);
        bool showFrom = IsSelfTrans || !IsOutTrans;
        bool showTo = IsSelfTrans || IsOutTrans;
        inspector.Q<ObjectField>("From").style.display = new StyleEnum<DisplayStyle>(showFrom ? DisplayStyle.Flex : DisplayStyle.None);
        inspector.Q<ObjectField>("From").RegisterValueChangedCallback((e) => UpdateTransitionList());
        inspector.Q<ObjectField>("To").style.display = new StyleEnum<DisplayStyle>(showTo ? DisplayStyle.Flex : DisplayStyle.None);
        inspector.Q<ObjectField>("To").RegisterValueChangedCallback((e) => UpdateTransitionList());
        
        inspector.Q<IntegerField>("Priority").RegisterValueChangedCallback((e) => UpdateTransitionList());
        
        inspector.Q<EnumField>("TransitionTiming").style.display = new StyleEnum<DisplayStyle>(transtition.From.MarkAsBlendingState ? DisplayStyle.None : DisplayStyle.Flex);
        inspector.Q<EnumField>("TransitionTiming").RegisterValueChangedCallback((e) => {
            inspector.Q<VisualElement>("CustomCancleFrames").style.display = new StyleEnum<DisplayStyle>(
                curTransition.TransitionTiming == FSMTransition.FSMTransitionTiming.Custom ? DisplayStyle.Flex : DisplayStyle.None);
        });
        inspector.Q<VisualElement>("CustomCancleFrames").style.display = new StyleEnum<DisplayStyle>(
            curTransition.TransitionTiming == FSMTransition.FSMTransitionTiming.Custom ? DisplayStyle.Flex : DisplayStyle.None);

        inspector.Q<Toggle>("UseInputBuffer").style.display = new StyleEnum<DisplayStyle>(transtition.From.MarkAsBlendingState ? DisplayStyle.None : DisplayStyle.Flex);
        inspector.Q<Toggle>("UseInputBuffer").RegisterValueChangedCallback((e) => {
            inspector.Q<VisualElement>("CustomInputBufferFrames").style.display = new StyleEnum<DisplayStyle>(
                e.newValue ? DisplayStyle.Flex : DisplayStyle.None);
        });
        inspector.Q<VisualElement>("CustomInputBufferFrames").style.display = new StyleEnum<DisplayStyle>(
            curTransition.UseInputBuffer ? DisplayStyle.Flex : DisplayStyle.None);

    }
    #endregion

    #region Variables
    private void OnSelectVar(IEnumerable<object> list)
    {
        if (list == null || list.Count() == 0)
        {
            SetCurVar(null);
            return;
        }
        SetCurVar(list.First() as FSMBase.VarItem);
    }

    private void SetCurVar(FSMBase.VarItem item)
    {
        curVar = item;
        var e = rootVisualElement.Q<VisualElement>("VariableInfo");
        if (curVar == null)
        {
            e.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            return;
        }
        e.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        var name = e.Q<TextField>();
        name.value = curVar.Name;
        name.RegisterValueChangedCallback((evt) =>
        {
            curVar.Name = name.value;
            varList.Rebuild();
        });
        var type = e.Q<EnumField>();
        type.value = curVar.Type;
        type.RegisterValueChangedCallback((evt) =>
        {
            if (curVar.Type == (FSMCondition.FSMConditionVariableType)evt.newValue) return;
            curVar.Type = (FSMCondition.FSMConditionVariableType)evt.newValue;
            varList.Rebuild();
            RebuildVarInfo();
        });

        RebuildVarInfo();
    }

    private void RebuildVarInfo()
    {
        if (curVar == null) return;

        var e = rootVisualElement.Q<VisualElement>("VariableInfo");
        var intF = e.Q<IntegerField>();
        intF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        intF.value = curVar.Base_Int;
        intF.RegisterValueChangedCallback((evt) =>
        {
            curVar.Base_Int = evt.newValue;
            varList.Rebuild();
        });
        var floatF = e.Q<FloatField>();
        floatF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        floatF.value = curVar.Base_Float;
        floatF.RegisterValueChangedCallback((evt) =>
        {
            curVar.Base_Float = evt.newValue;
            varList.Rebuild();
        });
        var boolF = e.Q<Toggle>();
        boolF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        boolF.value = curVar.Base_Bool;
        boolF.RegisterValueChangedCallback((evt) =>
        {
            curVar.Base_Bool = evt.newValue;
            varList.Rebuild();
        });
        switch (curVar.Type)
        {
            case FSMCondition.FSMConditionVariableType.Trigger:
                break;
            case FSMCondition.FSMConditionVariableType.Int:
                intF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                break;
            case FSMCondition.FSMConditionVariableType.Float:
                floatF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                break;
            case FSMCondition.FSMConditionVariableType.Bool:
                boolF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                break;
        }
    }

    private void BindVar(VisualElement e, int i)
    {
        var item = varList.itemsSource[i] as FSMBase.VarItem;
        var name = e.Q<Label>();
        name.text = item.Name;

        var intF = e.Q<IntegerField>();
        intF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        intF.value = item.Base_Int;
        intF.RegisterValueChangedCallback((evt) =>
        {
            item.Base_Int = evt.newValue;
            RebuildVarInfo();
        });
        var floatF = e.Q<FloatField>();
        floatF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        floatF.value = item.Base_Float;
        floatF.RegisterValueChangedCallback((evt) =>
        {
            item.Base_Float = evt.newValue;
            RebuildVarInfo();
        });
        var boolF = e.Q<Toggle>();
        boolF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        boolF.value = item.Base_Bool;
        boolF.RegisterValueChangedCallback((evt) =>
        {
            item.Base_Bool = evt.newValue;
            RebuildVarInfo();
        });
        switch (item.Type)
        {
            case FSMCondition.FSMConditionVariableType.Trigger:
                break;
            case FSMCondition.FSMConditionVariableType.Int:
                intF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                break;
            case FSMCondition.FSMConditionVariableType.Float:
                floatF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                break;
            case FSMCondition.FSMConditionVariableType.Bool:
                boolF.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                break;
        }
    }

    private void AddVar()
    {
        if (fsm == null) return;

        var inst = new FSMBase.VarItem();
        inst.Name = "New Variable";
        inst.Type = FSMCondition.FSMConditionVariableType.Bool;
        inst.Base_Bool = false;
        fsm.Variables.Add(inst);
        varList.Rebuild();
        varList.SetSelection(varList.itemsSource.IndexOf(inst));
    }

    private void RemoveVar()
    {
        var sel = varList.selectedItem as FSMBase.VarItem;
        if (sel == null) return;

        fsm.Variables.Remove(sel);
        varList.Rebuild();
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


    #region [Obsolete] Anim

    GameObject gameObject;
    AnimationClip clip;
    Editor gameObjectEditor;
    float time = 0.0f;
    PreviewRenderUtility previewRenderUtility;
    GameObject previewObject;

    [Obsolete]
    private void ShowAnimPreview()
    {
        var preview = rootVisualElement.Q<IMGUIContainer>("Preview");
        preview.onGUIHandler = () =>
        {
            gameObject = (GameObject)EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true);
            clip = (AnimationClip)EditorGUILayout.ObjectField(clip, typeof(AnimationClip), true);
            time = EditorGUILayout.FloatField(time);

            GUIStyle bgColor = new GUIStyle();
            bgColor.normal.background = EditorGUIUtility.whiteTexture;

            if (gameObject != null && clip != null)
            {
                if (gameObjectEditor == null)
                {
                    gameObjectEditor = Editor.CreateEditor(gameObject);
                    previewRenderUtility = new PreviewRenderUtility(true, true);
                    previewRenderUtility.cameraFieldOfView = 30f;
                    previewRenderUtility.camera.nearClipPlane = 0.3f;
                    previewRenderUtility.camera.farClipPlane = 1000;
                    previewObject = Instantiate(gameObject);
                    previewObject.hideFlags = HideFlags.HideAndDontSave;
                    previewRenderUtility.AddSingleGO(previewObject);
                }
                if (gameObjectEditor.target != gameObject)
                {
                    previewRenderUtility.Cleanup();
                    DestroyImmediate(gameObjectEditor);
                    gameObjectEditor = Editor.CreateEditor(gameObject);
                    previewRenderUtility = new PreviewRenderUtility(true, true);
                    previewRenderUtility.cameraFieldOfView = 30f;
                    previewRenderUtility.camera.nearClipPlane = 0.3f;
                    previewRenderUtility.camera.farClipPlane = 1000;
                }
                Debug.Log("run");
                //time += Time.deltaTime;
                //if (time > clip.length) time = 0.0f;
                //AnimationMode.StartAnimationMode();
                //AnimationMode.BeginSampling();
                //AnimationMode.SampleAnimationClip(gameObject, clip, time);
                //AnimationMode.EndSampling();
                //gameObjectEditor.DrawPreview(GUILayoutUtility.GetRect(256, 256));
                //AnimationMode.StopAnimationMode();
                Rect r = GUILayoutUtility.GetRect(256, 256);
                previewRenderUtility.BeginPreview(GUILayoutUtility.GetRect(256, 256), bgColor);

                var previewCamera = previewRenderUtility.camera;

                previewCamera.transform.position =
                    gameObject.transform.position + new Vector3(0, 2.5f, -5);

                previewCamera.transform.LookAt(gameObject.transform);

                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(previewObject, clip, time);

                previewCamera.Render();

                previewRenderUtility.EndAndDrawPreview(r);

                AnimationMode.EndSampling();
                AnimationMode.StopAnimationMode();
            }
            //gameObjectEditor.ReloadPreviewInstances();
            //gameObjectEditor.OnPreviewGUI(GUILayoutUtility.GetRect(256, 256), bgColor);
            //gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(256, 256), bgColor);
        };
    } 
    #endregion
}
