using System;

namespace Neo.IO.Caching
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class ReflectionCacheAttribute : Attribute
    {
        /// <summary>
        /// Type
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        public ReflectionCacheAttribute(Type type)
        {
            Type = type;
        }
    }
}
