// Copyright (C) 2015-2024 The Neo Project.
//
// Benchmark.OpCodes.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System.Text;

namespace Neo.VM
{
    [MemoryDiagnoser]
    public class BenchmarkOpCodes
    {
        #region Fields
        private const string Root = "../../../../../../../../../tests/Neo.VM.Tests/";
        private BenchmarkEngine[]? _opCodesArraysClearItems;
        private BenchmarkEngine[]? _opCodesArraysPack;
        private BenchmarkEngine[]? _opCodesArraysNewArrayT;
        private BenchmarkEngine[]? _opCodesArraysPickItem;
        private BenchmarkEngine[]? _opCodesArraysPackStruct;
        private BenchmarkEngine[]? _opCodesArraysSetItem;
        private BenchmarkEngine[]? _opCodesArraysNewStruct0;
        private BenchmarkEngine[]? _opCodesArraysReverseItems;
        private BenchmarkEngine[]? _opCodesArraysNewMap;
        private BenchmarkEngine[]? _opCodesArraysHasKey;
        private BenchmarkEngine[]? _opCodesArraysNewArray;
        private BenchmarkEngine[]? _opCodesArraysKeys;
        private BenchmarkEngine[]? _opCodesArraysAppend;
        private BenchmarkEngine[]? _opCodesArraysRemove;
        private BenchmarkEngine[]? _opCodesArraysPackMap;
        private BenchmarkEngine[]? _opCodesArraysValues;
        private BenchmarkEngine[]? _opCodesArraysNewArray0;
        private BenchmarkEngine[]? _opCodesArraysUnpack;
        private BenchmarkEngine[]? _opCodesArraysSize;
        private BenchmarkEngine[]? _opCodesArraysNewStruct;
        private BenchmarkEngine[]? _opCodesStackXDrop;
        private BenchmarkEngine[]? _opCodesStackReverseN;
        private BenchmarkEngine[]? _opCodesStackReverse4;
        private BenchmarkEngine[]? _opCodesStackClear;
        private BenchmarkEngine[]? _opCodesStackReverse3;
        private BenchmarkEngine[]? _opCodesStackRot;
        private BenchmarkEngine[]? _opCodesStackPick;
        private BenchmarkEngine[]? _opCodesStackNip;
        private BenchmarkEngine[]? _opCodesStackRoll;
        private BenchmarkEngine[]? _opCodesStackDepth;
        private BenchmarkEngine[]? _opCodesStackSwap;
        private BenchmarkEngine[]? _opCodesStackTuck;
        private BenchmarkEngine[]? _opCodesStackOver;
        private BenchmarkEngine[]? _opCodesStackDrop;
        private BenchmarkEngine[]? _opCodesSlotStsFld6;
        private BenchmarkEngine[]? _opCodesSlotInitSlot;
        private BenchmarkEngine[]? _opCodesSlotLdLoc4;
        private BenchmarkEngine[]? _opCodesSlotLdArg;
        private BenchmarkEngine[]? _opCodesSlotStArg0;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD0;
        private BenchmarkEngine[]? _opCodesSlotLdArg4;
        private BenchmarkEngine[]? _opCodesSlotLdArg5;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD1;
        private BenchmarkEngine[]? _opCodesSlotLdLoc;
        private BenchmarkEngine[]? _opCodesSlotStArg1;
        private BenchmarkEngine[]? _opCodesSlotLdLoc5;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD6;
        private BenchmarkEngine[]? _opCodesSlotLdArg2;
        private BenchmarkEngine[]? _opCodesSlotStsFld0;
        private BenchmarkEngine[]? _opCodesSlotLdLoc2;
        private BenchmarkEngine[]? _opCodesSlotStArg6;
        private BenchmarkEngine[]? _opCodesSlotLdLoc3;
        private BenchmarkEngine[]? _opCodesSlotStsFld1;
        private BenchmarkEngine[]? _opCodesSlotInitSSlot;
        private BenchmarkEngine[]? _opCodesSlotLdArg3;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD;
        private BenchmarkEngine[]? _opCodesSlotLdArg0;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD4;
        private BenchmarkEngine[]? _opCodesSlotStArg4;
        private BenchmarkEngine[]? _opCodesSlotLdLoc0;
        private BenchmarkEngine[]? _opCodesSlotStsFld2;
        private BenchmarkEngine[]? _opCodesSlotStsFld3;
        private BenchmarkEngine[]? _opCodesSlotLdLoc1;
        private BenchmarkEngine[]? _opCodesSlotStArg5;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD5;
        private BenchmarkEngine[]? _opCodesSlotLdArg1;
        private BenchmarkEngine[]? _opCodesSlotStArg2;
        private BenchmarkEngine[]? _opCodesSlotLdLoc6;
        private BenchmarkEngine[]? _opCodesSlotStArg;
        private BenchmarkEngine[]? _opCodesSlotStsFld4;
        private BenchmarkEngine[]? _opCodesSlotLdArg6;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD2;
        private BenchmarkEngine[]? _opCodesSlotLdSFLD3;
        private BenchmarkEngine[]? _opCodesSlotStLoc;
        private BenchmarkEngine[]? _opCodesSlotStsFld;
        private BenchmarkEngine[]? _opCodesSlotStsFld5;
        private BenchmarkEngine[]? _opCodesSlotStArg3;
        private BenchmarkEngine[]? _opCodesSpliceSubStr;
        private BenchmarkEngine[]? _opCodesSpliceCat;
        private BenchmarkEngine[]? _opCodesSpliceMemCpy;
        private BenchmarkEngine[]? _opCodesSpliceLeft;
        private BenchmarkEngine[]? _opCodesSpliceRight;
        private BenchmarkEngine[]? _opCodesSpliceNewBuffer;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally6;
        private BenchmarkEngine[]? _opCodesControlJmpLeL;
        private BenchmarkEngine[]? _opCodesControlJmpEqL;
        private BenchmarkEngine[]? _opCodesControlJmpLe;
        private BenchmarkEngine[]? _opCodesControlJmpNe;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally7;
        private BenchmarkEngine[]? _opCodesControlAssertMsg;
        private BenchmarkEngine[]? _opCodesControlJmpGt;
        private BenchmarkEngine[]? _opCodesControlJmpL;
        private BenchmarkEngine[]? _opCodesControlJmpIfL;
        private BenchmarkEngine[]? _opCodesControlTryFinally;
        private BenchmarkEngine[]? _opCodesControlJmpNeL;
        private BenchmarkEngine[]? _opCodesControlJmpIfNot;
        private BenchmarkEngine[]? _opCodesControlAbortMsg;
        private BenchmarkEngine[]? _opCodesControlCall;
        private BenchmarkEngine[]? _opCodesControlCallL;
        private BenchmarkEngine[]? _opCodesControlJmpGeL;
        private BenchmarkEngine[]? _opCodesControlRet;
        private BenchmarkEngine[]? _opCodesControlJmpGtL;
        private BenchmarkEngine[]? _opCodesControlJmpEq;
        private BenchmarkEngine[]? _opCodesControlSysCall;
        private BenchmarkEngine[]? _opCodesControlCallA;
        private BenchmarkEngine[]? _opCodesControlAssert;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally2;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally3;
        private BenchmarkEngine[]? _opCodesControlNop;
        private BenchmarkEngine[]? _opCodesControlJmpGe;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally10;
        private BenchmarkEngine[]? _opCodesControlJmpLt;
        private BenchmarkEngine[]? _opCodesControlJmpIf;
        private BenchmarkEngine[]? _opCodesControlAbort;
        private BenchmarkEngine[]? _opCodesControlJmpIfNotL;
        private BenchmarkEngine[]? _opCodesControlTryCatch;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally4;
        private BenchmarkEngine[]? _opCodesControlJmp;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally8;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally9;
        private BenchmarkEngine[]? _opCodesControlJmpLtL;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally;
        private BenchmarkEngine[]? _opCodesControlTryCatchFinally5;
        private BenchmarkEngine[]? _opCodesControlThrow;
        private BenchmarkEngine[]? _opCodesPushPushA;
        private BenchmarkEngine[]? _opCodesPushPushM1ToPush16;
        private BenchmarkEngine[]? _opCodesPushPushInt8ToPushInt256;
        private BenchmarkEngine[]? _opCodesPushPushData1;
        private BenchmarkEngine[]? _opCodesPushPushData2;
        private BenchmarkEngine[]? _opCodesPushPushNull;
        private BenchmarkEngine[]? _opCodesPushPushData4;
        private BenchmarkEngine[]? _opCodesArithmeticGe;
        private BenchmarkEngine[]? _opCodesArithmeticLt;
        private BenchmarkEngine[]? _opCodesArithmeticModMul;
        private BenchmarkEngine[]? _opCodesArithmeticNumNotEqual;
        private BenchmarkEngine[]? _opCodesArithmeticNot;
        private BenchmarkEngine[]? _opCodesArithmeticModPow;
        private BenchmarkEngine[]? _opCodesArithmeticLe;
        private BenchmarkEngine[]? _opCodesArithmeticShl;
        private BenchmarkEngine[]? _opCodesArithmeticGt;
        private BenchmarkEngine[]? _opCodesArithmeticPow;
        private BenchmarkEngine[]? _opCodesArithmeticNumEqual;
        private BenchmarkEngine[]? _opCodesArithmeticSign;
        private BenchmarkEngine[]? _opCodesArithmeticSqrt;
        private BenchmarkEngine[]? _opCodesArithmeticShr;
        private BenchmarkEngine[]? _opCodesBitwiseLogicOr;
        private BenchmarkEngine[]? _opCodesBitwiseLogicEqual;
        private BenchmarkEngine[]? _opCodesBitwiseLogicInvert;
        private BenchmarkEngine[]? _opCodesBitwiseLogicXor;
        private BenchmarkEngine[]? _opCodesBitwiseLogicNotEqual;
        private BenchmarkEngine[]? _opCodesBitwiseLogicAnd;
        private BenchmarkEngine[]? _opCodesTypesConvert;
        private BenchmarkEngine[]? _opCodesTypesIsType;
        private BenchmarkEngine[]? _opCodesTypesIsNull;
        #endregion

