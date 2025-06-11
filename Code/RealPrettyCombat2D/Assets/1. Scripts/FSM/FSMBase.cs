using NaughtyAttributes;
using RotaryHeart.Lib.SerializableDictionary;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

namespace Assets._1._Scripts
{
    [CreateAssetMenu(fileName = "FSMBase", menuName = "FSM/Base")]
    public class FSMBase : ScriptableObject
    {
        [SerializeField]
        public FSMState BaseState;
        [SerializeField]
        public List<FSMState> States = new List<FSMState>();
        [SerializeField]
        public List<FSMTransition> Transitions = new List<FSMTransition>();
        [SerializeField]
        public List<VarItem> Variables = new List<VarItem>();

        private Dictionary<FSMState, List<FSMTransition>> TransitionMap;
        private Dictionary<string, object> VariableMap = new Dictionary<string, object>();
        private Animator Anim;
        private Dictionary<string, List<Action>> TriggerEventMap = new Dictionary<string, List<Action>>();

        private FSMState CurrentState;
        /// <summary>
        /// 선입력된 트렌지션
        /// </summary>
        private HashSet<FSMTransition> QueuedTransition = new HashSet<FSMTransition>();

        #region class VarItem
        [Serializable]
        public class VarItem
        {
            [SerializeField]
            public string Name;
            [SerializeField]
            public FSMCondition.FSMConditionVariableType Type;
            [ShowIf(nameof(FSMCondition.FSMConditionVariableType), FSMCondition.FSMConditionVariableType.Int)]
            [AllowNesting]
            [SerializeField]
            public int Base_Int;
            [ShowIf(nameof(FSMCondition.FSMConditionVariableType), FSMCondition.FSMConditionVariableType.Float)]
            [AllowNesting]
            [SerializeField]
            public float Base_Float;
            [ShowIf(nameof(FSMCondition.FSMConditionVariableType), FSMCondition.FSMConditionVariableType.Bool)]
            [AllowNesting]
            [SerializeField]
            public bool Base_Bool;
        }
        #endregion


        public void Init(Animator anim)
        {
            this.Anim = anim;

            TransitionMap = new Dictionary<FSMState, List<FSMTransition>>();
            foreach (var t in Transitions)
            {
                var state = t.From;
                if (!TransitionMap.ContainsKey(state))
                {
                    TransitionMap[state] = new List<FSMTransition>();
                }
                TransitionMap[state].Add(t);
            }

            foreach (var v in Variables)
            {
                VariableMap.Add(v.Name, v.Type switch
                {
                    FSMCondition.FSMConditionVariableType.Bool => v.Base_Bool,
                    FSMCondition.FSMConditionVariableType.Int => v.Base_Int,
                    FSMCondition.FSMConditionVariableType.Float => v.Base_Float,
                    FSMCondition.FSMConditionVariableType.Trigger => false,
                    _ => null
                });
            }

            CurrentState = BaseState;
            QueuedTransition.Clear();
            CurrentState.TriggerOnEnter(VariableMap, this);
        }

        public void Update()
        {
            CurrentState.TriggerEvent(GetCurrentFrame(), VariableMap, this);
            CheckTransition(VariableMap);
            foreach (var variable in Variables)
            {
                if (variable.Type == FSMCondition.FSMConditionVariableType.Trigger && VariableMap.ContainsKey(variable.Name) && (bool)VariableMap[variable.Name])
                {
                    // Reset trigger variable after use
                    VariableMap[variable.Name] = false;
                }
            }
        }

        private int GetTotalFrames()
        {
            if (Anim == null) return 0;
            var stateInfo = Anim.GetCurrentAnimatorClipInfo(0)[0];
            return Mathf.FloorToInt(stateInfo.clip.length * stateInfo.clip.frameRate);
        }

        private int GetCurrentFrame()
        {
            if (Anim == null) return 0;
            var stateInfo = Anim.GetCurrentAnimatorClipInfo(0)[0];
            float normalTime = Anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f; // Get the normalized time (0 to 1)
            return Mathf.FloorToInt(normalTime * stateInfo.clip.length * stateInfo.clip.frameRate);
        }

