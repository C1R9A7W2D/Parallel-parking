#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ParkingSystem.FuzzySystem.Inputs;

namespace ParkingSystem.FuzzySystem.Interfaces
{
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

        [Header("Статистика (только чтение)")]
        [SerializeField] private int totalRules;
        [SerializeField] private int enabledRules;
        [SerializeField] private int rulesByPhaseSearching;
        [SerializeField] private int rulesByPhaseApproaching;
        [SerializeField] private int rulesByPhaseAligning;
        [SerializeField] private int rulesByPhaseReversing;
        [SerializeField] private int rulesByPhaseAdjusting;
        [SerializeField] private int rulesByPhaseCompleted;
        [SerializeField] private int rulesByPhaseEmergency;

#if UNITY_EDITOR
        /// <summary>
        /// Валидация и обновление статистики при изменении в инспекторе
        /// </summary>
        void OnValidate()
        {
            UpdateStatistics();
            ValidateRules();
        }

        /// <summary>
        /// Обновление статистики правил
        /// </summary>
        private void UpdateStatistics()
        {
            if (parkingRules == null)
            {
                totalRules = 0;
                enabledRules = 0;
                rulesByPhaseSearching = 0;
                rulesByPhaseApproaching = 0;
                rulesByPhaseAligning = 0;
                rulesByPhaseReversing = 0;
                rulesByPhaseAdjusting = 0;
                rulesByPhaseCompleted = 0;
                rulesByPhaseEmergency = 0;
                return;
            }

            totalRules = parkingRules.Length;
            enabledRules = parkingRules.Count(r => r.isEnabled);

            // Подсчет правил по фазам
            rulesByPhaseSearching = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Searching);
            rulesByPhaseApproaching = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Approaching);
            rulesByPhaseAligning = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Aligning);
            rulesByPhaseReversing = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Reversing);
            rulesByPhaseAdjusting = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Adjusting);
            rulesByPhaseCompleted = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Completed);
            rulesByPhaseEmergency = parkingRules.Count(r => r.applicablePhase == ParkingPhase.Emergency);
        }

        /// <summary>
        /// Проверка корректности правил
        /// </summary>
        private void ValidateRules()
        {
            if (parkingRules == null) return;

            HashSet<string> ruleNames = new HashSet<string>();
            bool hasErrors = false;

            for (int i = 0; i < parkingRules.Length; i++)
            {
                var rule = parkingRules[i];

                // Проверка уникальности имен
                if (!string.IsNullOrEmpty(rule.ruleName))
                {
                    if (ruleNames.Contains(rule.ruleName))
                    {
                        Debug.LogError($"FuzzyRuleBase '{name}': Дублирующееся имя правила '{rule.ruleName}'");
                        hasErrors = true;
                    }
                    ruleNames.Add(rule.ruleName);
                }

                // Проверка веса правила
                if (rule.weight < 0.1f || rule.weight > 2.0f)
                {
                    Debug.LogWarning($"FuzzyRuleBase '{name}': Правило '{rule.ruleName}' имеет недопустимый вес {rule.weight:F2}. Должен быть в диапазоне [0.1, 2.0]");
                }

                // Проверка условий
                if (rule.conditions == null || rule.conditions.Length == 0)
                {
                    Debug.LogWarning($"FuzzyRuleBase '{name}': Правило '{rule.ruleName}' не имеет условий");
                }

                // Проверка действий
                if (rule.actions == null || rule.actions.Length == 0)
                {
                    Debug.LogWarning($"FuzzyRuleBase '{name}': Правило '{rule.ruleName}' не имеет действий");
                }
                else
                {
                    // Проверка уверенности действий
                    foreach (var action in rule.actions)
                    {
                        if (action.confidence < 0.1f || action.confidence > 1.0f)
                        {
                            Debug.LogWarning($"FuzzyRuleBase '{name}': Правило '{rule.ruleName}' имеет действие с недопустимой уверенностью {action.confidence:F2}");
                        }
                    }
                }
            }

            if (!hasErrors && parkingRules.Length > 0)
            {
                // Debug.Log($"FuzzyRuleBase '{name}': Все {parkingRules.Length} правил прошли валидацию");
            }
        }
