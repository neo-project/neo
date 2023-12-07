// Copyright (C) 2016-2023 The Neo Project.
//
// The neo-vm is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using T = Neo.VM.Types.StackItem;

namespace Neo.VM.StronglyConnectedComponents
{
    class Tarjan
    {
        private readonly IEnumerable<T> vertexs;
        private readonly LinkedList<HashSet<T>> components = new();
        private readonly Stack<T> stack = new();
        private int index = 0;

        public Tarjan(IEnumerable<T> vertexs)
        {
            this.vertexs = vertexs;
        }

        public LinkedList<HashSet<T>> Invoke()
        {
            foreach (var v in vertexs)
            {
                if (v.DFN < 0)
                {
                    StrongConnectNonRecursive(v);
                }
            }
            return components;
        }

        private void StrongConnect(T v)
        {
            v.DFN = v.LowLink = ++index;
            stack.Push(v);
            v.OnStack = true;

            foreach (T w in v.Successors)
            {
                if (w.DFN < 0)
                {
                    StrongConnect(w);
                    v.LowLink = Math.Min(v.LowLink, w.LowLink);
                }
                else if (w.OnStack)
                {
                    v.LowLink = Math.Min(v.LowLink, w.DFN);
                }
            }

            if (v.LowLink == v.DFN)
            {
                HashSet<T> scc = new(ReferenceEqualityComparer.Instance);
                T w;
                do
                {
                    w = stack.Pop();
                    w.OnStack = false;
                    scc.Add(w);
                } while (v != w);
                components.AddLast(scc);
            }
        }

        private void StrongConnectNonRecursive(T v)
        {
            Stack<(T node, T?, IEnumerator<T>?, int)> sstack = new();
            sstack.Push((v, null, null, 0));
            while (sstack.TryPop(out var state))
            {
                v = state.node;
                var (_, w, s, n) = state;
                switch (n)
                {
                    case 0:
                        v.DFN = v.LowLink = ++index;
                        stack.Push(v);
                        v.OnStack = true;
                        s = v.Successors.GetEnumerator();
                        goto case 2;
                    case 1:
                        v.LowLink = Math.Min(v.LowLink, w!.LowLink);
                        goto case 2;
                    case 2:
                        while (s!.MoveNext())
                        {
                            w = s.Current;
                            if (w.DFN < 0)
                            {
                                sstack.Push((v, w, s, 1));
                                v = w;
                                goto case 0;
                            }
                            else if (w.OnStack)
                            {
                                v.LowLink = Math.Min(v.LowLink, w.DFN);
                            }
                        }
                        if (v.LowLink == v.DFN)
                        {
                            HashSet<T> scc = new(ReferenceEqualityComparer.Instance);
                            do
                            {
                                w = stack.Pop();
                                w.OnStack = false;
                                scc.Add(w);
                            } while (v != w);
                            components.AddLast(scc);
                        }
                        break;
                }
            }
        }
    }
}
