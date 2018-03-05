using Neo.IO;
using Neo.Network.Payloads;
using System.Collections.Generic;

namespace Neo.Network.Queues
{
    public class ReceiveMessageQueue : MessageQueue<ParsedMessage>
    {
        bool IsHighPriorityMessage(string command, ISerializable payload)
        {
            switch (command)
            {
                case "block":
                case "consensus":
                case "getheaders":
                case "getblocks":
                    {
                        return true;
                    }
                case "invpool":
                    {
                        return true;
                    }
                case "inv":
                    {
                        if (payload is InvPayload inv && inv.Type != InventoryType.TX)
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
            Queue<ParsedMessage> message_queue =
                IsHighPriorityMessage(command, payload) ?
                QueueHigh : QueueLow;

            lock (message_queue)
            {
                message_queue.Enqueue(new ParsedMessage(command, payload));
            }
        }
    }
}