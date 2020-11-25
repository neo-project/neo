using System;

namespace Neo.Plugins
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequiredServicesAttribute : Attribute
    {
        public Type[] RequiredServices { get; }

        public RequiredServicesAttribute(params Type[] requiredServices)
        {
            this.RequiredServices = requiredServices;
        }
    }
}