        [GlobalSetup]
        public void Setup()
        {
            _opCodesArraysClearItems = LoadJson("./Tests/OpCodes/Arrays/CLEARITEMS.json", OpCode.CLEARITEMS);
            _opCodesArraysPack = LoadJson("./Tests/OpCodes/Arrays/PACK.json", OpCode.PACK);
            _opCodesArraysNewArrayT = LoadJson("./Tests/OpCodes/Arrays/NEWARRAY_T.json", OpCode.NEWARRAY_T);
            _opCodesArraysPickItem = LoadJson("./Tests/OpCodes/Arrays/PICKITEM.json", OpCode.PICKITEM);
            _opCodesArraysPackStruct = LoadJson("./Tests/OpCodes/Arrays/PACKSTRUCT.json", OpCode.PACKSTRUCT);
            _opCodesArraysSetItem = LoadJson("./Tests/OpCodes/Arrays/SETITEM.json", OpCode.SETITEM);
            _opCodesArraysNewStruct0 = LoadJson("./Tests/OpCodes/Arrays/NEWSTRUCT0.json", OpCode.NEWSTRUCT0);
            _opCodesArraysReverseItems = LoadJson("./Tests/OpCodes/Arrays/REVERSEITEMS.json", OpCode.REVERSEITEMS);
            _opCodesArraysNewMap = LoadJson("./Tests/OpCodes/Arrays/NEWMAP.json", OpCode.NEWMAP);
            _opCodesArraysHasKey = LoadJson("./Tests/OpCodes/Arrays/HASKEY.json", OpCode.HASKEY);
            _opCodesArraysNewArray = LoadJson("./Tests/OpCodes/Arrays/NEWARRAY.json", OpCode.NEWARRAY);
            _opCodesArraysKeys = LoadJson("./Tests/OpCodes/Arrays/KEYS.json", OpCode.KEYS);
            _opCodesArraysAppend = LoadJson("./Tests/OpCodes/Arrays/APPEND.json", OpCode.APPEND);
            _opCodesArraysRemove = LoadJson("./Tests/OpCodes/Arrays/REMOVE.json", OpCode.REMOVE);
            _opCodesArraysPackMap = LoadJson("./Tests/OpCodes/Arrays/PACKMAP.json", OpCode.PACKMAP);
            _opCodesArraysValues = LoadJson("./Tests/OpCodes/Arrays/VALUES.json", OpCode.VALUES);
            _opCodesArraysNewArray0 = LoadJson("./Tests/OpCodes/Arrays/NEWARRAY0.json", OpCode.NEWARRAY0);
            _opCodesArraysUnpack = LoadJson("./Tests/OpCodes/Arrays/UNPACK.json", OpCode.UNPACK);
            _opCodesArraysSize = LoadJson("./Tests/OpCodes/Arrays/SIZE.json", OpCode.SIZE);
            _opCodesArraysNewStruct = LoadJson("./Tests/OpCodes/Arrays/NEWSTRUCT.json", OpCode.NEWSTRUCT);
            _opCodesStackXDrop = LoadJson("./Tests/OpCodes/Stack/XDROP.json", OpCode.XDROP);
            _opCodesStackReverseN = LoadJson("./Tests/OpCodes/Stack/REVERSEN.json", OpCode.REVERSEN);
            _opCodesStackReverse4 = LoadJson("./Tests/OpCodes/Stack/REVERSE4.json", OpCode.REVERSE4);
            _opCodesStackClear = LoadJson("./Tests/OpCodes/Stack/CLEAR.json", OpCode.CLEAR);
            _opCodesStackReverse3 = LoadJson("./Tests/OpCodes/Stack/REVERSE3.json", OpCode.REVERSE3);
            _opCodesStackRot = LoadJson("./Tests/OpCodes/Stack/ROT.json", OpCode.ROT);
            _opCodesStackPick = LoadJson("./Tests/OpCodes/Stack/PICK.json", OpCode.PICK);
            _opCodesStackNip = LoadJson("./Tests/OpCodes/Stack/NIP.json", OpCode.NIP);
            _opCodesStackRoll = LoadJson("./Tests/OpCodes/Stack/ROLL.json", OpCode.ROLL);
            _opCodesStackDepth = LoadJson("./Tests/OpCodes/Stack/DEPTH.json", OpCode.DEPTH);
            _opCodesStackSwap = LoadJson("./Tests/OpCodes/Stack/SWAP.json", OpCode.SWAP);
            _opCodesStackTuck = LoadJson("./Tests/OpCodes/Stack/TUCK.json", OpCode.TUCK);
            _opCodesStackOver = LoadJson("./Tests/OpCodes/Stack/OVER.json", OpCode.OVER);
            _opCodesStackDrop = LoadJson("./Tests/OpCodes/Stack/DROP.json", OpCode.DROP);
            _opCodesSlotStsFld6 = LoadJson("./Tests/OpCodes/Slot/STSFLD6.json", OpCode.STSFLD6);
            _opCodesSlotInitSlot = LoadJson("./Tests/OpCodes/Slot/INITSLOT.json", OpCode.INITSLOT);
            _opCodesSlotLdLoc4 = LoadJson("./Tests/OpCodes/Slot/LDLOC4.json", OpCode.LDLOC4);
            _opCodesSlotLdArg = LoadJson("./Tests/OpCodes/Slot/LDARG.json", OpCode.LDARG);
            _opCodesSlotStArg0 = LoadJson("./Tests/OpCodes/Slot/STARG0.json", OpCode.STARG0);
            _opCodesSlotLdSFLD0 = LoadJson("./Tests/OpCodes/Slot/LDSFLD0.json", OpCode.LDSFLD0);
            _opCodesSlotLdArg4 = LoadJson("./Tests/OpCodes/Slot/LDARG4.json", OpCode.LDARG4);
            _opCodesSlotLdArg5 = LoadJson("./Tests/OpCodes/Slot/LDARG5.json", OpCode.LDARG5);
            _opCodesSlotLdSFLD1 = LoadJson("./Tests/OpCodes/Slot/LDSFLD1.json", OpCode.LDSFLD1);
            _opCodesSlotLdLoc = LoadJson("./Tests/OpCodes/Slot/LDLOC.json", OpCode.LDLOC);
            _opCodesSlotStArg1 = LoadJson("./Tests/OpCodes/Slot/STARG1.json", OpCode.STARG1);
            _opCodesSlotLdLoc5 = LoadJson("./Tests/OpCodes/Slot/LDLOC5.json", OpCode.LDLOC5);
            _opCodesSlotLdSFLD6 = LoadJson("./Tests/OpCodes/Slot/LDSFLD6.json", OpCode.LDSFLD6);
            _opCodesSlotLdArg2 = LoadJson("./Tests/OpCodes/Slot/LDARG2.json", OpCode.LDARG2);
            _opCodesSlotStsFld0 = LoadJson("./Tests/OpCodes/Slot/STSFLD0.json", OpCode.STSFLD0);
            _opCodesSlotLdLoc2 = LoadJson("./Tests/OpCodes/Slot/LDLOC2.json", OpCode.LDLOC2);
            _opCodesSlotStArg6 = LoadJson("./Tests/OpCodes/Slot/STARG6.json", OpCode.STARG6);
            _opCodesSlotLdLoc3 = LoadJson("./Tests/OpCodes/Slot/LDLOC3.json", OpCode.LDLOC3);
            _opCodesSlotStsFld1 = LoadJson("./Tests/OpCodes/Slot/STSFLD1.json", OpCode.STSFLD1);
            _opCodesSlotInitSSlot = LoadJson("./Tests/OpCodes/Slot/INITSSLOT.json", OpCode.INITSSLOT);
            _opCodesSlotLdArg3 = LoadJson("./Tests/OpCodes/Slot/LDARG3.json", OpCode.LDARG3);
            _opCodesSlotLdSFLD = LoadJson("./Tests/OpCodes/Slot/LDSFLD.json", OpCode.LDSFLD);
            _opCodesSlotLdArg0 = LoadJson("./Tests/OpCodes/Slot/LDARG0.json", OpCode.LDARG0);
            _opCodesSlotLdSFLD4 = LoadJson("./Tests/OpCodes/Slot/LDSFLD4.json", OpCode.LDSFLD4);
            _opCodesSlotStArg4 = LoadJson("./Tests/OpCodes/Slot/STARG4.json", OpCode.STARG4);
            _opCodesSlotLdLoc0 = LoadJson("./Tests/OpCodes/Slot/LDLOC0.json", OpCode.LDLOC0);
            _opCodesSlotStsFld2 = LoadJson("./Tests/OpCodes/Slot/STSFLD2.json", OpCode.STSFLD2);
            _opCodesSlotStsFld3 = LoadJson("./Tests/OpCodes/Slot/STSFLD3.json", OpCode.STSFLD3);
            _opCodesSlotLdLoc1 = LoadJson("./Tests/OpCodes/Slot/LDLOC1.json", OpCode.LDLOC1);
            _opCodesSlotStArg5 = LoadJson("./Tests/OpCodes/Slot/STARG5.json", OpCode.STARG5);
            _opCodesSlotLdSFLD5 = LoadJson("./Tests/OpCodes/Slot/LDSFLD5.json", OpCode.LDSFLD5);
            _opCodesSlotLdArg1 = LoadJson("./Tests/OpCodes/Slot/LDARG1.json", OpCode.LDARG1);
            _opCodesSlotStArg2 = LoadJson("./Tests/OpCodes/Slot/STARG2.json", OpCode.STARG2);
            _opCodesSlotLdLoc6 = LoadJson("./Tests/OpCodes/Slot/LDLOC6.json", OpCode.LDLOC6);
            _opCodesSlotStArg = LoadJson("./Tests/OpCodes/Slot/STARG.json", OpCode.STARG);
            _opCodesSlotStsFld4 = LoadJson("./Tests/OpCodes/Slot/STSFLD4.json", OpCode.STSFLD4);
            _opCodesSlotLdArg6 = LoadJson("./Tests/OpCodes/Slot/LDARG6.json", OpCode.LDARG6);
            _opCodesSlotLdSFLD2 = LoadJson("./Tests/OpCodes/Slot/LDSFLD2.json", OpCode.LDSFLD2);
            _opCodesSlotLdSFLD3 = LoadJson("./Tests/OpCodes/Slot/LDSFLD3.json", OpCode.LDSFLD3);
            _opCodesSlotStLoc = LoadJson("./Tests/OpCodes/Slot/STLOC.json", OpCode.STLOC);
            _opCodesSlotStsFld = LoadJson("./Tests/OpCodes/Slot/STSFLD.json", OpCode.STSFLD);
            _opCodesSlotStsFld5 = LoadJson("./Tests/OpCodes/Slot/STSFLD5.json", OpCode.STSFLD5);
            _opCodesSlotStArg3 = LoadJson("./Tests/OpCodes/Slot/STARG3.json", OpCode.STARG3);
            _opCodesSpliceSubStr = LoadJson("./Tests/OpCodes/Splice/SUBSTR.json", OpCode.SUBSTR);
            _opCodesSpliceCat = LoadJson("./Tests/OpCodes/Splice/CAT.json", OpCode.CAT);
            _opCodesSpliceMemCpy = LoadJson("./Tests/OpCodes/Splice/MEMCPY.json", OpCode.MEMCPY);
            _opCodesSpliceLeft = LoadJson("./Tests/OpCodes/Splice/LEFT.json", OpCode.LEFT);
            _opCodesSpliceRight = LoadJson("./Tests/OpCodes/Splice/RIGHT.json", OpCode.RIGHT);
            _opCodesSpliceNewBuffer = LoadJson("./Tests/OpCodes/Splice/NEWBUFFER.json", OpCode.NEWBUFFER);
            _opCodesControlTryCatchFinally6 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY6.json", OpCode.TRY);
            _opCodesControlJmpLeL = LoadJson("./Tests/OpCodes/Control/JMPLE_L.json", OpCode.JMPLE_L);
            _opCodesControlJmpEqL = LoadJson("./Tests/OpCodes/Control/JMPEQ_L.json", OpCode.JMPEQ_L);
            _opCodesControlJmpLe = LoadJson("./Tests/OpCodes/Control/JMPLE.json", OpCode.JMPLE);
            _opCodesControlJmpNe = LoadJson("./Tests/OpCodes/Control/JMPNE.json", OpCode.JMPNE);
            _opCodesControlTryCatchFinally7 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY7.json", OpCode.TRY);
            _opCodesControlAssertMsg = LoadJson("./Tests/OpCodes/Control/ASSERTMSG.json", OpCode.ASSERTMSG);
            _opCodesControlJmpGt = LoadJson("./Tests/OpCodes/Control/JMPGT.json", OpCode.JMPGT);
            _opCodesControlJmpL = LoadJson("./Tests/OpCodes/Control/JMP_L.json", OpCode.JMP_L);
            _opCodesControlJmpIfL = LoadJson("./Tests/OpCodes/Control/JMPIF_L.json", OpCode.JMPIF_L);
            _opCodesControlTryFinally = LoadJson("./Tests/OpCodes/Control/TRY_FINALLY.json", OpCode.TRY);
            _opCodesControlJmpNeL = LoadJson("./Tests/OpCodes/Control/JMPNE_L.json", OpCode.JMPNE_L);
            _opCodesControlJmpIfNot = LoadJson("./Tests/OpCodes/Control/JMPIFNOT.json", OpCode.JMPIFNOT);
            _opCodesControlAbortMsg = LoadJson("./Tests/OpCodes/Control/ABORTMSG.json", OpCode.ABORTMSG);
            _opCodesControlCall = LoadJson("./Tests/OpCodes/Control/CALL.json", OpCode.CALL);
            _opCodesControlCallL = LoadJson("./Tests/OpCodes/Control/CALL_L.json", OpCode.CALL_L);
            _opCodesControlJmpGeL = LoadJson("./Tests/OpCodes/Control/JMPGE_L.json", OpCode.JMPGE_L);
            _opCodesControlRet = LoadJson("./Tests/OpCodes/Control/RET.json", OpCode.RET);
            _opCodesControlJmpGtL = LoadJson("./Tests/OpCodes/Control/JMPGT_L.json", OpCode.JMPGT_L);
            _opCodesControlJmpEq = LoadJson("./Tests/OpCodes/Control/JMPEQ.json", OpCode.JMPEQ);
            _opCodesControlSysCall = LoadJson("./Tests/OpCodes/Control/SYSCALL.json", OpCode.SYSCALL);
            _opCodesControlCallA = LoadJson("./Tests/OpCodes/Control/CALLA.json", OpCode.CALLA);
            _opCodesControlAssert = LoadJson("./Tests/OpCodes/Control/ASSERT.json", OpCode.ASSERT);
            _opCodesControlTryCatchFinally2 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY2.json", OpCode.TRY);
            _opCodesControlTryCatchFinally3 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY3.json", OpCode.TRY);
            _opCodesControlNop = LoadJson("./Tests/OpCodes/Control/NOP.json", OpCode.NOP);
            _opCodesControlJmpGe = LoadJson("./Tests/OpCodes/Control/JMPGE.json", OpCode.JMPGE);
            _opCodesControlTryCatchFinally10 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY10.json", OpCode.TRY);
            _opCodesControlJmpLt = LoadJson("./Tests/OpCodes/Control/JMPLT.json", OpCode.JMPLT);
            _opCodesControlJmpIf = LoadJson("./Tests/OpCodes/Control/JMPIF.json", OpCode.JMPIF);
            _opCodesControlAbort = LoadJson("./Tests/OpCodes/Control/ABORT.json", OpCode.ABORT);
            _opCodesControlJmpIfNotL = LoadJson("./Tests/OpCodes/Control/JMPIFNOT_L.json", OpCode.JMPIFNOT_L);
            _opCodesControlTryCatch = LoadJson("./Tests/OpCodes/Control/TRY_CATCH.json", OpCode.TRY);
            _opCodesControlTryCatchFinally4 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY4.json", OpCode.TRY);
            _opCodesControlJmp = LoadJson("./Tests/OpCodes/Control/JMP.json", OpCode.JMP);
            _opCodesControlTryCatchFinally8 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY8.json", OpCode.TRY);
            _opCodesControlTryCatchFinally9 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY9.json", OpCode.TRY);
            _opCodesControlJmpLtL = LoadJson("./Tests/OpCodes/Control/JMPLT_L.json", OpCode.JMPLT_L);
            _opCodesControlTryCatchFinally = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY.json", OpCode.TRY);
            _opCodesControlTryCatchFinally5 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY5.json", OpCode.TRY);
            _opCodesControlThrow = LoadJson("./Tests/OpCodes/Control/THROW.json", OpCode.THROW);
            _opCodesPushPushA = LoadJson("./Tests/OpCodes/Push/PUSHA.json", OpCode.PUSHA);
            _opCodesPushPushM1ToPush16 = LoadJson("./Tests/OpCodes/Push/PUSHM1_to_PUSH16.json", OpCode.PUSH16);
            _opCodesPushPushInt8ToPushInt256 = LoadJson("./Tests/OpCodes/Push/PUSHINT8_to_PUSHINT256.json", OpCode.PUSHINT256);
            _opCodesPushPushData1 = LoadJson("./Tests/OpCodes/Push/PUSHDATA1.json", OpCode.PUSHDATA1);
            _opCodesPushPushData2 = LoadJson("./Tests/OpCodes/Push/PUSHDATA2.json", OpCode.PUSHDATA2);
            _opCodesPushPushNull = LoadJson("./Tests/OpCodes/Push/PUSHNULL.json", OpCode.PUSHNULL);
            _opCodesPushPushData4 = LoadJson("./Tests/OpCodes/Push/PUSHDATA4.json", OpCode.PUSHDATA4);
            _opCodesArithmeticGe = LoadJson("./Tests/OpCodes/Arithmetic/GE.json", OpCode.GE);
            _opCodesArithmeticLt = LoadJson("./Tests/OpCodes/Arithmetic/LT.json", OpCode.LT);
            _opCodesArithmeticModMul = LoadJson("./Tests/OpCodes/Arithmetic/MODMUL.json", OpCode.MODMUL);
            _opCodesArithmeticNumNotEqual = LoadJson("./Tests/OpCodes/Arithmetic/NUMNOTEQUAL.json", OpCode.NUMNOTEQUAL);
            _opCodesArithmeticNot = LoadJson("./Tests/OpCodes/Arithmetic/NOT.json", OpCode.NOT);
            _opCodesArithmeticModPow = LoadJson("./Tests/OpCodes/Arithmetic/MODPOW.json", OpCode.MODPOW);
            _opCodesArithmeticLe = LoadJson("./Tests/OpCodes/Arithmetic/LE.json", OpCode.LE);
            _opCodesArithmeticShl = LoadJson("./Tests/OpCodes/Arithmetic/SHL.json", OpCode.SHL);
            _opCodesArithmeticGt = LoadJson("./Tests/OpCodes/Arithmetic/GT.json", OpCode.GT);
            _opCodesArithmeticPow = LoadJson("./Tests/OpCodes/Arithmetic/POW.json", OpCode.POW);
            _opCodesArithmeticNumEqual = LoadJson("./Tests/OpCodes/Arithmetic/NUMEQUAL.json", OpCode.NUMEQUAL);
            _opCodesArithmeticSign = LoadJson("./Tests/OpCodes/Arithmetic/SIGN.json", OpCode.SIGN);
            _opCodesArithmeticSqrt = LoadJson("./Tests/OpCodes/Arithmetic/SQRT.json", OpCode.SQRT);
            _opCodesArithmeticShr = LoadJson("./Tests/OpCodes/Arithmetic/SHR.json", OpCode.SHR);
            _opCodesBitwiseLogicOr = LoadJson("./Tests/OpCodes/BitwiseLogic/OR.json", OpCode.OR);
            _opCodesBitwiseLogicEqual = LoadJson("./Tests/OpCodes/BitwiseLogic/EQUAL.json", OpCode.EQUAL);
            _opCodesBitwiseLogicInvert = LoadJson("./Tests/OpCodes/BitwiseLogic/INVERT.json", OpCode.INVERT);
            _opCodesBitwiseLogicXor = LoadJson("./Tests/OpCodes/BitwiseLogic/XOR.json", OpCode.XOR);
            _opCodesBitwiseLogicNotEqual = LoadJson("./Tests/OpCodes/BitwiseLogic/NOTEQUAL.json", OpCode.NOTEQUAL);
            _opCodesBitwiseLogicAnd = LoadJson("./Tests/OpCodes/BitwiseLogic/AND.json", OpCode.AND);
            _opCodesTypesConvert = LoadJson("./Tests/OpCodes/Types/CONVERT.json", OpCode.CONVERT);
            _opCodesTypesIsType = LoadJson("./Tests/OpCodes/Types/ISTYPE.json", OpCode.ISTYPE);
            _opCodesTypesIsNull = LoadJson("./Tests/OpCodes/Types/ISNULL.json", OpCode.ISNULL);
        }

