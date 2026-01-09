using System;
using System.Collections.Generic;
using System.Drawing;

namespace rpg_deneme.Core
{
    public class Notification
    {
        public string Message { get; set; }
        public Color Color { get; set; } = Color.White;
        public int Duration { get; set; } = 180; // in ticks (~3 sec at 60fps)
        public int InitialDuration { get; set; } = 180;
    }

    public static class NotificationManager
    {
        private static List<Notification> _notifications = new List<Notification>();
        private static readonly object _lock = new object();

        // Passives / Buffs
        public class PassiveBuff
        {
            public string Name;
            public string IconType; // "ManaRegen", "AttackBoost" etc.
            public int Value;
        }

        private static List<PassiveBuff> _activeBuffs = new List<PassiveBuff>();

        public static void AddNotification(string message, Color color)
        {
            lock (_lock)
            {
                _notifications.Add(new Notification { Message = message, Color = color });
                if (_notifications.Count > 5)
                {
                    _notifications.RemoveAt(0); // Keep last 5
                }
            }
        }

        public static void AddNotification(string message)
        {
            AddNotification(message, Color.Yellow); // Default color
        }

        public static void ClearBuffs()
        {
            _activeBuffs.Clear();
        }

        public static void AddBuff(string name, string iconType, int value)
        {
            // Simple logic: if exists update, else add
            var existing = _activeBuffs.Find(x => x.Name == name);
            if (existing != null)
            {
                existing.Value = value;
            }
            else
            {
                _activeBuffs.Add(new PassiveBuff { Name = name, IconType = iconType, Value = value });
            }
        }

        public static void Update()
        {
            lock (_lock)
            {
                for (int i = _notifications.Count - 1; i >= 0; i--)
                {
                    _notifications[i].Duration--;
                    if (_notifications[i].Duration <= 0)
                    {
                        _notifications.RemoveAt(i);
                    }
                }
            }
        }

        public static List<Notification> GetNotifications()
        {
            lock (_lock)
            {
                return new List<Notification>(_notifications);
            }
        }

        public static List<PassiveBuff> GetBuffs()
        {
            return new List<PassiveBuff>(_activeBuffs);
        }
    }
}
