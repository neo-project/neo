using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Akka.Util.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Neo.IO.Json;
using Neo.Plugins;

namespace Neo.Network.RPC.Server
{
    public class RpcServer : IRpcServer
    {
        #region Variables

        private const int MaxPostValue = 1024 * 1024 * 2;

        private IWebHost _host;
        private readonly RpcConfig _config;
        private IDictionary<string, IDictionary<string, RcpTargetAndMethod>> _operations = new Dictionary<string, IDictionary<string, RcpTargetAndMethod>>();
        private IDictionary<Type, Func<HttpContext, object>> _specialParameterInjectors = new Dictionary<Type, Func<HttpContext, object>>();

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config">Config</param>
        public RpcServer(RpcConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            InjectSpecialParameter(ctx => ctx);
        }

        private static JObject CreateErrorResponse(bool isNewVersion, string id, int code, string message, string stacktrace = null)
        {
            var response = CreateResponse(isNewVersion, id);
            response["error"] = new JObject
            {
                ["code"] = code,
                ["message"] = message
            };

#if DEBUG
            if (stacktrace != null)
            {
                response["error"]["data"] = stacktrace;
            }
#endif

            return response;
        }

        private static JObject CreateResponse(bool isNewVersion, string id)
        {
            var response = new JObject
            {
                ["jsonrpc"] = isNewVersion ? "3.0" : "2.0"
            };

            if (!string.IsNullOrEmpty(id))
            {
                response["id"] = id;
            }

            return response;
        }

        private static JObject CreateResponse(bool isNewVersion, string id, object result)
        {
            var response = CreateResponse(isNewVersion, id);
            response["result"] = JObject.FromPrimitive(result);
            return response;
        }

