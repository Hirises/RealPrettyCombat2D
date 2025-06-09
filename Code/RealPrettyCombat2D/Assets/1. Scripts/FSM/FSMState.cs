using System;
using System.Collections;
using UnityEngine;

namespace Assets._1._Scripts
{
    [CreateAssetMenu(fileName = "State", menuName = "FSM/State")]
    public class FSMState : ScriptableObject
    {
        [SerializeField]
        public string UniqueName;
        [SerializeField]
        public AnimationClip Animation;
    }
}