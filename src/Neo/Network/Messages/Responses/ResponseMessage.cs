// Copyright (C) 2015-2025 The Neo Project.
//
// ResponseMessageBase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Xml;

namespace Neo.Network.Messages.Responses
{
    internal abstract class ResponseMessage
    {
        private readonly XmlDocument _document;
        protected string _serviceType;
        private readonly string _typeName;

        protected ResponseMessage(XmlDocument response, string serviceType, string typeName)
        {
            _document = response;
            _serviceType = serviceType;
            _typeName = typeName;
        }

        protected XmlNode GetNode()
        {
            var nsm = new XmlNamespaceManager(_document.NameTable);
            nsm.AddNamespace("responseNs", _serviceType);

            var typeName = _typeName;
            var messageName = typeName.Substring(0, typeName.Length - "Message".Length);
            var node = _document.SelectSingleNode("//responseNs:" + messageName, nsm) ??
                throw new InvalidOperationException("The response is invalid: " + messageName);

            return node;
        }
    }
}
