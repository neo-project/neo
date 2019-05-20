using System;

namespace Neo.SmartContract.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AllowedTriggerAttribute : Attribute
    {
        /// <summary>
        /// Allowed types
        /// </summary>
        public TriggerType[] AllowedTypes { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="allowedTypes">Allowed types</param>
        public AllowedTriggerAttribute(params TriggerType[] allowedTypes)
        {
            AllowedTypes = allowedTypes;
        }
    }
}