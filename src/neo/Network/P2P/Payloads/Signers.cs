using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Signers : ISerializable, IEnumerable<Signer>
    {
        private Signer[] _signers;
        private Dictionary<UInt160, Signer> _cache;

        public int Size => Entries.GetVarSize();

        public Signer[] Entries
        {
            get => _signers;
            set
            {
                var cache = value.ToDictionary(u => u.Account);
                if (cache.Count != value.Length)
                    throw new FormatException();

                _cache = cache;
                _signers = value;
                Sender = value.Length > 0 ? value[0].Account : UInt160.Zero;
            }
        }

        public Signer this[UInt160 hash]
        {
            get
            {
                if (_cache.TryGetValue(hash, out var value)) return value;
                return null;
            }
        }

        /// <summary>
        /// Correspond with the first entry of Signers
        /// </summary>
        public UInt160 Sender { get; private set; } = UInt160.Zero;

        /// <summary>
        /// Keys
        /// </summary>
        public IEnumerable<UInt160> Keys => _cache.Keys;

        /// <summary>
        /// Count
        /// </summary>
        public int Count => _signers.Length;

        public void Deserialize(BinaryReader reader)
        {
            Entries = reader.ReadSerializableArray<Signer>(byte.MaxValue);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Entries);
        }

        public Signers() { }

        /// <summary>
        /// Constructor with values
        /// </summary>
        /// <param name="entries">Entries</param>
        public Signers(params Signer[] entries)
        {
            Entries = entries;
        }

        public JObject ToJson()
        {
            return Entries.Select(p => p.ToJson()).ToArray();
        }

        /// <summary>
        /// Try get value
        /// </summary>
        /// <param name="hash">Hash</param>
        /// <param name="signer">Signer</param>
        /// <returns></returns>
        public bool TryGetValue(UInt160 hash, out Signer signer)
        {
            if (_cache.TryGetValue(hash, out signer)) return true;
            return false;
        }

        public IEnumerator<Signer> GetEnumerator()
        {
            return (IEnumerator<Signer>)_signers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _signers.GetEnumerator();
        }

        public static implicit operator Signers(Signer signer) => new Signers(signer);
        public static implicit operator Signers(Signer[] signers) => new Signers(signers);
    }
}