        [Benchmark]
        public void TestOpCodesArraysClearItems() => RunBench(_opCodesArraysClearItems);

        [Benchmark]
        public void TestOpCodesArraysPack() => RunBench(_opCodesArraysPack);

        [Benchmark]
        public void TestOpCodesArraysNewArrayT() => RunBench(_opCodesArraysNewArrayT);

        [Benchmark]
        public void TestOpCodesArraysPickItem() => RunBench(_opCodesArraysPickItem);

        [Benchmark]
        public void TestOpCodesArraysPackStruct() => RunBench(_opCodesArraysPackStruct);

        [Benchmark]
        public void TestOpCodesArraysSetItem() => RunBench(_opCodesArraysSetItem);

        [Benchmark]
        public void TestOpCodesArraysNewStruct0() => RunBench(_opCodesArraysNewStruct0);

        [Benchmark]
        public void TestOpCodesArraysReverseItems() => RunBench(_opCodesArraysReverseItems);

        [Benchmark]
        public void TestOpCodesArraysNewMap() => RunBench(_opCodesArraysNewMap);

        [Benchmark]
        public void TestOpCodesArraysHasKey() => RunBench(_opCodesArraysHasKey);

        [Benchmark]
        public void TestOpCodesArraysNewArray() => RunBench(_opCodesArraysNewArray);

