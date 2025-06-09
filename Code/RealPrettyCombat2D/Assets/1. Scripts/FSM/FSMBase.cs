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
        public string BaseState;
        [SerializeField]
        public List<FSMState> States = new List<FSMState>();
        [SerializeField]
        public List<FSMTransition> Transitions = new List<FSMTransition>();
        [SerializeField]
        public SerializableDictionaryBase<string, FSMCondition.FSMConditionVariableType> Variables = new SerializableDictionaryBase<string, FSMCondition.FSMConditionVariableType>();

        private Dictionary<string, List<FSMTransition>> TransitionMap;
        private List <FSMTransition> GenericTransitions;
        private Dictionary<string, FSMState> StateMap = new Dictionary<string, FSMState>();
        private Dictionary<string, object> VariableMap = new Dictionary<string, object>();

        private FSMState CurrentState;

        private void CreateStateMap()
        {
            StateMap = new Dictionary<string, FSMState>();
            foreach (var s in States)
            {
                StateMap.Add(s.UniqueName, s);
            }
        }

        public void Awake()
        {
            CreateStateMap();

            GenericTransitions = new List<FSMTransition>();
            TransitionMap = new Dictionary<string, List<FSMTransition>>();
            foreach(var t in Transitions)
            {
                if(t.From.Length == 0)
                {
                    GenericTransitions.Add(t);
                    continue;
                }

                foreach (var state in t.From)
                {
                    if (!TransitionMap.ContainsKey(state))
                    {
                        TransitionMap[state] = new List<FSMTransition>();
                    }
                    TransitionMap[state].Add(t);
                }
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

            CurrentState = StateMap[BaseState];
        }

        public void Update()
        {
            CheckTransition(VariableMap);
        }

        private void CheckTransition(Dictionary<string, object> variables)
        {
            if (TransitionMap.ContainsKey(CurrentState.UniqueName))
            {
                var list = TransitionMap[CurrentState.UniqueName];
                foreach (var trans in list)
                {
                    if (trans.CheckTransition(variables))
                    {
                        PerformTransition(trans);
                        return;
                    }
                }
            }

            foreach (var trans in GenericTransitions)
            {
                if (trans.CheckTransition(variables))
                {
                    PerformTransition(trans);
                    return;
                }
            }
        }

        private void PerformTransition(FSMTransition transition)
        {
            CurrentState = StateMap[transition.To];
        }
    }
}