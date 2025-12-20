using ParkingSystem.FuzzySystem.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Нечеткие выходные данные от системы с методами дефаззификации
    /// </summary>
    [System.Serializable]
    public class FuzzyOutput
    {
        // Нечеткие значения для каждой выходной переменной
        public Dictionary<string, float> throttleOutput;    // Выход газа/тормоза
        public Dictionary<string, float> steeringOutput;    // Выход поворота
        public Dictionary<string, float> brakeOutput;       // Выход тормоза
        public Dictionary<string, float> phaseOutput;       // Выход фазы парковки

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public FuzzyOutput()
        {
            throttleOutput = new Dictionary<string, float>();
            steeringOutput = new Dictionary<string, float>();
            brakeOutput = new Dictionary<string, float>();
            phaseOutput = new Dictionary<string, float>();
        }

        /// <summary>
        /// Получить все выходные данные как словарь
        /// </summary>
        public Dictionary<string, Dictionary<string, float>> GetAllOutputs()
        {
            return new Dictionary<string, Dictionary<string, float>>
            {
                ["Throttle"] = throttleOutput,
                ["Steering"] = steeringOutput,
                ["Brake"] = brakeOutput,
                ["ParkingPhase"] = phaseOutput
            };
        }

        /// <summary>
        /// Основной метод дефаззификации
        /// </summary>
        public float Defuzzify(Dictionary<string, float> fuzzySet, float minValue, float maxValue,
                              DefuzzificationMethod method = DefuzzificationMethod.Centroid, int samples = 100)
        {
            if (fuzzySet == null || fuzzySet.Count == 0)
                return (minValue + maxValue) / 2f;

            switch (method)
            {
                case DefuzzificationMethod.Centroid:
                    return DefuzzifyCentroid(fuzzySet, minValue, maxValue, samples);

                case DefuzzificationMethod.Bisector:
                    return DefuzzifyBisector(fuzzySet, minValue, maxValue, samples);

                case DefuzzificationMethod.MeanOfMax:
                    return DefuzzifyMeanOfMax(fuzzySet, minValue, maxValue, samples);

                case DefuzzificationMethod.Max:
                    return DefuzzifyMax(fuzzySet, minValue, maxValue);

                default:
                    return DefuzzifyCentroid(fuzzySet, minValue, maxValue, samples);
            }
        }

        /// <summary>
        /// Метод центроида (центра тяжести)
        /// </summary>
        private float DefuzzifyCentroid(Dictionary<string, float> fuzzySet, float minValue, float maxValue, int samples)
        {
            float step = (maxValue - minValue) / samples;
            float numerator = 0f;
            float denominator = 0f;

            for (float x = minValue; x <= maxValue; x += step)
            {
                float mu = GetMembershipAtPoint(fuzzySet, x, minValue, maxValue);
                numerator += x * mu;
                denominator += mu;
            }

            return denominator > 0 ? numerator / denominator : (minValue + maxValue) / 2f;
        }

        /// <summary>
        /// Метод биссектрисы
        /// </summary>
        private float DefuzzifyBisector(Dictionary<string, float> fuzzySet, float minValue, float maxValue, int samples)
        {
            float step = (maxValue - minValue) / samples;

            // Вычисляем общую площадь
            float totalArea = 0f;
            List<float> areas = new List<float>();
            List<float> xValues = new List<float>();

            for (float x = minValue; x <= maxValue; x += step)
            {
                float mu = GetMembershipAtPoint(fuzzySet, x, minValue, maxValue);
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

            return (minValue + maxValue) / 2f;
        }

        /// <summary>
        /// Среднее максимумов
        /// </summary>
        private float DefuzzifyMeanOfMax(Dictionary<string, float> fuzzySet, float minValue, float maxValue, int samples)
        {
            float step = (maxValue - minValue) / samples;
            float maxMu = 0f;
            List<float> maxPoints = new List<float>();

            // Находим максимальное значение принадлежности
            for (float x = minValue; x <= maxValue; x += step)
            {
                float mu = GetMembershipAtPoint(fuzzySet, x, minValue, maxValue);
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
                return (minValue + maxValue) / 2f;

            // Возвращаем среднее точек с максимальной принадлежностью
            float sum = 0f;
            foreach (float point in maxPoints)
                sum += point;

            return sum / maxPoints.Count;
        }

        /// <summary>
        /// Метод максимума (первый максимум)
        /// </summary>
        private float DefuzzifyMax(Dictionary<string, float> fuzzySet, float minValue, float maxValue)
        {
            float step = (maxValue - minValue) / 100f; // Используем 100 семплов для поиска максимума
            float maxMu = 0f;
            float maxX = (minValue + maxValue) / 2f;

            for (float x = minValue; x <= maxValue; x += step)
            {
                float mu = GetMembershipAtPoint(fuzzySet, x, minValue, maxValue);
                if (mu > maxMu)
                {
                    maxMu = mu;
                    maxX = x;
                }
            }

            return maxX;
        }

        /// <summary>
        /// Получить значение принадлежности в точке x
        /// </summary>
        private float GetMembershipAtPoint(Dictionary<string, float> fuzzySet, float x, float minValue, float maxValue)
        {
            float maxMembership = 0f;

            foreach (var kvp in fuzzySet)
            {
                string term = kvp.Key;
                float activation = kvp.Value;

                if (activation <= 0f)
                    continue;

                // Преобразуем лингвистический терм в числовое значение с треугольной функцией
                float termValue = TermToValue(term, minValue, maxValue);
                float sigma = (maxValue - minValue) * 0.1f; // 10% диапазона для гауссовой функции

                // Используем гауссову функцию для сглаживания
                float membership = GaussianMembership(x, termValue, sigma);

                // Обрезаем на уровне активации
                membership = Mathf.Min(membership, activation);
                maxMembership = Mathf.Max(maxMembership, membership);
            }

            return maxMembership;
        }

        /// <summary>
        /// Гауссова функция принадлежности
        /// </summary>
        private float GaussianMembership(float x, float mean, float sigma)
        {
            if (sigma <= 0) return 0f;
            float exponent = -0.5f * Mathf.Pow((x - mean) / sigma, 2);
            return Mathf.Exp(exponent);
        }

        /// <summary>
        /// Преобразование терма в числовое значение
        /// </summary>
        private float TermToValue(string term, float min, float max)
        {
            // Словарь преобразования для стандартных термов
            Dictionary<string, float> termValues = new Dictionary<string, float>
            {
                // Для Throttle/Steering (-1 до 1)
                ["FullReverse"] = -1f,
                ["Reverse"] = -0.7f,
                ["SlowReverse"] = -0.4f,
                ["Zero"] = 0f,
                ["SlowForward"] = 0.4f,
                ["Forward"] = 0.7f,
                ["FullForward"] = 1f,

                ["HardLeft"] = -1f,
                ["Left"] = -0.6f,
                ["SlightLeft"] = -0.3f,
                ["Center"] = 0f,
                ["SlightRight"] = 0.3f,
                ["Right"] = 0.6f,
                ["HardRight"] = 1f,

                // Для Brake (0 до 1)
                ["NoBrake"] = 0f,
                ["LightBrake"] = 0.25f,
                ["MediumBrake"] = 0.5f,
                ["HardBrake"] = 0.75f,
                ["EmergencyBrake"] = 1f,

                // Для ParkingPhase (0 до 6)
                ["Searching"] = 0f,
                ["Approaching"] = 1f,
                ["Aligning"] = 2f,
                ["Reversing"] = 3f,
                ["Adjusting"] = 4f,
                ["Completed"] = 5f,
                ["Emergency"] = 6f
            };

            if (termValues.ContainsKey(term))
                return Mathf.Lerp(min, max, (termValues[term] + 1f) / 2f); // Нормализация к диапазону [min, max]

            // Если терм не найден, возвращаем середину диапазона
            return (min + max) / 2f;
        }

        /// <summary>
        /// Получить доминирующий терм (с максимальной степенью принадлежности)
        /// </summary>
        public string GetDominantTerm(Dictionary<string, float> fuzzySet)
        {
            if (fuzzySet == null || fuzzySet.Count == 0)
                return string.Empty;

            string dominantTerm = string.Empty;
            float maxMembership = 0f;

            foreach (var kvp in fuzzySet)
            {
                if (kvp.Value > maxMembership)
                {
                    maxMembership = kvp.Value;
                    dominantTerm = kvp.Key;
                }
            }

            return dominantTerm;
        }

        /// <summary>
        /// Очистить все выходные данные
        /// </summary>
        public void Clear()
        {
            throttleOutput.Clear();
            steeringOutput.Clear();
            brakeOutput.Clear();
            phaseOutput.Clear();
        }

        /// <summary>
        /// Проверка наличия данных
        /// </summary>
        public bool HasData()
        {
            return throttleOutput.Count > 0 || steeringOutput.Count > 0 ||
                   brakeOutput.Count > 0 || phaseOutput.Count > 0;
        }

        /// <summary>
        /// Строковое представление для отладки
        /// </summary>
        public override string ToString()
        {
            return $"Throttle: {GetDominantTerm(throttleOutput)} ({throttleOutput.Count} термов), " +
                   $"Steering: {GetDominantTerm(steeringOutput)} ({steeringOutput.Count} термов), " +
                   $"Brake: {GetDominantTerm(brakeOutput)} ({brakeOutput.Count} термов), " +
                   $"Phase: {GetDominantTerm(phaseOutput)} ({phaseOutput.Count} термов)";
        }
    }
}