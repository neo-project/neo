using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.Plugins
{
    public class NeoPlugin
    {
        static NeoRpcPlugin[] RpcLoaded = null;
        static NeoLogPlugin[] LogLoaded = null;

        /// <summary>
        /// Load neo plugins
        /// </summary>
        static NeoPlugin()
        {
            // Search in plugins directory
            string path = Path.Combine(".", "plugins");
            if (!Directory.Exists(path))
                return;

            Type trpc = typeof(NeoRpcPlugin);
            Type tlog = typeof(NeoLogPlugin);

            List<NeoRpcPlugin> lrpc = new List<NeoRpcPlugin>();
            List<NeoLogPlugin> llog = new List<NeoLogPlugin>();

            // Search libraries
            foreach (string lib in Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly))
            {
                // Should be ideal to load it into a separate AppDomain, to adjust security privileges, but it requires NEO files to be signed.
                Assembly asm = Assembly.Load(File.ReadAllBytes(lib));

                foreach (Type tp in asm.GetTypes())
                {
                    if (trpc.IsAssignableFrom(tp))
                    {
                        // Try to load them
                        NeoRpcPlugin pl = (NeoRpcPlugin)Activator.CreateInstance(tp);
                        if (pl.Load() && pl.IsLoaded) lrpc.Add(pl);
                    }
                    else
                    {
                        if (tlog.IsAssignableFrom(tp))
                        {
                            // Try to load them
                            NeoLogPlugin pl = (NeoLogPlugin)Activator.CreateInstance(tp);
                            if (pl.Load() && pl.IsLoaded) llog.Add(pl);
                        }
                    }
                }
            }

            // Set plugins
            RpcLoaded = lrpc.Count <= 0 ? null : lrpc.ToArray();
            LogLoaded = llog.Count <= 0 ? null : llog.ToArray();
        }

        #region Properties
        /// <summary>
        /// Is loaded
        /// </summary>
        public bool IsLoaded { get; private set; }
        #endregion

        /// <summary>
        /// Private constructor
        /// </summary>
        protected NeoPlugin() { }

        #region Load/Unload
        /// <summary>
        /// Load Plugin
        /// </summary>
        /// <returns>Return true if is loaded successfully</returns>
        public virtual bool Load()
        {
            IsLoaded = true;
            return true;
        }
        /// <summary>
        /// Unload plugin
        /// </summary>
        public virtual void Unload()
        {
            IsLoaded = false;
        }
        #endregion

        #region static
        /// <summary>
        /// Unload all plugins
        /// </summary>
        public static void UnloadAll()
        {
            if (LogLoaded != null)
            {
                foreach (NeoPlugin plg in LogLoaded) try { plg.Unload(); } catch { }
                LogLoaded = null;
            }
            if (RpcLoaded != null)
            {
                foreach (NeoPlugin plg in RpcLoaded) try { plg.Unload(); } catch { }
                RpcLoaded = null;
            }
        }
        /// <summary>
        /// Log Exception
        /// </summary>
        /// <param name="error">Error</param>
        public static void BroadcastLog(Exception error)
        {
            if (LogLoaded == null) return;

            foreach (NeoLogPlugin log in LogLoaded)
                log.Log(error);
        }
        /// <summary>
        /// Log Message
        /// </summary>
        /// <param name="message">Message</param>
        public static void BroadcastLog(string msg)
        {
            if (LogLoaded == null) return;

            foreach (NeoLogPlugin log in LogLoaded)
                log.Log(msg);
        }
        /// <summary>
        /// Execute Rpc Call
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <returns>Return restulting object</returns>
        public static JObject BroadcastRpcCall(NeoRpcPluginArgs args)
        {
            if (RpcLoaded == null) return null;

            JObject ret = null;
            foreach (NeoRpcPlugin rpc in RpcLoaded)
            {
                ret = rpc.RpcCall(args);
                if (args.Handle && ret != null) break;
            }

            return ret;
        }
        #endregion
    }
}