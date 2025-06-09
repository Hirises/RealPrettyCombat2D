using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets._1._Scripts
{
    [Serializable]
    public class FSMCondition
    {
        public enum FSMConditionVariableType
        {
            Trigger,
            Bool,
            Int,
            Float
        }

        [SerializeField]
        public FSMConditionVariableType Type;
        [SerializeField]
        public string UniqueId;

        public enum FSMConditionVariableEquation
        {
            LesserThen,
            LessOrEqual,
            Equal,
            GreaterOrEqual,
            GreaterThen,
        }

        private bool IsNumericCondition => Type == FSMConditionVariableType.Float || Type == FSMConditionVariableType.Int;
        [ShowIf("Type", FSMConditionVariableType.Bool)]
        [Label("When")]
        [AllowNesting]
        [SerializeField]
        public bool BoolCondition_Ture;
        [ShowIf(nameof(IsNumericCondition))]
        [Label("When")]
        [AllowNesting]
        [SerializeField]
        public FSMConditionVariableEquation NumericCondition_EquationType;
        [ShowIf("Type", FSMConditionVariableType.Int)]
        [Label("Threshold")]
        [AllowNesting]
        [SerializeField]
        public int IntCondition_Threshold;
        [ShowIf("Type", FSMConditionVariableType.Float)]
        [AllowNesting]
        [Label("Threshold")]
        [SerializeField]
        public float FloatCondition_Threshold;

        public bool CheckCondition(Dictionary<string, object> variables)
        {
            if (!variables.ContainsKey(UniqueId)) throw new InvalidOperationException($"변수 {UniqueId}는 존재하지 않습니다!");

            switch (Type)
            {
                case FSMConditionVariableType.Trigger:
                    return (bool)variables[UniqueId];
                case FSMConditionVariableType.Bool:
                    return BoolCondition_Ture == (bool)variables[UniqueId];
                case FSMConditionVariableType.Int:
                    switch (NumericCondition_EquationType)
                    {
                        case FSMConditionVariableEquation.LesserThen:
                            return IntCondition_Threshold < (int)variables[UniqueId];
                        case FSMConditionVariableEquation.LessOrEqual:
                            return IntCondition_Threshold <= (int)variables[UniqueId];
                        case FSMConditionVariableEquation.Equal:
                            return IntCondition_Threshold == (int)variables[UniqueId];
                        case FSMConditionVariableEquation.GreaterOrEqual:
                            return IntCondition_Threshold >= (int)variables[UniqueId];
                        case FSMConditionVariableEquation.GreaterThen:
                            return IntCondition_Threshold > (int)variables[UniqueId];
                    }
                    return false;
                case FSMConditionVariableType.Float:
                    switch (NumericCondition_EquationType)
                    {
                        case FSMConditionVariableEquation.LesserThen:
                            return FloatCondition_Threshold < (float)variables[UniqueId];
                        case FSMConditionVariableEquation.LessOrEqual:
                            return FloatCondition_Threshold <= (float)variables[UniqueId];
                        case FSMConditionVariableEquation.Equal:
                            return FloatCondition_Threshold == (float)variables[UniqueId];
                        case FSMConditionVariableEquation.GreaterOrEqual:
                            return FloatCondition_Threshold >= (float)variables[UniqueId];
                        case FSMConditionVariableEquation.GreaterThen:
                            return FloatCondition_Threshold > (float)variables[UniqueId];
                    }
                    return false;
            }
            return false;
        }
    }
}