using NaughtyAttributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._1._Scripts
{
    [CreateAssetMenu(fileName = "State", menuName = "FSM/State")]
    public class FSMState : ScriptableObject
    {
        [SerializeField]
        public string UniqueName;
        [HideInInspector]
        public string ModifiedName => MarkAsBlendingState ? $"({UniqueName})" : UniqueName;
        [SerializeField]
        public bool MarkAsBlendingState = false;
        [SerializeField]
        public List<FSMStateEvent> Events = new List<FSMStateEvent>();

        [System.Serializable]
        public class FSMStateEvent
        {
            public enum EventTiming
            {
                OnEnter,
                OnExit,
                CustomFrame,
            }

            public EventTiming timing;
            [ShowIf(nameof(timing), EventTiming.CustomFrame)]
            [AllowNesting]
            public int targetFrame;
            public string variableName;
            public FSMCondition.FSMConditionVariableType variableType;
            [ShowIf(nameof(variableType), FSMCondition.FSMConditionVariableType.Int)]
            [AllowNesting]
            public int intValue;
            [ShowIf(nameof(variableType), FSMCondition.FSMConditionVariableType.Float)]
            [AllowNesting]
            public float floatValue;
            [ShowIf(nameof(variableType), FSMCondition.FSMConditionVariableType.Bool)]
            [AllowNesting]
            public bool boolValue;
        }

        public void TriggerEvent(int currentFrame, Dictionary<string, object> variables, FSMBase fsm)
        {
            foreach (var e in Events)
            {
                if (e.timing == FSMStateEvent.EventTiming.CustomFrame && e.targetFrame == currentFrame)
                {
                    RunEvent(e, variables, fsm);
                }
            }
        }

        public void TriggerOnEnter(Dictionary<string, object> variables, FSMBase fsm)
        {
            foreach (var e in Events)
            {
                if (e.timing == FSMStateEvent.EventTiming.OnEnter)
                {
                    RunEvent(e, variables, fsm);
                }
            }
        }

        public void TriggerOnExit(Dictionary<string, object> variables, FSMBase fsm)
        {
            foreach (var e in Events)
            {
                if (e.timing == FSMStateEvent.EventTiming.OnExit)
                {
                    RunEvent(e, variables, fsm);
                }
            }
        }

        private void RunEvent(FSMStateEvent e, Dictionary<string, object> variables, FSMBase fsm)
        {
            switch (e.variableType)
            {
                case FSMCondition.FSMConditionVariableType.Int:
                    fsm.SetVariable(e.variableName, e.intValue);
                    break;
                case FSMCondition.FSMConditionVariableType.Float:
                    fsm.SetVariable(e.variableName, e.floatValue);
                    break;
                case FSMCondition.FSMConditionVariableType.Bool:
                    fsm.SetVariable(e.variableName, e.boolValue);
                    break;
                case FSMCondition.FSMConditionVariableType.Trigger:
                    fsm.SetVariable(e.variableName, true);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported variable type: {e.variableType}");
            }
        }
    }
}