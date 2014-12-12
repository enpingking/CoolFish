using System.Collections.Generic;
using System.Threading.Tasks;
using CoolFishNS.RemoteNotification.Payloads;

namespace CoolFishNS.RemoteNotification.Analytics
{
    internal class AnalyticsManager
    {
        private readonly SQSManager manager;

        public AnalyticsManager()
        {
            manager = new SQSManager();
        }

        public void SendAnalyticsEvent(double eventDurationMS, string eventType, Dictionary<string, string> data = null)
        {
            Task.Run(() =>
            {
                manager.SendAnalyticsPayload(new AnalyticsPayload
                {
                    EventDuration = eventDurationMS,
                    EventType = eventType,
                    Data = data
                });
            });
        }
    }
}