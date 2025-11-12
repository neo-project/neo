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

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace Neo.Network;

/// <summary>
/// Provides methods for interacting with UPnP devices.
/// </summary>
public static class UPnP
{
    private static string? s_serviceUrl;

    /// <summary>
    /// Gets or sets the timeout for discovering the UPnP device.
    /// </summary>
    public static TimeSpan TimeOut { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Sends an Udp broadcast message to discover the UPnP device.
    /// </summary>
    /// <returns><see langword="true"/> if the UPnP device is successfully discovered; otherwise, <see langword="false"/>.</returns>
    public static bool Discover()
    {
        using Socket s = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        s.ReceiveTimeout = (int)TimeOut.TotalMilliseconds;
        s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
        var req = "M-SEARCH * HTTP/1.1\r\n" +
        "HOST: 239.255.255.250:1900\r\n" +
        "ST:upnp:rootdevice\r\n" +
        "MAN:\"ssdp:discover\"\r\n" +
        "MX:3\r\n\r\n";
        var data = Encoding.ASCII.GetBytes(req);
        var ipe = new IPEndPoint(IPAddress.Broadcast, 1900);
        var start = DateTime.UtcNow;

        try
        {
            s.SendTo(data, ipe);
            s.SendTo(data, ipe);
            s.SendTo(data, ipe);
        }
        catch
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[0x1000];

        do
        {
            int length;
            try
            {
                length = s.Receive(buffer);

                var resp = Encoding.ASCII.GetString(buffer[..length]).ToLowerInvariant();
                if (resp.Contains("upnp:rootdevice"))
                {
                    resp = resp[(resp.IndexOf("location:") + 9)..];
                    resp = resp[..resp.IndexOf('\r')].Trim();
                    if (!string.IsNullOrEmpty(s_serviceUrl = GetServiceUrl(resp)))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        while (DateTime.UtcNow - start < TimeOut);

        return false;
    }

    private static string? GetServiceUrl(string resp)
    {
        try
        {
            var desc = new XmlDocument() { XmlResolver = null };
            desc.Load(resp);
            var nsMgr = new XmlNamespaceManager(desc.NameTable);
            nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
            var typen = desc.SelectSingleNode("//tns:device/tns:deviceType/text()", nsMgr);
            if (!typen!.Value!.Contains("InternetGatewayDevice"))
                return null;
            var node = desc.SelectSingleNode("//tns:service[contains(tns:serviceType,\"WANIPConnection\")]/tns:controlURL/text()", nsMgr);
            if (node == null)
                return null;
            var eventnode = desc.SelectSingleNode("//tns:service[contains(tns:serviceType,\"WANIPConnection\")]/tns:eventSubURL/text()", nsMgr);
            return CombineUrls(resp, node.Value!);
        }
        catch { return null; }
    }

    private static string CombineUrls(string resp, string p)
    {
        var n = resp.IndexOf("://");
        n = resp.IndexOf('/', n + 3);
        return resp[..n] + p;
    }

    /// <summary>
    /// Attempt to create a port forwarding.
    /// </summary>
    /// <param name="port">The port to forward.</param>
    /// <param name="protocol">The <see cref="ProtocolType"/> of the port.</param>
    /// <param name="description">The description of the forward.</param>
    public static void ForwardPort(int port, ProtocolType protocol, string description)
    {
        if (string.IsNullOrEmpty(s_serviceUrl))
            throw new InvalidOperationException("UPnP service is not available. Please call UPnP.Discover() and ensure a UPnP device is detected on the network before attempting to forward ports.");
        SOAPRequest(s_serviceUrl, "<u:AddPortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
            "<NewRemoteHost></NewRemoteHost><NewExternalPort>" + port.ToString() + "</NewExternalPort><NewProtocol>" + protocol.ToString().ToUpper() + "</NewProtocol>" +
            "<NewInternalPort>" + port.ToString() + "</NewInternalPort><NewInternalClient>" + Dns.GetHostAddresses(Dns.GetHostName()).First(p => p.AddressFamily == AddressFamily.InterNetwork).ToString() +
            "</NewInternalClient><NewEnabled>1</NewEnabled><NewPortMappingDescription>" + description +
        "</NewPortMappingDescription><NewLeaseDuration>0</NewLeaseDuration></u:AddPortMapping>", "AddPortMapping");
    }

    /// <summary>
    /// Attempt to delete a port forwarding.
    /// </summary>
    /// <param name="port">The port to forward.</param>
    /// <param name="protocol">The <see cref="ProtocolType"/> of the port.</param>
    public static void DeleteForwardingRule(int port, ProtocolType protocol)
    {
        if (string.IsNullOrEmpty(s_serviceUrl))
            throw new InvalidOperationException("UPnP service is not available. Please call UPnP.Discover() and ensure a UPnP device is detected on the network before attempting to delete port forwarding rules.");
        SOAPRequest(s_serviceUrl,
        "<u:DeletePortMapping xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
        "<NewRemoteHost>" +
        "</NewRemoteHost>" +
        "<NewExternalPort>" + port + "</NewExternalPort>" +
        "<NewProtocol>" + protocol.ToString().ToUpper() + "</NewProtocol>" +
        "</u:DeletePortMapping>", "DeletePortMapping");
    }

    /// <summary>
    /// Attempt to get the external IP address of the local host.
    /// </summary>
    /// <returns>The external IP address of the local host.</returns>
    public static IPAddress GetExternalIP()
    {
        if (string.IsNullOrEmpty(s_serviceUrl))
            throw new InvalidOperationException("UPnP service is not available. Please call UPnP.Discover() and ensure a UPnP device is detected on the network before attempting to retrieve the external IP address.");
        var xdoc = SOAPRequest(s_serviceUrl, "<u:GetExternalIPAddress xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">" +
        "</u:GetExternalIPAddress>", "GetExternalIPAddress");
        var nsMgr = new XmlNamespaceManager(xdoc.NameTable);
        nsMgr.AddNamespace("tns", "urn:schemas-upnp-org:device-1-0");
        var ip = xdoc.SelectSingleNode("//NewExternalIPAddress/text()", nsMgr)!.Value!;
        return IPAddress.Parse(ip);
    }

    private static XmlDocument SOAPRequest(string url, string soap, string function)
    {
        var req = "<?xml version=\"1.0\"?>" +
        "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
        "<s:Body>" +
        soap +
        "</s:Body>" +
        "</s:Envelope>";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("SOAPACTION", $"\"urn:schemas-upnp-org:service:WANIPConnection:1#{function}\"");
        request.Headers.Add("Content-Type", "text/xml; charset=\"utf-8\"");
        request.Content = new StringContent(req);
        using var http = new HttpClient();
        using var response = http.SendAsync(request).GetAwaiter().GetResult();
        using var stream = response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        var resp = new XmlDocument() { XmlResolver = null };
        resp.Load(stream);
        return resp;
    }
}