        private void CheckTransition(Dictionary<string, object> variables)
        {
            FSMTransition availableTrans = null;
            int currentFrame = GetCurrentFrame();
            int totalFrames = GetTotalFrames();

            //선입력 검사
            foreach (var trans in QueuedTransition)
            {
                if (trans.CheckTransitionTiming(currentFrame, totalFrames))
                {
                    //수행가능한 타이밍이면 -> 트렌지션 추가
                    if (availableTrans == null || trans.priority > availableTrans.priority) availableTrans = trans;
                }
            }

            //새로운 트렌지션 검사
            if (TransitionMap.ContainsKey(CurrentState))
            {
                var list = TransitionMap[CurrentState];
                foreach (var trans in list)
                {
                    if (trans.From.MarkAsBlendingState && trans.CheckTransition(variables)) //블렌딩 스테이트에서 나가는 트렌지션이라면, 타이밍 체크를 생략
                    {
                        if (availableTrans == null || trans.priority > availableTrans.priority) availableTrans = trans;
                    }
                    else if (trans.CheckTransition(variables))
                    {
                        if(trans.CheckTransitionTiming(currentFrame, totalFrames))
                        {
                            //수행가능한 타이밍이면 -> 트렌지션 추가
                            if (availableTrans == null || trans.priority > availableTrans.priority) availableTrans = trans;
                        }
                        else
                        {
                            //수행 불가능한 타이밍이면 -> 선입력 추가
                            if (trans.UseInputBuffer) foreach (var frame in trans.CustomInputBufferFrames)
                                    if (currentFrame >= frame.x && currentFrame <= frame.y) QueuedTransition.Add(trans);
                        }
                    }
                }

                //결정된 트렌지션 수행
                if (availableTrans != null) PerformTransition(availableTrans);
            }

        }

        private void PerformTransition(FSMTransition transition)
        {
            Debug.Log($"Change State From {CurrentState.UniqueName} To {transition.To.UniqueName}");
            CurrentState.TriggerOnExit(VariableMap, this);
            CurrentState = transition.To;
            QueuedTransition.Clear();
            CurrentState.TriggerOnEnter(VariableMap, this);

            if (CurrentState.MarkAsBlendingState)
            {
                //블렌딩 스테이트라면 -> 즉시 트렌지션 체크
                CheckTransition(VariableMap);
                return;
            }

            Anim.Play(CurrentState.UniqueName);
        }

        #region Getter & Setter
        public void AddTriggerEvent(string triggerName, Action action)
        {
            if (Variables.Find(v => v.Name == triggerName && v.Type == FSMCondition.FSMConditionVariableType.Trigger) == null)
            {
                Debug.LogWarning($"Trigger {triggerName} does not exist in FSMBase.");
                return;
            }
            if (!TriggerEventMap.ContainsKey(triggerName))
            {
                TriggerEventMap[triggerName] = new List<Action>();
            }
            TriggerEventMap[triggerName].Add(action);
        }

        public void RemoveTriggerEvent(string triggerName, Action action)
        {
            if (!TriggerEventMap.ContainsKey(triggerName))
            {
                Debug.LogWarning($"Trigger {triggerName} does not exist in FSMBase.");
                return;
            }
            TriggerEventMap[triggerName].Remove(action);
        }

        public void SetVariable(string name, object value)
        {
            if (!VariableMap.ContainsKey(name))
            {
                Debug.LogWarning($"Variable {name} does not exist in FSMBase. Creating a new one.");
                return;
            }
            VariableMap[name] = value;

            if (value is bool && TriggerEventMap.ContainsKey(name) && TriggerEventMap[name].Count > 0 && (bool)value)
            {
                foreach (var action in TriggerEventMap[name])
                {
                    action.Invoke();
                }
            }
        }

        public T GetVariable<T>(string name)
        {
            if (!VariableMap.ContainsKey(name))
            {
                Debug.LogWarning($"Variable {name} does not exist in FSMBase.");
                return default;
            }
            return (T)VariableMap[name];
        } 
        #endregion
    }
}