using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ParkingSystem.FuzzySystem.Inputs;
using static ParkingSystem.FuzzySystem.Inputs.LinguisticVariables;

namespace ParkingSystem.FuzzySystem.Interfaces
{
    /// <summary>
    /// Реализация движка нечетких правил для 2D парковки
    /// </summary>
    public class FuzzyRuleEngine : MonoBehaviour, IFuzzyRuleEngine
    {
        [Header("Настройки движка")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private DefuzzificationMethod defuzzMethod = DefuzzificationMethod.Centroid;

        private FuzzyRuleBase ruleBase;
        private Dictionary<string, OutputFuzzySet> outputSets;
        private List<FuzzyRuleResult> lastExecutionResults = new List<FuzzyRuleResult>();

        // Лингвистические переменные для выходов
        private Dictionary<string, List<string>> outputTerms = new Dictionary<string, List<string>>
        {
            ["Throttle"] = new List<string> { "FullReverse", "Reverse", "SlowReverse", "Zero", "SlowForward", "Forward", "FullForward" },
            ["Steering"] = new List<string> { "HardLeft", "Left", "SlightLeft", "Center", "SlightRight", "Right", "HardRight" },
            ["Brake"] = new List<string> { "NoBrake", "LightBrake", "MediumBrake", "HardBrake", "EmergencyBrake" },
            ["ParkingPhase"] = new List<string> { "Searching", "Approaching", "Aligning", "Reversing", "Adjusting", "Completed", "Emergency" }
        };

        void Awake()
        {
            InitializeOutputSets();
        }

        /// <summary>
        /// Инициализация нечетких множеств для выходных переменных
        /// </summary>
        private void InitializeOutputSets()
        {
            outputSets = new Dictionary<string, OutputFuzzySet>
            {
                ["Throttle"] = new OutputFuzzySet
                {
                    name = "Throttle",
                    minValue = -1f,
                    maxValue = 1f,
                    terms = outputTerms["Throttle"],
                    // Параметры для треугольных функций: [a, b, c] для каждого терма
                    parameters = new Dictionary<string, float[]>
                    {
                        ["FullReverse"] = new float[] { -1f, -1f, -0.6f },
                        ["Reverse"] = new float[] { -1f, -0.7f, -0.3f },
                        ["SlowReverse"] = new float[] { -0.7f, -0.4f, 0f },
                        ["Zero"] = new float[] { -0.3f, 0f, 0.3f },
                        ["SlowForward"] = new float[] { 0f, 0.4f, 0.7f },
                        ["Forward"] = new float[] { 0.3f, 0.7f, 1f },
                        ["FullForward"] = new float[] { 0.6f, 1f, 1f }
                    }
                },
                ["Steering"] = new OutputFuzzySet
                {
                    name = "Steering",
                    minValue = -1f,
                    maxValue = 1f,
                    terms = outputTerms["Steering"],
                    parameters = new Dictionary<string, float[]>
                    {
                        ["HardLeft"] = new float[] { -1f, -1f, -0.6f },
                        ["Left"] = new float[] { -1f, -0.7f, -0.3f },
                        ["SlightLeft"] = new float[] { -0.7f, -0.4f, 0f },
                        ["Center"] = new float[] { -0.3f, 0f, 0.3f },
                        ["SlightRight"] = new float[] { 0f, 0.4f, 0.7f },
                        ["Right"] = new float[] { 0.3f, 0.7f, 1f },
                        ["HardRight"] = new float[] { 0.6f, 1f, 1f }
                    }
                },
                ["Brake"] = new OutputFuzzySet
                {
                    name = "Brake",
                    minValue = 0f,
                    maxValue = 1f,
                    terms = outputTerms["Brake"],
                    parameters = new Dictionary<string, float[]>
                    {
                        ["NoBrake"] = new float[] { 0f, 0f, 0.2f },
                        ["LightBrake"] = new float[] { 0f, 0.25f, 0.5f },
                        ["MediumBrake"] = new float[] { 0.25f, 0.5f, 0.75f },
                        ["HardBrake"] = new float[] { 0.5f, 0.75f, 1f },
                        ["EmergencyBrake"] = new float[] { 0.8f, 1f, 1f }
                    }
                },
                ["ParkingPhase"] = new OutputFuzzySet
                {
                    name = "ParkingPhase",
                    minValue = 0f,
                    maxValue = 6f,
                    terms = outputTerms["ParkingPhase"],
                    parameters = new Dictionary<string, float[]>
                    {
                        ["Searching"] = new float[] { 0f, 0f, 1f },
                        ["Approaching"] = new float[] { 0f, 1f, 2f },
                        ["Aligning"] = new float[] { 1f, 2f, 3f },
                        ["Reversing"] = new float[] { 2f, 3f, 4f },
                        ["Adjusting"] = new float[] { 3f, 4f, 5f },
                        ["Completed"] = new float[] { 4f, 5f, 6f },
                        ["Emergency"] = new float[] { 5f, 6f, 6f }
                    }
                }
            };
        }

        /// <summary>
        /// Основной метод выполнения всех правил
        /// </summary>
        public ParkingOutput ExecuteRules(FuzzyInputData fuzzyInput)
        {
            if (ruleBase == null || ruleBase.parkingRules == null)
            {
                Debug.LogError("FuzzyRuleEngine: База правил не установлена!");
                return ParkingOutput.Zero;
            }

            if (!fuzzyInput.HasData())
            {
                Debug.LogWarning("FuzzyRuleEngine: Входные данные пусты!");
                return ParkingOutput.Zero;
            }

            lastExecutionResults.Clear();
            Dictionary<string, Dictionary<string, float>> aggregatedOutputs = new Dictionary<string, Dictionary<string, float>>
            {
                ["Throttle"] = new Dictionary<string, float>(),
                ["Steering"] = new Dictionary<string, float>(),
                ["Brake"] = new Dictionary<string, float>(),
                ["ParkingPhase"] = new Dictionary<string, float>()
            };

            // Шаг 1: Агрегация - выполнение всех правил
            foreach (var rule in ruleBase.parkingRules)
            {
                if (!rule.isEnabled) continue;

                FuzzyRuleResult ruleResult = ExecuteSingleRule(rule, fuzzyInput);
                lastExecutionResults.Add(ruleResult);

                if (ruleResult.wasTriggered && ruleResult.firingStrength > 0.01f)
                {
                    // Применяем результат правила к агрегированным выходам
                    AggregateRuleResult(aggregatedOutputs, rule, ruleResult);
                }
            }

            // Шаг 2: Дефаззификация
            ParkingOutput crispOutput = Defuzzify(aggregatedOutputs);

            if (enableLogging)
            {
                LogExecutionSummary(fuzzyInput, crispOutput);
            }

            return crispOutput;
        }

        /// <summary>
        /// Выполнение одного правила
        /// </summary>
        private FuzzyRuleResult ExecuteSingleRule(FuzzyRule rule, FuzzyInputData fuzzyInput)
        {
            FuzzyRuleResult result = new FuzzyRuleResult
            {
                ruleName = rule.ruleName,
                wasTriggered = false,
                firingStrength = 0f,
                triggeredOutputs = new List<string>().ToArray(),
                debugMessage = ""
            };

            try
            {
                // Вычисляем силу активации правила (AND всех условий)
                float firingStrength = 1f;
                List<string> triggeredConditions = new List<string>();

                foreach (var condition in rule.conditions)
                {
                    var variableData = fuzzyInput.GetVariable(condition.variableName);
                    if (variableData == null || !variableData.ContainsKey(condition.termName))
                    {
                        firingStrength = 0f;
                        result.debugMessage = $"Условие {condition.variableName} is {condition.termName} не найдено";
                        break;
                    }

                    float membership = variableData[condition.termName];

                    if (condition.@operator == FuzzyOperator.Is)
                    {
                        firingStrength = Mathf.Min(firingStrength, membership);
                        if (membership > 0.1f)
                            triggeredConditions.Add($"{condition.variableName}={condition.termName}({membership:F2})");
                    }
                    else if (condition.@operator == FuzzyOperator.IsNot)
                    {
                        firingStrength = Mathf.Min(firingStrength, 1f - membership);
                        if (membership < 0.9f)
                            triggeredConditions.Add($"NOT {condition.variableName}={condition.termName}({membership:F2})");
                    }
                    // Операторы AND/OR обрабатываются структурой правил
                }

                // Применяем вес правила
                firingStrength *= rule.weight;

                if (firingStrength > 0.01f)
                {
                    result.wasTriggered = true;
                    result.firingStrength = firingStrength;
                    result.triggeredOutputs = rule.actions.Select(a => $"{a.outputVariable}={a.outputTerm}").ToArray();
                    result.debugMessage = $"Сработало: {string.Join(", ", triggeredConditions)} → сила={firingStrength:F2}";
                }
                else
                {
                    result.debugMessage = "Правило не активировано";
                }
            }
            catch (Exception ex)
            {
                result.debugMessage = $"Ошибка выполнения: {ex.Message}";
                Debug.LogError($"Ошибка в правиле {rule.ruleName}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Агрегация результатов правил
        /// </summary>
        private void AggregateRuleResult(
            Dictionary<string, Dictionary<string, float>> aggregatedOutputs,
            FuzzyRule rule,
            FuzzyRuleResult ruleResult)
        {
            foreach (var action in rule.actions)
            {
                if (!aggregatedOutputs.ContainsKey(action.outputVariable))
                    continue;

                float value = ruleResult.firingStrength * action.confidence;
                var outputDict = aggregatedOutputs[action.outputVariable];

                // Метод агрегации MAX (берем максимальное значение для терма)
                if (outputDict.ContainsKey(action.outputTerm))
                {
                    outputDict[action.outputTerm] = Mathf.Max(outputDict[action.outputTerm], value);
                }
                else
                {
                    outputDict[action.outputTerm] = value;
                }
            }
        }

        /// <summary>
        /// Дефаззификация агрегированных выходов
        /// </summary>
        private ParkingOutput Defuzzify(Dictionary<string, Dictionary<string, float>> aggregatedOutputs)
        {
            ParkingOutput output = ParkingOutput.Zero;

            try
            {
                // Дефаззификация для каждой выходной переменной
                output.throttle = DefuzzifyVariable("Throttle", aggregatedOutputs["Throttle"]);
                output.steering = DefuzzifyVariable("Steering", aggregatedOutputs["Steering"]);
                output.brake = DefuzzifyVariable("Brake", aggregatedOutputs["Brake"]) > 0.5f; // Порог для тормоза

                // Для фазы парковки используем метод максимума (выбираем терм с наибольшей принадлежностью)
                output.suggestedPhase = DefuzzifyParkingPhase(aggregatedOutputs["ParkingPhase"]);

                // Дополнительная логика для аварийной остановки
                if (aggregatedOutputs["Brake"].ContainsKey("EmergencyBrake") &&
                    aggregatedOutputs["Brake"]["EmergencyBrake"] > 0.7f)
                {
                    output.emergencyStop = true;
                    output.brake = true;
                    output.throttle = 0f;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка дефаззификации: {ex.Message}");
            }

            return output;
        }

        /// <summary>
        /// Дефаззификация одной переменной
        /// </summary>
        private float DefuzzifyVariable(string variableName, Dictionary<string, float> memberships)
        {
            if (memberships.Count == 0)
                return 0f; // Нейтральное значение

            var outputSet = outputSets[variableName];

            switch (ruleBase?.defuzzMethod ?? defuzzMethod)
            {
                case DefuzzificationMethod.Centroid:
                    return DefuzzifyCentroid(outputSet, memberships);

                case DefuzzificationMethod.Bisector:
                    return DefuzzifyBisector(outputSet, memberships);

                case DefuzzificationMethod.MeanOfMax:
                    return DefuzzifyMeanOfMax(outputSet, memberships);

                case DefuzzificationMethod.Max:
                    return DefuzzifyMax(outputSet, memberships);

                default:
                    return DefuzzifyCentroid(outputSet, memberships);
            }
        }

        /// <summary>
        /// Метод центроида (центра тяжести)
        /// </summary>
        private float DefuzzifyCentroid(OutputFuzzySet outputSet, Dictionary<string, float> memberships, int samples = 100)
        {
            float min = outputSet.minValue;
            float max = outputSet.maxValue;
            float step = (max - min) / samples;

            float numerator = 0f;
            float denominator = 0f;

            for (float x = min; x <= max; x += step)
            {
                float mu = GetAggregatedMembership(outputSet, memberships, x);
                numerator += x * mu;
                denominator += mu;
            }

            if (denominator == 0f)
                return (min + max) / 2f;

            return numerator / denominator;
        }

        /// <summary>
        /// Метод биссектрисы
        /// </summary>
        private float DefuzzifyBisector(OutputFuzzySet outputSet, Dictionary<string, float> memberships, int samples = 100)
        {
            float min = outputSet.minValue;
            float max = outputSet.maxValue;
            float step = (max - min) / samples;

            // Находим общую площадь
            float totalArea = 0f;
            List<float> areas = new List<float>();
            List<float> xValues = new List<float>();

            for (float x = min; x <= max; x += step)
            {
                float mu = GetAggregatedMembership(outputSet, memberships, x);
                areas.Add(mu);
                xValues.Add(x);
                totalArea += mu * step;
            }

            // Находим точку, где площадь слева = площади справа
            float halfArea = totalArea / 2f;
            float currentArea = 0f;

            for (int i = 0; i < areas.Count; i++)
            {
                currentArea += areas[i] * step;
                if (currentArea >= halfArea)
                {
                    return xValues[i];
                }
            }

            return (min + max) / 2f;
        }

        /// <summary>
        /// Среднее максимумов
        /// </summary>
        private float DefuzzifyMeanOfMax(OutputFuzzySet outputSet, Dictionary<string, float> memberships, int samples = 100)
        {
            float min = outputSet.minValue;
            float max = outputSet.maxValue;
            float step = (max - min) / samples;

            float maxMu = 0f;
            List<float> maxPoints = new List<float>();

            // Находим максимальное значение принадлежности
            for (float x = min; x <= max; x += step)
            {
                float mu = GetAggregatedMembership(outputSet, memberships, x);
                if (mu > maxMu)
                {
                    maxMu = mu;
                    maxPoints.Clear();
                    maxPoints.Add(x);
                }
                else if (Mathf.Approximately(mu, maxMu))
                {
                    maxPoints.Add(x);
                }
            }

            if (maxPoints.Count == 0)
                return (min + max) / 2f;

            // Возвращаем среднее точек с максимальной принадлежностью
            return maxPoints.Average();
        }

        /// <summary>
        /// Метод максимума (первый максимум)
        /// </summary>
        private float DefuzzifyMax(OutputFuzzySet outputSet, Dictionary<string, float> memberships, int samples = 100)
        {
            float min = outputSet.minValue;
            float max = outputSet.maxValue;
            float step = (max - min) / samples;

            float maxMu = 0f;
            float maxX = (min + max) / 2f;

            for (float x = min; x <= max; x += step)
            {
                float mu = GetAggregatedMembership(outputSet, memberships, x);
                if (mu > maxMu)
                {
                    maxMu = mu;
                    maxX = x;
                }
            }

            return maxX;
        }

        /// <summary>
        /// Получить агрегированную принадлежность в точке x
        /// </summary>
        private float GetAggregatedMembership(OutputFuzzySet outputSet, Dictionary<string, float> memberships, float x)
        {
            float maxMu = 0f;

            foreach (var kvp in memberships)
            {
                string term = kvp.Key;
                float activation = kvp.Value;

                if (activation <= 0f || !outputSet.parameters.ContainsKey(term))
                    continue;

                float[] paramsForTerm = outputSet.parameters[term];
                float mu = FuzzyMembership.Triangular(x, paramsForTerm[0], paramsForTerm[1], paramsForTerm[2]);

                // Обрезаем на уровне активации (метод клиппинга)
                mu = Mathf.Min(mu, activation);
                maxMu = Mathf.Max(maxMu, mu); // Агрегация MAX
            }

            return maxMu;
        }

        /// <summary>
        /// Дефаззификация фазы парковки (метод максимума)
        /// </summary>
        private ParkingPhase DefuzzifyParkingPhase(Dictionary<string, float> memberships)
        {
            if (memberships.Count == 0)
                return ParkingPhase.Searching;

            string maxTerm = "";
            float maxValue = 0f;

            foreach (var kvp in memberships)
            {
                if (kvp.Value > maxValue)
                {
                    maxValue = kvp.Value;
                    maxTerm = kvp.Key;
                }
            }

            // Преобразуем строку в enum
            if (Enum.TryParse<ParkingPhase>(maxTerm, out ParkingPhase phase))
            {
                return phase;
            }

            return ParkingPhase.Searching;
        }

        /// <summary>
        /// Тестирование конкретного правила
        /// </summary>
        public FuzzyOutput TestRule(string ruleName, FuzzyInputData testInput)
        {
            var rule = ruleBase?.GetRule(ruleName);
            if (rule == null)
            {
                Debug.LogError($"Правило '{ruleName}' не найдено!");
                return new FuzzyOutput();
            }

            var ruleResult = ExecuteSingleRule(rule, testInput);

            // Создаем FuzzyOutput на основе результатов тестирования
            FuzzyOutput output = new FuzzyOutput
            {
                throttleOutput = new Dictionary<string, float>(),
                steeringOutput = new Dictionary<string, float>(),
                brakeOutput = new Dictionary<string, float>(),
                phaseOutput = new Dictionary<string, float>()
            };

            if (ruleResult.wasTriggered)
            {
                foreach (var action in rule.actions)
                {
                    float value = ruleResult.firingStrength * action.confidence;

                    switch (action.outputVariable)
                    {
                        case "Throttle":
                            output.throttleOutput[action.outputTerm] = value;
                            break;
                        case "Steering":
                            output.steeringOutput[action.outputTerm] = value;
                            break;
                        case "Brake":
                            output.brakeOutput[action.outputTerm] = value;
                            break;
                        case "ParkingPhase":
                            output.phaseOutput[action.outputTerm] = value;
                            break;
                    }
                }
            }

            Debug.Log($"Тестирование правила '{ruleName}': {(ruleResult.wasTriggered ? "Сработало" : "Не сработало")}, " +
                     $"сила={ruleResult.firingStrength:F2}, сообщение={ruleResult.debugMessage}");

            return output;
        }

        /// <summary>
        /// Установка базы знаний
        /// </summary>
        public void SetRuleBase(FuzzyRuleBase ruleBase)
        {
            this.ruleBase = ruleBase;
            Debug.Log($"FuzzyRuleEngine: База правил установлена '{ruleBase.name}' ({ruleBase.parkingRules.Length} правил)");
        }

        /// <summary>
        /// Получить информацию о всех правилах
        /// </summary>
        public RuleInfo[] GetAllRulesInfo()
        {
            if (ruleBase == null || ruleBase.parkingRules == null)
                return Array.Empty<RuleInfo>();

            return ruleBase.parkingRules.Select(rule => new RuleInfo
            {
                name = rule.ruleName,
                description = rule.description,
                antecedents = rule.conditions.Select(c => $"{c.variableName} {c.@operator} {c.termName}").ToArray(),
                consequents = rule.actions.Select(a => $"{a.outputVariable} = {a.outputTerm} (уверенность: {a.confidence:F2})").ToArray(),
                weight = rule.weight,
                isEnabled = rule.isEnabled
            }).ToArray();
        }

        /// <summary>
        /// Включить/выключить логирование
        /// </summary>
        public void SetLoggingEnabled(bool enabled)
        {
            enableLogging = enabled;
            Debug.Log($"FuzzyRuleEngine: Логирование {(enabled ? "включено" : "выключено")}");
        }

        /// <summary>
        /// Логирование результатов выполнения
        /// </summary>
        private void LogExecutionSummary(FuzzyInputData input, ParkingOutput output)
        {
            int triggeredRules = lastExecutionResults.Count(r => r.wasTriggered);

            string summary = $"\n=== FuzzyRuleEngine: Результаты выполнения ===\n" +
                           $"Активировано правил: {triggeredRules}/{ruleBase.parkingRules.Length}\n" +
                           $"Выходные команды: throttle={output.throttle:F2}, steering={output.steering:F2}, " +
                           $"brake={output.brake}, phase={output.suggestedPhase}\n";

            // Логируем только сработавшие правила
            foreach (var result in lastExecutionResults.Where(r => r.wasTriggered))
            {
                summary += $"- {result.ruleName}: сила={result.firingStrength:F2}, {result.debugMessage}\n";
            }

            Debug.Log(summary);
        }

        /// <summary>
        /// Получить результаты последнего выполнения
        /// </summary>
        public List<FuzzyRuleResult> GetLastExecutionResults() => lastExecutionResults;

        /// <summary>
        /// Вспомогательный класс для выходных нечетких множеств
        /// </summary>
        private class OutputFuzzySet
        {
            public string name;
            public float minValue;
            public float maxValue;
            public List<string> terms;
            public Dictionary<string, float[]> parameters; // Параметры для каждого терма
        }
    }
}