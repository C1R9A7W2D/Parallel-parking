using ParkingSystem.FuzzySystem;
using ParkingSystem.FuzzySystem.Inputs;
using UnityEngine;

namespace ParkingSystem.FuzzySystem.Interfaces
{
    /// <summary>
    /// Четкие выходные команды управления для 2D автомобиля
    /// </summary>
    [System.Serializable]
    public struct ParkingOutput
    {
        [Header("Управление движением")]
        [Range(-1f, 1f)]
        [Tooltip("Газ/тормоз: -1 = полный назад, 0 = стоп, 1 = полный вперед")]
        public float throttle;

        [Range(-1f, 1f)]
        [Tooltip("Руль: -1 = полный левый, 0 = прямо, 1 = полный правый")]
        public float steering;

        [Tooltip("Тормоз: true = тормозить, false = не тормозить")]
        public bool brake;

        [Header("Управление парковкой")]
        [Tooltip("Аварийная остановка")]
        public bool emergencyStop;

        [Tooltip("Переключить направление движения (вперед/назад)")]
        public bool toggleForward;

        [Tooltip("Предложенная фаза парковки")]
        public ParkingPhase suggestedPhase;

        [Header("Статус системы")]
        [Tooltip("Сообщение для отладки")]
        public string debugMessage;

        [Tooltip("Время генерации команды")]
        public float timestamp;

        /// <summary>
        /// Нулевая команда (остановка)
        /// </summary>
        public static ParkingOutput Zero => new ParkingOutput
        {
            throttle = 0f,
            steering = 0f,
            brake = false,
            emergencyStop = false,
            toggleForward = false,
            suggestedPhase = ParkingPhase.Searching,
            debugMessage = "Zero command",
            timestamp = Time.time
        };

        /// <summary>
        /// Команда для движения вперед
        /// </summary>
        public static ParkingOutput Forward(float speed = 0.5f) => new ParkingOutput
        {
            throttle = Mathf.Clamp(speed, 0f, 1f),
            steering = 0f,
            brake = false,
            emergencyStop = false,
            toggleForward = false,
            suggestedPhase = ParkingPhase.Searching,
            debugMessage = "Forward command",
            timestamp = Time.time
        };

        /// <summary>
        /// Команда для движения назад
        /// </summary>
        public static ParkingOutput Reverse(float speed = 0.3f) => new ParkingOutput
        {
            throttle = Mathf.Clamp(-speed, -1f, 0f),
            steering = 0f,
            brake = false,
            emergencyStop = false,
            toggleForward = false,
            suggestedPhase = ParkingPhase.Reversing,
            debugMessage = "Reverse command",
            timestamp = Time.time
        };

        /// <summary>
        /// Команда для поворота
        /// </summary>
        public static ParkingOutput Turn(float turnAmount, float throttleAmount = 0f) => new ParkingOutput
        {
            throttle = Mathf.Clamp(throttleAmount, -1f, 1f),
            steering = Mathf.Clamp(turnAmount, -1f, 1f),
            brake = false,
            emergencyStop = false,
            toggleForward = false,
            suggestedPhase = ParkingPhase.Searching,
            debugMessage = $"Turn command: {turnAmount:F2}",
            timestamp = Time.time
        };

        /// <summary>
        /// Команда для остановки
        /// </summary>
        public static ParkingOutput Stop() => new ParkingOutput
        {
            throttle = 0f,
            steering = 0f,
            brake = true,
            emergencyStop = false,
            toggleForward = false,
            suggestedPhase = ParkingPhase.Completed,
            debugMessage = "Stop command",
            timestamp = Time.time
        };

        /// <summary>
        /// Команда для аварийной остановки
        /// </summary>
        public static ParkingOutput EmergencyStop(string reason = "") => new ParkingOutput
        {
            throttle = 0f,
            steering = 0f,
            brake = true,
            emergencyStop = true,
            toggleForward = false,
            suggestedPhase = ParkingPhase.Emergency,
            debugMessage = $"EMERGENCY STOP: {reason}",
            timestamp = Time.time
        };

        /// <summary>
        /// Проверка, является ли команда нулевой
        /// </summary>
        public bool IsZero => Mathf.Approximately(throttle, 0f) &&
                             Mathf.Approximately(steering, 0f) &&
                             !brake && !emergencyStop;

        /// <summary>
        /// Проверка, является ли команда аварийной
        /// </summary>
        public bool IsEmergency => emergencyStop;

        /// <summary>
        /// Получить абсолютную скорость (без учета направления)
        /// </summary>
        public float GetAbsoluteSpeed() => Mathf.Abs(throttle);

        /// <summary>
        /// Получить направление движения (1 = вперед, -1 = назад, 0 = стоп)
        /// </summary>
        public int GetDirection()
        {
            if (Mathf.Approximately(throttle, 0f)) return 0;
            return throttle > 0f ? 1 : -1;
        }

        /// <summary>
        /// Объединение двух команд (используется для смешивания)
        /// </summary>
        public static ParkingOutput Lerp(ParkingOutput a, ParkingOutput b, float t)
        {
            t = Mathf.Clamp01(t);

            return new ParkingOutput
            {
                throttle = Mathf.Lerp(a.throttle, b.throttle, t),
                steering = Mathf.Lerp(a.steering, b.steering, t),
                brake = t > 0.5f ? b.brake : a.brake, // Пороговое значение
                emergencyStop = a.emergencyStop || b.emergencyStop, // Аварийная остановка приоритетна
                toggleForward = t > 0.5f ? b.toggleForward : a.toggleForward,
                suggestedPhase = t > 0.5f ? b.suggestedPhase : a.suggestedPhase,
                debugMessage = $"Lerp: {a.debugMessage} → {b.debugMessage}",
                timestamp = Time.time
            };
        }

        /// <summary>
        /// Строковое представление для отладки
        /// </summary>
        public override string ToString()
        {
            string direction = GetDirection() switch
            {
                1 => "ВПЕРЕД",
                -1 => "НАЗАД",
                _ => "СТОП"
            };

            string steeringDir = steering switch
            {
                < -0.3f => "ЛЕВО",
                > 0.3f => "ПРАВО",
                _ => "ПРЯМО"
            };

            return $"[{suggestedPhase}] {direction} throttle={throttle:F2}, {steeringDir} steering={steering:F2}, " +
                   $"brake={brake}, emergency={emergencyStop}, {debugMessage}";
        }
    }
}