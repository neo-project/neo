using Akka.Actor;
using Akka.Configuration;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Actors;
using Neo.Ledger;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Consensus
{
    public abstract class ConsensusTimeProvider
    {
        private static ConsensusTimeProvider current =
            DefaultTimeProvider.Instance;

        public static ConsensusTimeProvider Current
        {
           get { return ConsensusTimeProvider.current; }
           set
           {
               if (value == null)
               {
                   throw new ArgumentNullException("value");
               }
               ConsensusTimeProvider.current = value;
           }
       }

       public abstract DateTime UtcNow { get; }

       public static void ResetToDefault()
       {
           ConsensusTimeProvider.current = DefaultTimeProvider.Instance;
       }
    }


    internal class DefaultTimeProvider : ConsensusTimeProvider
    {
        private static DefaultTimeProvider current = new DefaultTimeProvider();
        public static DefaultTimeProvider Instance
        {
             get { return DefaultTimeProvider.current; }
             set
             {
                 if (value == null)
                 {
                     throw new ArgumentNullException("value");
                 }
                 DefaultTimeProvider.current = value;
             }
        }

        public override DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
