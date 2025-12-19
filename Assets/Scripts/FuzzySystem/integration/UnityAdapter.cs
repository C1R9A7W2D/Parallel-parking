using UnityEngine;
using ParkingSystem.FuzzySystem.Inputs;
using System.Collections.Generic;

namespace ParkingSystem.Integration
{
    public static class UnityAdapter
    {
        private static Camera mainCamera;
        private static bool cameraCached = false;

        public static Camera GetMainCamera()
        {
            if (!cameraCached || mainCamera == null)
            {
                mainCamera = Camera.main;
                cameraCached = true;
            }
            return mainCamera;
        }

        /// <summary>
        /// 2D Raycast для обнаружения препятствий
        /// </summary>
        public static RaycastHit2D? RaycastForObstacles(Vector2 origin, Vector2 direction, float maxDistance, LayerMask layerMask)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, layerMask);
            return hit.collider != null ? (RaycastHit2D?)hit : null;
        }

        /// <summary>
        /// SphereCast для 2D (используем CircleCast)
        /// </summary>
        public static RaycastHit2D? SphereCastForObstacles(Vector2 origin, float radius, Vector2 direction, float maxDistance)
        {
            RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, maxDistance);
            return hit.collider != null ? (RaycastHit2D?)hit : null;
        }

        /// <summary>
        /// Найти парковочные места в радиусе (2D)
        /// </summary>
        public static ParkingSpot[] FindParkingSpotsInRadius(Vector2 center, float radius)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
            List<ParkingSpot> spots = new List<ParkingSpot>();

            foreach (Collider2D collider in colliders)
            {
                if (collider.CompareTag("Parking spot"))
                {
                    spots.Add(new ParkingSpot
                    {
                        position = collider.transform.position,
                        width = GetColliderWidth2D(collider),
                        isAvailable = IsSpotAvailable2D(collider.transform.position),
                        angle = collider.transform.eulerAngles.z // Вращение по Z
                    });
                }
            }

            return spots.ToArray();
        }

        private static float GetColliderWidth2D(Collider2D collider)
        {
            if (collider is BoxCollider2D box)
                return box.size.x * collider.transform.lossyScale.x;
            if (collider is CircleCollider2D circle)
                return circle.radius * 2 * collider.transform.lossyScale.x;
            return 2.5f;
        }

        private static bool IsSpotAvailable2D(Vector2 position)
        {
            float radius = 2.5f;
            Collider2D[] obstacles = Physics2D.OverlapCircleAll(position, radius);

            foreach (Collider2D col in obstacles)
            {
                if (col.CompareTag("Car") || col.CompareTag("Obstacle"))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Нарисовать луч для отладки (работает и в 2D)
        /// </summary>
        public static void DrawDebugRay(Vector3 start, Vector3 direction, Color color, float duration = 0.1f)
        {
            Debug.DrawRay(start, direction, color, duration);
        }

        /// <summary>
        /// Нарисовать луч с попаданием (2D)
        /// </summary>
        public static void DrawDebugRayWithHit(Vector2 start, Vector2 direction, float distance, Color color, float duration = 0.1f)
        {
            Debug.DrawRay(start, direction * distance, color, duration);

            Vector2 hitPoint = start + direction.normalized * distance;
            Debug.DrawLine(hitPoint - Vector2.up * 0.1f, hitPoint + Vector2.up * 0.1f, color, duration);
            Debug.DrawLine(hitPoint - Vector2.right * 0.1f, hitPoint + Vector2.right * 0.1f, color, duration);
        }
    }
}