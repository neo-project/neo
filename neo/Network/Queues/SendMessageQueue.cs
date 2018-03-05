using Neo.IO;
using Neo.Network.Payloads;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Network.Queues
{
    public class SendMessageQueue : MessageQueue<Message>
    {
        bool IsHighPriorityMessage(string command, ISerializable payload, out bool isSingle)
        {
            switch (command)
            {
                case "addr":
                case "getaddr":
                case "getblocks":
                case "getheaders":
                case "mempool": isSingle = true; break;
                default: isSingle = false; break;
            }

            switch (command)
            {
                case "alert":
                case "consensus":
                case "filteradd":
                case "filterclear":
                case "filterload":
                case "getaddr":
                case "mempool": return true;
                case "inv":
                    {
                        if (payload is InvPayload p && p.Type != InventoryType.TX)
                            return true;

                        return false;
                    }
                default: return false;
            }
        }
        /// <summary>
        /// Enqueue a message
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="payload">Payload</param>
        public void Enqueue(string command, ISerializable payload)
        {
            Queue<Message> message_queue =
                IsHighPriorityMessage(command, payload, out bool isSingle) ?
                QueueHigh : QueueLow;

            lock (message_queue)
            {
                if (!isSingle || message_queue.All(p => p.Command != command))
                {
                    message_queue.Enqueue(Message.Create(command, payload));
                }
            }
        }
    }
}