        private JObject ProcessRequest(HttpContext context)
        {
            string postBody = null;
            var pNamed = areParametersNamed(context);

            if (HttpMethods.Post.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                if (!context.Request.ContentLength.HasValue || context.Request.ContentLength > MaxPostValue)
                {
                    return CreateErrorResponse(pNamed, null, -32700, "The post body is too long, max size is " + MaxPostValue);
                }

                postBody = new StreamReader(context.Request.Body).ReadToEnd();
            }

            var controller = extractController(context);
            var method = extractMethod(context, postBody);
            var id = extractId(context, postBody);

            if (string.IsNullOrEmpty(method))
            {
                return CreateErrorResponse(pNamed, id, -32700, "Method not informed");
            }

            try {

                if (pNamed)
                {
                    var result = CallOperation(context, controller, method, extractParamsAsDictionary(context, postBody));
                    return CreateResponse(pNamed, id, result);
                }
                else
                {
                    var result = CallOperation(context, controller, method, extractParamsAsArray(context, postBody));
                    return CreateResponse(pNamed, id, result);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                return CreateErrorResponse(pNamed, id, ex.HResult, ex.Message, ex.StackTrace);
            }
        }

        private async Task ProcessAsync(HttpContext context)
        {
            if(IsIpAddressAllowed(context.Connection.RemoteIpAddress) == false)
            {
                var pNamed = areParametersNamed(context);

                Log("Unauthorized request " + context.Connection.RemoteIpAddress, LogLevel.Warning);

                context.Response.StatusCode = 401;
                var unathorizedResponse = CreateErrorResponse(pNamed, null, 401, "Forbidden");
                context.Response.ContentType = "application/json-rpc";
                await context.Response.WriteAsync(unathorizedResponse.ToString(), Encoding.UTF8);

                return;
            }

            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";

            var response = ProcessRequest(context);

            if (response == null || (response as JArray)?.Count == 0) return;

            context.Response.ContentType = "application/json-rpc";
            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
        }

        private bool IsIpAddressAllowed(IPAddress ip)
        {
            return _config.IpBlacklist == null || !_config.IpBlacklist.Contains(ip);
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            // Check started

            if (_host != null)
            {
                Log("RPC server already started");
                return;
            }

            _host = new WebHostBuilder().UseKestrel(options => options.Listen(_config.ListenEndPoint, listenOptions =>
            {
                // Config SSL

                if (_config.Ssl != null && _config.Ssl.IsValid)
                    listenOptions.UseHttps(_config.Ssl.Path, _config.Ssl.Password, httpsConnectionAdapterOptions =>
                    {
                        if (_config.TrustedAuthorities is null || _config.TrustedAuthorities.Length == 0)
                        {
                            return;
                        }

                        httpsConnectionAdapterOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                        httpsConnectionAdapterOptions.ClientCertificateValidation = (cert, chain, err) =>
                        {
                            if (err != SslPolicyErrors.None)
                            {
                                return false;
                            }

                            X509Certificate2 authority = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                            return _config.TrustedAuthorities.Contains(authority.Thumbprint);
                        };
                    });
            }))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            _host.Start();
            Log($"RPC server started on {_config.ListenEndPoint.Address}:{_config.ListenEndPoint.Port}");
        }

        /// <summary>
        /// Stops the server
        /// </summary>
        public  void Stop()
        {
            if (_host == null)
            {
                Log("RPC server already stopped");
                return;
            }

            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }

            Log("RPC server stopped");
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Register an operation that can be called by the server.
        /// Usage:
        /// server.BindOperation("controllerName", "operationName", new Func<int, bool>(MyMethod));
        /// </summary>
        /// <param name="controllerName">controller name is used to organize many operations in a group</param>
        /// <param name="operationName">operation name</param>
        /// <param name="anyMethod">the method to be called when the operation is called</param>
        public void BindOperation(string controllerName, string operationName, Delegate anyMethod)
        {
            BindOperation(controllerName, operationName, anyMethod.Target, anyMethod.Method);
        }

        /// <summary>
        /// Register an operation that can be called by the server.
        /// Usage:
        /// server.BindOperation("controllerName", "operationName", new Func<int, bool>(MyMethod));
        /// </summary>
        /// <param name="controllerName">controller name is used to organize many operations in a group</param>
        /// <param name="operationName">operation name</param>
        /// <param name="target">caller object of the method</param>
        /// <param name="anyMethod">the method to be called when the operation is called</param>
        public void BindOperation(string controllerName, string operationName, object target, MethodInfo anyMethod)
        {
            controllerName = controllerName ?? "$root";

            if (!_operations.ContainsKey(controllerName))
            {
                _operations.Add(controllerName, new Dictionary<string, RcpTargetAndMethod>());
            }

            var controller = _operations[controllerName];

            var callerAndMethod = new RcpTargetAndMethod()
            {
                Target = target,
                Method = anyMethod
            };

            if (!controller.ContainsKey(operationName))
            {
                controller.Add(operationName, callerAndMethod);
            }
            else
            {
                controller[operationName] = callerAndMethod;
            }
        }

        /// <summary>
        /// Register many operations organized in a controller class,
        /// The operations should be methods annotated with [RpcMethod] or [RpcMethod("operationName")]
        /// </summary>
        /// <typeparam name="T">the controller class</typeparam>
        public void BindController<T>() where T : new()
        {
            var controller = typeof(T);
            var controllerInstance = new T();
            BindController(controller, controllerInstance);
        }

        /// <summary>
        /// Register many operations organized in a controller class,
        /// The operations should be methods annotated with [RpcOperation] or [RpcOperation("operationName")]
        /// </summary>
        /// <param name="controller">the controller class</param>
        public void BindController(Type controller)
        {
            var controllerInstance = Activator.CreateInstance(controller);
            BindController(controller, controllerInstance);
        }

        /// <summary>
        /// Register many operations organized in a controller class,
        /// The operations should be methods annotated with [RpcOperation] or [RpcOperation("operationName")]
        /// </summary>
        /// <param name="controller">the controller class</param>
        private void BindController(Type controller, object controllerInstance)
        {
            var controllerName = controller.Name;
            var controllerAttr = controller.GetCustomAttributes<RpcControllerAttribute>(false).FirstOrDefault();

            if (controllerAttr != null && !string.IsNullOrEmpty(controllerAttr.Name))
            {
                controllerName = controllerAttr.Name;
            }

            var methods = controller.GetMethods()
                .Select(m => (Method: m, Attribute: m.GetCustomAttributes<RpcMethodAttribute>(false).FirstOrDefault()));

            foreach (var i in methods)
            {
                var name = i.Method.Name;

                if (i.Attribute != null && !string.IsNullOrEmpty(i.Attribute.Name))
                {
                    name = i.Attribute.Name;
                }

                BindOperation(controllerName, name, controllerInstance, i.Method);
            }
        }

        /// <summary>
        /// Calls the server operation
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        /// <param name="operationName">the operation name</param>
        /// <param name="parameters">all parameters expected by the operation</param>
        /// <returns>the return of the operation, not casted</returns>
        public object CallOperation(HttpContext context, string controllerName, string operationName, params object[] parameters)
        {
            controllerName = controllerName ?? "$root";

            if (_operations.ContainsKey(controllerName) && _operations[controllerName].ContainsKey(operationName))
            {
                var method = _operations[controllerName][operationName];
                var methodParameters = method.Method.GetParameters();
                List<object> paramsList = new List<object>();

                var m = 0;
                var offsetM = 0;
                while (m + offsetM < methodParameters.Length)
                {
                    var mParam = methodParameters[m + offsetM];

                    if (_specialParameterInjectors.ContainsKey(mParam.ParameterType))
                    {
                        paramsList.Add(_specialParameterInjectors[mParam.ParameterType](context));

                        offsetM++;
                    }
                    else if (m < parameters.Length)
                    {
                        paramsList.Add(ConvertParameter(parameters[m], mParam));

                        m++;
                    }
                    else
                    {
                        paramsList.Add(mParam.DefaultValue);

                        m++;
                    }
                }

                return method.Method.Invoke(method.Target, paramsList.ToArray());
            }

            throw new ArgumentException("Operation not found: " + (controllerName.Equals("$root") ? "(no controller) " : controllerName + "/") + operationName);
        }

        /// <summary>
        /// Calls the server operation
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        /// <param name="operationName">the operation name</param>
        /// <param name="parameters">all parameters expected by the operation, as a dictionary, to match the names</param>
        /// <returns>the return of the operation, not casted</returns>
        public object CallOperation(HttpContext context, string controllerName, string operationName, IDictionary<string, object> parameters)
        {
            controllerName = controllerName ?? "$root";

            if (_operations.ContainsKey(controllerName) && _operations[controllerName].ContainsKey(operationName))
            {
                var method = _operations[controllerName][operationName];
                var methodParameters = method.Method.GetParameters();

                var paramNames = methodParameters.Select(p => p.Name).ToArray();
                var reorderedParams = new List<object>();

                foreach (var mParam in methodParameters) {
                    var paramIndex = Array.IndexOf(paramNames, mParam.Name);

                    if (_specialParameterInjectors.ContainsKey(mParam.ParameterType))
                    {
                        reorderedParams.Insert(paramIndex, _specialParameterInjectors[mParam.ParameterType](context));
                    } else if (parameters.ContainsKey(mParam.Name))
                    {
                        reorderedParams.Insert(paramIndex, ConvertParameter(parameters[mParam.Name], mParam));
                    }
                    else
                    {
                        reorderedParams.Add(mParam.DefaultValue);
                    }
                }

                return method.Method.Invoke(method.Target, reorderedParams.ToArray());
            }

            throw new ArgumentException("Operation not found: " + (controllerName.Equals("$root") ? "" : controllerName + "/") + operationName);
        }

        private static object ConvertParameter(object p, ParameterInfo paramInfo)
        {
            var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);

            if (p == null)
            {
                return null;
            }

            if (converter.CanConvertFrom(p.GetType()))
            {
                return converter.ConvertFrom(p);
            }

            try
            {
                return Convert.ChangeType(p, paramInfo.ParameterType);
            } catch {
                throw new ArgumentException("Wrong parameter type, expected "
                                            + paramInfo.Name
                                            + " to be "
                                            + paramInfo.ParameterType
                                            + " and it is " + p.GetType());
            }
        }

