using System.Collections.Generic;
using UnityEngine;
using static ParkingSystem.FuzzySystem.Inputs.LinguisticVariables;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Конвертер четких значений в нечеткие (фаззификатор)
    /// Адаптирован под nullable FuzzyVariable
    /// </summary>
    public class CrispToFuzzyConverter
    {
        private LinguisticVariables linguisticVariables;

        public CrispToFuzzyConverter(LinguisticVariables variables)
        {
            this.linguisticVariables = variables;

            if (linguisticVariables == null)
            {
                Debug.LogWarning("LinguisticVariables не предоставлены, создаем пустой конвертер");
            }
        }

        /// <summary>
        /// Основной метод фаззификации входных данных
        /// </summary>
        public FuzzyInputData Convert(ParkingInput crispInput)
        {
            if (linguisticVariables == null)
            {
                Debug.LogError("LinguisticVariables не установлены! Возвращаем пустые данные.");
                return new FuzzyInputData();
            }

            FuzzyInputData fuzzyData = new FuzzyInputData();

            try
            {
                // 1. Фаззификация расстояний от датчиков
                FuzzifySensorDistances(crispInput, fuzzyData);

                // 2. Фаззификация парковочных мест
                FuzzifyParkingSpotData(crispInput, fuzzyData);

                // 3. Фаззификация углов
                FuzzifyAngles(crispInput, fuzzyData);

                // 4. Фаззификация скорости
                FuzzifySpeedData(crispInput, fuzzyData);

                // 5. Фаззификация позиционных ошибок
                FuzzifyPositionErrors(crispInput, fuzzyData);

                // 6. Установка метаданных (используем значения из crispInput)
                fuzzyData.timestamp = crispInput.timestamp;
                fuzzyData.frameCount = crispInput.frameCount;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка фаззификации: {ex.Message}\n{ex.StackTrace}");
            }

            return fuzzyData;
        }

        /// <summary>
        /// Фаззификация данных с датчиков расстояния
        /// </summary>
        private void FuzzifySensorDistances(ParkingInput crispInput, FuzzyInputData fuzzyData)
        {
            // Передние датчики
            if (crispInput.sensorDistances != null && crispInput.sensorDistances.Length >= 8)
            {
                fuzzyData.frontDistance = SafeFuzzify(
                    crispInput.sensorDistances[0],
                    linguisticVariables.GetVariable("frontDistance"),
                    "FrontDistance"
                );

                fuzzyData.rearDistance = SafeFuzzify(
                    crispInput.sensorDistances[4],
                    linguisticVariables.GetVariable("rearDistance"),
                    "RearDistance"
                );

                fuzzyData.leftSideDistance = SafeFuzzify(
                    crispInput.sensorDistances[6],
                    linguisticVariables.GetVariable("leftSideDistance"),
                    "LeftSideDistance"
                );

                fuzzyData.rightSideDistance = SafeFuzzify(
                    crispInput.sensorDistances[2],
                    linguisticVariables.GetVariable("rightSideDistance"),
                    "RightSideDistance"
                );
            }
            // После блока, где создаются fuzzyData.frontDistance и другие, добавьте:
            if (fuzzyData.frontDistance.ContainsKey("Close"))
                Debug.Log($"Front is 'Close': {fuzzyData.frontDistance["Close"]}");
            if (fuzzyData.rearDistance.ContainsKey("Far"))
                Debug.Log($"Rear is 'Far': {fuzzyData.rearDistance["Far"]}");
        }

        /// <summary>
        /// Фаззификация данных о парковочных местах
        /// </summary>
        private void FuzzifyParkingSpotData(ParkingInput crispInput, FuzzyInputData fuzzyData)
        {
            if (crispInput.availableSpots != null && crispInput.availableSpots.Length > 0)
            {
                var nearestSpot = crispInput.FindNearestParkingSpot();
                if (nearestSpot.HasValue)
                {
                    fuzzyData.parkingSpotWidth = SafeFuzzify(
                        nearestSpot.Value.width,
                        linguisticVariables.GetVariable("parkingSpotWidth"),
                        "ParkingSpotWidth"
                    );
                }
            }
        }

        /// <summary>
        /// Фаззификация угловых данных
        /// </summary>
        private void FuzzifyAngles(ParkingInput crispInput, FuzzyInputData fuzzyData)
        {
            fuzzyData.alignmentAngle = SafeFuzzify(
                crispInput.carRotation,
                linguisticVariables.GetVariable("alignmentAngle"),
                "AlignmentAngle"
            );

            // Вычисляем угол подхода к парковочному месту, если есть цель
            if (crispInput.availableSpots != null && crispInput.availableSpots.Length > 0)
            {
                var targetSpot = crispInput.FindNearestParkingSpot();
                if (targetSpot.HasValue)
                {
                    float approachAngle = CalculateApproachAngle(crispInput, targetSpot.Value);
                    fuzzyData.approachAngle = SafeFuzzify(
                        approachAngle,
                        linguisticVariables.GetVariable("approachAngle"),
                        "ApproachAngle"
                    );
                }
            }
        }

        /// <summary>
        /// Фаззификация скоростных данных
        /// </summary>
        private void FuzzifySpeedData(ParkingInput crispInput, FuzzyInputData fuzzyData)
        {
            fuzzyData.currentSpeed = SafeFuzzify(
                crispInput.carSpeed,
                linguisticVariables.GetVariable("currentSpeed"),
                "CurrentSpeed"
            );

            // Вычисляем ошибку скорости (целевая скорость - текущая)
            float targetSpeed = crispInput.movesForward ? crispInput.carMaxSpeed * 0.5f : -crispInput.carMaxSpeed * 0.3f;
            float speedError = targetSpeed - crispInput.carSpeed;

            fuzzyData.speedError = SafeFuzzify(
                speedError,
                linguisticVariables.GetVariable("speedError"),
                "SpeedError"
            );
        }

        /// <summary>
        /// Фаззификация позиционных ошибок
        /// </summary>
        private void FuzzifyPositionErrors(ParkingInput crispInput, FuzzyInputData fuzzyData)
        {
            if (crispInput.availableSpots != null && crispInput.availableSpots.Length > 0)
            {
                var targetSpot = crispInput.FindNearestParkingSpot();
                if (targetSpot.HasValue)
                {
                    float lateralError = CalculateLateralError(crispInput.carPosition, targetSpot.Value);
                    float longitudinalError = CalculateLongitudinalError(crispInput.carPosition, targetSpot.Value);

                    fuzzyData.lateralError = SafeFuzzify(
                        lateralError,
                        linguisticVariables.GetVariable("lateralError"),
                        "LateralError"
                    );

                    fuzzyData.longitudinalError = SafeFuzzify(
                        longitudinalError,
                        linguisticVariables.GetVariable("longitudinalError"),
                        "LongitudinalError"
                    );
                }
            }
        }

        /// <summary>
        /// Безопасная фаззификация с проверкой на null
        /// </summary>
        public Dictionary<string, float> SafeFuzzify(float crispValue, FuzzyVariable? variableNullable, string variableName)
        {
            if (!variableNullable.HasValue)
            {
                Debug.LogWarning($"Переменная {variableName} не найдена в LinguisticVariables");
                return new Dictionary<string, float>();
            }

            FuzzyVariable variable = variableNullable.Value;

            // Проверяем, инициализированы ли fuzzySets
            if (variable.fuzzySets == null || variable.fuzzySets.Length == 0)
            {
                Debug.LogWarning($"Переменная {variableName} не имеет fuzzySets");
                return new Dictionary<string, float>();
            }

            return FuzzifyValue(crispValue, variable);
        }

        /// <summary>
        /// Основная логика фаззификации
        /// </summary>
        private Dictionary<string, float> FuzzifyValue(float crispValue, FuzzyVariable variable)
        {
            Dictionary<string, float> result = new Dictionary<string, float>();

            // Нормализуем значение к диапазону переменной
            float normalizedValue = Mathf.Clamp(crispValue, variable.minValue, variable.maxValue);

            // Вычисляем степень принадлежности для каждого терма
            foreach (var fuzzySet in variable.fuzzySets)
            {
                if (fuzzySet == null) continue;

                float membership = fuzzySet.GetMembership(normalizedValue);
                if (membership > 0.001f) // Игнорируем очень маленькие значения
                {
                    result[fuzzySet.name] = membership;
                }
            }

            return result;
        }

        /// <summary>
        /// Расчет угла подхода к парковочному месту (2D версия)
        /// </summary>
        private float CalculateApproachAngle(ParkingInput input, ParkingSpot spot)
        {
            Vector2 carPos = input.carPosition;
            Vector2 spotPos = new Vector2(spot.position.x, spot.position.y); // Используем XY

            Vector2 toSpot = spotPos - carPos;
            float targetAngle = Mathf.Atan2(toSpot.y, toSpot.x) * Mathf.Rad2Deg;

            // Разница между текущим углом автомобиля и углом к цели
            // В 2D вращение по Z, поэтому используем carRotation как есть
            return Mathf.DeltaAngle(input.carRotation, targetAngle);
        }

        /// <summary>
        /// Расчет боковой ошибки (2D версия)
        /// </summary>
        private float CalculateLateralError(Vector2 carPosition, ParkingSpot spot)
        {
            Vector2 spotPos = new Vector2(spot.position.x, spot.position.y);
            return Vector2.Distance(carPosition, spotPos);
        }

        /// <summary>
        /// Расчет продольной ошибки (2D версия)
        /// </summary>
        private float CalculateLongitudinalError(Vector2 carPosition, ParkingSpot spot)
        {
            Vector2 spotPos = new Vector2(spot.position.x, spot.position.y);
            Vector2 toSpot = spotPos - carPosition;

            // Угол парковочного места в радианах (вращение по Z)
            float spotAngleRad = spot.angle * Mathf.Deg2Rad;
            Vector2 spotDirection = new Vector2(Mathf.Cos(spotAngleRad), Mathf.Sin(spotAngleRad));

            float projection = Vector2.Dot(toSpot, spotDirection);
            return Mathf.Abs(projection);
        }

        /// <summary>
        /// Установка новых лингвистических переменных
        /// </summary>
        public void SetLinguisticVariables(LinguisticVariables variables)
        {
            this.linguisticVariables = variables;
        }

        /// <summary>
        /// Быстрая фаззификация одного значения
        /// </summary>
        public Dictionary<string, float> QuickFuzzify(float crispValue, string variableName)
        {
            var variable = linguisticVariables.GetVariable(variableName);
            return SafeFuzzify(crispValue, variable, variableName);
        }
    }
}