using UnityEngine;

public class SimpleTurnInPlace : MonoBehaviour
{
    [Header("Настройки поворота")]
    [SerializeField] private float turnSpeed = 90f; // градусов в секунду
    [SerializeField] private bool turnRight = true;
    [SerializeField] private bool continuous = true; // непрерывный поворот

    [Header("Тестовые параметры")]
    [SerializeField] private float turnDuration = 2f; // если не continuous

    private float timer = 0f;

    void Update()
    {
        float turnAngle = turnRight ? turnSpeed : -turnSpeed;

        if (continuous)
        {
            // Непрерывный поворот
            transform.Rotate(0, 0, turnAngle * Time.deltaTime);
        }
        else
        {
            // Поворот на определенное время
            if (timer < turnDuration)
            {
                transform.Rotate(0, 0, turnAngle * Time.deltaTime);
                timer += Time.deltaTime;
            }
            else
            {
                Debug.Log("Поворот завершен");
            }
        }
    }

    void OnGUI()
    {
        // Простой UI для управления
        GUI.Label(new Rect(10, 10, 200, 30), $"Угол: {transform.eulerAngles.z:F1}°");

        if (GUI.Button(new Rect(10, 50, 100, 30), "Повернуть 90°"))
        {
            transform.Rotate(0, 0, turnRight ? 90 : -90);
        }

        if (GUI.Button(new Rect(10, 90, 100, 30), "Сброс поворота"))
        {
            transform.rotation = Quaternion.identity;
        }
    }
}