        /// <summary>
        /// removes a previous registered operation from the server
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        /// <param name="operationName">the operation name</param>
        public void UnbindOperation(string controllerName, string operationName)
        {
            controllerName = controllerName ?? "$root";

            if (_operations.ContainsKey(controllerName) && _operations[controllerName].ContainsKey(operationName))
            {
                _operations[controllerName].Remove(operationName);
            }
        }

        /// <summary>
        /// removes all operations of a previous registered controller and registered operation with the informed
        /// controller name
        /// </summary>
        /// <param name="controllerName">the controller name</param>
        public void UnbindController(string controllerName)
        {
            controllerName = controllerName ?? "$root";

            if (_operations.ContainsKey(controllerName))
            {
                _operations.Remove(controllerName);
            }
        }

        /// <summary>
        /// removes all registered operations and controllers
        /// </summary>
        public void UnbindAllOperations()
        {
            _operations.Clear();
        }

        public void InjectSpecialParameter<T>(Func<HttpContext, T> parameterConstructor)
        {
            _specialParameterInjectors.Add(typeof(T), ctx => parameterConstructor(ctx));
        }

        private string extractController(HttpContext context)
        {
            // skip 1 because we dont need the things before the first bar
            var pathParts = context.Request.Path.Value.Split('/').Skip(1).ToArray();

            if (pathParts.Length > 1)
            {
                // ANY localhost:10332/controllername/methodname
                return pathParts[0];
            }

            // ANY localhost:10332/methodname
            // there is no controllername, will use $root
            return null;

            // Doesn't work on this conditions, it's not necessary because controllers are for the new pattern
            // - GET localhost:10332?controller=controllername
            // - POST localhost:10332 BODY{ "controller": "controllername" }
        }

