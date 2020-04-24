using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NNS
{
    partial class NNSContract
    {
        public class Resolver
        {
            public UInt160 Manager { set; get; }
            public string Name { set; get; }
            public UInt160 Address { set; get; }
            public string Text { set; get; }
            public RecordType RecordType { set; get; }

            public Resolver(string name, UInt160 address, UInt160 manager)
            {
                this.Name = name;
                this.Address = address;
                this.Manager = manager;
            }

            public void setText(string text, RecordType recordType)
            {
                if (recordType == RecordType.A)
                {
                    Address = UInt160.Parse(text);
                    Text = text;
                }
                else if (recordType == RecordType.TXT)
                {
                    Text = text;
                }
            }

            public string resolve()
            {
                return toString();
            }

            public string toString()
            {
                return "Text:" + Text + " RecordType" + RecordType;
            }
        }

        public enum RecordType : byte
        {
            A = 0x00,
            CNAME = 0x01,
            TXT = 0x10,
            NS = 0x11
        }
    }
}
