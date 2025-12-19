using ParkingSystem.FuzzySystem.Inputs;
using ParkingSystem.FuzzySystem.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkingSystem.Integration
{
    public static class EventManager
    {
        // 1. ОБЪЯВЛЕНИЕ события OnEmergencyStop
        public static event Action<string> OnEmergencyStop;

        // 2. Остальные события (примеры)
        public static event Action<ParkingInput> OnParkingInputUpdated;
        public static event Action<FuzzyInputData> OnFuzzyInputUpdated;
        public static event Action<ParkingOutput> OnParkingCommandExecuted;
        public static event Action<Vector2> OnCarPositionChanged;
        public static event Action<string> OnSystemMessage;

        // 3. СТАТИЧЕСКИЙ МЕТОД для вызова события (корректный)
        public static void TriggerEmergencyStop(string reason)
        {
            Debug.LogError($"[EventManager] АВАРИЙНАЯ ОСТАНОВКА: {reason}");
            OnEmergencyStop?.Invoke(reason);
        }

        // 4. Остальные статические методы-триггеры (примеры)
        public static void TriggerParkingCommand(ParkingOutput command)
        {
            OnParkingCommandExecuted?.Invoke(command);
        }

        public static void TriggerCarPositionChanged(Vector2 position)
        {
            OnCarPositionChanged?.Invoke(position);
        }

        public static void TriggerSystemMessage(string message)
        {
            Debug.Log($"[EventManager] {message}");
            OnSystemMessage?.Invoke(message);
        }

        // 5. Очередь событий (опционально, для порядка)
        private static Queue<SystemEvent> eventQueue = new Queue<SystemEvent>();
        private static readonly object queueLock = new object();

        private struct SystemEvent
        {
            public string EventType;
            public object Data;
            public float Timestamp;
        }

        public static void QueueEvent(string eventType, object data = null)
        {
            lock (queueLock)
            {
                eventQueue.Enqueue(new SystemEvent
                {
                    EventType = eventType,
                    Data = data,
                    Timestamp = Time.time
                });
            }
        }

        public static void ProcessEventQueue()
        {
            lock (queueLock)
            {
                while (eventQueue.Count > 0)
                {
                    var sysEvent = eventQueue.Dequeue();
                    // Здесь можно добавить логику обработки
                    Debug.Log($"[Очередь] Событие: {sysEvent.EventType}");
                }
            }
        }
    }
}