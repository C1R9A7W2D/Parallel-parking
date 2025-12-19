using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    [SerializeField]
    float rotation = 0;
    [SerializeField]
    Vector2 position = new Vector2(-10, 0.5f);
    [SerializeField]
    float sensorRange = 3f;
    [SerializeField]
    float maxSpeed = 3f;
    [SerializeField]
    bool movesForward = true;

    private float speed = 1f;
    private LayerMask targetMask;
    private List<Transform> visibleCars;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));
        targetMask = LayerMask.GetMask("Car");
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion rot = Quaternion.Euler(0, 0, rotation);
        Vector3 moveDirection = (rot * Vector3.up).normalized;
        transform.localPosition += moveDirection * speed * Time.deltaTime;

        DetectTargets();
    }

    void DetectTargets()
    {
        visibleCars = new List<Transform>();
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, sensorRange, targetMask);
        foreach (Collider2D target in targetsInViewRadius)
        {
            visibleCars.Add(target.transform);
        }
    }

    void ChangeSpeed(float delta)
    {
        speed += delta;
    }

    void Rotate(float degrees)
    {
        rotation += degrees;
        transform.Rotate(0, 0, degrees);
    }

    // Для визуализации в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sensorRange);
    }
}
