using System;

namespace AntShares.Core.Scripts
{
    internal static class InterfaceEngine
    {
        public static bool ExecuteOp(Stack stack, Stack altStack, InterfaceOp code)
        {
            switch (code)
            {
                case InterfaceOp.IOP_SYSTEMTIME:
                    return OpSystemTime(stack, altStack);
                case InterfaceOp.IOP_CHAINHEIGHT:
                    return OpChainHeight(stack, altStack);
                default:
                    return false;
            }
        }

        private static bool OpSystemTime(Stack stack, Stack altStack)
        {
            stack.Push(DateTime.Now.ToTimestamp());
            return true;
        }

        private static bool OpChainHeight(Stack stack, Stack altStack)
        {
            if (Blockchain.Default == null) return false;
            stack.Push(Blockchain.Default.Height);
            return true;
        }
    }
}
