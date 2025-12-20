using UnityEngine;
using System;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Лингвистические переменные для нечеткой системы
    /// </summary>
    [CreateAssetMenu(fileName = "NewLinguisticVariables", menuName = "Parking System/Linguistic Variables")]
    public class LinguisticVariables : ScriptableObject
    {
        [Header("Дистанционные переменные")]
        public FuzzyVariable frontDistance;      // Расстояние вперед (0-10 м)
        public FuzzyVariable rearDistance;       // Расстояние назад (0-10 м)
        public FuzzyVariable leftSideDistance;   // Расстояние слева (0-5 м)
        public FuzzyVariable rightSideDistance;  // Расстояние справа (0-5 м)
        public FuzzyVariable parkingSpotWidth;   // Ширина парковочного места (2-4 м)

        [Header("Угловые переменные")]
        public FuzzyVariable alignmentAngle;     // Угол выравнивания (-45 до 45 градусов)
        public FuzzyVariable approachAngle;      // Угол подхода (-90 до 90 градусов)

        [Header("Позиционные переменные")]
        public FuzzyVariable lateralError;       // Боковая ошибка (0-2 м)
        public FuzzyVariable longitudinalError;  // Продольная ошибка (0-5 м)

        [Header("Скоростные переменные")]
        public FuzzyVariable currentSpeed;       // Текущая скорость (0-5 м/с)
        public FuzzyVariable speedError;         // Ошибка скорости (-2 до 2 м/с)

        /// <summary>
        /// Инициализация переменных значениями по умолчанию
        /// </summary>
        [ContextMenu("Initialize Defaults")]
        public void InitializeDefaults()
        {
            // Расстояние вперед (0-10 м)
            frontDistance = new FuzzyVariable
            {
                name = "FrontDistance",
                minValue = 0f,
                maxValue = 10f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VeryClose", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 1f } },
            new FuzzySet { name = "Close", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1.5f, 2.5f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 2f, 3f, 4f } },
            new FuzzySet { name = "Far", type = MembershipType.Triangular, parameters = new float[] { 3f, 5f, 7f } },
            new FuzzySet { name = "VeryFar", type = MembershipType.Triangular, parameters = new float[] { 6f, 10f, 10f } }
                }
            };

            // Расстояние назад (0-10 м)
            rearDistance = new FuzzyVariable
            {
                name = "RearDistance",
                minValue = 0f,
                maxValue = 10f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VeryClose", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 1f } },
            new FuzzySet { name = "Close", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1.5f, 2.5f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 2f, 3f, 4f } },
            new FuzzySet { name = "Far", type = MembershipType.Triangular, parameters = new float[] { 3f, 5f, 7f } },
            new FuzzySet { name = "VeryFar", type = MembershipType.Triangular, parameters = new float[] { 6f, 10f, 10f } }
                }
            };

            // Расстояние слева (0-5 м)
            leftSideDistance = new FuzzyVariable
            {
                name = "LeftSideDistance",
                minValue = 0f,
                maxValue = 5f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VeryClose", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 1f } },
            new FuzzySet { name = "Close", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1.5f, 2.5f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 2f, 3f, 4f } },
            new FuzzySet { name = "Far", type = MembershipType.Triangular, parameters = new float[] { 3f, 4f, 5f } },
            new FuzzySet { name = "VeryFar", type = MembershipType.Triangular, parameters = new float[] { 4f, 5f, 5f } }
                }
            };

            // Расстояние справа (0-5 м)
            rightSideDistance = new FuzzyVariable
            {
                name = "RightSideDistance",
                minValue = 0f,
                maxValue = 5f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VeryClose", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 1f } },
            new FuzzySet { name = "Close", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1.5f, 2.5f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 2f, 3f, 4f } },
            new FuzzySet { name = "Far", type = MembershipType.Triangular, parameters = new float[] { 3f, 4f, 5f } },
            new FuzzySet { name = "VeryFar", type = MembershipType.Triangular, parameters = new float[] { 4f, 5f, 5f } }
                }
            };

            // Ширина парковочного места (2-4 м)
            parkingSpotWidth = new FuzzyVariable
            {
                name = "ParkingSpotWidth",
                minValue = 2f,
                maxValue = 4f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VeryNarrow", type = MembershipType.Triangular, parameters = new float[] { 2f, 2f, 2.5f } },
            new FuzzySet { name = "Narrow", type = MembershipType.Triangular, parameters = new float[] { 2f, 2.5f, 3f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 2.5f, 3f, 3.5f } },
            new FuzzySet { name = "Wide", type = MembershipType.Triangular, parameters = new float[] { 3f, 3.5f, 4f } },
            new FuzzySet { name = "VeryWide", type = MembershipType.Triangular, parameters = new float[] { 3.5f, 4f, 4f } }
                }
            };

            // Угол выравнивания (-45 до 45 градусов)
            alignmentAngle = new FuzzyVariable
            {
                name = "AlignmentAngle",
                minValue = -45f,
                maxValue = 45f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "LargeLeft", type = MembershipType.Triangular, parameters = new float[] { -45f, -45f, -25f } },
            new FuzzySet { name = "SmallLeft", type = MembershipType.Triangular, parameters = new float[] { -30f, -15f, 0f } },
            new FuzzySet { name = "Aligned", type = MembershipType.Triangular, parameters = new float[] { -10f, 0f, 10f } },
            new FuzzySet { name = "SmallRight", type = MembershipType.Triangular, parameters = new float[] { 0f, 15f, 30f } },
            new FuzzySet { name = "LargeRight", type = MembershipType.Triangular, parameters = new float[] { 25f, 45f, 45f } }
                }
            };

            // Угол подхода (-90 до 90 градусов)
            approachAngle = new FuzzyVariable
            {
                name = "ApproachAngle",
                minValue = -90f,
                maxValue = 90f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "LargeLeft", type = MembershipType.Triangular, parameters = new float[] { -90f, -90f, -45f } },
            new FuzzySet { name = "SmallLeft", type = MembershipType.Triangular, parameters = new float[] { -60f, -30f, 0f } },
            new FuzzySet { name = "Aligned", type = MembershipType.Triangular, parameters = new float[] { -15f, 0f, 15f } },
            new FuzzySet { name = "SmallRight", type = MembershipType.Triangular, parameters = new float[] { 0f, 30f, 60f } },
            new FuzzySet { name = "LargeRight", type = MembershipType.Triangular, parameters = new float[] { 45f, 90f, 90f } }
                }
            };

            // Боковая ошибка (0-2 м)
            lateralError = new FuzzyVariable
            {
                name = "LateralError",
                minValue = 0f,
                maxValue = 2f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VerySmall", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 0.5f } },
            new FuzzySet { name = "Small", type = MembershipType.Triangular, parameters = new float[] { 0f, 0.5f, 1f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1f, 1.5f } },
            new FuzzySet { name = "Large", type = MembershipType.Triangular, parameters = new float[] { 1f, 1.5f, 2f } },
            new FuzzySet { name = "VeryLarge", type = MembershipType.Triangular, parameters = new float[] { 1.5f, 2f, 2f } }
                }
            };

            // Продольная ошибка (0-5 м)
            longitudinalError = new FuzzyVariable
            {
                name = "LongitudinalError",
                minValue = 0f,
                maxValue = 5f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "VerySmall", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 1f } },
            new FuzzySet { name = "Small", type = MembershipType.Triangular, parameters = new float[] { 0f, 1f, 2f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 1f, 2f, 3f } },
            new FuzzySet { name = "Large", type = MembershipType.Triangular, parameters = new float[] { 2f, 3f, 4f } },
            new FuzzySet { name = "VeryLarge", type = MembershipType.Triangular, parameters = new float[] { 3f, 5f, 5f } }
                }
            };

            // Текущая скорость (0-5 м/с)
            currentSpeed = new FuzzyVariable
            {
                name = "CurrentSpeed",
                minValue = 0f,
                maxValue = 5f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "Stopped", type = MembershipType.Triangular, parameters = new float[] { 0f, 0f, 0.5f } },
            new FuzzySet { name = "VerySlow", type = MembershipType.Triangular, parameters = new float[] { 0f, 0.5f, 1f } },
            new FuzzySet { name = "Slow", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1f, 2f } },
            new FuzzySet { name = "Medium", type = MembershipType.Triangular, parameters = new float[] { 1f, 2f, 3f } },
            new FuzzySet { name = "Fast", type = MembershipType.Triangular, parameters = new float[] { 2f, 3f, 4f } },
            new FuzzySet { name = "VeryFast", type = MembershipType.Triangular, parameters = new float[] { 3f, 5f, 5f } }
                }
            };

            // Ошибка скорости (-2 до 2 м/с)
            speedError = new FuzzyVariable
            {
                name = "SpeedError",
                minValue = -2f,
                maxValue = 2f,
                fuzzySets = new FuzzySet[]
                {
            new FuzzySet { name = "LargeNegative", type = MembershipType.Triangular, parameters = new float[] { -2f, -2f, -1f } },
            new FuzzySet { name = "SmallNegative", type = MembershipType.Triangular, parameters = new float[] { -1.5f, -1f, -0.5f } },
            new FuzzySet { name = "Zero", type = MembershipType.Triangular, parameters = new float[] { -0.5f, 0f, 0.5f } },
            new FuzzySet { name = "SmallPositive", type = MembershipType.Triangular, parameters = new float[] { 0.5f, 1f, 1.5f } },
            new FuzzySet { name = "LargePositive", type = MembershipType.Triangular, parameters = new float[] { 1f, 2f, 2f } }
                }
            };
        }

        /// <summary>
        /// Проверка корректности всех переменных
        /// </summary>
        public bool ValidateVariables()
        {
            bool isValid = true;

            // Проверяем все переменные, не только некоторые
            if (IsVariableInvalid(frontDistance, "FrontDistance"))
                isValid = false;

            if (IsVariableInvalid(rearDistance, "RearDistance"))
                isValid = false;

            if (IsVariableInvalid(leftSideDistance, "LeftSideDistance"))
                isValid = false;

            if (IsVariableInvalid(rightSideDistance, "RightSideDistance"))
                isValid = false;

            if (IsVariableInvalid(parkingSpotWidth, "ParkingSpotWidth"))
                isValid = false;

            if (IsVariableInvalid(alignmentAngle, "AlignmentAngle"))
                isValid = false;

            if (IsVariableInvalid(approachAngle, "ApproachAngle"))
                isValid = false;

            if (IsVariableInvalid(lateralError, "LateralError"))
                isValid = false;

            if (IsVariableInvalid(longitudinalError, "LongitudinalError"))
                isValid = false;

            if (IsVariableInvalid(currentSpeed, "CurrentSpeed"))
                isValid = false;

            if (IsVariableInvalid(speedError, "SpeedError"))
                isValid = false;

            return isValid;
        }

        /// <summary>
        /// Получить переменную по имени
        /// </summary>
        public FuzzyVariable? GetVariable(string variableName)
        {
            switch (variableName.ToLower())
            {
                case "frontdistance":
                case "front_distance":
                    return frontDistance;
                case "reardistance":
                case "rear_distance":
                    return rearDistance;
                case "leftsidedistance":
                case "left_side_distance":
                    return leftSideDistance;
                case "rightsidedistance":
                case "right_side_distance":
                    return rightSideDistance;
                case "parkingspotwidth":
                case "parking_spot_width":
                    return parkingSpotWidth;
                case "alignmentangle":
                case "alignment_angle":
                    return alignmentAngle;
                case "approachangle":
                case "approach_angle":
                    return approachAngle;
                case "lateralerror":
                case "lateral_error":
                    return lateralError;
                case "longitudinalerror":
                case "longitudinal_error":
                    return longitudinalError;
                case "currentspeed":
                case "current_speed":
                    return currentSpeed;
                case "speederror":
                case "speed_error":
                    return speedError;
                default:
                    Debug.LogWarning($"Неизвестная лингвистическая переменная: {variableName}");
                    return null; // Возвращаем null для неизвестных имен
            }
        }

        /// <summary>
        /// Вспомогательный метод для проверки валидности переменной
        /// </summary>
        private bool IsVariableInvalid(FuzzyVariable variable, string variableName)
        {
            // Проверяем, пустой ли массив fuzzySets или сам массив null
            if (variable.fuzzySets == null || variable.fuzzySets.Length == 0)
            {
                Debug.LogError($"{variableName} не инициализирована!");
                return true;
            }

            // Дополнительная проверка: убедимся, что все fuzzySets имеют корректные параметры
            foreach (var fuzzySet in variable.fuzzySets)
            {
                if (string.IsNullOrEmpty(fuzzySet.name) || fuzzySet.parameters == null)
                {
                    Debug.LogError($"{variableName} содержит некорректный FuzzySet!");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Лингвистическая переменная
        /// </summary>
        [System.Serializable]
        public struct FuzzyVariable
        {
            public string name;              // Имя переменной
            public float minValue;           // Минимальное значение
            public float maxValue;           // Максимальное значение
            public FuzzySet[] fuzzySets;     // Нечеткие множества (термы)

            /// <summary>
            /// Получить диапазон значений
            /// </summary>
            public float Range => maxValue - minValue;

            /// <summary>
            /// Нормализовать значение к диапазону [0, 1]
            /// </summary>
            public float Normalize(float value)
            {
                return Mathf.InverseLerp(minValue, maxValue, value);
            }

            /// <summary>
            /// Денормализовать значение из диапазона [0, 1]
            /// </summary>
            public float Denormalize(float normalizedValue)
            {
                return Mathf.Lerp(minValue, maxValue, normalizedValue);
            }
        }

        /// <summary>
        /// Тип функции принадлежности
        /// </summary>
        public enum MembershipType
        {
            Triangular,     // Треугольная
            Trapezoidal,    // Трапециевидная
            Gaussian,       // Гауссова
            Bell,          // Колоколообразная
            Sigmoid        // Сигмоидальная
        }

        /// <summary>
        /// Нечеткое множество (терм)
        /// </summary>
        [System.Serializable]
        public class FuzzySet
        {
            public string name;              // Имя терма (VeryLow, Low, Medium, High, VeryHigh)
            public MembershipType type;      // Тип функции принадлежности
            public float[] parameters;       // Параметры функции

            /// <summary>
            /// Вычислить степень принадлежности значения
            /// </summary>
            public float GetMembership(float crispValue)
            {
                if (parameters == null || parameters.Length == 0)
                    return 0f;

                switch (type)
                {
                    case MembershipType.Triangular:
                        if (parameters.Length < 3) return 0f;
                        return Triangular(crispValue, parameters[0], parameters[1], parameters[2]);

                    case MembershipType.Trapezoidal:
                        if (parameters.Length < 4) return 0f;
                        return Trapezoidal(crispValue, parameters[0], parameters[1], parameters[2], parameters[3]);

                    case MembershipType.Gaussian:
                        if (parameters.Length < 2) return 0f;
                        return Gaussian(crispValue, parameters[0], parameters[1]);

                    case MembershipType.Bell:
                        if (parameters.Length < 3) return 0f;
                        return Bell(crispValue, parameters[0], parameters[1], parameters[2]);

                    case MembershipType.Sigmoid:
                        if (parameters.Length < 2) return 0f;
                        return Sigmoid(crispValue, parameters[0], parameters[1]);

                    default:
                        return 0f;
                }
            }

            // Треугольная функция
            private float Triangular(float x, float a, float b, float c)
            {
                if (x <= a || x >= c) return 0f;
                if (x == b) return 1f;
                if (x > a && x < b) return (x - a) / (b - a);
                return (c - x) / (c - b);
            }

            // Трапециевидная функция
            private float Trapezoidal(float x, float a, float b, float c, float d)
            {
                if (x <= a || x >= d) return 0f;
                if (x >= b && x <= c) return 1f;
                if (x > a && x < b) return (x - a) / (b - a);
                return (d - x) / (d - c);
            }

            // Гауссова функция
            private float Gaussian(float x, float mean, float sigma)
            {
                if (sigma <= 0) return 0f;
                float exponent = -0.5f * Mathf.Pow((x - mean) / sigma, 2);
                return Mathf.Exp(exponent);
            }

            // Колоколообразная функция
            private float Bell(float x, float a, float b, float c)
            {
                return 1f / (1f + Mathf.Pow(Mathf.Abs((x - c) / a), 2 * b));
            }

            // Сигмоидальная функция
            private float Sigmoid(float x, float a, float c)
            {
                return 1f / (1f + Mathf.Exp(-a * (x - c)));
            }
        }
    }
}