#endif

        /// <summary>
        /// Получить правило по имени
        /// </summary>
        public FuzzyRule GetRule(string ruleName)
        {
            if (parkingRules == null) return null;

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
            if (parkingRules == null) return new FuzzyRule[0];

            return parkingRules
                .Where(rule => rule.applicablePhase == phase && rule.isEnabled)
                .ToArray();
        }

        /// <summary>
        /// Получить правила для фазы поиска места
        /// </summary>
        public FuzzyRule[] GetSearchingRules() => GetRulesForPhase(ParkingPhase.Searching);

        /// <summary>
        /// Получить правила для фазы подъезда
        /// </summary>
        public FuzzyRule[] GetApproachingRules() => GetRulesForPhase(ParkingPhase.Approaching);

        /// <summary>
        /// Получить правила для фазы выравнивания
        /// </summary>
        public FuzzyRule[] GetAligningRules() => GetRulesForPhase(ParkingPhase.Aligning);

        /// <summary>
        /// Получить правила для фазы заднего хода
        /// </summary>
        public FuzzyRule[] GetReversingRules() => GetRulesForPhase(ParkingPhase.Reversing);

        /// <summary>
        /// Получить правила для фазы корректировки
        /// </summary>
        public FuzzyRule[] GetAdjustingRules() => GetRulesForPhase(ParkingPhase.Adjusting);

        /// <summary>
        /// Получить правила для аварийной фазы
        /// </summary>
        public FuzzyRule[] GetEmergencyRules() => GetRulesForPhase(ParkingPhase.Emergency);

        /// <summary>
        /// Включить все правила
        /// </summary>
        public void EnableAllRules()
        {
            if (parkingRules == null) return;

            foreach (var rule in parkingRules)
                rule.isEnabled = true;
        }

        /// <summary>
        /// Выключить все правила
        /// </summary>
        public void DisableAllRules()
        {
            if (parkingRules == null) return;

            foreach (var rule in parkingRules)
                rule.isEnabled = false;
        }

        /// <summary>
        /// Установить вес для всех правил
        /// </summary>
        public void SetAllRulesWeight(float weight)
        {
            if (parkingRules == null) return;

            float clampedWeight = Mathf.Clamp(weight, 0.1f, 2.0f);
            foreach (var rule in parkingRules)
                rule.weight = clampedWeight;
        }

        /// <summary>
        /// Создать правила по умолчанию (редакторский метод)
        /// </summary>
#if UNITY_EDITOR
        [ContextMenu("Создать правила по умолчанию")]
        public void CreateDefaultRulesEditor()
        {
            CreateDefaultRules();
            EditorUtility.SetDirty(this);
            UpdateStatistics();
            Debug.Log($"FuzzyRuleBase '{name}': Создано {parkingRules.Length} правил по умолчанию");
        }
