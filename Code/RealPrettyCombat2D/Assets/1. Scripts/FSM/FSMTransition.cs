using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._1._Scripts
{
    [CreateAssetMenu(fileName ="Transition", menuName = "FSM/Transition")]
    public class FSMTransition : ScriptableObject
    {
        [SerializeField]
        public FSMState From;
        [SerializeField]
        public FSMState To;
        [SerializeField]
        public int priority;
        [SerializeField]
        public FSMCondition[] Conditions;

        public bool CheckTransition(Dictionary<string, object> variables)
        {
            foreach (var c in Conditions)
            {
                if (!c.CheckCondition(variables)) return false;
            }
            return true;
        }
    }
}