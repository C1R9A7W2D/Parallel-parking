using UnityEngine;
using System.Collections.Generic;
using ParkingSystem.FuzzySystem.Inputs;

namespace ParkingSystem.FuzzySystem.Inputs
{
    public class SensorDataCollector
    {
        private CarAI carAI;
        private Transform carTransform;
        private List<Vector2> obstaclePositions = new List<Vector2>();
        private List<GameObject> dynamicObstacles = new List<GameObject>();
        private GameObject[] parkingSpotObjects;
        private LayerMask obstacleMask;

        public SensorDataCollector(CarAI carAI, CarSpawner carSpawner = null)
        {
            this.carAI = carAI;
            this.carTransform = carAI.transform;
            this.obstacleMask = carAI.GetObstacleMask();
            InitializeEnvironment();
        }

        private void InitializeEnvironment()
        {
            FindStaticObstacles();
            FindParkingSpots();
            FindDynamicObstacles();
        }

        private void FindStaticObstacles()
        {
            obstaclePositions.Clear();
            GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            foreach (GameObject obstacle in obstacles)
            {
                obstaclePositions.Add(obstacle.transform.position);
            }
        }

        private void FindParkingSpots()
        {
            parkingSpotObjects = GameObject.FindGameObjectsWithTag("Parking spot");
        }

        private void FindDynamicObstacles()
        {
            dynamicObstacles.Clear();
            CarAI[] allCars = Object.FindObjectsOfType<CarAI>();
            foreach (CarAI car in allCars)
            {
                if (car.gameObject != carAI.gameObject)
                {
                    dynamicObstacles.Add(car.gameObject);
                }
            }
        }

        public ParkingInput CollectSensorData()
        {
            ParkingInput input = new ParkingInput();

            try
            {
                // Данные автомобиля в 2D
                input.carPosition = carAI.Position;
                input.carRotation = carAI.Rotation; // Угол по Z
                input.carSpeed = carAI.CurrentSpeed;
                input.carMaxSpeed = carAI.MaxSpeed;
                input.movesForward = carAI.IsMovingForward;
                input.sensorRange = carAI.SensorRange;

                // Данные датчиков (8 лучей в 2D)
                input.sensorDistances = CastSensorRays2D();
                Debug.Log($"Датчики (сырые): [0]={input.sensorDistances[0]:F2}, [2]={input.sensorDistances[2]:F2}, [4]={input.sensorDistances[4]:F2}, [6]={input.sensorDistances[6]:F2}");

                // Парковочные места
                input.availableSpots = GetAvailableParkingSpots2D();

                // Препятствия
                input.nearbyObstacles = GetNearbyObstacles2D();

                // Метаданные
                input.timestamp = Time.time;
                input.frameCount = Time.frameCount;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Ошибка сбора данных 2D: {ex.Message}");
            }

            return input;
        }

        private float[] CastSensorRays2D()
        {
            float sensorRange = carAI.SensorRange;
            float[] distances = new float[8];
            Vector2 carPos = carTransform.position;

            // В 2D направление вперед - transform.up
            Vector2 forward = carTransform.up;
            Vector2 right = carTransform.right;

            Vector2[] directions = {
                forward,                          // Вперед (0°)
                (forward + right).normalized,     // Вперед-вправо (45°)
                right,                            // Вправо (90°)
                (-forward + right).normalized,    // Назад-вправо (135°)
                -forward,                         // Назад (180°)
                (-forward - right).normalized,    // Назад-влево (225°)
                -right,                           // Влево (270°)
                (forward - right).normalized      // Вперед-влево (315°)
            };

            for (int i = 0; i < 8; i++)
            {
                // 3. Используйте obstacleMask в Physics2D.Raycast:
                RaycastHit2D hit = Physics2D.Raycast(carPos, directions[i], sensorRange, obstacleMask);

                if (hit.collider != null)
                {
                    distances[i] = hit.distance;
                    Debug.DrawRay(carPos, directions[i] * hit.distance,
                        GetRayColor(hit.distance, sensorRange), 0.1f);
                }
                else
                {
                    distances[i] = sensorRange;
                    Debug.DrawRay(carPos, directions[i] * sensorRange, Color.green, 0.1f);
                }
            }
            return distances;
        }

