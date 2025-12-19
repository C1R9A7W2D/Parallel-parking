using UnityEngine;
using System.Collections.Generic;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Главный процессор нечетких входов
    /// Координирует работу всех компонентов фаззификации
    /// </summary>
    public class SensorFuzzyProcessor
    {
        private LinguisticVariables linguisticVariables;
        private CrispToFuzzyConverter fuzzyConverter;
        private bool isInitialized = false;

        public SensorFuzzyProcessor(LinguisticVariables vars)
        {
            if (vars == null)
            {
                Debug.LogError("SensorFuzzyProcessor: LinguisticVariables не могут быть null!");
                return;
            }

            this.linguisticVariables = vars;
            this.fuzzyConverter = new CrispToFuzzyConverter(vars);
            this.isInitialized = true;

            // Проверяем валидность переменных
            if (!vars.ValidateVariables())
            {
                Debug.LogWarning("Некоторые лингвистические переменные не инициализированы!");
            }

            Debug.Log("SensorFuzzyProcessor инициализирован");
        }

        /// <summary>
        /// Основной метод обработки входных данных
        /// </summary>
        public FuzzyInputData Process(ParkingInput crispInput)
        {
            if (!isInitialized)
            {
                Debug.LogError("SensorFuzzyProcessor не инициализирован! Возвращаем пустые данные.");
                return new FuzzyInputData();
            }

            if (crispInput == null)
            {
                Debug.LogError("ParkingInput не может быть null!");
                return new FuzzyInputData();
            }

            // Логирование входных данных для отладки
            if (ShouldLogDebugInfo())
            {
                LogInputData(crispInput);
            }

            // Выполняем фаззификацию
            FuzzyInputData fuzzyData = fuzzyConverter.Convert(crispInput);

            // Валидация результатов
            ValidateFuzzyData(fuzzyData);

            return fuzzyData;
        }

        /// <summary>
        /// Пакетная обработка нескольких входных данных
        /// </summary>
        public List<FuzzyInputData> ProcessBatch(List<ParkingInput> crispInputs)
        {
            List<FuzzyInputData> results = new List<FuzzyInputData>();

            foreach (var input in crispInputs)
            {
                results.Add(Process(input));
            }

            return results;
        }

        /// <summary>
        /// Обработка только определенных переменных (для тестирования)
        /// </summary>
        public Dictionary<string, Dictionary<string, float>> ProcessSpecificVariables(
            ParkingInput crispInput,
            string[] variableNames)
        {
            var result = new Dictionary<string, Dictionary<string, float>>();

            foreach (var varName in variableNames)
            {
                var variable = linguisticVariables.GetVariable(varName);
                if (variable.HasValue)
                {
                    // В зависимости от имени переменной извлекаем соответствующее значение
                    float crispValue = GetCrispValueForVariable(crispInput, varName);
                    var fuzzified = fuzzyConverter.SafeFuzzify(crispValue, variable, varName);
                    result[varName] = fuzzified;
                }
            }

            return result;
        }

        /// <summary>
        /// Получить четкое значение для конкретной переменной
        /// </summary>
        private float GetCrispValueForVariable(ParkingInput input, string variableName)
        {
            switch (variableName.ToLower())
            {
                case "frontdistance":
                    return input.GetFrontDistance();
                case "reardistance":
                    return input.GetRearDistance();
                case "leftsidedistance":
                    return input.GetLeftDistance();
                case "rightsidedistance":
                    return input.GetRightDistance();
                case "parkingspotwidth":
                    var spot = input.FindNearestParkingSpot();
                    return spot.HasValue ? spot.Value.width : 0f;
                case "alignmentangle":
                    return input.carRotation;
                case "currentspeed":
                    return input.carSpeed;
                case "lateralerror":
                    spot = input.FindNearestParkingSpot();
                    return spot.HasValue ? CalculateLateralError(input.carPosition, spot.Value) : 0f;
                case "longitudinalerror":
                    spot = input.FindNearestParkingSpot();
                    return spot.HasValue ? CalculateLongitudinalError(input.carPosition, spot.Value) : 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Валидация фаззифицированных данных
        /// </summary>
        private void ValidateFuzzyData(FuzzyInputData data)
        {
            bool hasValidData = false;

            // Проверяем, есть ли хотя бы одна переменная с данными
            if (data.frontDistance != null && data.frontDistance.Count > 0) hasValidData = true;
            if (data.rearDistance != null && data.rearDistance.Count > 0) hasValidData = true;
            if (data.alignmentAngle != null && data.alignmentAngle.Count > 0) hasValidData = true;
            if (data.currentSpeed != null && data.currentSpeed.Count > 0) hasValidData = true;

            if (!hasValidData)
            {
                Debug.LogWarning("SensorFuzzyProcessor: Фаззифицированные данные пусты или невалидны!");
            }
        }

        /// <summary>
        /// Логирование входных данных для отладки
        /// </summary>
        private void LogInputData(ParkingInput input)
        {
            string log = $"Входные данные:\n" +
                        $"Позиция: {input.carPosition:F2}, Угол: {input.carRotation:F1}°\n" +
                        $"Скорость: {input.carSpeed:F2}/{input.carMaxSpeed:F1} м/с\n" +
                        $"Датчики: [{string.Join(", ", input.sensorDistances)}]\n" +
                        $"Мест: {input.availableSpots?.Length ?? 0}, Препятствий: {input.nearbyObstacles?.Length ?? 0}";

            Debug.Log(log);
        }

        /// <summary>
        /// Расчет боковой ошибки (для внутреннего использования)
        /// </summary>
        private float CalculateLateralError(Vector2 carPosition, ParkingSpot spot)
        {
            Vector2 spotPos = new Vector2(spot.position.x, spot.position.z);
            return Vector2.Distance(carPosition, spotPos);
        }

        /// <summary>
        /// Расчет продольной ошибки (для внутреннего использования)
        /// </summary>
        private float CalculateLongitudinalError(Vector2 carPosition, ParkingSpot spot)
        {
            Vector2 spotPos = new Vector2(spot.position.x, spot.position.z);
            Vector2 toSpot = spotPos - carPosition;
            float spotAngleRad = spot.angle * Mathf.Deg2Rad;
            Vector2 spotDirection = new Vector2(Mathf.Cos(spotAngleRad), Mathf.Sin(spotAngleRad));
            return Mathf.Abs(Vector2.Dot(toSpot, spotDirection));
        }

        /// <summary>
        /// Проверка, нужно ли логировать отладочную информацию
        /// </summary>
        private bool ShouldLogDebugInfo()
        {
            // Можно добавить флаг из конфигурации
            return Application.isEditor && Debug.isDebugBuild;
        }

        /// <summary>
        /// Сброс процессора
        /// </summary>
        public void Reset()
        {
            // Сброс состояния, если нужно
        }

        /// <summary>
        /// Проверка инициализации
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Получить лингвистические переменные
        /// </summary>
        public LinguisticVariables GetLinguisticVariables() => linguisticVariables;
    }
}