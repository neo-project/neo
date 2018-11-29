using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo
{
    public abstract class TimeProvider
    {
        private static TimeProvider current =
            DefaultTimeProvider.Instance;

        public static TimeProvider Current
        {
           get { return TimeProvider.current; }
           internal set
           {
               if (value != null)
               {
                   TimeProvider.current = value;
               }
           }
       }

       public abstract DateTime UtcNow { get; }

       internal static void ResetToDefault()
       {
           TimeProvider.current = DefaultTimeProvider.Instance;
       }
    }


    internal class DefaultTimeProvider : TimeProvider
    {
        private static readonly DefaultTimeProvider current = new DefaultTimeProvider();
        public static DefaultTimeProvider Instance
        {
             get { return DefaultTimeProvider.current; }
        }

        public override DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