        private ParkingSpot[] GetAvailableParkingSpots2D()
        {
            List<ParkingSpot> availableSpots = new List<ParkingSpot>();

            if (parkingSpotObjects == null) return availableSpots.ToArray();

            Vector2 carPos = carTransform.position;

            foreach (GameObject spotObj in parkingSpotObjects)
            {
                if (spotObj == null) continue;

                Vector3 spotPos = spotObj.transform.position;

                if (IsSpotAvailable2D(spotPos))
                {
                    ParkingSpot spot = new ParkingSpot
                    {
                        position = spotPos,
                        width = CalculateSpotWidth2D(spotObj),
                        length = CalculateSpotLength2D(spotObj),
                        angle = spotObj.transform.eulerAngles.z, // Угол по Z в 2D
                        isAvailable = true,
                        distanceToCar = Vector2.Distance(carPos, spotPos)
                    };

                    availableSpots.Add(spot);
                }
            }

            return availableSpots.ToArray();
        }

        private bool IsSpotAvailable2D(Vector3 spotPosition)
        {
            float checkRadius = 2.5f;

            // Проверяем статические препятствия
            foreach (Vector2 obstaclePos in obstaclePositions)
            {
                if (Vector2.Distance(spotPosition, obstaclePos) < checkRadius)
                    return false;
            }

            // Проверяем динамические препятствия
            foreach (GameObject obstacle in dynamicObstacles)
            {
                if (obstacle != null &&
                    Vector2.Distance(spotPosition, obstacle.transform.position) < checkRadius)
                    return false;
            }

            // Проверяем самого себя
            if (Vector2.Distance(spotPosition, carAI.Position) < checkRadius)
                return false;

            return true;
        }

        private float CalculateSpotWidth2D(GameObject spotObject)
        {
            BoxCollider2D boxCollider = spotObject.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
                return boxCollider.size.x * spotObject.transform.lossyScale.x;

            SpriteRenderer spriteRenderer = spotObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                return spriteRenderer.bounds.size.x;

            return 2.5f;
        }

        private float CalculateSpotLength2D(GameObject spotObject)
        {
            BoxCollider2D boxCollider = spotObject.GetComponent<BoxCollider2D>();
            if (boxCollider != null)
                return boxCollider.size.y * spotObject.transform.lossyScale.y;

            SpriteRenderer spriteRenderer = spotObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                return spriteRenderer.bounds.size.y;

            return 5f;
        }

        private Obstacle[] GetNearbyObstacles2D()
        {
            List<Obstacle> obstacles = new List<Obstacle>();
            Vector2 carPos = carAI.Position;
            float checkRadius = 10f;

            // Статические препятствия
            foreach (Vector2 obstaclePos in obstaclePositions)
            {
                float distance = Vector2.Distance(carPos, obstaclePos);
                if (distance <= checkRadius)
                {
                    obstacles.Add(new Obstacle
                    {
                        position = obstaclePos,
                        distance = distance,
                        type = ObstacleType.Static,
                        size = 1.0f
                    });
                }
            }

            // Динамические препятствия
            foreach (GameObject obstacle in dynamicObstacles)
            {
                if (obstacle == null) continue;

                float distance = Vector2.Distance(carPos, obstacle.transform.position);
                if (distance <= checkRadius)
                {
                    obstacles.Add(new Obstacle
                    {
                        position = obstacle.transform.position,
                        distance = distance,
                        type = ObstacleType.Dynamic,
                        size = 1.5f
                    });
                }
            }

            return obstacles.ToArray();
        }

        private Color GetRayColor(float distance, float maxRange)
        {
            float ratio = distance / maxRange;
            if (ratio < 0.3f) return Color.red;
            if (ratio < 0.6f) return Color.yellow;
            return Color.green;
        }
    }
}