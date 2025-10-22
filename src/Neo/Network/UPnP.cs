// Copyright (C) 2015-2025 The Neo Project.
//
// UPnP.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.Exceptions;
using Neo.Extensions.Xml;
using Neo.Network.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace Neo.Network
{
    /// <summary>
    /// Provides methods for interacting with UPnP devices.
    /// </summary>
    internal static class UPnP
    {
        private static readonly IPAddress s_ipv4MulticastAddress = IPAddress.Parse("239.255.255.250");
        private static readonly IPAddress s_ipv6LinkLocalMulticastAddress = IPAddress.Parse("FF02::C");
        private static readonly IPAddress s_ipv6LinkSiteMulticastAddress = IPAddress.Parse("FF05::C");

        private static readonly Dictionary<Uri, UpnpNatDeviceInfo> s_devices = [];

        private static List<UdpClient> s_udpClients = null;

        private static readonly string[] s_serviceTypes = new[]{
            "WANIPConnection:2",
            "WANPPPConnection:2",
            "WANIPConnection:1",
            "WANPPPConnection:1"
        };

        private static string EncodeMessage(string serviceType, IPAddress address)
        {
            var fmtAddress = string.Format(
                address.AddressFamily == AddressFamily.InterNetwork ? "{0}" : "[{0}]",
                address);

            var s = "M-SEARCH * HTTP/1.1\r\n"
                    + "HOST: " + fmtAddress + ":1900\r\n"
                    + "MAN: \"ssdp:discover\"\r\n"
                    + "MX: 3\r\n"
                    + "ST: urn:schemas-upnp-org:service:{0}\r\n\r\n";
            //        + "ST:upnp:rootdevice\r\n\r\n";
            //        + "ST:ssdp:all\r\n\r\n";
            return string.Format(CultureInfo.InvariantCulture, s, serviceType);
        }

        private static IEnumerable<IPAddress> UnicastAddresses() =>
            IpAddresses(i => i.UnicastAddresses.Select(s => s.Address));

        private static IEnumerable<IPAddress> IpAddresses(Func<IPInterfaceProperties, IEnumerable<IPAddress>> ipExtractor) =>
            NetworkInterface.GetAllNetworkInterfaces()
            .Where(w => w.OperationalStatus == OperationalStatus.Up || w.OperationalStatus == OperationalStatus.Unknown)
            .SelectMany(sm => ipExtractor(sm.GetIPProperties()))
            .Where(w => w.AddressFamily == AddressFamily.InterNetwork || w.AddressFamily == AddressFamily.InterNetworkV6);

        private static List<UdpClient> CreateUdpClients()
        {
            var clients = new List<UdpClient>();

            try
            {
                var ips = UnicastAddresses();
                foreach (var ipAddress in ips)
                {
                    try
                    {
                        var ipEndPoint = new IPEndPoint(ipAddress, 0);
                        var udpClient = new UdpClient(ipEndPoint);
                        clients.Add(udpClient);
                    }
                    catch (Exception ex)
                    {
                        var error = string.Format("[{0}] {1}", ipAddress, ex.Message);
                        Utility.Log(nameof(UPnP), LogLevel.Warning, error);
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.Log(nameof(UPnP), LogLevel.Warning, ex.Message);
                clients.Add(new UdpClient(0));
            }

            return clients;
        }

        private static bool IsValidControllerService(string serviceType) =>
            s_serviceTypes
                .Select(s => string.Format("urn:schemas-upnp-org:service:{0}", s))
                .Where(w => serviceType.Contains(w, StringComparison.OrdinalIgnoreCase))
                .Any();

        private static XmlDocument ReadXmlResponse(HttpContent response)
        {
            using var reader = new StreamReader(response.ReadAsStream(), Encoding.UTF8);
            var servicesXml = reader.ReadToEnd();
            var xmldoc = new XmlDocument();
            xmldoc.LoadXml(servicesXml);
            return xmldoc;
        }

        [return: MaybeNull]
        private static UpnpNatDeviceInfo BuildUpnpNatDeviceInfo(IPAddress localAddress, Uri location)
        {
            var request = new HttpClient()
            {
                DefaultRequestVersion = new(1, 1),
                Timeout = TimeSpan.FromSeconds(30),
            };

            request.DefaultRequestHeaders.AcceptLanguage.Add(new("en"));

            using var response = request.GetAsync(location).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode == false)
                throw new Exception($"Couldn't get services list: {response.StatusCode}");

            var xmlDoc = ReadXmlResponse(response.Content);
            var ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");

            var services = xmlDoc.SelectNodes("//ns:service", ns);
            foreach (XmlNode service in services)
            {
                var serviceType = service.GetXmlElementText("serviceType");
                if (IsValidControllerService(serviceType) == false) continue;

                var serviceControlUrl = service.GetXmlElementText("controlURL");
                return new(localAddress, location, serviceControlUrl, serviceType);
            }

            return null;
        }

        private static void Discover(UdpClient client)
        {
            Discover(client, IPAddress.Broadcast);
            Discover(client, s_ipv4MulticastAddress);
            if (Socket.OSSupportsIPv6)
            {
                Discover(client, s_ipv6LinkLocalMulticastAddress);
                Discover(client, s_ipv6LinkSiteMulticastAddress);
            }
        }

        private static void Discover(UdpClient client, IPAddress address)
        {
            var searchEndPoint = new IPEndPoint(address, 1900);

            foreach (var serviceType in s_serviceTypes)
            {
                var dataX = EncodeMessage(serviceType, address);
                var data = Encoding.ASCII.GetBytes(dataX);

                for (var i = 0; i < 3; i++)
                    client.Send(data, data.Length, searchEndPoint);
            }
        }

        private static void Discover()
        {
            s_udpClients = CreateUdpClients();

            foreach (var socket in s_udpClients)
            {
                socket.TryCatch<UdpClient, Exception>(
                    Discover,
                    (c, e) =>
                    {
                        var error = string.Format("[{0}] {1}", (IPEndPoint)c.Client.LocalEndPoint, e.Message);
                        Utility.Log(nameof(UPnP), LogLevel.Warning, error);
                    }
                );
            }
        }

        private static void Receive()
        {
            foreach (var client in s_udpClients.Where(w => w.Available > 0))
            {
                var localHost = ((IPEndPoint)client.Client.LocalEndPoint).Address;
                var receivedFrom = new IPEndPoint(IPAddress.None, 0);
                var buffer = client.Receive(ref receivedFrom);
                _ = AnalyzeReceivedResponse(localHost, buffer, receivedFrom);
            }
        }

        private static void CloseUdpClients()
        {
            foreach (var udpClient in s_udpClients)
                udpClient.Close();
        }

        [return: MaybeNull]
        private static UpnpNatDeviceInfo AnalyzeReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endPoint)
        {
            var dataString = Encoding.UTF8.GetString(response);
            var message = new DiscoveryResponseMessage(dataString);
            var serviceType = message["ST"];

            if (IsValidControllerService(serviceType) == false)
                return null;

            var location = message["Location"] ?? message["AL"];
            var locationUri = new Uri(location);
            var deviceInfo = BuildUpnpNatDeviceInfo(localAddress, locationUri);

            _ = s_devices.TryAdd(locationUri, deviceInfo);

            return deviceInfo;
        }

        public static IReadOnlyDictionary<Uri, UpnpNatDeviceInfo> Search()
        {
            Discover();
            Receive();
            CloseUdpClients();

            return s_devices;
        }
    }
}