#endif

        /// <summary>
        /// Создать правила по умолчанию (реализация)
        /// </summary>
        private void CreateDefaultRules()
        {
            // Создаем 70 правил для парковки
            parkingRules = new FuzzyRule[]
            {
                // === ФАЗА ПОИСКА МЕСТА (Searching) - 15 правил ===
                CreateRule("RULE_001_SEARCH_SLOW_FORWARD", "Медленное движение вперед при поиске места",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.8f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_002_SEARCH_STOP_IF_OBSTACLE", "Остановка при обнаружении близкого препятствия впереди",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "Brake", outputTerm = "LightBrake", confidence = 0.7f }
                    }),

                CreateRule("RULE_003_SEARCH_TURN_RIGHT_IF_LEFT_CLOSE", "Поворот вправо если слева близко препятствие",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f }
                    }),

                CreateRule("RULE_004_SEARCH_TURN_LEFT_IF_RIGHT_CLOSE", "Поворот влево если справа близко препятствие",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "LeftSideDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightLeft", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f }
                    }),

                CreateRule("RULE_005_SEARCH_ACCELERATE_IF_CLEAR", "Ускорение если путь свободен",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryFar", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Slow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Forward", confidence = 0.7f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_006_SEARCH_SLOW_DOWN_IF_MEDIUM_CLOSE", "Замедление при среднем расстоянии впереди",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "Medium", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Fast", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.8f },
                        new() { outputVariable = "Brake", outputTerm = "LightBrake", confidence = 0.4f }
                    }),

                CreateRule("RULE_007_SEARCH_ZIGZAG_FOR_SPOT", "Зигзагообразное движение для поиска места",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightLeft", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.7f }
                    }),

                CreateRule("RULE_008_SEARCH_STOP_AND_CHECK", "Остановка для проверки окружающей обстановки",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is },
                        new() { variableName = "ParkingSpotWidth", termName = "Wide", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Approaching", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_009_SEARCH_AVOID_DEAD_END", "Разворот при обнаружении тупика",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.9f },
                        new() { outputVariable = "Steering", outputTerm = "HardLeft", confidence = 0.8f }
                    }),

                CreateRule("RULE_010_SEARCH_FOLLOW_RIGHT_WALL", "Следование вдоль правой стены",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "RightSideDistance", termName = "Medium", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightLeft", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "Forward", confidence = 0.7f }
                    }),

                CreateRule("RULE_011_SEARCH_SCAN_AREA", "Сканирование области поиска",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is },
                        new() { variableName = "ParkingSpotWidth", termName = "VeryNarrow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardRight", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.5f }
                    }),

                CreateRule("RULE_012_SEARCH_KEEP_DISTANCE", "Поддержание безопасной дистанции",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "Close", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Medium", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 0.8f },
                        new() { outputVariable = "Brake", outputTerm = "LightBrake", confidence = 0.6f }
                    }),

                CreateRule("RULE_013_SEARCH_CHANGE_LANE", "Перестроение в другой ряд",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryFar", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Left", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f }
                    }),

                CreateRule("RULE_014_SEARCH_REVERSE_IF_BLOCKED", "Движение назад если путь заблокирован",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Reverse", confidence = 0.9f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 1.0f }
                    }),

                CreateRule("RULE_015_SEARCH_FIND_ALTERNATIVE", "Поиск альтернативного пути",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardRight", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.7f }
                    }),

                // === ФАЗА ПОДЪЕЗДА (Approaching) - 15 правил ===
                CreateRule("RULE_016_APPROACH_SLOW_TO_SPOT", "Медленный подъезд к найденному месту",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ParkingSpotWidth", termName = "Wide", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Medium", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.9f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.8f }
                    }),

                CreateRule("RULE_017_APPROACH_ADJUST_ANGLE_SMALL_LEFT", "Корректировка угла подъезда (небольшой левый поворот)",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ApproachAngle", termName = "SmallRight", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightLeft", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f }
                    }),

                CreateRule("RULE_018_APPROACH_ADJUST_ANGLE_SMALL_RIGHT", "Корректировка угла подъезда (небольшой правый поворот)",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ApproachAngle", termName = "SmallLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f }
                    }),

                CreateRule("RULE_019_APPROACH_STOP_BEFORE_MANEUVER", "Остановка перед началом маневра парковки",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "LongitudinalError", termName = "Small", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "Brake", outputTerm = "MediumBrake", confidence = 0.8f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Aligning", confidence = 0.9f }
                    }),

                CreateRule("RULE_020_APPROACH_BACK_UP_IF_TOO_CLOSE", "Отъезд назад если слишком близко к месту",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.8f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_021_APPROACH_CENTER_TO_SPOT", "Центрирование относительно места",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "LateralError", termName = "Medium", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.7f }
                    }),

                CreateRule("RULE_022_APPROACH_CHECK_SIDES", "Проверка боковых расстояний при подъезде",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Or }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Searching", confidence = 0.7f }
                    }),

                CreateRule("RULE_023_APPROACH_CALCULATE_ANGLE", "Расчет оптимального угла подъезда",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ApproachAngle", termName = "LargeLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Right", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.5f }
                    }),

                CreateRule("RULE_024_APPROACH_SPOT_TOO_NARROW", "Место слишком узкое - продолжить поиск",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ParkingSpotWidth", termName = "VeryNarrow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Searching", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.7f }
                    }),

                CreateRule("RULE_025_APPROACH_PERFECT_ANGLE", "Идеальный угол подъезда достигнут",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ApproachAngle", termName = "Aligned", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Aligning", confidence = 0.9f }
                    }),

                CreateRule("RULE_026_APPROACH_ALIGN_LATERAL", "Боковое выравнивание при подъезде",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "LateralError", termName = "Large", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardRight", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f }
                    }),

                CreateRule("RULE_027_APPROACH_CHECK_REAR", "Проверка пространства сзади",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.8f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_028_APPROACH_ADJUST_SPEED", "Корректировка скорости подъезда",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "SpeedError", termName = "LargePositive", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 0.9f },
                        new() { outputVariable = "Brake", outputTerm = "LightBrake", confidence = 0.7f }
                    }),

                CreateRule("RULE_029_APPROACH_PREPARE_REVERSE", "Подготовка к заднему ходу",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "LongitudinalError", termName = "VerySmall", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Aligning", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_030_APPROACH_ABORT_BAD_ANGLE", "Прерывание при плохом угле",
                    ParkingPhase.Approaching,
                    new RuleCondition[] {
                        new() { variableName = "ApproachAngle", termName = "LargeRight", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Reverse", confidence = 0.9f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Searching", confidence = 0.8f }
                    }),

                // === ФАЗА ВЫРАВНИВАНИЯ (Aligning) - 15 правил ===
                CreateRule("RULE_031_ALIGN_HARD_LEFT_STEER", "Резкий левый поворот для выравнивания",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "LargeRight", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardLeft", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.5f }
                    }),

                CreateRule("RULE_032_ALIGN_HARD_RIGHT_STEER", "Резкий правый поворот для выравнивания",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "LargeLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardRight", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.5f }
                    }),

                CreateRule("RULE_033_ALIGN_SLIGHT_CORRECTION_LEFT", "Небольшая корректировка влево",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "SmallRight", @operator = FuzzyOperator.Is },
                        new() { variableName = "LateralError", termName = "Small", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightLeft", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 0.8f }
                    }),

                CreateRule("RULE_034_ALIGN_SLIGHT_CORRECTION_RIGHT", "Небольшая корректировка вправо",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "SmallLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "LateralError", termName = "Small", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 0.8f }
                    }),

                CreateRule("RULE_035_ALIGN_PERFECT_ALIGNMENT", "Идеальное выравнивание - начало заднего хода",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "Aligned", @operator = FuzzyOperator.Is },
                        new() { variableName = "LateralError", termName = "VerySmall", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Reversing", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.7f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 1.0f }
                    }),

                CreateRule("RULE_036_ALIGN_STOP_IF_SIDE_CLOSE", "Остановка если сбоку слишком близко",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "Brake", outputTerm = "HardBrake", confidence = 0.9f }
                    }),

                CreateRule("RULE_037_ALIGN_ADJUST_POSITION_FORWARD", "Корректировка позиции движением вперед",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "LongitudinalError", termName = "Large", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.7f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_038_ALIGN_CHECK_PARALLEL", "Проверка параллельности автомобиля",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "Aligned", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Reversing", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_039_ALIGN_MINOR_ADJUSTMENT", "Незначительная корректировка",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "SmallLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 0.9f }
                    }),

                CreateRule("RULE_040_ALIGN_CENTER_IN_SPOT", "Центрирование в парковочном месте",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "LateralError", termName = "Medium", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.5f }
                    }),

                CreateRule("RULE_041_ALIGN_REVERSE_TO_ADJUST", "Движение назад для корректировки",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "LargeLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardRight", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "Reverse", confidence = 0.7f }
                    }),

                CreateRule("RULE_042_ALIGN_FORWARD_TO_ADJUST", "Движение вперед для корректировки",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "LargeRight", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardLeft", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.7f }
                    }),

                CreateRule("RULE_043_ALIGN_SIDE_CLEARANCE_CHECK", "Проверка бокового зазора",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Or }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Adjusting", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_044_ALIGN_READY_FOR_REVERSE", "Готовность к заднему ходу",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "Aligned", @operator = FuzzyOperator.Is },
                        new() { variableName = "LateralError", termName = "Small", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Reversing", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.8f }
                    }),

                CreateRule("RULE_045_ALIGN_ABORT_PARKING", "Прерывание парковки при проблемах",
                    ParkingPhase.Aligning,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Emergency", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                // === ФАЗА ЗАДНЕГО ХОДА (Reversing) - 15 правил ===
                CreateRule("RULE_046_REVERSE_SLOW_BACKWARD", "Медленное движение назад в парковочное место",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is },
                        new() { variableName = "AlignmentAngle", termName = "Aligned", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.8f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_047_REVERSE_STEER_LEFT_IF_RIGHT_CLOSE", "Поворот влево при заднем ходе если справа близко",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Medium", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Left", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.6f }
                    }),

                CreateRule("RULE_048_REVERSE_STEER_RIGHT_IF_LEFT_CLOSE", "Поворот вправо при заднем ходе если слева близко",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Medium", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Right", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.6f }
                    }),

                CreateRule("RULE_049_REVERSE_STOP_IF_REAR_CLOSE", "Остановка если сзади слишком близко",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "Brake", outputTerm = "HardBrake", confidence = 0.9f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Adjusting", confidence = 0.8f }
                    }),

                CreateRule("RULE_050_REVERSE_CORRECT_ANGLE_SLIGHT_LEFT", "Корректировка угла небольшим поворотом влево",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "SmallRight", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightLeft", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.5f }
                    }),

                CreateRule("RULE_051_REVERSE_CORRECT_ANGLE_SLIGHT_RIGHT", "Корректировка угла небольшим поворотом вправо",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "SmallLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.5f }
                    }),

                CreateRule("RULE_052_REVERSE_ACCELERATE_SLIGHTLY", "Легкое ускорение если путь свободен",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "VeryFar", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Reverse", confidence = 0.7f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_053_REVERSE_CHECK_FRONT_CLEARANCE", "Проверка переднего зазора",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Adjusting", confidence = 0.8f }
                    }),

                CreateRule("RULE_054_REVERSE_STRAIGHTEN_WHEELS", "Выравнивание колес при движении назад",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "Aligned", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 1.0f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.8f }
                    }),

                CreateRule("RULE_055_REVERSE_CORRECT_LATERAL_ERROR", "Корректировка боковой ошибки",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "LateralError", termName = "Medium", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Medium", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.5f }
                    }),

                CreateRule("RULE_056_REVERSE_AVOID_SIDE_CONTACT", "Избежание бокового контакта",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Or }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Emergency", confidence = 0.9f }
                    }),

                CreateRule("RULE_057_REVERSE_APPROACH_COMPLETE", "Приближение к завершению заднего хода",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "Close", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "VerySlow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Adjusting", confidence = 0.9f }
                    }),

                CreateRule("RULE_058_REVERSE_EMERGENCY_STOP", "Аварийная остановка при заднем ходе",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Medium", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Brake", outputTerm = "EmergencyBrake", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Emergency", confidence = 1.0f }
                    }),

                CreateRule("RULE_059_REVERSE_READJUST_IF_NEEDED", "Повторная корректировка при необходимости",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "LargeLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Aligning", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.7f }
                    }),

                CreateRule("RULE_060_REVERSE_COMPLETE_SUCCESS", "Успешное завершение заднего хода",
                    ParkingPhase.Reversing,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Adjusting", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                // === ФАЗА КОРРЕКТИРОВКИ (Adjusting) - 10 правил ===
                CreateRule("RULE_061_ADJUST_FORWARD_IF_TOO_DEEP", "Движение вперед если заехал слишком глубоко",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.8f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_062_ADJUST_BACKWARD_IF_TOO_SHALLOW", "Движение назад если недостаточно глубоко",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Close", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.8f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_063_ADJUST_CENTER_LEFT_RIGHT", "Центрирование влево-вправо",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "LateralError", termName = "Medium", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.5f }
                    }),

                CreateRule("RULE_064_ADJUST_COMPLETE_PARKING", "Завершение парковки - успех",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "Aligned", @operator = FuzzyOperator.Is },
                        new() { variableName = "LateralError", termName = "VerySmall", @operator = FuzzyOperator.Is },
                        new() { variableName = "LongitudinalError", termName = "VerySmall", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Completed", confidence = 1.0f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "Brake", outputTerm = "HardBrake", confidence = 1.0f }
                    }),

                CreateRule("RULE_065_ADJUST_MINOR_CORRECTION_FORWARD", "Незначительная корректировка вперед",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "LongitudinalError", termName = "Small", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.6f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_066_ADJUST_MINOR_CORRECTION_BACKWARD", "Незначительная корректировка назад",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "LongitudinalError", termName = "Small", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "SlowReverse", confidence = 0.6f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 0.9f }
                    }),

                CreateRule("RULE_067_ADJUST_SIDE_CLEARANCE_CHECK", "Проверка бокового зазора при корректировке",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "LeftSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RightSideDistance", termName = "VeryClose", @operator = FuzzyOperator.Or }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Completed", confidence = 0.8f }
                    }),

                CreateRule("RULE_068_ADJUST_ALIGN_WHEELS", "Выравнивание колес перед завершением",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "AlignmentAngle", termName = "SmallLeft", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "SlightRight", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_069_ADJUST_FINAL_CHECK", "Финальная проверка перед завершением",
                    ParkingPhase.Adjusting,
                    new RuleCondition[] {
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is },
                        new() { variableName = "LateralError", termName = "VerySmall", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Completed", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                // === АВАРИЙНЫЕ ПРАВИЛА (Emergency) - 5 правил ===
                CreateRule("RULE_070_EMERGENCY_FULL_STOP", "Полная аварийная остановка",
                    ParkingPhase.Emergency,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Fast", @operator = FuzzyOperator.Or }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Brake", outputTerm = "EmergencyBrake", confidence = 1.0f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f },
                        new() { outputVariable = "ParkingPhase", outputTerm = "Emergency", confidence = 1.0f }
                    }),

                CreateRule("RULE_071_EMERGENCY_AVOID_COLLISION", "Экстренное уклонение от столкновения",
                    ParkingPhase.Emergency,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "LeftSideDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardLeft", confidence = 0.9f },
                        new() { outputVariable = "Throttle", outputTerm = "FullForward", confidence = 0.8f }
                    }),

                CreateRule("RULE_072_EMERGENCY_STOP_AND_ASSESS", "Остановка и оценка ситуации",
                    ParkingPhase.Emergency,
                    new RuleCondition[] {
                        new() { variableName = "CurrentSpeed", termName = "Fast", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Brake", outputTerm = "HardBrake", confidence = 1.0f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_073_EMERGENCY_CLEAR_PATH", "Освобождение пути при аварийной ситуации",
                    ParkingPhase.Emergency,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryClose", @operator = FuzzyOperator.Is },
                        new() { variableName = "RearDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Throttle", outputTerm = "Reverse", confidence = 0.9f },
                        new() { outputVariable = "Steering", outputTerm = "Center", confidence = 1.0f }
                    }),

                CreateRule("RULE_074_EMERGENCY_RESTART_SYSTEM", "Перезапуск системы после аварии",
                    ParkingPhase.Emergency,
                    new RuleCondition[] {
                        new() { variableName = "CurrentSpeed", termName = "Stopped", @operator = FuzzyOperator.Is },
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "ParkingPhase", outputTerm = "Searching", confidence = 0.8f },
                        new() { outputVariable = "Throttle", outputTerm = "Zero", confidence = 1.0f }
                    }),

                CreateRule("RULE_075_SEARCH_ALTERNATING_TURN", "Чередующийся поворот для поиска",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "Far", @operator = FuzzyOperator.Is },
                        new() { variableName = "CurrentSpeed", termName = "Slow", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "Left", confidence = 0.7f },
                        new() { outputVariable = "Throttle", outputTerm = "SlowForward", confidence = 0.8f }
                    }),

                CreateRule("RULE_076_SEARCH_OSCILLATE", "Осцилляция для активного поиска",
                    ParkingPhase.Searching,
                    new RuleCondition[] {
                        new() { variableName = "FrontDistance", termName = "VeryFar", @operator = FuzzyOperator.Is }
                    },
                    new RuleAction[] {
                        new() { outputVariable = "Steering", outputTerm = "HardRight", confidence = 0.6f },
                        new() { outputVariable = "Throttle", outputTerm = "Forward", confidence = 0.7f }
                    })
            };
        }

        /// <summary>
        /// Вспомогательный метод создания правила
        /// </summary>
        private FuzzyRule CreateRule(string name, string desc, ParkingPhase phase,
                                    RuleCondition[] conditions, RuleAction[] actions)
        {
            return new FuzzyRule
            {
                ruleName = name,
                description = desc,
                applicablePhase = phase,
                isEnabled = true,
                weight = 1.0f,
                conditions = conditions,
                actions = actions
            };
        }
    }

    [System.Serializable]
    public class FuzzyRule
    {
        [Header("Общая информация")]
        public string ruleName;                  // Имя правила (уникальное)
        public string description;               // Описание правила на русском
        public ParkingPhase applicablePhase;     // Фаза парковки, для которой правило применяется
        public bool isEnabled = true;           // Включено ли правило
        [Range(0.1f, 2.0f)]
        public float weight = 1.0f;             // Вес правила (важность)

        [Header("Условия (ЕСЛИ)")]
        [Tooltip("Все условия объединяются оператором AND")]
        public RuleCondition[] conditions;       // Антецеденты (условия)

        [Header("Действия (ТО)")]
        [Tooltip("Все действия выполняются параллельно")]
        public RuleAction[] actions;             // Консеквенты (действия)

        /// <summary>
        /// Проверка, применимо ли правило к текущим данным
        /// </summary>
        public bool IsApplicable(FuzzyInputData inputData)
        {
            if (!isEnabled || conditions == null || conditions.Length == 0)
                return false;

            // Базовая проверка - правило применимо, если все переменные в условиях существуют
            foreach (var condition in conditions)
            {
                if (!inputData.HasVariableData(condition.variableName))
                    return false;
            }

            return true;
        }
    }

    [System.Serializable]
    public struct RuleCondition
    {
        [Tooltip("Имя входной переменной (FrontDistance, AlignmentAngle и т.д.)")]
        public string variableName;

        [Tooltip("Имя лингвистического терма (VeryClose, SmallLeft и т.д.)")]
        public string termName;

        [Tooltip("Оператор сравнения")]
        public FuzzyOperator @operator;
    }

    [System.Serializable]
    public struct RuleAction
    {
        [Tooltip("Выходная переменная (Throttle, Steering, Brake, ParkingPhase)")]
        public string outputVariable;

        [Tooltip("Выходной лингвистический терм")]
        public string outputTerm;

        [Range(0.1f, 1.0f)]
        [Tooltip("Уверенность в действии (0-1)")]
        public float confidence;
    }

    public enum FuzzyOperator
    {
        Is,     // Является (значение принадлежит терму)
        IsNot,  // Не является (значение не принадлежит терму)
        And,    // И (для комбинирования условий)
        Or      // Или (для комбинирования условий)
    }

    public enum DefuzzificationMethod
    {
        Centroid,   // Центроид (центр тяжести)
        Bisector,   // Биссектриса
        MeanOfMax,  // Среднее максимумов
        Max         // Первый максимум
    }

    public enum AggregationMethod
    {
        Max,    // Максимум (максимальное значение)
        Sum,    // Сумма (суммирование значений)
        Prod    // Произведение (перемножение значений)
    }
}