using UnityEngine;
using System.Collections.Generic;

namespace ParkingSystem.FuzzySystem
{
    /// <summary>
    /// ��������� ���������� � AI ��� �������� (2D ������)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CarAI : MonoBehaviour
    {
        [Header("�������� ���������")]
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float maxSpeed = 3f;
        [SerializeField] private float sensorRange = 3f;
        [SerializeField] private bool movesForward = true;

        [Header("���������� ���������")]
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private LayerMask obstacleMask = -1;

        // ����������
        private Rigidbody2D rb;
        private List<Transform> visibleTargets = new List<Transform>();

        // === ��������� �������� ��� ����� ������� ===
        public Vector2 Position => transform.position; // 2D �������
        public float Rotation => transform.eulerAngles.z; // �������� �� Z � 2D
        public float CurrentSpeed => currentSpeed;
        public float MaxSpeed => maxSpeed;
        public bool IsMovingForward => movesForward;
        public float SensorRange => sensorRange;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            SetupRigidbody2D();

            if (obstacleMask.value == -1)
            {
                obstacleMask = LayerMask.GetMask("Car", "Obstacle", "Wall");
            }
        }

        void FixedUpdate()
        {
            UpdateMovement();
            DetectTargets();
        }

        private void SetupRigidbody2D()
        {
            if (rb == null) return;

            rb.mass = 1000f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 5f;
            rb.gravityScale = 0f; // ��� ����������
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void UpdateMovement()
        {
            if (rb == null) return;

            Vector2 moveDirection = movesForward ? transform.up : -transform.up;
            rb.linearVelocity = moveDirection * currentSpeed;

            if (rb.linearVelocity.magnitude > maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
            }
        }

        private void DetectTargets()
        {
            visibleTargets.Clear();

            Collider2D[] targets = Physics2D.OverlapCircleAll(
                transform.position,
                sensorRange,
                obstacleMask
            );

            foreach (Collider2D target in targets)
            {
                if (target.gameObject != gameObject && !target.isTrigger)
                {
                    visibleTargets.Add(target.transform);
                }
            }
        }

        // === ��������� ������ ��� ����� ������� ===

        public void ChangeSpeed(float delta)
        {
            currentSpeed = Mathf.Clamp(currentSpeed + delta, -maxSpeed, maxSpeed);

            if (Mathf.Sign(currentSpeed) != (movesForward ? 1 : -1))
            {
                movesForward = currentSpeed >= 0;
            }
        }

        public void Rotate(float degrees)
        {
            float speedFactor = 1f - Mathf.Abs(currentSpeed) / maxSpeed * 0.5f;
            float maxRotation = rotationSpeed * Time.fixedDeltaTime * speedFactor;

            degrees = Mathf.Clamp(degrees, -maxRotation, maxRotation);
            transform.Rotate(0, 0, degrees);
        }

        public void SetSpeedImmediate(float newSpeed)
        {
            currentSpeed = Mathf.Clamp(newSpeed, -maxSpeed, maxSpeed);
            movesForward = currentSpeed >= 0;
        }

        public void StopImmediate()
        {
            currentSpeed = 0f;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        public LayerMask GetObstacleMask()
        {
            return obstacleMask;
        }

        public void ToggleMovementDirection()
        {
            movesForward = !movesForward;
            currentSpeed = -currentSpeed;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sensorRange);

            Gizmos.color = movesForward ? Color.green : Color.red;
            Vector3 direction = movesForward ? transform.up : -transform.up;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}