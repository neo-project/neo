// Copyright (C) 2015-2021 The Neo Project.
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
        /// <param name="gt1_bytes">gt1 point as byteArray</param>
        /// <param name="gt2_bytes">gt2 point as byteArray</param>
        /// <returns></returns>
        public static byte[] Point_Add(byte[] p1_bytes,byte[] p2_bytes)
        {
            GObject p1 = new GObject(p1_bytes);
            GObject p2 = new GObject(p2_bytes);
            IntPtr result = IntPtr.Zero;
            try
            {
                result = GObject.Add(p1, p2);
            }
            catch (Exception e)
            {
                throw new Exception("Bls12 dll error: Add error:" + e);
            }
            byte[] buffer = result.ToByteArray((int)p1.type);
            return buffer;
        }

        /// <summary>
        /// Mul operation of gt point and integer
        /// </summary>
        /// <param name="gt_bytes">gt point as byteArray</param>
        /// <param name="multi">the mulitiplier</param>
        /// <returns></returns>
        public static byte[] Point_Mul(byte[] p_bytes, long multi)
        {
            GObject p = new GObject(p_bytes);
            IntPtr result = IntPtr.Zero;
            try
            {
                UInt64 x = 0;
                if (multi < 0)
                {
                    x = Convert.ToUInt64(-multi);
                    p = GObject.Neg(p);
                    result = GObject.Mul(p, x);
                }
                else
                {
                    x = Convert.ToUInt64(multi);
                    result = GObject.Mul(p, x);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Bls12 dll error: Mul error:" + e );
            }
            byte[] buffer = result.ToByteArray((int)p.type);
            return buffer;
        }

        /// <summary>
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1_bytes"></param>
        /// <param name="g2_bytes"></param>
        /// <returns></returns>
        public static byte[] Point_Pairing(byte[] g1_bytes, byte[] g2_bytes)
        {
            GObject g1 = new GObject(g1_bytes);
            GObject g2 = new GObject(g2_bytes);
            IntPtr result = IntPtr.Zero;
            try
            {
                result = g1_g2_pairing(g1.ptr, g2.ptr);
            }
            catch (Exception e)
            {
                throw new Exception("Bls12 dll error: Pairing error:" + e);
            }
            byte[] buffer = result.ToByteArray(576);
            return buffer;
        }
    }

    internal class GObject
    {
        public IntPtr ptr;
        public GType type;

        public GObject(GType t,IntPtr ptr)
        {
            this.ptr = ptr;
            this.type = t;
        }
        public GObject(byte[] g)
        {
            int len = g.Length;
            if (len == (int)GType.G1 || len == (int)GType.G2 || len == (int)GType.Gt)
            {
                IntPtr tmp = Marshal.AllocHGlobal(len);
                Marshal.Copy(g, 0, tmp, len);
                this.type = (GType)len;
                this.ptr = tmp;
            }
            else throw new Exception("Bls12_381:valid point length");
        }

        public static IntPtr Add(GObject p1,GObject p2)
        {
            IntPtr result = IntPtr.Zero;
            if (p1.type != p2.type)
            {
                throw new Exception("Bls12_381:type missmatch");
            }
            if(p1.type == GType.G1)
            {
                result = Bls12381.g1_add(p1.ptr, p2.ptr);
            }else if(p1.type == GType.G2)
            {
                result = Bls12381.g2_add(p1.ptr, p2.ptr);
            }else if(p1.type == GType.Gt)
            {
                result = Bls12381.gt_add(p1.ptr, p2.ptr);
            }
            else
            {
                throw new Exception("Bls12_381:valid point length");
            }
            return result;
        }

        public static GObject Neg(GObject p)
        {
            IntPtr result = IntPtr.Zero;
            if (p.type == GType.G1)
            {
                result = Bls12381.g1_neg(p.ptr);
            }
            else if (p.type == GType.G2)
            {
                result = Bls12381.g2_neg(p.ptr);
            }
            else if (p.type == GType.Gt)
            {
                result = Bls12381.gt_neg(p.ptr);
            }
            else
            {
                throw new Exception("Bls12_381:valid point length");
            }
            GObject x = new GObject(p.type, result);
            return x;
        }

        public static IntPtr Mul(GObject p, UInt64 x)
        {
            IntPtr result = IntPtr.Zero;
            if (p.type == GType.G1)
            {
                result = Bls12381.g1_mul(p.ptr, x);
            }
            else if (p.type == GType.G2)
            {
                result = Bls12381.g2_mul(p.ptr, x);
            }
            else if (p.type == GType.Gt)
            {
                result = Bls12381.gt_mul(p.ptr, x);
            }
            else
            {
                throw new Exception("Bls12_381:valid point length");
            }
            return result;
        }

        ~GObject()
        {
            try
            {
                if(type == GType.G1)
                {
                    Bls12381.g1_dispose(ptr);
                }
                else if(type == GType.G2)
                {
                    Bls12381.g2_dispose(ptr);
                }
                else if(type == GType.Gt)
                {
                    Bls12381.gt_dispose(ptr);
                }
                else
                {
                    throw new Exception("Bls12 error: point type missmatched");
                }
                
            }
            catch (Exception)
            {
                throw new Exception("Bls12 dll error: dispose failed");
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
