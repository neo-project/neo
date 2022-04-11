// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Runtime.InteropServices;

namespace Neo.Cryptography
{
    /// <summary>
    /// A bls12_381 helper class 
    /// </summary>
    public static class Bls12381
    {
        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gt_add(IntPtr gt1, IntPtr gt2);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gt_mul(IntPtr gt, UInt64 multi);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gt_neg(IntPtr gt);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g1_add(IntPtr g1_1, IntPtr g1_2);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g1_mul(IntPtr g1, UInt64 multi);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g1_neg(IntPtr g1);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g2_add(IntPtr g2_1, IntPtr g2_2);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g2_mul(IntPtr g2, UInt64 multi);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g2_neg(IntPtr g2);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern void gt_dispose(IntPtr rawPtr);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern void g1_dispose(IntPtr rawPtr);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern void g2_dispose(IntPtr rawPtr);

        [DllImport("bls12381", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g1_g2_pairing(IntPtr g1, IntPtr g2);

        /// <summary>
        /// Add operation of two gt point
        /// </summary>
        /// <param name="p1Binary">Gt point1 as byteArray</param>
        /// <param name="p2Binary">Gt point2 as byteArray</param>
        /// <returns></returns>
        public static byte[] PointAdd(byte[] p1Binary, byte[] p2Binary)
        {
            GObject p1 = new GObject(p1Binary);
            GObject p2 = new GObject(p2Binary);
            try
            {
                return GObject.Add(p1, p2).ToByteArray((int)p1.type);
            }
            catch (Exception e)
            {
                throw new Exception($"Bls12381 operation fault, type:dll-add, error:{e}");
            }
        }

        /// <summary>
        /// Mul operation of gt point and mulitiplier
        /// </summary>
        /// <param name="pBinary">Gt point as byteArray</param>
        /// <param name="multi">Multiplier</param>
        /// <returns></returns>
        public static byte[] PointMul(byte[] pBinary, long multi)
        {
            try
            {
                GObject p = multi < 0 ? new GObject(pBinary).Neg() : new GObject(pBinary);
                var x = Convert.ToUInt64(Math.Abs(multi));
                return GObject.Mul(p, x).ToByteArray((int)p.type);
            }
            catch (Exception e)
            {
                throw new Exception($"Bls12381 operation fault, type:dll-mul, error:{e}");
            }
        }

        /// <summary>
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1Binary">Gt point1 as byteArray</param>
        /// <param name="g2Binary">Gt point2 as byteArray</param>
        /// <returns></returns>
        public static byte[] PointPairing(byte[] g1Binary, byte[] g2Binary)
        {
            try
            {
                return g1_g2_pairing(new GObject(g1Binary).ptr, new GObject(g2Binary).ptr).ToByteArray(576);
            }
            catch (Exception e)
            {
                throw new Exception($"Bls12381 operation fault, type:dll-pairing, error:{e}");
            }
        }
    }

    internal class GObject
    {
        public IntPtr ptr;
        public GType type;

        public GObject(GType t, IntPtr ptr)
        {
            this.ptr = ptr;
            this.type = t;
        }
        public GObject(byte[] g)
        {
            int len = g.Length;
            if (len is (int)GType.G1 or (int)GType.G2 or (int)GType.Gt)
            {
                IntPtr tmp = Marshal.AllocHGlobal(len);
                Marshal.Copy(g, 0, tmp, len);
                this.type = (GType)len;
                this.ptr = tmp;
            }
            else throw new Exception($"Bls12381 operation falut,type:format,error:valid point length");
        }

        public static IntPtr Add(GObject p1, GObject p2)
        {
            if (p1.type != p2.type)
            {
                throw new Exception($"Bls12381 operation fault, type:format, error:type mismatch");
            }
            return p1.type switch
            {
                GType.G1 => Bls12381.g1_add(p1.ptr, p2.ptr),
                GType.G2 => Bls12381.g2_add(p1.ptr, p2.ptr),
                GType.Gt => Bls12381.gt_add(p1.ptr, p2.ptr),
                _ => throw new Exception($"Bls12381 operation fault,type:format,error:valid point length")
            };
        }

        public static GObject Neg(GObject p)
        {
            return p.Neg();
        }

        public GObject Neg()
        {
            ptr = type switch
            {
                GType.G1 => Bls12381.g1_neg(ptr),
                GType.G2 => Bls12381.g2_neg(ptr),
                GType.Gt => Bls12381.gt_neg(ptr),
                _ => throw new Exception($"Bls12381 operation fault, type:format, error:valid point length")
            };
            return this;
        }

        public static IntPtr Mul(GObject p, UInt64 x)
        {
            return p.type switch
            {
                GType.G1 => Bls12381.g1_mul(p.ptr, x),
                GType.G2 => Bls12381.g2_mul(p.ptr, x),
                GType.Gt => Bls12381.gt_mul(p.ptr, x),
                _ => throw new Exception($"Bls12381 operation falut,type:format,error:valid point length")
            };
        }

        ~GObject()
        {
            try
            {
                switch (type)
                {
                    case GType.G1:
                        Bls12381.g1_dispose(ptr);
                        break;
                    case GType.G2:
                        Bls12381.g2_dispose(ptr);
                        break;
                    case GType.Gt:
                        Bls12381.gt_dispose(ptr);
                        break;
                    default:
                        throw new Exception($"Bls12381 operation fault, type:format, error:type mismatch");
                }
            }
            catch (Exception)
            {
                throw new Exception($"Bls12381 operation falut,type:format,error:dispose failed");
            }
        }
    }

    internal enum GType
    {
        G1 = 96,
        G2 = 192,
        Gt = 576
    }
}
