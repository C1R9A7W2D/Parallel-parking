using ParkingSystem.FuzzySystem;
using ParkingSystem.FuzzySystem.Inputs;
using ParkingSystem.FuzzySystem.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkingSystem.Integration
{
    /// <summary>
    /// Главный контроллер интеграции системы парковки для 2D
    /// </summary>
    [RequireComponent(typeof(CarAI))]
    public class FuzzyParkingController : MonoBehaviour
    {
        [Header("Компоненты системы")]
        [SerializeField] private CarSpawner carSpawner;
        [SerializeField] private LinguisticVariables linguisticVariables;
        [SerializeField] private FuzzyRuleBase ruleBase;
        [SerializeField] private GameObject ruleEngineObject; // GameObject с компонентом IFuzzyRuleEngine

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

        /// <summary>
        /// Инициализация всех компонентов системы (2D версия)
        /// </summary>
        private void InitializeComponents()
        {
            try
            {
                // 1. Получаем компонент CarAI (2D версия)
                carAI = GetComponent<CarAI>();
                if (carAI == null)
                {
                    throw new NullReferenceException("CarAI компонент не найден на GameObject!");
                }

                // 2. Инициализируем сборщик данных с датчиков (работает с 2D физикой)
                sensorCollector = new SensorDataCollector(carAI, carSpawner);
                Debug.Log($"[FuzzyParkingController] SensorDataCollector инициализирован для 2D");

                // 3. Загружаем или проверяем лингвистические переменные
                if (linguisticVariables != null)
                {
                    // Проверяем, что переменные инициализированы
                    if (!linguisticVariables.ValidateVariables())
                    {
                        Debug.LogWarning("Некоторые лингвистические переменные не инициализированы!");
                        // Можно вызвать инициализацию по умолчанию
                        linguisticVariables.InitializeDefaults();
                    }

                    // Создаем процессор фаззификации
                    fuzzyProcessor = new SensorFuzzyProcessor(linguisticVariables);
                    Debug.Log($"[FuzzyParkingController] SensorFuzzyProcessor создан");
                }
                else
                {
                    // Пробуем загрузить из Resources
                    Debug.LogWarning("LinguisticVariables не назначены в инспекторе, пытаемся загрузить из Resources...");
                    linguisticVariables = Resources.Load<LinguisticVariables>("DefaultLinguisticVars");

                    if (linguisticVariables == null)
                    {
                        // Создаем временные переменные по умолчанию
                        Debug.LogError("Не удалось загрузить лингвистические переменные! Создаем временные...");
                        linguisticVariables = ScriptableObject.CreateInstance<LinguisticVariables>();
                        linguisticVariables.InitializeDefaults();
                    }

                    fuzzyProcessor = new SensorFuzzyProcessor(linguisticVariables);
                    Debug.Log($"[FuzzyParkingController] LinguisticVariables загружены из Resources");
                }

                // 4. Создаем интерфейс управления автомобилем (адаптирован для 2D CarAI)
                commandInterface = new VehicleCommandInterface(carAI);
                Debug.Log($"[FuzzyParkingController] VehicleCommandInterface создан");

                // 5. Инициализация движка нечетких правил
                InitializeRuleEngine();

                // 6. Устанавливаем базу правил для движка
                if (ruleBase != null && ruleEngine != null)
                {
                    ruleEngine.SetRuleBase(ruleBase);
                    Debug.Log($"[FuzzyParkingController] База правил установлена: {ruleBase.name}");
                }
                else if (ruleEngine != null)
                {
                    Debug.LogWarning("FuzzyRuleBase не назначена в инспекторе!");
                }

                // 7. Настраиваем частоту обновления
                updateInterval = 1f / Mathf.Clamp(updateFrequency, 1f, 60f);
                lastUpdateTime = Time.time;

                // 8. Подписываемся на события CarAI (опционально)
                // carAI.OnSpeedChanged += OnCarSpeedChanged;
                // carAI.OnDirectionChanged += OnCarDirectionChanged;

                // 9. Выводим информацию об инициализации
                string initLog = $"\n=== Система парковки 2D инициализирована ===\n" +
                                $"Автомобиль: {carAI.gameObject.name}\n" +
                                $"Позиция: {carAI.Position}\n" +
                                $"Скорость: {carAI.CurrentSpeed}/{carAI.MaxSpeed}\n" +
                                $"Датчики: {carAI.SensorRange}м\n" +
                                $"Частота обновления: {updateFrequency} Гц\n" +
                                $"Движок правил: {(ruleEngine != null ? ruleEngine.GetType().Name : "Нет")}\n" +
                                $"База правил: {(ruleBase != null ? ruleBase.name : "Нет")}";

                Debug.Log(initLog);
                LogStatus("Система парковки 2D успешно инициализирована");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка инициализации системы 2D: {ex.Message}\n{ex.StackTrace}");
                enabled = false;
                throw;
            }
        }

        /// <summary>
        /// Инициализация движка нечетких правил
        /// </summary>
        private void InitializeRuleEngine()
        {
            // Вариант 1: Ищем движок на этом же GameObject
            ruleEngine = GetComponent<IFuzzyRuleEngine>();

            // Вариант 2: Ищем на дочерних объектах
            if (ruleEngine == null)
            {
                ruleEngine = GetComponentInChildren<IFuzzyRuleEngine>();
            }

            // Вариант 3: Используем назначенный GameObject
            if (ruleEngine == null && ruleEngineObject != null)
            {
                ruleEngine = ruleEngineObject.GetComponent<IFuzzyRuleEngine>();
            }

            // Вариант 4: Создаем заглушку, если движок не найден
            if (ruleEngine == null)
            {
                Debug.LogWarning("IFuzzyRuleEngine не найден, создаем DummyRuleEngine");

                // Создаем GameObject для движка
                GameObject engineGO = new GameObject("DummyRuleEngine");
                engineGO.transform.SetParent(transform);
                engineGO.transform.localPosition = Vector3.zero;

                // Добавляем компонент заглушки
                var dummyEngine = engineGO.AddComponent<DummyRuleEngine>();
                ruleEngine = dummyEngine;

                // Включаем логирование для отладки
                ruleEngine.SetLoggingEnabled(enableDebug);

                Debug.Log($"[FuzzyParkingController] Создан DummyRuleEngine на {engineGO.name}");
            }
            else
            {
                Debug.Log($"[FuzzyParkingController] Найден движок правил: {ruleEngine.GetType().Name}");

                // Настраиваем логирование в зависимости от флага отладки
                ruleEngine.SetLoggingEnabled(enableDebug);
            }

            // Проверяем, что движок инициализирован
            if (ruleEngine == null)
            {
                throw new InvalidOperationException("Не удалось инициализировать движок нечетких правил!");
            }
        }

        /// <summary>
        /// Обновление системы парковки (вызывается из Update или FixedUpdate)
        /// </summary>
        private void UpdateParkingSystem()
        {
            if (isEmergency)
            {
                HandleEmergency();
                return;
            }

            try
            {
                // 1. Сбор данных с датчиков (2D физика)
                currentInput = sensorCollector.CollectSensorData();
                OnInputUpdated?.Invoke(currentInput);

                // 2. Проверка безопасности
                if (CheckSafetyViolations(currentInput))
                {
                    TriggerEmergencyStop("Обнаружено препятствие слишком близко!");
                    return;
                }

                // 3. Фаззификация входных данных
                if (fuzzyProcessor != null && fuzzyProcessor.IsInitialized)
                {
                    currentFuzzyInput = fuzzyProcessor.Process(currentInput);
                    OnFuzzyInputUpdated?.Invoke(currentFuzzyInput);
                }
                else
                {
                    Debug.LogWarning("FuzzyProcessor не инициализирован, пропускаем фаззификацию");
                    return;
                }

                // 4. Выполнение нечетких правил
                if (ruleEngine != null)
                {
                    // Используем правильный тип FuzzyInputData
                    currentOutput = ruleEngine.ExecuteRules(currentFuzzyInput);

                    // 5. Применение команд к автомобилю (2D управление)
                    commandInterface.ApplyCommand(currentOutput);

                    // 6. Отправка события
                    OnCommandGenerated?.Invoke(currentOutput);

                    // 7. Логирование для отладки
                    if (enableDebug && Time.frameCount % 30 == 0) // Каждые 30 кадров
                    {
                        Debug.Log($"[ParkingSystem] Команда: " +
                                 $"throttle={currentOutput.throttle:F2}, " +
                                 $"steering={currentOutput.steering:F2}, " +
                                 $"brake={currentOutput.brake}, " +
                                 $"phase={currentOutput.suggestedPhase}");
                    }
                }
                else
                {
                    Debug.LogError("RuleEngine не инициализирован!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка в цикле парковки 2D: {ex.Message}\n{ex.StackTrace}");
                TriggerEmergencyStop($"Системная ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверка нарушений безопасности
        /// </summary>
        private bool CheckSafetyViolations(ParkingInput input)
        {
            if (input.sensorDistances == null || input.sensorDistances.Length == 0)
                return false;

            for (int i = 0; i < input.sensorDistances.Length; i++)
            {
                float distance = input.sensorDistances[i];
                if (distance < emergencyStopDistance)
                {
                    // ДОБАВЬТЕ ЭТУ СТРОКУ:
                    Debug.Log($"Датчик {i}: расстояние {distance} < {emergencyStopDistance}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Обработка аварийной ситуации
        /// </summary>
        private void HandleEmergency()
        {
            ParkingOutput emergencyCommand = new ParkingOutput
            {
                throttle = 0f, // В 2D: 0 = остановка
                steering = 0f,
                brake = true,
                emergencyStop = true,
                suggestedPhase = ParkingPhase.Emergency
            };

            commandInterface.ApplyCommand(emergencyCommand);
            OnCommandGenerated?.Invoke(emergencyCommand);
        }

        // ========== Публичные методы управления системой ==========

        public void TriggerEmergencyStop(string reason = "")
        {
            if (!isEmergency)
            {
                isEmergency = true;
                LogStatus($"АВАРИЙНАЯ ОСТАНОВКА 2D: {reason}");
                EventManager.TriggerEmergencyStop(reason);
            }
        }

        public void ResetEmergency()
        {
            if (isEmergency)
            {
                isEmergency = false;
                LogStatus("Аварийная ситуация 2D сброшена");
            }
        }

        public void StartSystem()
        {
            isSystemActive = true;
            lastUpdateTime = Time.time;
            LogStatus("Система парковки 2D запущена");
        }

        public void StopSystem()
        {
            isSystemActive = false;

            ParkingOutput stopCommand = new ParkingOutput
            {
                throttle = 0f,
                steering = 0f,
                brake = true,
                emergencyStop = false,
                suggestedPhase = ParkingPhase.Searching
            };

            commandInterface.ApplyCommand(stopCommand);
            LogStatus("Система парковки 2D остановлена");
        }

        public void PauseSystem()
        {
            isSystemActive = false;
            LogStatus("Система парковки 2D на паузе");
        }

        public void ResumeSystem()
        {
            isSystemActive = true;
            lastUpdateTime = Time.time;
            LogStatus("Система парковки 2D возобновлена");
        }

        /// <summary>
        /// Логирование статуса системы
        /// </summary>
        private void LogStatus(string message)
        {
            if (enableDebug)
            {
                Debug.Log($"[ParkingSystem2D] {message}");
            }

            OnSystemStatusChanged?.Invoke(message);
            EventManager.TriggerSystemMessage(message);
        }

        // ========== Методы доступа к данным ==========

        public ParkingInput GetCurrentInput() => currentInput;
        public FuzzyInputData GetCurrentFuzzyInput() => currentFuzzyInput;
        public ParkingOutput GetCurrentOutput() => currentOutput;
        public bool IsSystemActive => isSystemActive;
        public bool IsEmergency => isEmergency;
        public CarAI GetCarAI() => carAI;
        public IFuzzyRuleEngine GetRuleEngine() => ruleEngine;
        public LinguisticVariables GetLinguisticVariables() => linguisticVariables;

        /// <summary>
        /// Изменить частоту обновления системы
        /// </summary>
        public void SetUpdateFrequency(float frequency)
        {
            updateFrequency = Mathf.Clamp(frequency, 1f, 60f);
            updateInterval = 1f / updateFrequency;
            LogStatus($"Частота обновления установлена: {updateFrequency} Гц");
        }

        /// <summary>
        /// Назначить новую базу правил во время выполнения
        /// </summary>
        public void SetRuleBase(FuzzyRuleBase newRuleBase)
        {
            if (newRuleBase == null)
            {
                Debug.LogError("Попытка назначить null базу правил!");
                return;
            }

            ruleBase = newRuleBase;
            if (ruleEngine != null)
            {
                ruleEngine.SetRuleBase(ruleBase);
                LogStatus($"База правил изменена на: {ruleBase.name}");
            }
        }

        /// <summary>
        /// Назначить новые лингвистические переменные
        /// </summary>
        public void SetLinguisticVariables(LinguisticVariables newVariables)
        {
            if (newVariables == null)
            {
                Debug.LogError("Попытка назначить null лингвистические переменные!");
                return;
            }

            linguisticVariables = newVariables;
            fuzzyProcessor = new SensorFuzzyProcessor(linguisticVariables);
            LogStatus($"Лингвистические переменные обновлены");
        }

        // ========== Unity методы ==========

        void Awake()
        {
            InitializeComponents();
        }

        void Start()
        {
            StartSystem();
        }

        void Update()
        {
            if (!isSystemActive || isEmergency) return;

            if (!useFixedUpdate && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateParkingSystem();
                lastUpdateTime = Time.time;
            }
        }

        void FixedUpdate()
        {
            if (!isSystemActive || isEmergency) return;

            if (useFixedUpdate && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateParkingSystem();
                lastUpdateTime = Time.time;
            }
        }

        void OnDestroy()
        {
            StopSystem();

            // Отписываемся от событий
            if (carAI != null)
            {
                // carAI.OnSpeedChanged -= OnCarSpeedChanged;
                // carAI.OnDirectionChanged -= OnCarDirectionChanged;
            }

            LogStatus("Система парковки 2D выключена");
        }

        void OnValidate()
        {
            // В редакторе обновляем интервал при изменении частоты
            if (updateFrequency > 0)
            {
                updateInterval = 1f / updateFrequency;
            }
        }

        // ========== Вспомогательные классы ==========

        /// <summary>
        /// Заглушка движка правил для тестирования (2D версия)
        /// </summary>
        [System.Serializable]
        public class DummyRuleEngine : MonoBehaviour, IFuzzyRuleEngine
        {
            private bool loggingEnabled = false;
            private FuzzyRuleBase currentRuleBase;
            private float oscillationTimer = 0f;

            public ParkingOutput ExecuteRules(FuzzyInputData fuzzyInput)
            {
                if (loggingEnabled)
                    Debug.Log("DummyRuleEngine: Выполнение правил (заглушка)");

                // Простейшая логика для тестирования в 2D
                oscillationTimer += Time.deltaTime;

                float throttle = Mathf.Sin(oscillationTimer * 0.5f) * 0.3f + 0.2f; // -0.1..0.5
                float steering = Mathf.Cos(oscillationTimer * 0.3f) * 0.4f; // -0.4..0.4

                // Автоматическое переключение направления при достижении границ
                if (Mathf.Abs(steering) > 0.35f)
                {
                    throttle *= -0.5f; // Медленное движение назад
                }

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

            public FuzzyOutput TestRule(string ruleName, FuzzyInputData testInput)
            {
                if (loggingEnabled)
                    Debug.Log($"DummyRuleEngine: Тестирование правила '{ruleName}'");

                // Создаем тестовые выходные данные
                FuzzyOutput output = new FuzzyOutput
                {
                    throttleOutput = new Dictionary<string, float>
                    {
                        { "VeryLow", 0.1f },
                        { "Low", 0.3f },
                        { "Medium", 0.7f },
                        { "High", 0.4f },
                        { "VeryHigh", 0.1f }
                    },
                    steeringOutput = new Dictionary<string, float>
                    {
                        { "HardLeft", 0.2f },
                        { "Left", 0.5f },
                        { "Center", 0.8f },
                        { "Right", 0.3f },
                        { "HardRight", 0.1f }
                    },
                    brakeOutput = new Dictionary<string, float>
                    {
                        { "NoBrake", 0.9f },
                        { "LightBrake", 0.2f },
                        { "HardBrake", 0.05f }
                    },
                    phaseOutput = new Dictionary<string, float>
                    {
                        { "Searching", 0.8f },
                        { "Approaching", 0.4f },
                        { "Aligning", 0.2f },
                        { "Reversing", 0.1f },
                        { "Completed", 0.05f }
                    }
                };

                return output;
            }

            public void SetRuleBase(FuzzyRuleBase ruleBase)
            {
                currentRuleBase = ruleBase;
                if (loggingEnabled)
                    Debug.Log($"DummyRuleEngine: Установлена база правил '{(ruleBase != null ? ruleBase.name : "null")}'");
            }

            public RuleInfo[] GetAllRulesInfo()
            {
                return new RuleInfo[]
                {
                    new RuleInfo
                    {
                        name = "RULE_001_SLOW_DOWN_IF_CLOSE",
                        description = "Замедлиться если препятствие близко впереди (2D)",
                        antecedents = new string[] { "FrontDistance is VeryClose" },
                        consequents = new string[] { "Throttle is Low", "Brake is LightBrake" },
                        weight = 0.95f,
                        isEnabled = true
                    },
                    new RuleInfo
                    {
                        name = "RULE_002_TURN_IF_SIDE_CLOSE",
                        description = "Повернуть если препятствие близко сбоку (2D)",
                        antecedents = new string[] { "LeftSideDistance is VeryClose" },
                        consequents = new string[] { "Steering is Right" },
                        weight = 0.85f,
                        isEnabled = true
                    },
                    new RuleInfo
                    {
                        name = "RULE_003_FIND_SPOT",
                        description = "Начать поиск места если скорость низкая (2D)",
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
                if (enabled)
                    Debug.Log($"DummyRuleEngine: Логирование включено");
            }
        }
    }
}