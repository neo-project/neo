// Copyright (C) 2015-2024 The Neo Project.
//
// Result.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace LevelDB
{
    //public class Result : LevelDBHandle, IEnumerable<byte>, IEnumerable<int>
    //{
    //    private int length;
    //    public Result(IntPtr handle, int length)
    //    {
    //        this.Handle = handle;
    //        this.length = length;

    //        BitConverter.ToInt32(null, 0);
    //    }


    //    public byte[] Get(byte[] key, ReadOptions options)
    //    {
    //        IntPtr error;
    //        int length;
    //        var v = LevelDBInterop.leveldb_get(this.Handle, options.Handle, key, key.Length, out length, out error);
    //        Throw(error);

    //        if (v != IntPtr.Zero)
    //        {
    //            var bytes = new byte[length];
    //            Marshal.Copy(v, bytes, 0, length);
    //            Marshal.FreeHGlobal(v);
    //            return bytes;
    //        }
    //        return null;
    //    }
    //}
}
