using System.Collections.Generic;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Нечеткие выходные данные от системы
    /// </summary>
    public class FuzzyOutput
    {
        // Нечеткие значения для каждой выходной переменной
        public Dictionary<string, float> throttleOutput;    // Выход газа/тормоза
        public Dictionary<string, float> steeringOutput;    // Выход поворота
        public Dictionary<string, float> brakeOutput;       // Выход тормоза
        public Dictionary<string, float> phaseOutput;       // Выход фазы парковки

        // Методы агрегации и дефаззификации
        public float Defuzzify(Dictionary<string, float> fuzzySet, float minValue, float maxValue)
        {
            // Простейшая дефаззификация - средневзвешенное
            float sum = 0f;
            float weightSum = 0f;

            foreach (var kvp in fuzzySet)
            {
                // Преобразуем лингвистический терм в числовое значение
                float numericValue = TermToValue(kvp.Key, minValue, maxValue);
                sum += numericValue * kvp.Value;
                weightSum += kvp.Value;
            }

            return weightSum > 0 ? sum / weightSum : 0f;
        }

        private float TermToValue(string term, float min, float max)
        {
            // Простое преобразование термов в значения
            switch (term.ToLower())
            {
                case "verylow": return min;
                case "low": return min + (max - min) * 0.25f;
                case "medium": return min + (max - min) * 0.5f;
                case "high": return min + (max - min) * 0.75f;
                case "veryhigh": return max;
                default: return (min + max) / 2f;
            }
        }
    }
}