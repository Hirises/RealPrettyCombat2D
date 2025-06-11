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
        [SerializeField]
        public FSMTransitionTiming TransitionTiming = FSMTransitionTiming.Immediate;
        [SerializeField]
        [ShowIf(nameof(TransitionTiming), FSMTransitionTiming.Custom)]
        public List<Vector2Int> CustomCancleFrames = new List<Vector2Int>();
        [SerializeField]
        public bool UseInputBuffer = false;
        [SerializeField]
        [ShowIf(nameof(UseInputBuffer))]
        [AllowNesting]
        public List<Vector2Int> CustomInputBufferFrames = new List<Vector2Int>();

        public enum FSMTransitionTiming
        {
            Immediate,
            EndOfAnimation,
            Custom
        }

        public bool CheckTransitionTiming(int currentFrame, int totalFrames)
        {
            switch (TransitionTiming)
            {
                case FSMTransitionTiming.Immediate:
                    return true;
                case FSMTransitionTiming.EndOfAnimation:
                    return currentFrame == totalFrames - 1;
                case FSMTransitionTiming.Custom:
                    foreach (var frame in CustomCancleFrames)
                    {
                        if (currentFrame >= frame.x && currentFrame <= frame.y)
                            return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

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