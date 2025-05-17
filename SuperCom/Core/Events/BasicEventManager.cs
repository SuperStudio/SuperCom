using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Core.Events
{
    public static class BasicEventManager
    {
        private class EventData
        {
            public EventType Type { get; set; }
            public Action<object> Action { get; set; }

            public EventData()
            {

            }

            public EventData(EventType type, Action<object> action)
            {
                Type = type;
                Action = action;
            }
        }



        private static List<EventData> ActionList { get; set; }

        private static readonly object _lock = new object();

        static BasicEventManager()
        {
            lock (_lock)
                ActionList = new List<EventData>();
        }


        public static void RegisterEvent(EventType type, Action<object> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            EventData eventData = new EventData(type, action);
            lock (_lock)
                ActionList.Add(eventData);
        }

        public static void SendEvent(EventType type, object data)
        {
            lock (_lock) {
                foreach (EventData eventData in ActionList) {
                    if (eventData.Type == type)
                        eventData.Action(data);
                }
            }
        }

    }
}
