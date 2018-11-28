using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Time
{
    public abstract class TimeProvider
    {
        private static TimeProvider current =
            DefaultTimeProvider.Instance;

        public static TimeProvider Current
        {
           get { return TimeProvider.current; }
           set
           {
               if (value == null)
               {
                   throw new ArgumentNullException("value");
               }
               TimeProvider.current = value;
           }
       }

       public abstract DateTime UtcNow { get; }

       public static void ResetToDefault()
       {
           TimeProvider.current = DefaultTimeProvider.Instance;
       }
    }


    public class DefaultTimeProvider : TimeProvider
    {
        private static DefaultTimeProvider current = new DefaultTimeProvider();
        public static DefaultTimeProvider Instance
        {
             get { return DefaultTimeProvider.current; }
             set
             {
                 if (value == null)
                 {
                     throw new ArgumentNullException("value");
                 }
                 DefaultTimeProvider.current = value;
             }
        }

        public override DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
