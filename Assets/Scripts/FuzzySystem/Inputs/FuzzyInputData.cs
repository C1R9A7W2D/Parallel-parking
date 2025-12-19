using UnityEngine;
using System.Collections.Generic;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Структура для хранения фаззифицированных входных данных системы
    /// Все лингвистические переменные хранятся как словари [терм -> степень принадлежности]
    /// </summary>
    [System.Serializable]
    public class FuzzyInputData
    {
        // ========== ДИСТАНЦИОННЫЕ ПЕРЕМЕННЫЕ ==========
        public Dictionary<string, float> frontDistance;      // Расстояние вперед
        public Dictionary<string, float> rearDistance;       // Расстояние назад
        public Dictionary<string, float> leftSideDistance;   // Расстояние слева
        public Dictionary<string, float> rightSideDistance;  // Расстояние справа
        public Dictionary<string, float> parkingSpotWidth;   // Ширина парковочного места

        // ========== УГЛОВЫЕ ПЕРЕМЕННЫЕ ==========
        public Dictionary<string, float> alignmentAngle;     // Угол выравнивания автомобиля
        public Dictionary<string, float> approachAngle;      // Угол подхода к месту

        // ========== ПОЗИЦИОННЫЕ ПЕРЕМЕННЫЕ ==========
        public Dictionary<string, float> lateralError;       // Боковая ошибка позиционирования
        public Dictionary<string, float> longitudinalError;  // Продольная ошибка позиционирования

        // ========== СКОРОСТНЫЕ ПЕРЕМЕННЫЕ ==========
        public Dictionary<string, float> currentSpeed;       // Текущая скорость автомобиля
        public Dictionary<string, float> speedError;         // Ошибка скорости (целевая - текущая)

        // ========== МЕТАДАННЫЕ ==========
        public float timestamp;      // Время создания данных
        public int frameCount;       // Номер кадра

        /// <summary>
        /// Конструктор - инициализирует все словари
        /// </summary>
        public FuzzyInputData()
        {
            frontDistance = new Dictionary<string, float>();
            rearDistance = new Dictionary<string, float>();
            leftSideDistance = new Dictionary<string, float>();
            rightSideDistance = new Dictionary<string, float>();
            parkingSpotWidth = new Dictionary<string, float>();

            alignmentAngle = new Dictionary<string, float>();
            approachAngle = new Dictionary<string, float>();

            lateralError = new Dictionary<string, float>();
            longitudinalError = new Dictionary<string, float>();

            currentSpeed = new Dictionary<string, float>();
            speedError = new Dictionary<string, float>();

            timestamp = Time.time;
            frameCount = Time.frameCount;
        }

        /// <summary>
        /// Получить фаззифицированное значение переменной по имени
        /// </summary>
        public Dictionary<string, float> GetVariable(string variableName)
        {
            switch (variableName.ToLower().Replace("_", ""))
            {
                case "frontdistance":
                    return frontDistance;
                case "reardistance":
                    return rearDistance;
                case "leftsidedistance":
                    return leftSideDistance;
                case "rightsidedistance":
                    return rightSideDistance;
                case "parkingspotwidth":
                    return parkingSpotWidth;
                case "alignmentangle":
                    return alignmentAngle;
                case "approachangle":
                    return approachAngle;
                case "lateralerror":
                    return lateralError;
                case "longitudinalerror":
                    return longitudinalError;
                case "currentspeed":
                    return currentSpeed;
                case "speederror":
                    return speedError;
                default:
                    Debug.LogWarning($"FuzzyInputData.GetVariable: Неизвестная переменная '{variableName}'");
                    return new Dictionary<string, float>();
            }
        }

        /// <summary>
        /// Проверка наличия данных в структуре
        /// </summary>
        public bool HasData()
        {
            // Проверяем хотя бы одну переменную на наличие данных
            return (frontDistance.Count > 0) || (rearDistance.Count > 0) ||
                   (alignmentAngle.Count > 0) || (currentSpeed.Count > 0) ||
                   (lateralError.Count > 0) || (speedError.Count > 0);
        }

        /// <summary>
        /// Проверка наличия данных в конкретной переменной
        /// </summary>
        public bool HasVariableData(string variableName)
        {
            var variable = GetVariable(variableName);
            return variable != null && variable.Count > 0;
        }

        /// <summary>
        /// Получить доминирующий терм для переменной (с максимальной степенью принадлежности)
        /// </summary>
        public string GetDominantTerm(string variableName)
        {
            var variable = GetVariable(variableName);
            if (variable == null || variable.Count == 0)
                return string.Empty;

            string dominantTerm = string.Empty;
            float maxMembership = 0f;

            foreach (var kvp in variable)
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
        /// Получить максимальную степень принадлежности для переменной
        /// </summary>
        public float GetMaxMembership(string variableName)
        {
            var variable = GetVariable(variableName);
            if (variable == null || variable.Count == 0)
                return 0f;

            float max = 0f;
            foreach (var kvp in variable)
            {
                if (kvp.Value > max)
                    max = kvp.Value;
            }
            return max;
        }

        /// <summary>
        /// Получить среднюю степень принадлежности для переменной
        /// </summary>
        public float GetAverageMembership(string variableName)
        {
            var variable = GetVariable(variableName);
            if (variable == null || variable.Count == 0)
                return 0f;

            float sum = 0f;
            foreach (var kvp in variable)
            {
                sum += kvp.Value;
            }
            return sum / variable.Count;
        }

        /// <summary>
        /// Проверка, активирован ли конкретный терм переменной (степень > порога)
        /// </summary>
        public bool IsTermActive(string variableName, string termName, float threshold = 0.1f)
        {
            var variable = GetVariable(variableName);
            if (variable == null || !variable.ContainsKey(termName))
                return false;

            return variable[termName] >= threshold;
        }

        /// <summary>
        /// Очистить все данные
        /// </summary>
        public void Clear()
        {
            frontDistance.Clear();
            rearDistance.Clear();
            leftSideDistance.Clear();
            rightSideDistance.Clear();
            parkingSpotWidth.Clear();
            alignmentAngle.Clear();
            approachAngle.Clear();
            lateralError.Clear();
            longitudinalError.Clear();
            currentSpeed.Clear();
            speedError.Clear();
        }

        /// <summary>
        /// Создать копию данных
        /// </summary>
        public FuzzyInputData Clone()
        {
            FuzzyInputData clone = new FuzzyInputData();

            clone.frontDistance = new Dictionary<string, float>(this.frontDistance);
            clone.rearDistance = new Dictionary<string, float>(this.rearDistance);
            clone.leftSideDistance = new Dictionary<string, float>(this.leftSideDistance);
            clone.rightSideDistance = new Dictionary<string, float>(this.rightSideDistance);
            clone.parkingSpotWidth = new Dictionary<string, float>(this.parkingSpotWidth);

            clone.alignmentAngle = new Dictionary<string, float>(this.alignmentAngle);
            clone.approachAngle = new Dictionary<string, float>(this.approachAngle);

            clone.lateralError = new Dictionary<string, float>(this.lateralError);
            clone.longitudinalError = new Dictionary<string, float>(this.longitudinalError);

            clone.currentSpeed = new Dictionary<string, float>(this.currentSpeed);
            clone.speedError = new Dictionary<string, float>(this.speedError);

            clone.timestamp = this.timestamp;
            clone.frameCount = this.frameCount;

            return clone;
        }

        /// <summary>
        /// Строковое представление для отладки
        /// </summary>
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== FUZZY INPUT DATA ===");

            AddVariableToString(sb, "FrontDistance", frontDistance);
            AddVariableToString(sb, "RearDistance", rearDistance);
            AddVariableToString(sb, "AlignmentAngle", alignmentAngle);
            AddVariableToString(sb, "CurrentSpeed", currentSpeed);
            AddVariableToString(sb, "SpeedError", speedError);

            sb.AppendLine($"Timestamp: {timestamp:F2}s, Frame: {frameCount}");

            return sb.ToString();
        }

        /// <summary>
        /// Вспомогательный метод для форматирования переменной
        /// </summary>
        private void AddVariableToString(System.Text.StringBuilder sb, string name, Dictionary<string, float> variable)
        {
            if (variable != null && variable.Count > 0)
            {
                sb.Append($"{name}: ");
                foreach (var kvp in variable)
                {
                    if (kvp.Value > 0.05f) // Показываем только значимые значения
                    {
                        sb.Append($"{kvp.Key}({kvp.Value:F2}) ");
                    }
                }
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Проверить валидность данных (базовая проверка)
        /// </summary>
        public bool IsValid()
        {
            // Проверяем, что все обязательные словари инициализированы
            return frontDistance != null && rearDistance != null &&
                   alignmentAngle != null && currentSpeed != null &&
                   timestamp > 0;
        }

        /// <summary>
        /// Получить все переменные, у которых есть данные
        /// </summary>
        public List<string> GetPopulatedVariables()
        {
            List<string> populated = new List<string>();

            if (frontDistance.Count > 0) populated.Add("FrontDistance");
            if (rearDistance.Count > 0) populated.Add("RearDistance");
            if (leftSideDistance.Count > 0) populated.Add("LeftSideDistance");
            if (rightSideDistance.Count > 0) populated.Add("RightSideDistance");
            if (parkingSpotWidth.Count > 0) populated.Add("ParkingSpotWidth");
            if (alignmentAngle.Count > 0) populated.Add("AlignmentAngle");
            if (approachAngle.Count > 0) populated.Add("ApproachAngle");
            if (lateralError.Count > 0) populated.Add("LateralError");
            if (longitudinalError.Count > 0) populated.Add("LongitudinalError");
            if (currentSpeed.Count > 0) populated.Add("CurrentSpeed");
            if (speedError.Count > 0) populated.Add("SpeedError");

            return populated;
        }
    }
}