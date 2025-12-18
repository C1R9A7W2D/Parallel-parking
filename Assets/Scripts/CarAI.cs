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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion rot = Quaternion.Euler(0, 0, rotation);
        Vector3 moveDirection = (rot * Vector3.up).normalized;
        transform.localPosition += moveDirection * speed * Time.deltaTime;
    }

    void ChangeSpeed(float delta)
    {
        speed += delta;
    }

    void Rotate(float degrees)
    {
        rotation += degrees;
        transform.Rotate(0, 0, rotation);
    }
}