        [Benchmark]
        public void TestOpCodesArraysKeys() => RunBench(_opCodesArraysKeys);

        [Benchmark]
        public void TestOpCodesArraysAppend() => RunBench(_opCodesArraysAppend);

        [Benchmark]
        public void TestOpCodesArraysRemove() => RunBench(_opCodesArraysRemove);

        [Benchmark]
        public void TestOpCodesArraysPackMap() => RunBench(_opCodesArraysPackMap);

        [Benchmark]
        public void TestOpCodesArraysValues() => RunBench(_opCodesArraysValues);

        [Benchmark]
        public void TestOpCodesArraysNewArray0() => RunBench(_opCodesArraysNewArray0);

        [Benchmark]
        public void TestOpCodesArraysUnpack() => RunBench(_opCodesArraysUnpack);

        [Benchmark]
        public void TestOpCodesArraysSize() => RunBench(_opCodesArraysSize);

        [Benchmark]
        public void TestOpCodesArraysNewStruct() => RunBench(_opCodesArraysNewStruct);

        [Benchmark]
        public void TestOpCodesStackXDrop() => RunBench(_opCodesStackXDrop);

        [Benchmark]
        public void TestOpCodesStackReverseN() => RunBench(_opCodesStackReverseN);

        [Benchmark]
        public void TestOpCodesStackReverse4() => RunBench(_opCodesStackReverse4);

