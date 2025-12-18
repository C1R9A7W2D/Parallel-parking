using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject carPrefab;
    private const float PARKING_FILLING = 0.75f;
    private Quaternion SPAWN_ROTATION = Quaternion.Euler(0, 0, 90);
    private List<Vector3> carsPositions = new List<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Vector3[] parkingSpotsPositions = GetParkingSpotsPositions();
        SpawnCarsInParkingSpots(parkingSpotsPositions);
    }

    private static Vector3[] GetParkingSpotsPositions()
    {
        GameObject[] parkingSpots = GameObject.FindGameObjectsWithTag("Parking spot");
        Vector3[] parkingSpotPositions = new Vector3[parkingSpots.Length];

        for (int i = 0; i < parkingSpots.Length; i++)
            parkingSpotPositions[i] = parkingSpots[i].transform.position;

        return parkingSpotPositions;
    }

    private void SpawnCarsInParkingSpots(Vector3[] parkingSpotsPositions)
    {
        System.Random random = new();

        for(int i = 0; i < parkingSpotsPositions.Length; i++)
        {
            if (random.NextDouble() < PARKING_FILLING)
            {
                Instantiate(carPrefab, parkingSpotsPositions[i], SPAWN_ROTATION);
                carsPositions.Add(parkingSpotsPositions[i]);
            }
        }
    }

    public List<Vector3> GetCarsPositions()
    {
        return carsPositions;
    }
}
