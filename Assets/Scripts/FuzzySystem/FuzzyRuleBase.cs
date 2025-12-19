using UnityEngine;
using ParkingSystem.FuzzySystem.Inputs;

namespace ParkingSystem.FuzzySystem.Interfaces
{
    /// <summary>
    /// База нечетких правил (ScriptableObject)
    /// Создается и настраивается вторым разработчиком
    /// </summary>
    [CreateAssetMenu(fileName = "NewFuzzyRuleBase", menuName = "Parking System/Fuzzy Rule Base")]
    public class FuzzyRuleBase : ScriptableObject
    {
        [Header("Настройки правил")]
        public FuzzyRule[] parkingRules;        // Правила парковки
        public float ruleWeightMultiplier = 1f; // Множитель весов правил

        [Header("Метод дефаззификации")]
        public DefuzzificationMethod defuzzMethod = DefuzzificationMethod.Centroid;

        [Header("Агрегация правил")]
        public AggregationMethod aggregationMethod = AggregationMethod.Max;

        /// <summary>
        /// Получить правило по имени
        /// </summary>
        public FuzzyRule GetRule(string ruleName)
        {
            foreach (var rule in parkingRules)
            {
                if (rule.ruleName == ruleName)
                    return rule;
            }
            return null;
        }

        /// <summary>
        /// Получить все правила для определенной фазы парковки
        /// </summary>
        public FuzzyRule[] GetRulesForPhase(ParkingPhase phase)
        {
            System.Collections.Generic.List<FuzzyRule> phaseRules =
                new System.Collections.Generic.List<FuzzyRule>();

            foreach (var rule in parkingRules)
            {
                if (rule.applicablePhase == phase && rule.isEnabled)
                    phaseRules.Add(rule);
            }

            return phaseRules.ToArray();
        }
    }

    /// <summary>
    /// Нечеткое правило
    /// </summary>
    [System.Serializable]
    public class FuzzyRule
    {
        public string ruleName;                  // Имя правила
        public string description;               // Описание правила
        public ParkingPhase applicablePhase;     // Фаза парковки
        public bool isEnabled = true;           // Включено ли правило
        public float weight = 1.0f;             // Вес правила

        [Header("Условия (ЕСЛИ)")]
        public RuleCondition[] conditions;       // Антецеденты

        [Header("Действия (ТО)")]
        public RuleAction[] actions;             // Консеквенты

        /// <summary>
        /// Проверка, применимо ли правило к текущим данным
        /// </summary>
        public bool IsApplicable(FuzzyInputData inputData)
        {
            // Логика проверки условий будет реализована вторым разработчиком
            return true;
        }
    }

    /// <summary>
    /// Условие правила
    /// </summary>
    [System.Serializable]
    public struct RuleCondition
    {
        public string variableName;      // Имя переменной (FrontDistance, AlignmentAngle и т.д.)
        public string termName;          // Имя терма (VeryClose, SmallLeft и т.д.)
        public FuzzyOperator @operator;  // Оператор (Is, IsNot)
    }

    /// <summary>
    /// Действие правила
    /// </summary>
    [System.Serializable]
    public struct RuleAction
    {
        public string outputVariable;    // Выходная переменная (Throttle, Steering и т.д.)
        public string outputTerm;        // Выходной терм (Slow, TurnLeft и т.д.)
        public float confidence;         // Уверенность (0-1)
    }

    /// <summary>
    /// Операторы нечеткой логики
    /// </summary>
    public enum FuzzyOperator
    {
        Is,     // Является
        IsNot,  // Не является
        And,    // И
        Or      // Или
    }

    /// <summary>
    /// Методы дефаззификации
    /// </summary>
    public enum DefuzzificationMethod
    {
        Centroid,   // Центроид
        Bisector,   // Биссектриса
        MeanOfMax,  // Среднее максимумов
        Max         // Максимум
    }

    /// <summary>
    /// Методы агрегации
    /// </summary>
    public enum AggregationMethod
    {
        Max,    // Максимум
        Sum,    // Сумма
        Prod    // Произведение
    }
}