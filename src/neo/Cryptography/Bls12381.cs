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


        [DllImport("bls12381.dylib", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gt_add(IntPtr gt1, IntPtr gt2);

        [DllImport("bls12381.dylib", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gt_mul(IntPtr gt, int multi);

        [DllImport("bls12381.dylib", CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_dispose(IntPtr rawPtr);

        [DllImport("bls12381.dylib", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr g1_g2_pairing(IntPtr g1, IntPtr g2);


        /// <summary>
        /// Add operation of two gt point
        /// </summary>
        /// <param name="gt1_bytes">gt1 point as byteArray</param>
        /// <param name="gt2_bytes">gt2 point as byteArray</param>
        /// <returns></returns>
        public static byte[] Point_Add(byte[] gt1_bytes,byte[] gt2_bytes)
        {
            GObject gt1 = new GObject(gt1_bytes);

            GObject gt2 = new GObject(gt2_bytes);

            IntPtr result = IntPtr.Zero;

            try
            {
                result = gt_add(gt1.ptr, gt2.ptr);
            }
            catch (Exception e)
            {
                throw new Exception("Bls12 dll error: Add error:" + e);
            }
            

            byte[] buffer = result.ToByteArray((int)GType.Gt);

            return buffer;

        }

        /// <summary>
        /// Mul operation of gt point and integer
        /// </summary>
        /// <param name="gt_bytes">gt point as byteArray</param>
        /// <param name="multi">the mulitiplier</param>
        /// <returns></returns>
        public static byte[] Point_Mul(byte[] gt_bytes, int multi)
        {
            GObject gt = new GObject(gt_bytes);

            IntPtr result = IntPtr.Zero;
            try
            {
                result = gt_mul(gt.ptr, multi);
            }
            catch (Exception e)
            {
                throw new Exception("Bls12 dll error: Mul error:" + e );
            }
            byte[] buffer = result.ToByteArray((int)GType.Gt);

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
        public GObject(byte[] g)
        {
            int len = g.Length;

            if (len == (int)GType.G1 || len == (int)GType.G2 || len == (int)GType.Gt)
            {
                Marshal.Copy(g, 0, ptr, len);
                type = (GType)len;

            }

            else throw new Exception("Bls12_381:valid point length");
        }

        ~GObject()
        {
            try
            {
                Bls12381.g_dispose(ptr);
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
