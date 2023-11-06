// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics;
using Neo.SmartContract.Manifest;

namespace Neo.SmartContract.Native
{
    [DebuggerDisplay("{Descriptor.Name}")]
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = true)]
    internal class ContractEventAttribute : Attribute
    {
        public int Order { get; init; }
        public ContractEventDescriptor Descriptor { get; set; }
        public Hardfork? ActiveIn { get; init; } = null;

        public ContractEventAttribute(int order, string name, string arg1Name, ContractParameterType arg1Value)
        {
            Order = order;
            Descriptor = new ContractEventDescriptor()
            {
                Name = name,
                Parameters = new ContractParameterDefinition[]
                {
                    new ContractParameterDefinition()
                    {
                        Name = arg1Name,
                        Type = arg1Value
                    }
                }
            };
        }

        public ContractEventAttribute(int order, string name,
            string arg1Name, ContractParameterType arg1Value,
            string arg2Name, ContractParameterType arg2Value)
        {
            Order = order;
            Descriptor = new ContractEventDescriptor()
            {
                Name = name,
                Parameters = new ContractParameterDefinition[]
                {
                    new ContractParameterDefinition()
                    {
                        Name = arg1Name,
                        Type = arg1Value
                    },
                    new ContractParameterDefinition()
                    {
                        Name = arg2Name,
                        Type = arg2Value
                    }
                }
            };
        }

        public ContractEventAttribute(int order, string name,
           string arg1Name, ContractParameterType arg1Value,
           string arg2Name, ContractParameterType arg2Value,
           string arg3Name, ContractParameterType arg3Value
           )
        {
            Order = order;
            Descriptor = new ContractEventDescriptor()
            {
                Name = name,
                Parameters = new ContractParameterDefinition[]
                {
                    new ContractParameterDefinition()
                    {
                        Name = arg1Name,
                        Type = arg1Value
                    },
                    new ContractParameterDefinition()
                    {
                        Name = arg2Name,
                        Type = arg2Value
                    },
                    new ContractParameterDefinition()
                    {
                        Name = arg3Name,
                        Type = arg3Value
                    }
                }
            };
        }

        public ContractEventAttribute(int order, string name,
            string arg1Name, ContractParameterType arg1Value,
            string arg2Name, ContractParameterType arg2Value,
            string arg3Name, ContractParameterType arg3Value,
            string arg4Name, ContractParameterType arg4Value
            )
        {
            Order = order;
            Descriptor = new ContractEventDescriptor()
            {
                Name = name,
                Parameters = new ContractParameterDefinition[]
                {
                    new ContractParameterDefinition()
                    {
                        Name = arg1Name,
                        Type = arg1Value
                    },
                    new ContractParameterDefinition()
                    {
                        Name = arg2Name,
                        Type = arg2Value
                    },
                    new ContractParameterDefinition()
                    {
                        Name = arg3Name,
                        Type = arg3Value
                    },
                    new ContractParameterDefinition()
                    {
                        Name = arg4Name,
                        Type = arg4Value
                    }
                }
            };
        }
    }
}