        [Benchmark]
        public void TestOpCodesStackClear() => RunBench(_opCodesStackClear);

        [Benchmark]
        public void TestOpCodesStackReverse3() => RunBench(_opCodesStackReverse3);

        [Benchmark]
        public void TestOpCodesStackRot() => RunBench(_opCodesStackRot);

        [Benchmark]
        public void TestOpCodesStackPick() => RunBench(_opCodesStackPick);

        [Benchmark]
        public void TestOpCodesStackNip() => RunBench(_opCodesStackNip);

        [Benchmark]
        public void TestOpCodesStackRoll() => RunBench(_opCodesStackRoll);

        [Benchmark]
        public void TestOpCodesStackDepth() => RunBench(_opCodesStackDepth);

        [Benchmark]
        public void TestOpCodesStackSwap() => RunBench(_opCodesStackSwap);

        [Benchmark]
        public void TestOpCodesStackTuck() => RunBench(_opCodesStackTuck);

        [Benchmark]
        public void TestOpCodesStackOver() => RunBench(_opCodesStackOver);

        [Benchmark]
        public void TestOpCodesStackDrop() => RunBench(_opCodesStackDrop);

        [Benchmark]
        public void TestOpCodesSlotStsFld6() => RunBench(_opCodesSlotStsFld6);

