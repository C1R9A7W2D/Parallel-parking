using ParkingSystem.FuzzySystem;
using ParkingSystem.FuzzySystem.Interfaces;
using UnityEngine;

namespace ParkingSystem.Integration
{
    public class VehicleCommandInterface
    {
        private CarAI carAI;
        private float currentThrottle = 0f;
        private float currentSteering = 0f;

        public VehicleCommandInterface(CarAI carAI)
        {
            this.carAI = carAI;
        }

        public void ApplyCommand(ParkingOutput command)
        {
            if (carAI == null) return;

            if (command.emergencyStop)
            {
                ApplyEmergencyBrake();
                return;
            }

            ApplyThrottle(command.throttle);
            ApplySteering(command.steering);

            if (command.brake) ApplyBrake();
            if (command.toggleForward) carAI.ToggleMovementDirection();
        }

        private void ApplyThrottle(float targetThrottle)
        {
            targetThrottle = Mathf.Clamp(targetThrottle, -1f, 1f);

            float maxDelta = 2f * Time.deltaTime;
            currentThrottle = Mathf.MoveTowards(currentThrottle, targetThrottle, maxDelta);

            float targetSpeed = currentThrottle * carAI.MaxSpeed;
            float speedDelta = targetSpeed - carAI.CurrentSpeed;

            if (Mathf.Abs(speedDelta) > 0.01f)
            {
                carAI.ChangeSpeed(speedDelta * Time.deltaTime);
            }
        }

        private void ApplySteering(float targetSteering)
        {
            targetSteering = Mathf.Clamp(targetSteering, -1f, 1f);

            float maxDelta = 180f * Time.deltaTime; // Градусов в секунду
            currentSteering = Mathf.MoveTowards(currentSteering, targetSteering, maxDelta);

            // Преобразование в угол поворота (-1..1 → -45..45 градусов)
            float steeringAngle = currentSteering * 45f;

            if (Mathf.Abs(steeringAngle) > 0.1f)
            {
                carAI.Rotate(steeringAngle * Time.deltaTime);
            }
        }

        private void ApplyBrake()
        {
            float brakeForce = 5f;
            float brakeDelta = -carAI.CurrentSpeed * brakeForce * Time.deltaTime;
            carAI.ChangeSpeed(brakeDelta);
            currentThrottle = 0f;
        }

        private void ApplyEmergencyBrake()
        {
            carAI.StopImmediate();
            currentThrottle = 0f;
            currentSteering = 0f;
        }
    }
}