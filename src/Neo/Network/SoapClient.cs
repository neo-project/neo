// Copyright (C) 2015-2025 The Neo Project.
//
// SoapClient.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions.Xml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Xml;

namespace Neo.Network
{
    internal class SoapClient
    {
        private readonly string _serviceType;
        private readonly Uri _url;

        public SoapClient(Uri url, string serviceType)
        {
            _url = url;
            _serviceType = serviceType;
        }

        public XmlDocument Invoke(string operationName, IDictionary<string, object> args)
        {
            var messageBody = BuildMessageBody(operationName, args);
            var request = BuildHttpClient(operationName);
            var bodyContent = new StringContent(messageBody, Encoding.UTF8, "text/xml");

            using var response = request.PostAsync(_url, bodyContent).GetAwaiter().GetResult();

            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var responseXml = GetXmlDocument(responseBody);

            return responseXml;
        }

        private HttpClient BuildHttpClient(string operationName)
        {
            var request = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            request.DefaultRequestHeaders.TryAddWithoutValidation("SOAPACTION", "\"" + _serviceType + "#" + operationName + "\"");
            return request;
        }

        private string BuildMessageBody(string operationName, IEnumerable<KeyValuePair<string, object>> args)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<s:Envelope ");
            sb.AppendLine("   xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
            sb.AppendLine("   s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">");
            sb.AppendLine("   <s:Body>");
            sb.AppendLine("	  <u:" + operationName + " xmlns:u=\"" + _serviceType + "\">");

            foreach (var a in args)
            {
                sb.AppendLine("		 <" + a.Key + ">" + Convert.ToString(a.Value, CultureInfo.InvariantCulture) +
                              "</" + a.Key + ">");
            }

            sb.AppendLine("	  </u:" + operationName + ">");
            sb.AppendLine("   </s:Body>");
            sb.Append("</s:Envelope>\r\n\r\n");

            return sb.ToString();
        }

        private static XmlDocument GetXmlDocument(string response)
        {
            XmlNode node;
            var doc = new XmlDocument();
            doc.LoadXml(response);

            var nsm = new XmlNamespaceManager(doc.NameTable);

            // Error messages should be found under this namespace
            nsm.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");

            // Check to see if we have a fault code message.
            if ((node = doc.SelectSingleNode("//errorNs:UPnPError", nsm)) != null)
            {
                var errorMessage = node.GetXmlElementText("errorDescription");
                throw new Exception(errorMessage);
            }

            return doc;
        }
    }
}
