using ParkingSystem.FuzzySystem.Inputs;
using ParkingSystem.Integration;
using UnityEngine;

namespace ParkingSystem.FuzzySystem.Interfaces
{
    /// <summary>
    /// Интерфейс движка нечетких правил
    /// </summary>
    public interface IFuzzyRuleEngine
    {
        /// <summary>
        /// Выполнить все правила на основе фаззифицированных входных данных
        /// </summary>
        ParkingOutput ExecuteRules(FuzzyInputData fuzzyInput);

        /// <summary>
        /// Установить базу знаний (правила)
        /// </summary>
        void SetRuleBase(FuzzyRuleBase ruleBase);

        /// <summary>
        /// Протестировать конкретное правило
        /// </summary>
        FuzzyOutput TestRule(string ruleName, FuzzyInputData testInput);

        /// <summary>
        /// Получить информацию о всех правилах
        /// </summary>
        RuleInfo[] GetAllRulesInfo();

        /// <summary>
        /// Включить/выключить логирование выполнения правил
        /// </summary>
        void SetLoggingEnabled(bool enabled);
    }

    /// <summary>
    /// Результат выполнения одного правила
    /// </summary>
    [System.Serializable]
    public struct FuzzyRuleResult
    {
        public string ruleName;          // Имя тестируемого правила
        public bool wasTriggered;        // Было ли правило активировано
        public float firingStrength;     // Степень активации правила (0-1)
        public string[] triggeredOutputs; // Какие выходные термы были активированы
        public string debugMessage;      // Сообщение для отладки
    }

    /// <summary>
    /// Информация о правиле
    /// </summary>
    public struct RuleInfo
    {
        public string name;
        public string description;
        public string[] antecedents;
        public string[] consequents;
        public float weight;
        public bool isEnabled;
    }
}