        [Benchmark]
        public void TestOpCodesSlotInitSlot() => RunBench(_opCodesSlotInitSlot);

        [Benchmark]
        public void TestOpCodesSlotLdLoc4() => RunBench(_opCodesSlotLdLoc4);

        [Benchmark]
        public void TestOpCodesSlotLdArg() => RunBench(_opCodesSlotLdArg);

        [Benchmark]
        public void TestOpCodesSlotStArg0() => RunBench(_opCodesSlotStArg0);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD0() => RunBench(_opCodesSlotLdSFLD0);

        [Benchmark]
        public void TestOpCodesSlotLdArg4() => RunBench(_opCodesSlotLdArg4);

        [Benchmark]
        public void TestOpCodesSlotLdArg5() => RunBench(_opCodesSlotLdArg5);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD1() => RunBench(_opCodesSlotLdSFLD1);

        [Benchmark]
        public void TestOpCodesSlotLdLoc() => RunBench(_opCodesSlotLdLoc);

        [Benchmark]
        public void TestOpCodesSlotStArg1() => RunBench(_opCodesSlotStArg1);

        [Benchmark]
        public void TestOpCodesSlotLdLoc5() => RunBench(_opCodesSlotLdLoc5);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD6() => RunBench(_opCodesSlotLdSFLD6);

        [Benchmark]
        public void TestOpCodesSlotLdArg2() => RunBench(_opCodesSlotLdArg2);

        [Benchmark]
        public void TestOpCodesSlotStsFld0() => RunBench(_opCodesSlotStsFld0);

        [Benchmark]
        public void TestOpCodesSlotLdLoc2() => RunBench(_opCodesSlotLdLoc2);

        [Benchmark]
        public void TestOpCodesSlotStArg6() => RunBench(_opCodesSlotStArg6);

        [Benchmark]
        public void TestOpCodesSlotLdLoc3() => RunBench(_opCodesSlotLdLoc3);

        [Benchmark]
        public void TestOpCodesSlotStsFld1() => RunBench(_opCodesSlotStsFld1);

