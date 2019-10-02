using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Neo.IO.Json;

namespace Neo.Network.RPC.Server
{
    public interface IRpcServer
    {
        /// <summary>
        /// Starts the server
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the server
        /// </summary>
        void Stop();

        /// <summary>
        /// Register an operation that can be called by the server,
        /// eg.:
        /// server.BindOperation("controllerName", "operationName", new Func<int, bool>(MyMethod));
        /// </summary>
        /// <param name="controllerName">controller name is used to organize many operations in a group</param>
        /// <param name="operationName">operation name</param>
        /// <param name="anyMethod">the method to be called when the operation is called</param>
        void BindOperation(string controllerName, string operationName, Delegate anyMethod);

        /// <summary>
        /// Register an operation that can be called by the server,
        /// eg.:
        /// server.BindOperation("controllerName", "operationName", new Func<int, bool>(MyMethod));
        /// </summary>
        /// <param name="controllerName">controller name is used to organize many operations in a group</param>
        /// <param name="operationName">operation name</param>
        /// <param name="target">caller object of the method</param>
        /// <param name="anyMethod">the method to be called when the operation is called</param>
        void BindOperation(string controllerName, string operationName, object target, MethodInfo anyMethod);

        /// <summary>
        /// Register many operations organized in a controller class,
        /// The operations should be methods annotated with [RpcMethod] or [RpcMethod("operationName")]
        /// </summary>
        /// <param name="controller">the controller class</param>
        void BindController(Type controller);

        /// <summary>
        /// Register many operations organized in a controller class,
        /// The operations should be methods annotated with [RpcMethod] or [RpcMethod("operationName")]
        /// </summary>
        /// <typeparam name="T">the controller class</typeparam>
        void BindController<T>() where T : new();

        /// <summary>
        /// Calls the server operation
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        /// <param name="operationName">the operation name</param>
        /// <param name="parameters">all parameters expected by the operation</param>
        /// <returns>the return of the operation, not casted</returns>
        object CallOperation(HttpContext context, string controllerName, string operationName, params object[] parameters);

        /// <summary>
        /// Calls the server operation
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        /// <param name="operationName">the operation name</param>
        /// <param name="parameters">all parameters expected by the operation organized by name</param>
        /// <returns>the return of the operation, not casted</returns>
        object CallOperation(HttpContext context, string controllerName, string operationName, IDictionary<string, object> parameters);

        /// <summary>
        /// removes a previous registered operation from the server
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        /// <param name="operationName">the operation name</param>
        void UnbindOperation(string controllerName, string operationName);

        /// <summary>
        /// removes all operations of a previous registered controller and registered operation with the informed
        /// controller name
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        void UnbindController(string controllerName);

        /// <summary>
        /// removes all registered operations and controllers
        /// </summary>
        void UnbindAllOperations();

        /// <summary>
        /// Adds a special parameter injection callback to be called everytime its required a parameter of that type
        /// </summary>
        /// <param name="parameterConstructor"></param>
        /// <typeparam name="T">type asked</typeparam>
        void InjectSpecialParameter<T>(Func<HttpContext, T> parameterConstructor);
    }
}
