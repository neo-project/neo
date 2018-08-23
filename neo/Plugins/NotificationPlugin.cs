using System.Collections.Generic;

namespace Neo.Plugins
{
    public abstract class NotificationPlugin : Plugin
    {
        private static readonly List<NotificationPlugin> instances = new List<NotificationPlugin>();

        public new static IEnumerable<NotificationPlugin> Instances => instances;

        public string UserAgent { get; set; }

        protected NotificationPlugin()
        {
            instances.Add(this);
        }

        public abstract bool StartPersistingNotifications();
        public abstract bool StartRESTApi();

    }
}
