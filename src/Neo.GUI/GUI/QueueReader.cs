// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-gui is free software distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Neo.GUI
{
    internal class QueueReader : TextReader
    {
        private readonly Queue<string> queue = new Queue<string>();
        private string current;
        private int index;

        public void Enqueue(string str)
        {
            queue.Enqueue(str);
        }

        public override int Peek()
        {
            while (string.IsNullOrEmpty(current))
            {
                while (!queue.TryDequeue(out current))
                    Thread.Sleep(100);
                index = 0;
            }
            return current[index];
        }

        public override int Read()
        {
            int c = Peek();
            if (c != -1)
                if (++index >= current.Length)
                    current = null;
            return c;
        }
    }
}
