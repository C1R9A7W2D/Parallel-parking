using ParkingSystem.FuzzySystem;
using ParkingSystem.FuzzySystem.Inputs;
using ParkingSystem.FuzzySystem.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkingSystem.Integration
{
    /// <summary>
    /// Главный контроллер интеграции системы парковки
    /// </summary>
    [RequireComponent(typeof(CarAI))]
    public class FuzzyParkingController : MonoBehaviour
    {
        [Header("Компоненты системы")]
        [SerializeField] private CarSpawner carSpawner;
        [SerializeField] private LinguisticVariables linguisticVariables;
        [SerializeField] private FuzzyRuleBase ruleBase;

        [Header("Настройки системы")]
        [SerializeField] private float updateFrequency = 10f;
        [SerializeField] private bool useFixedUpdate = false;
        [SerializeField] private bool enableDebug = true;

        [Header("Пороги безопасности")]
        [SerializeField] private float emergencyStopDistance = 0.5f;
        [SerializeField] private float warningDistance = 1.0f;

        // Компоненты системы
        private CarAI carAI;
        private SensorDataCollector sensorCollector;
        private SensorFuzzyProcessor fuzzyProcessor;
        private VehicleCommandInterface commandInterface;
        private IFuzzyRuleEngine ruleEngine;

        // Текущие данные
        private ParkingInput currentInput;
        private FuzzyInputData currentFuzzyInput;
        private ParkingOutput currentOutput;

        // Состояние системы
        private bool isSystemActive = true;
        private bool isEmergency = false;
        private float lastUpdateTime;
        private float updateInterval;

        // События
        public event Action<ParkingInput> OnInputUpdated;
        public event Action<FuzzyInputData> OnFuzzyInputUpdated;
        public event Action<ParkingOutput> OnCommandGenerated;
        public event Action<string> OnSystemStatusChanged;

        void Awake()
        {
            carAI = GetComponent<CarAI>(); // CarAI теперь 2D

            if (carAI == null)
            {
                Debug.LogError("FuzzyParkingController: CarAI компонент не найден!");
                enabled = false;
                return;
            }

            InitializeComponents();
            updateInterval = 1f / updateFrequency;
            LogStatus("Система парковки 2D инициализирована");
        }

        private void InitializeComponents()
        {
            try
            {
                // Инициализация для 2D
                sensorCollector = new SensorDataCollector(carAI, carSpawner);

                // ... остальной код без изменений
                // ВАЖНО: SensorDataCollector теперь работает с 2D физикой
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка инициализации системы 2D: {ex.Message}");
                enabled = false;
            }
        }

        void Start()
        {
            StartSystem();
        }

        void Update()
        {
            if (!isSystemActive) return;

            if (!useFixedUpdate && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateParkingSystem();
                lastUpdateTime = Time.time;
            }
        }

        void FixedUpdate()
        {
            if (!isSystemActive) return;

            if (useFixedUpdate && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateParkingSystem();
                lastUpdateTime = Time.time;
            }
        }

        void OnDestroy()
        {
            StopSystem();
            LogStatus("Система парковки выключена");
        }

        private void UpdateParkingSystem()
        {
            if (isEmergency)
            {
                HandleEmergency();
                return;
            }

            try
            {
                // 1. Сбор данных с датчиков
                currentInput = sensorCollector.CollectSensorData();
                OnInputUpdated?.Invoke(currentInput);

                // 2. Проверка безопасности
                if (CheckSafetyViolations(currentInput))
                {
                    TriggerEmergencyStop("Обнаружено препятствие слишком близко!");
                    return;
                }

                // 3. Фаззификация входных данных
                if (fuzzyProcessor != null)
                {
                    currentFuzzyInput = fuzzyProcessor.Process(currentInput);
                    OnFuzzyInputUpdated?.Invoke(currentFuzzyInput);
                }

                // 4. Выполнение нечетких правил
                if (ruleEngine != null)
                {
                    // Используем правильный тип FuzzyInputData
                    currentOutput = ruleEngine.ExecuteRules(currentFuzzyInput);

                    // 5. Применение команд к автомобилю
                    commandInterface.ApplyCommand(currentOutput);

                    // 6. Отправка события
                    OnCommandGenerated?.Invoke(currentOutput);

                    // 7. Логирование для отладки
                    if (enableDebug)
                    {
                        Debug.Log($"Команда: throttle={currentOutput.throttle:F2}, steering={currentOutput.steering:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка в цикле парковки: {ex.Message}");
                TriggerEmergencyStop($"Системная ошибка: {ex.Message}");
            }
        }

        private bool CheckSafetyViolations(ParkingInput input)
        {
            if (input.sensorDistances == null || input.sensorDistances.Length == 0)
                return false;

            foreach (float distance in input.sensorDistances)
            {
                if (distance < emergencyStopDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleEmergency()
        {
            ParkingOutput emergencyCommand = new ParkingOutput
            {
                throttle = -1f,
                steering = 0f,
                brake = true,
                emergencyStop = true
            };

            commandInterface.ApplyCommand(emergencyCommand);
            OnCommandGenerated?.Invoke(emergencyCommand);
        }

        public void TriggerEmergencyStop(string reason = "")
        {
            if (!isEmergency)
            {
                isEmergency = true;
                LogStatus($"АВАРИЙНАЯ ОСТАНОВКА: {reason}");

                // Используем статический метод EventManager
                ParkingSystem.Integration.EventManager.TriggerEmergencyStop(reason);
            }
        }

        public void ResetEmergency()
        {
            if (isEmergency)
            {
                isEmergency = false;
                LogStatus("Аварийная ситуация сброшена");
            }
        }

        public void StartSystem()
        {
            isSystemActive = true;
            lastUpdateTime = Time.time;
            LogStatus("Система парковки запущена");
        }

        public void StopSystem()
        {
            isSystemActive = false;

            ParkingOutput stopCommand = new ParkingOutput
            {
                throttle = 0f,
                steering = 0f,
                brake = true
            };

            commandInterface.ApplyCommand(stopCommand);
            LogStatus("Система парковки остановлена");
        }

        public void PauseSystem()
        {
            isSystemActive = false;
            LogStatus("Система парковки на паузе");
        }

        public void ResumeSystem()
        {
            isSystemActive = true;
            LogStatus("Система парковки возобновлена");
        }

        private void LogStatus(string message)
        {
            if (enableDebug)
            {
                Debug.Log($"[ParkingSystem] {message}");
            }

            OnSystemStatusChanged?.Invoke(message);
        }

        public ParkingInput GetCurrentInput() => currentInput;
        public FuzzyInputData GetCurrentFuzzyInput() => currentFuzzyInput;
        public ParkingOutput GetCurrentOutput() => currentOutput;
        public bool IsSystemActive => isSystemActive;
        public bool IsEmergency => isEmergency;

        public void SetUpdateFrequency(float frequency)
        {
            updateFrequency = Mathf.Clamp(frequency, 1f, 60f);
            updateInterval = 1f / updateFrequency;
            LogStatus($"Частота обновления установлена: {updateFrequency} Гц");
        }
    }

    /// <summary>
    /// Полная заглушка для движка правил (реализует IFuzzyRuleEngine)
    /// ВАЖНО: TestRule возвращает FuzzyOutput из ParkingSystem.FuzzySystem.Inputs
    /// </summary>
    public class DummyRuleEngine : IFuzzyRuleEngine
    {
        private bool loggingEnabled = false;
        private FuzzyRuleBase currentRuleBase;

        // 1. ПРАВИЛЬНАЯ СИГНАТУРА для ExecuteRules
        public ParkingOutput ExecuteRules(FuzzyInputData fuzzyInput)
        {
            if (loggingEnabled)
                Debug.Log("DummyRuleEngine: Выполнение правил (заглушка)");

            // Простейшая логика для тестирования
            float throttle = 0.3f;
            float steering = Mathf.Sin(Time.time) * 0.2f; // Легкое раскачивание

            return new ParkingOutput
            {
                throttle = throttle,
                steering = steering,
                brake = false,
                emergencyStop = false,
                toggleForward = false,
                suggestedPhase = ParkingPhase.Searching
            };
        }

        // 2. ПРАВИЛЬНАЯ СИГНАТУРА для TestRule - возвращает FuzzyOutput!
        public FuzzyOutput TestRule(string ruleName, FuzzyInputData testInput)
        {
            if (loggingEnabled)
                Debug.Log($"DummyRuleEngine: Тестирование правила '{ruleName}'");

            // Создаем и возвращаем НАСТОЯЩИЙ FuzzyOutput
            FuzzyOutput output = new FuzzyOutput();

            // Заполняем тестовыми данными для Throttle
            output.throttleOutput = new Dictionary<string, float>
            {
                { "VeryLow", 0.1f },
                { "Low", 0.3f },
                { "Medium", 0.7f },
                { "High", 0.4f },
                { "VeryHigh", 0.1f }
            };

            // Заполняем тестовыми данными для Steering
            output.steeringOutput = new Dictionary<string, float>
            {
                { "HardLeft", 0.2f },
                { "Left", 0.5f },
                { "Center", 0.8f },
                { "Right", 0.3f },
                { "HardRight", 0.1f }
            };

            // Заполняем тестовыми данными для Brake
            output.brakeOutput = new Dictionary<string, float>
            {
                { "NoBrake", 0.9f },
                { "LightBrake", 0.2f },
                { "HardBrake", 0.05f }
            };

            // Заполняем тестовыми данными для Phase
            output.phaseOutput = new Dictionary<string, float>
            {
                { "Searching", 0.8f },
                { "Approaching", 0.4f },
                { "Aligning", 0.2f },
                { "Reversing", 0.1f },
                { "Completed", 0.05f }
            };

            return output;
        }

        // 3. Остальные методы интерфейса
        public void SetRuleBase(FuzzyRuleBase ruleBase)
        {
            currentRuleBase = ruleBase;
            Debug.Log($"DummyRuleEngine: Установлена база правил '{(ruleBase != null ? ruleBase.name : "null")}'");
        }

        public RuleInfo[] GetAllRulesInfo()
        {
            // Возвращаем массив с тестовыми правилами
            return new RuleInfo[]
            {
                new RuleInfo
                {
                    name = "RULE_001_SLOW_DOWN_IF_CLOSE",
                    description = "Замедлиться если препятствие близко впереди",
                    antecedents = new string[] { "FrontDistance is VeryClose" },
                    consequents = new string[] { "Throttle is Low", "Brake is LightBrake" },
                    weight = 0.95f,
                    isEnabled = true
                },
                new RuleInfo
                {
                    name = "RULE_002_TURN_IF_SIDE_CLOSE",
                    description = "Повернуть если препятствие близко сбоку",
                    antecedents = new string[] { "LeftSideDistance is VeryClose" },
                    consequents = new string[] { "Steering is Right" },
                    weight = 0.85f,
                    isEnabled = true
                },
                new RuleInfo
                {
                    name = "RULE_003_FIND_SPOT",
                    description = "Начать поиск места если скорость низкая",
                    antecedents = new string[] { "CurrentSpeed is VerySlow", "ParkingSpotWidth is Wide" },
                    consequents = new string[] { "ParkingPhase is Searching" },
                    weight = 0.75f,
                    isEnabled = true
                }
            };
        }

        public void SetLoggingEnabled(bool enabled)
        {
            loggingEnabled = enabled;
            Debug.Log($"DummyRuleEngine: Логирование {(enabled ? "включено" : "выключено")}");
        }
    }
}