        [Benchmark]
        public void TestOpCodesSlotInitSSlot() => RunBench(_opCodesSlotInitSSlot);

        [Benchmark]
        public void TestOpCodesSlotLdArg3() => RunBench(_opCodesSlotLdArg3);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD() => RunBench(_opCodesSlotLdSFLD);

        [Benchmark]
        public void TestOpCodesSlotLdArg0() => RunBench(_opCodesSlotLdArg0);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD4() => RunBench(_opCodesSlotLdSFLD4);

        [Benchmark]
        public void TestOpCodesSlotStArg4() => RunBench(_opCodesSlotStArg4);

        [Benchmark]
        public void TestOpCodesSlotLdLoc0() => RunBench(_opCodesSlotLdLoc0);

        [Benchmark]
        public void TestOpCodesSlotStsFld2() => RunBench(_opCodesSlotStsFld2);

        [Benchmark]
        public void TestOpCodesSlotStsFld3() => RunBench(_opCodesSlotStsFld3);

        [Benchmark]
        public void TestOpCodesSlotLdLoc1() => RunBench(_opCodesSlotLdLoc1);

        [Benchmark]
        public void TestOpCodesSlotStArg5() => RunBench(_opCodesSlotStArg5);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD5() => RunBench(_opCodesSlotLdSFLD5);

        [Benchmark]
        public void TestOpCodesSlotLdArg1() => RunBench(_opCodesSlotLdArg1);

        [Benchmark]
        public void TestOpCodesSlotStArg2() => RunBench(_opCodesSlotStArg2);

        [Benchmark]
        public void TestOpCodesSlotLdLoc6() => RunBench(_opCodesSlotLdLoc6);

        [Benchmark]
        public void TestOpCodesSlotStArg() => RunBench(_opCodesSlotStArg);

        [Benchmark]
        public void TestOpCodesSlotStsFld4() => RunBench(_opCodesSlotStsFld4);

        [Benchmark]
        public void TestOpCodesSlotLdArg6() => RunBench(_opCodesSlotLdArg6);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD2() => RunBench(_opCodesSlotLdSFLD2);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD3() => RunBench(_opCodesSlotLdSFLD3);

        [Benchmark]
        public void TestOpCodesSlotStLoc() => RunBench(_opCodesSlotStLoc);

        [Benchmark]
        public void TestOpCodesSlotStsFld() => RunBench(_opCodesSlotStsFld);

        [Benchmark]
        public void TestOpCodesSlotStsFld5() => RunBench(_opCodesSlotStsFld5);

        [Benchmark]
        public void TestOpCodesSlotStArg3() => RunBench(_opCodesSlotStArg3);

        [Benchmark]
        public void TestOpCodesSpliceSubStr() => RunBench(_opCodesSpliceSubStr);

        [Benchmark]
        public void TestOpCodesSpliceCat() => RunBench(_opCodesSpliceCat);

        [Benchmark]
        public void TestOpCodesSpliceMemCpy() => RunBench(_opCodesSpliceMemCpy);

        [Benchmark]
        public void TestOpCodesSpliceLeft() => RunBench(_opCodesSpliceLeft);

        [Benchmark]
        public void TestOpCodesSpliceRight() => RunBench(_opCodesSpliceRight);

        [Benchmark]
        public void TestOpCodesSpliceNewBuffer() => RunBench(_opCodesSpliceNewBuffer);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally6() => RunBench(_opCodesControlTryCatchFinally6);

        [Benchmark]
        public void TestOpCodesControlJmpLeL() => RunBench(_opCodesControlJmpLeL);

        [Benchmark]
        public void TestOpCodesControlJmpEqL() => RunBench(_opCodesControlJmpEqL);

        [Benchmark]
        public void TestOpCodesControlJmpLe() => RunBench(_opCodesControlJmpLe);

        [Benchmark]
        public void TestOpCodesControlJmpNe() => RunBench(_opCodesControlJmpNe);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally7() => RunBench(_opCodesControlTryCatchFinally7);

        [Benchmark]
        public void TestOpCodesControlAssertMsg() => RunBench(_opCodesControlAssertMsg);

        [Benchmark]
        public void TestOpCodesControlJmpGt() => RunBench(_opCodesControlJmpGt);

        [Benchmark]
        public void TestOpCodesControlJmpL() => RunBench(_opCodesControlJmpL);

        [Benchmark]
        public void TestOpCodesControlJmpIfL() => RunBench(_opCodesControlJmpIfL);

        [Benchmark]
        public void TestOpCodesControlTryFinally() => RunBench(_opCodesControlTryFinally);

        [Benchmark]
        public void TestOpCodesControlJmpNeL() => RunBench(_opCodesControlJmpNeL);

        [Benchmark]
        public void TestOpCodesControlJmpIfNot() => RunBench(_opCodesControlJmpIfNot);

        [Benchmark]
        public void TestOpCodesControlAbortMsg() => RunBench(_opCodesControlAbortMsg);

        [Benchmark]
        public void TestOpCodesControlCall() => RunBench(_opCodesControlCall);

        [Benchmark]
        public void TestOpCodesControlCallL() => RunBench(_opCodesControlCallL);

        [Benchmark]
        public void TestOpCodesControlJmpGeL() => RunBench(_opCodesControlJmpGeL);

        [Benchmark]
        public void TestOpCodesControlRet() => RunBench(_opCodesControlRet);

        [Benchmark]
        public void TestOpCodesControlJmpGtL() => RunBench(_opCodesControlJmpGtL);

        [Benchmark]
        public void TestOpCodesControlJmpEq() => RunBench(_opCodesControlJmpEq);

        [Benchmark]
        public void TestOpCodesControlSysCall() => RunBench(_opCodesControlSysCall);

        [Benchmark]
        public void TestOpCodesControlCallA() => RunBench(_opCodesControlCallA);

        [Benchmark]
        public void TestOpCodesControlAssert() => RunBench(_opCodesControlAssert);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally2() => RunBench(_opCodesControlTryCatchFinally2);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally3() => RunBench(_opCodesControlTryCatchFinally3);

        [Benchmark]
        public void TestOpCodesControlNop() => RunBench(_opCodesControlNop);

