using ParkingSystem.FuzzySystem;
using ParkingSystem.FuzzySystem.Inputs;

namespace ParkingSystem.FuzzySystem.Interfaces
{
    /// <summary>
    /// Четкие выходные команды управления
    /// </summary>
    [System.Serializable]
    public struct ParkingOutput
    {
        [UnityEngine.Range(-1f, 1f)]
        public float throttle;           // -1..1 (полный назад..полный вперед)

        [UnityEngine.Range(-1f, 1f)]
        public float steering;           // -1..1 (полный левый..полный правый)

        public bool brake;               // Тормоз
        public bool emergencyStop;       // Экстренная остановка
        public bool toggleForward;       // Переключить направление
        public ParkingPhase suggestedPhase; // Предложенная фаза парковки

        public static ParkingOutput Zero => new ParkingOutput
        {
            throttle = 0f,
            steering = 0f,
            brake = false,
            emergencyStop = false,
            toggleForward = false
        };

        public override string ToString()
        {
            return $"Throttle: {throttle:F2}, Steering: {steering:F2}, " +
                   $"Brake: {brake}, Phase: {suggestedPhase}";
        }
    }
}