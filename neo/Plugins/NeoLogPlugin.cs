using System;

namespace Neo.Plugins
{
    public class NeoLogPlugin : NeoPlugin
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected NeoLogPlugin() : base() { }

        /// <summary>
        /// Log Exception
        /// </summary>
        /// <param name="error">Error</param>
        public virtual void Log(Exception error)
        {

        }
        /// <summary>
        /// Log Message
        /// </summary>
        /// <param name="message">Message</param>
        public virtual void Log(string message)
        {

        }
    }
}