        [Benchmark]
        public void TestOpCodesControlJmpGe() => RunBench(_opCodesControlJmpGe);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally10() => RunBench(_opCodesControlTryCatchFinally10);

        [Benchmark]
        public void TestOpCodesControlJmpLt() => RunBench(_opCodesControlJmpLt);

        [Benchmark]
        public void TestOpCodesControlJmpIf() => RunBench(_opCodesControlJmpIf);

        [Benchmark]
        public void TestOpCodesControlAbort() => RunBench(_opCodesControlAbort);

        [Benchmark]
        public void TestOpCodesControlJmpIfNotL() => RunBench(_opCodesControlJmpIfNotL);

        [Benchmark]
        public void TestOpCodesControlTryCatch() => RunBench(_opCodesControlTryCatch);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally4() => RunBench(_opCodesControlTryCatchFinally4);

        [Benchmark]
        public void TestOpCodesControlJmp() => RunBench(_opCodesControlJmp);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally8() => RunBench(_opCodesControlTryCatchFinally8);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally9() => RunBench(_opCodesControlTryCatchFinally9);

        [Benchmark]
        public void TestOpCodesControlJmpLtL() => RunBench(_opCodesControlJmpLtL);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally() => RunBench(_opCodesControlTryCatchFinally);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally5() => RunBench(_opCodesControlTryCatchFinally5);

        [Benchmark]
        public void TestOpCodesControlThrow() => RunBench(_opCodesControlThrow);

        [Benchmark]
        public void TestOpCodesPushPushA() => RunBench(_opCodesPushPushA);

        [Benchmark]
        public void TestOpCodesPushPushM1ToPush16() => RunBench(_opCodesPushPushM1ToPush16);

        [Benchmark]
        public void TestOpCodesPushPushInt8ToPushInt256() => RunBench(_opCodesPushPushInt8ToPushInt256);

        [Benchmark]
        public void TestOpCodesPushPushData1() => RunBench(_opCodesPushPushData1);

        [Benchmark]
        public void TestOpCodesPushPushData2() => RunBench(_opCodesPushPushData2);

        [Benchmark]
        public void TestOpCodesPushPushNull() => RunBench(_opCodesPushPushNull);

        [Benchmark]
        public void TestOpCodesPushPushData4() => RunBench(_opCodesPushPushData4);

        [Benchmark]
        public void TestOpCodesArithmeticGe() => RunBench(_opCodesArithmeticGe);

        [Benchmark]
        public void TestOpCodesArithmeticLt() => RunBench(_opCodesArithmeticLt);

        [Benchmark]
        public void TestOpCodesArithmeticModMul() => RunBench(_opCodesArithmeticModMul);

        [Benchmark]
        public void TestOpCodesArithmeticNumNotEqual() => RunBench(_opCodesArithmeticNumNotEqual);

        [Benchmark]
        public void TestOpCodesArithmeticNot() => RunBench(_opCodesArithmeticNot);

        [Benchmark]
        public void TestOpCodesArithmeticModPow() => RunBench(_opCodesArithmeticModPow);

        [Benchmark]
        public void TestOpCodesArithmeticLe() => RunBench(_opCodesArithmeticLe);

        [Benchmark]
        public void TestOpCodesArithmeticShl() => RunBench(_opCodesArithmeticShl);

        [Benchmark]
        public void TestOpCodesArithmeticGt() => RunBench(_opCodesArithmeticGt);

        [Benchmark]
        public void TestOpCodesArithmeticPow() => RunBench(_opCodesArithmeticPow);

        [Benchmark]
        public void TestOpCodesArithmeticNumEqual() => RunBench(_opCodesArithmeticNumEqual);

        [Benchmark]
        public void TestOpCodesArithmeticSign() => RunBench(_opCodesArithmeticSign);

        [Benchmark]
        public void TestOpCodesArithmeticSqrt() => RunBench(_opCodesArithmeticSqrt);

        [Benchmark]
        public void TestOpCodesArithmeticShr() => RunBench(_opCodesArithmeticShr);

        [Benchmark]
        public void TestOpCodesBitwiseLogicOr() => RunBench(_opCodesBitwiseLogicOr);

        [Benchmark]
        public void TestOpCodesBitwiseLogicEqual() => RunBench(_opCodesBitwiseLogicEqual);

        [Benchmark]
        public void TestOpCodesBitwiseLogicInvert() => RunBench(_opCodesBitwiseLogicInvert);

        [Benchmark]
        public void TestOpCodesBitwiseLogicXor() => RunBench(_opCodesBitwiseLogicXor);

        [Benchmark]
        public void TestOpCodesBitwiseLogicNotEqual() => RunBench(_opCodesBitwiseLogicNotEqual);

        [Benchmark]
        public void TestOpCodesBitwiseLogicAnd() => RunBench(_opCodesBitwiseLogicAnd);

        [Benchmark]
        public void TestOpCodesTypesConvert() => RunBench(_opCodesTypesConvert);

        [Benchmark]
        public void TestOpCodesTypesIsType() => RunBench(_opCodesTypesIsType);

        [Benchmark]
        public void TestOpCodesTypesIsNull() => RunBench(_opCodesTypesIsNull);

        private BenchmarkEngine[] LoadJson(string path, OpCode opCode)
        {
            var realFile = Path.GetFullPath(Root + path);
            var json = File.ReadAllText(realFile, Encoding.UTF8);
            var ut = json.DeserializeJson<VMUT>();
            Assert.IsFalse(string.IsNullOrEmpty(ut.Name), "Name is required");
            if (json != ut.ToJson().Replace("\r\n", "\n"))
            {
            }
            List<BenchmarkEngine> benchmarkEngines = new List<BenchmarkEngine>();
            foreach (var test in ut.Tests)
            {
                Assert.IsFalse(string.IsNullOrEmpty(test.Name), "Name is required");
                var engine = new TestEngine();
                if (test.Script.Length > 0)
                {
                    engine.LoadScript(test.Script);
                    var bench = new BenchmarkEngine(engine);
                    benchmarkEngines.Add(bench.ExecuteUntil(opCode));
                }
            }
            return benchmarkEngines.ToArray();
        }

        private void RunBench(BenchmarkEngine[]? benchmarkEngines)
        {
            foreach (var engine in benchmarkEngines)
            {
                engine.ExecuteBenchmark();
            }
        }
    }
}
