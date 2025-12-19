using UnityEngine;
using System;

namespace ParkingSystem.FuzzySystem.Inputs
{
    /// <summary>
    /// Входные данные для системы парковки, собранные с датчиков и окружения
    /// </summary>
    [System.Serializable]
    public class ParkingInput
    {
        [Header("Данные автомобиля")]
        public Vector2 carPosition;          // Позиция автомобиля (XZ плоскость)
        public float carRotation;            // Угол поворота в градусах (0 = смотрит вперед)
        public float carSpeed;               // Текущая скорость (м/с)
        public float carMaxSpeed;            // Максимальная скорость
        public bool movesForward;            // Направление движения
        public float sensorRange;            // Дальность датчиков

        [Header("Данные датчиков")]
        public float[] sensorDistances;      // 8 датчиков через 45 градусов

        [Header("Парковочные места")]
        public ParkingSpot[] availableSpots; // Доступные парковочные места

        [Header("Препятствия")]
        public Obstacle[] nearbyObstacles;   // Ближайшие препятствия

        [Header("Метаданные")]
        public float timestamp;              // Время сбора данных
        public int frameCount;               // Номер кадра

        /// <summary>
        /// Пустой конструктор для сериализации
        /// </summary>
        public ParkingInput() { }

        /// <summary>
        /// Конструктор с основными параметрами
        /// </summary>
        public ParkingInput(Vector2 position, float rotation, float speed, bool forward)
        {
            carPosition = position;
            carRotation = rotation;
            carSpeed = speed;
            movesForward = forward;
            timestamp = Time.time;
            frameCount = Time.frameCount;
        }

        /// <summary>
        /// Строковое представление для отладки
        /// </summary>
        public override string ToString()
        {
            return $"Позиция: {carPosition:F2}, Поворот: {carRotation:F1}°, " +
                   $"Скорость: {carSpeed:F2}/{carMaxSpeed:F1} м/с, " +
                   $"Вперед: {movesForward}";
        }

        /// <summary>
        /// Получить данные датчика по индексу
        /// </summary>
        public float GetSensorDistance(int index)
        {
            if (sensorDistances == null || index < 0 || index >= sensorDistances.Length)
                return float.MaxValue;

            return sensorDistances[index];
        }

        /// <summary>
        /// Получить расстояние вперед (датчик 0)
        /// </summary>
        public float GetFrontDistance() => GetSensorDistance(0);

        /// <summary>
        /// Получить расстояние назад (датчик 4)
        /// </summary>
        public float GetRearDistance() => GetSensorDistance(4);

        /// <summary>
        /// Получить расстояние слева (датчик 6)
        /// </summary>
        public float GetLeftDistance() => GetSensorDistance(6);

        /// <summary>
        /// Получить расстояние справа (датчик 2)
        /// </summary>
        public float GetRightDistance() => GetSensorDistance(2);

        /// <summary>
        /// Найти ближайшее препятствие
        /// </summary>
        public Obstacle? FindNearestObstacle()
        {
            if (nearbyObstacles == null || nearbyObstacles.Length == 0)
                return null;

            Obstacle nearest = nearbyObstacles[0];
            float minDistance = nearest.distance;

            for (int i = 1; i < nearbyObstacles.Length; i++)
            {
                if (nearbyObstacles[i].distance < minDistance)
                {
                    nearest = nearbyObstacles[i];
                    minDistance = nearest.distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Найти ближайшее парковочное место
        /// </summary>
        public ParkingSpot? FindNearestParkingSpot()
        {
            if (availableSpots == null || availableSpots.Length == 0)
                return null;

            ParkingSpot nearest = availableSpots[0];
            float minDistance = nearest.distanceToCar;

            for (int i = 1; i < availableSpots.Length; i++)
            {
                if (availableSpots[i].distanceToCar < minDistance)
                {
                    nearest = availableSpots[i];
                    minDistance = nearest.distanceToCar;
                }
            }

            return nearest;
        }
    }

    /// <summary>
    /// Парковочное место
    /// </summary>
    [System.Serializable]
    public struct ParkingSpot
    {
        public Vector3 position;      // Позиция центра места
        public float width;          // Ширина места
        public float length;         // Длина места
        public float angle;          // Угол ориентации
        public bool isAvailable;     // Доступность
        public float distanceToCar;  // Расстояние до автомобиля

        /// <summary>
        /// Проверка, подходит ли место для парковки
        /// </summary>
        public bool IsSuitableForParking(float carWidth, float carLength)
        {
            float widthMargin = 0.5f; // Запас по ширине
            float lengthMargin = 0.8f; // Запас по длине

            return isAvailable &&
                   width >= carWidth + widthMargin &&
                   length >= carLength + lengthMargin;
        }

        public override string ToString()
        {
            return $"Место: {position}, Ширина: {width:F1}м, Доступно: {isAvailable}";
        }
    }

    /// <summary>
    /// Препятствие
    /// </summary>
    [System.Serializable]
    public struct Obstacle
    {
        public Vector3 position;     // Позиция препятствия
        public float distance;       // Расстояние до автомобиля
        public ObstacleType type;    // Тип препятствия
        public float size;           // Размер (радиус или ширина)

        public override string ToString()
        {
            return $"Препятствие: {position}, Расстояние: {distance:F1}м, Тип: {type}";
        }
    }

    /// <summary>
    /// Тип препятствия
    /// </summary>
    public enum ObstacleType
    {
        Static,     // Статическое (столб, стена)
        Dynamic,    // Динамическое (другая машина, пешеход)
        Unknown     // Неизвестное
    }

    /// <summary>
    /// Фазы парковки
    /// </summary>
    public enum ParkingPhase
    {
        Searching,      // Поиск места
        Approaching,    // Подъезд к месту
        Aligning,       // Выравнивание
        Reversing,      // Задний ход
        Adjusting,      // Корректировка
        Completed,      // Завершено
        Emergency       // Аварийный режим
    }
}