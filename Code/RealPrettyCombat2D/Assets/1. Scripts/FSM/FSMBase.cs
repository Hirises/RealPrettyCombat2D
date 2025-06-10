using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;

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
        public SerializableDictionaryBase<string, FSMCondition.FSMConditionVariableType> Variables = new SerializableDictionaryBase<string, FSMCondition.FSMConditionVariableType>();

        private Dictionary<FSMState, List<FSMTransition>> TransitionMap;
        private Dictionary<string, object> VariableMap = new Dictionary<string, object>();
        private Animator Anim;

        private FSMState CurrentState;

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

            foreach (var v in Variables.Keys)
            {
                VariableMap.Add(v, Variables[v] switch
                {
                    FSMCondition.FSMConditionVariableType.Trigger => false,
                    FSMCondition.FSMConditionVariableType.Bool => false,
                    FSMCondition.FSMConditionVariableType.Int => 0,
                    FSMCondition.FSMConditionVariableType.Float => 0f,
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