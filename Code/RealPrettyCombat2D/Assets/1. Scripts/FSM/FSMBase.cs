using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using System;

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

        private FSMState CurrentState;

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
            foreach(var t in Transitions)
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
                VariableMap.Add(v.Name, v.Type switch { 
                    FSMCondition.FSMConditionVariableType.Bool => v.Base_Bool,
                    FSMCondition.FSMConditionVariableType.Int => v.Base_Int,
                    FSMCondition.FSMConditionVariableType.Float => v.Base_Float,
                    FSMCondition.FSMConditionVariableType.Trigger => true,
                    _ => null
                });
            }

            CurrentState = BaseState;
        }

        public void Update()
        {
            CheckTransition(VariableMap);
        }

        private void CheckTransition(Dictionary<string, object> variables)
        {
            if (TransitionMap.ContainsKey(CurrentState))
            {
                var list = TransitionMap[CurrentState];
                foreach (var trans in list)
                {
                    if (trans.CheckTransition(variables))
                    {
                        PerformTransition(trans);
                        return;
                    }
                }
            }
        }

        private void PerformTransition(FSMTransition transition)
        {
            CurrentState = transition.To;
            Debug.Log($"Change State To {CurrentState.UniqueName}");

            Anim.Play(CurrentState.UniqueName);
        }

        public void SetVariable(string name, object value)
        {
            VariableMap[name] = value;
        }
    }
}