        private string extractMethod(HttpContext context, string body)
        {
            if (areParametersNamed(context))
            {
                // ANY localhost:10332/controllername/methodname
                // ANY localhost:10332/methodname

                // skip 1 because we dont need the things before the first bar
                var pathParts = context.Request.Path.Value.Split('/').Skip(1).ToArray();

                return pathParts[pathParts.Length - 1];
            }

            if (HttpMethods.Get.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                // GET localhost:10332?method=methodname
                return context.Request.Query["method"];
            }

            // POST localhost:10332 BODY{ "method": "methodname" }
            var jobj = JObject.Parse(body);

            if (jobj.ContainsProperty("method"))
            {
                return jobj["method"].AsString();
            }

            return null;
        }

        private string extractId(HttpContext context, string body)
        {
            if (HttpMethods.Get.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                // GET localhost:10332?id=123
                return context.Request.Query["id"];
            }

            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            // POST localhost:10332 BODY{ "id": 123 }
            var jobj = JObject.Parse(body);

            if (jobj.ContainsProperty("id"))
            {
                return jobj["id"].AsString();
            }

            return null;
        }

        private IDictionary<string, object> extractParamsAsDictionary(HttpContext context, string body)
        {
            if (HttpMethods.Get.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                // GET localhost:10332/controllername/methodname?first=1&second=a
                return context.Request.Query.ToDictionary(p => p.Key, p => {
                    var pArray = (object[]) p.Value;

                    if (pArray.Length == 1)
                    {
                        return pArray[0];
                    }
                    else
                    {
                        return (object) pArray;
                    }
                });
            }

            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            // POST localhost:10332 BODY{ "first": 1, "second": "a" }
            return (IDictionary<string, object>) JObject.Parse(body).ToPrimitive();
        }

        private object[] extractParamsAsArray(HttpContext context, string body)
        {
            if (HttpMethods.Post.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                // POST localhost:10332 BODY{ "params": [1, "a"] }
                return (object[]) JObject.Parse(body)["params"].ToPrimitive();
            }

            // GET localhost:10332?params=[1, "a"]
            string par = context.Request.Query["params"];
            try
            {
                return (object[]) JObject.Parse(par).ToPrimitive();
            }
            catch
            {
                // Try in base64
                par = Encoding.UTF8.GetString(Convert.FromBase64String(par));
                return (object[]) JObject.Parse(par).ToPrimitive();
            }
        }

        private bool areParametersNamed(HttpContext context)
        {
            var pathParts = context.Request.Path.Value.Split('/').Skip(1).ToArray();
            return pathParts.Length > 0 && !string.IsNullOrEmpty(pathParts[0]);
        }

        private void Log(string message, LogLevel level = LogLevel.Info)
        {
            Plugin.Log(nameof(RpcServer), level, message);
        }
    }
}
