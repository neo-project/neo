// Copyright (C) 2015-2025 The Neo Project.
//
// TestUPnPMockServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.Messages.Requests;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Neo.UnitTests.Network
{
    internal class TestUPnPMockServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly TestUPnPServerConfig _serverConfig;
        public Action<HttpListenerContext> WhenRequestServiceDesc = WhenRequestService;
        public Action<HttpListenerContext> WhenGetExternalIpAddress = ResponseOk;
        public Action<HttpListenerContext> WhenAddPortMapping = ResponseOk;
        public Action<HttpListenerContext> WhenGetGenericPortMappingEntry = ResponseOk;
        public Action<HttpListenerContext> WhenDeletePortMapping = ResponseOk;
        public Func<string> WhenDiscoveryRequest;

        public TestUPnPMockServer() : this(new()) { }

        public TestUPnPMockServer(TestUPnPServerConfig serverConfig)
        {
            _serverConfig = serverConfig;
            _listener.Prefixes.Add(serverConfig.Prefix);
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            WhenDiscoveryRequest = HandleDiscoveryRequest;
        }
        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
        }

        private string HandleDiscoveryRequest() =>
            "HTTP/1.1 200 OK\r\n" +
            "Server: Custom/1.0 UPnP/1.0 Proc/Ver\r\n" +
            "EXT:\r\n" +
            "Location: " + _serverConfig.ServiceUrl + "\r\n" +
            "Cache-Control:max-age=1800\r\n" +
            "ST:urn:schemas-upnp-org:service:" + _serverConfig.ServiceType + "\r\n" +
            "USN:uuid:0000e068-20a0-00e0-20a0-48a802086048::urn:schemas-upnp-org:service:" + _serverConfig.ServiceType;

        private static void ResponseOk(HttpListenerContext context) =>
            SetStatus(context.Response, HttpStatusCode.OK, "OK");

        private static void SetStatus(HttpListenerResponse response, HttpStatusCode statusCode, string description)
        {
            response.StatusCode = (int)statusCode;
            response.StatusDescription = description;
            response.Close();
        }

        private static void WhenRequestService(HttpListenerContext context)
        {
            var responseBytes = File.OpenRead("Network/ServiceDescription.xml");

            responseBytes.CopyTo(context.Response.OutputStream);
            context.Response.OutputStream.Flush();
            SetStatus(context.Response, HttpStatusCode.OK, "OK");
        }

        private void StartAnnouncer()
        {
            Task.Run(() =>
            {
                var remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                using var udpClient = new UdpClient(1900);

                while (true)
                {
                    var bytes = udpClient.Receive(ref remoteIPEndPoint);

                    if (bytes == null || bytes.Length == 0)
                        return;

                    var response = WhenDiscoveryRequest();
                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    udpClient.Send(responseBytes, responseBytes.Length, remoteIPEndPoint);
                }
            });
        }

        private void ListenerCallback(IAsyncResult result)
        {
            if (!_listener.IsListening)
                return;

            try
            {
                var context = _listener.EndGetContext(result);
                var request = context.Request;

                if (request.Url.AbsoluteUri == _serverConfig.ServiceUrl)
                {
                    WhenRequestServiceDesc(context);
                    return;
                }

                if (request.Url.AbsoluteUri == _serverConfig.ControlUrl)
                {
                    var soapActionHeader = request.Headers["SOAPACTION"];
                    soapActionHeader = soapActionHeader[1..^1];

                    var soapActionHeaderParts = soapActionHeader.Split(['#']);
                    var serviceType = soapActionHeaderParts[0];
                    var soapAction = soapActionHeaderParts[1];
                    var buffer = new byte[request.ContentLength64 - 4];

                    request.InputStream.ReadExactly(buffer);

                    var body = Encoding.UTF8.GetString(buffer);
                    var envelop = XElement.Parse(body);

                    switch (soapAction)
                    {
                        case RequestMessage.GetExternalIpAddressActionName:
                            WhenGetExternalIpAddress(context);
                            return;
                        case RequestMessage.AddPortMappingActionName:
                            WhenAddPortMapping(context);
                            return;
                        case RequestMessage.GetGenericPortMappingEntryActionName:
                            WhenGetGenericPortMappingEntry(context);
                            return;
                        case RequestMessage.DeletePortMappingActionName:
                            WhenDeletePortMapping(context);
                            return;
                    }

                    SetStatus(context.Response, HttpStatusCode.OK, "OK");

                    return;
                }

                SetStatus(context.Response, HttpStatusCode.InternalServerError, "Internal Server Error");
            }
            catch { }
        }

        private void ProcessRequest()
        {
            try
            {
                var result = _listener.BeginGetContext(ListenerCallback, _listener);
                result.AsyncWaitHandle.WaitOne();
            }
            catch { }
        }

        public void Start()
        {
            StartAnnouncer();
            StartServer();
        }

        private void StartServer()
        {
            _listener.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    ProcessRequest();
                }
            });
        }
    }
}
