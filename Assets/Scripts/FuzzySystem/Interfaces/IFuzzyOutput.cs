using UnityEngine;

namespace ParkingSystem.FuzzySystem.Interfaces
{
    /// <summary>
    /// Интерфейс для работы с нечеткими выходными данными
    /// </summary>
    public interface IFuzzyOutput
    {
        /// <summary>
        /// Нечеткие выходные переменные (степени принадлежности)
        /// </summary>
        FuzzyOutputVariables OutputVariables { get; }

        /// <summary>
        /// Четкие выходные значения после дефаззификации
        /// </summary>
        ParkingOutput CrispOutput { get; }

        /// <summary>
        /// Выполнить дефаззификацию нечетких выходов
        /// </summary>
        /// <param name="fuzzyOutput">Нечеткие выходные данные</param>
        /// <returns>Четкие команды управления</returns>
        ParkingOutput Defuzzify(FuzzyOutputVariables fuzzyOutput);

        /// <summary>
        /// Обновить выходные данные
        /// </summary>
        /// <param name="fuzzyOutput">Новые нечеткие выходы</param>
        /// <param name="crispOutput">Новые четкие выходы</param>
        void UpdateOutput(FuzzyOutputVariables fuzzyOutput, ParkingOutput crispOutput);

        /// <summary>
        /// Получить степень принадлежности для конкретного терма выходной переменной
        /// </summary>
        /// <param name="variableName">Имя выходной переменной</param>
        /// <param name="termName">Имя терма</param>
        /// <returns>Степень принадлежности (0-1)</returns>
        float GetMembership(string variableName, string termName);

        /// <summary>
        /// Сбросить выходные данные
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Нечеткие выходные переменные
    /// </summary>
    [System.Serializable]
    public struct FuzzyOutputVariables
    {
        // Управление скоростью
        public FuzzyVariableOutput throttle;     // Газ/тормоз (-1 до 1)

        // Управление поворотом
        public FuzzyVariableOutput steering;     // Угол поворота (-1 до 1)

        // Управление торможением
        public FuzzyVariableOutput brake;        // Тормоз (0 или 1)

        // Фаза парковки
        public FuzzyVariableOutput parkingPhase; // Рекомендуемая фаза

        /// <summary>
        /// Инициализация пустых значений
        /// </summary>
        public FuzzyOutputVariables(bool initialize = true)
        {
            if (initialize)
            {
                throttle = new FuzzyVariableOutput("Throttle", -1f, 1f);
                steering = new FuzzyVariableOutput("Steering", -1f, 1f);
                brake = new FuzzyVariableOutput("Brake", 0f, 1f);
                parkingPhase = new FuzzyVariableOutput("ParkingPhase", 0f, 6f); // 6 фаз
            }
            else
            {
                throttle = default;
                steering = default;
                brake = default;
                parkingPhase = default;
            }
        }

        /// <summary>
        /// Проверка, есть ли данные
        /// </summary>
        public bool HasData()
        {
            return throttle.memberships.Count > 0 ||
                   steering.memberships.Count > 0 ||
                   brake.memberships.Count > 0;
        }

        /// <summary>
        /// Получить выходную переменную по имени
        /// </summary>
        public FuzzyVariableOutput GetVariable(string variableName)
        {
            switch (variableName.ToLower())
            {
                case "throttle":
                    return throttle;

                case "steering":
                    return steering;

                case "brake":
                    return brake;

                case "parkingphase":
                case "parking_phase":
                    return parkingPhase;

                default:
                    Debug.LogWarning($"Неизвестная выходная переменная: {variableName}");
                    return new FuzzyVariableOutput();
            }
        }
    }

    /// <summary>
    /// Нечеткая выходная переменная
    /// </summary>
    [System.Serializable]
    public struct FuzzyVariableOutput
    {
        public string name;              // Имя переменной
        public float minValue;           // Минимальное значение
        public float maxValue;           // Максимальное значение
        public MembershipDictionary memberships; // Степени принадлежности к термам

        public FuzzyVariableOutput(string name, float min, float max)
        {
            this.name = name;
            this.minValue = min;
            this.maxValue = max;
            this.memberships = new MembershipDictionary();
        }

        /// <summary>
        /// Добавить степень принадлежности для терма
        /// </summary>
        public void AddMembership(string term, float value)
        {
            memberships[term] = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Получить степень принадлежности для терма
        /// </summary>
        public float GetMembership(string term)
        {
            return memberships.ContainsKey(term) ? memberships[term] : 0f;
        }

        /// <summary>
        /// Очистить все степени принадлежности
        /// </summary>
        public void Clear()
        {
            memberships.Clear();
        }

        /// <summary>
        /// Получить максимальную степень принадлежности
        /// </summary>
        public float GetMaxMembership()
        {
            float max = 0f;
            foreach (var membership in memberships.Values)
            {
                if (membership > max)
                    max = membership;
            }
            return max;
        }

        /// <summary>
        /// Получить терм с максимальной степенью принадлежности
        /// </summary>
        public string GetDominantTerm()
        {
            string dominant = "";
            float max = 0f;

            foreach (var kvp in memberships)
            {
                if (kvp.Value > max)
                {
                    max = kvp.Value;
                    dominant = kvp.Key;
                }
            }

            return dominant;
        }
    }

    /// <summary>
    /// Словарь для хранения степеней принадлежности
    /// </summary>
    [System.Serializable]
    public class MembershipDictionary : System.Collections.Generic.Dictionary<string, float>
    {
        // Можно добавить дополнительные методы при необходимости
    }
}