using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject carPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instantiate(carPrefab, new Vector2(0, UnityEngine.Random.Range(-11, 9)), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
