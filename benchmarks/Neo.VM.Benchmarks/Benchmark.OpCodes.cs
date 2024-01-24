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
using Neo.Test;
using Neo.Test.Extensions;
using Neo.Test.Types;
using System.Text;

namespace Neo.VM
{
    public class BenchmarkOpCodes : VMJsonTestBase
    {
        #region Fields
        private const string Root = "../../../../../../../../../tests/Neo.VM.Tests/";
        private VMUT? _othersDebugger;
        private VMUT? _othersStackItemLimits;
        private VMUT? _othersScriptLogic;
        private VMUT? _othersOtherCases;
        private VMUT? _othersInvocationLimits;
        private VMUT? _othersInit;
        private VMUT? _othersStackLimits;
        private VMUT? _opCodesArraysClearItems;
        private VMUT? _opCodesArraysPack;
        private VMUT? _opCodesArraysNewArrayT;
        private VMUT? _opCodesArraysPickItem;
        private VMUT? _opCodesArraysPackStruct;
        private VMUT? _opCodesArraysSetItem;
        private VMUT? _opCodesArraysNewStruct0;
        private VMUT? _opCodesArraysReverseItems;
        private VMUT? _opCodesArraysNewMap;
        private VMUT? _opCodesArraysHasKey;
        private VMUT? _opCodesArraysNewArray;
        private VMUT? _opCodesArraysKeys;
        private VMUT? _opCodesArraysAppend;
        private VMUT? _opCodesArraysRemove;
        private VMUT? _opCodesArraysPackMap;
        private VMUT? _opCodesArraysValues;
        private VMUT? _opCodesArraysNewArray0;
        private VMUT? _opCodesArraysUnpack;
        private VMUT? _opCodesArraysSize;
        private VMUT? _opCodesArraysNewStruct;
        private VMUT? _opCodesStackXDrop;
        private VMUT? _opCodesStackReverseN;
        private VMUT? _opCodesStackReverse4;
        private VMUT? _opCodesStackClear;
        private VMUT? _opCodesStackReverse3;
        private VMUT? _opCodesStackRot;
        private VMUT? _opCodesStackPick;
        private VMUT? _opCodesStackNip;
        private VMUT? _opCodesStackRoll;
        private VMUT? _opCodesStackDepth;
        private VMUT? _opCodesStackSwap;
        private VMUT? _opCodesStackTuck;
        private VMUT? _opCodesStackOver;
        private VMUT? _opCodesStackDrop;
        private VMUT? _opCodesSlotStsFld6;
        private VMUT? _opCodesSlotInitSlot;
        private VMUT? _opCodesSlotLdLoc4;
        private VMUT? _opCodesSlotLdArg;
        private VMUT? _opCodesSlotStArg0;
        private VMUT? _opCodesSlotLdSFLD0;
        private VMUT? _opCodesSlotLdArg4;
        private VMUT? _opCodesSlotLdArg5;
        private VMUT? _opCodesSlotLdSFLD1;
        private VMUT? _opCodesSlotLdLoc;
        private VMUT? _opCodesSlotStArg1;
        private VMUT? _opCodesSlotLdLoc5;
        private VMUT? _opCodesSlotLdSFLD6;
        private VMUT? _opCodesSlotLdArg2;
        private VMUT? _opCodesSlotStsFld0;
        private VMUT? _opCodesSlotLdLoc2;
        private VMUT? _opCodesSlotStArg6;
        private VMUT? _opCodesSlotLdLoc3;
        private VMUT? _opCodesSlotStsFld1;
        private VMUT? _opCodesSlotInitSSlot;
        private VMUT? _opCodesSlotLdArg3;
        private VMUT? _opCodesSlotLdSFLD;
        private VMUT? _opCodesSlotLdArg0;
        private VMUT? _opCodesSlotLdSFLD4;
        private VMUT? _opCodesSlotStArg4;
        private VMUT? _opCodesSlotLdLoc0;
        private VMUT? _opCodesSlotStsFld2;
        private VMUT? _opCodesSlotStsFld3;
        private VMUT? _opCodesSlotLdLoc1;
        private VMUT? _opCodesSlotStArg5;
        private VMUT? _opCodesSlotLdSFLD5;
        private VMUT? _opCodesSlotLdArg1;
        private VMUT? _opCodesSlotStArg2;
        private VMUT? _opCodesSlotLdLoc6;
        private VMUT? _opCodesSlotStArg;
        private VMUT? _opCodesSlotStsFld4;
        private VMUT? _opCodesSlotLdArg6;
        private VMUT? _opCodesSlotLdSFLD2;
        private VMUT? _opCodesSlotLdSFLD3;
        private VMUT? _opCodesSlotStLoc;
        private VMUT? _opCodesSlotStsFld;
        private VMUT? _opCodesSlotStsFld5;
        private VMUT? _opCodesSlotStArg3;
        private VMUT? _opCodesSpliceSubStr;
        private VMUT? _opCodesSpliceCat;
        private VMUT? _opCodesSpliceMemCpy;
        private VMUT? _opCodesSpliceLeft;
        private VMUT? _opCodesSpliceRight;
        private VMUT? _opCodesSpliceNewBuffer;
        private VMUT? _opCodesControlTryCatchFinally6;
        private VMUT? _opCodesControlJmpLeL;
        private VMUT? _opCodesControlJmpEqL;
        private VMUT? _opCodesControlJmpLe;
        private VMUT? _opCodesControlJmpNe;
        private VMUT? _opCodesControlTryCatchFinally7;
        private VMUT? _opCodesControlAssertMsg;
        private VMUT? _opCodesControlJmpGt;
        private VMUT? _opCodesControlJmpL;
        private VMUT? _opCodesControlJmpIfL;
        private VMUT? _opCodesControlTryFinally;
        private VMUT? _opCodesControlJmpNeL;
        private VMUT? _opCodesControlJmpIfNot;
        private VMUT? _opCodesControlAbortMsg;
        private VMUT? _opCodesControlCall;
        private VMUT? _opCodesControlCallL;
        private VMUT? _opCodesControlJmpGeL;
        private VMUT? _opCodesControlRet;
        private VMUT? _opCodesControlJmpGtL;
        private VMUT? _opCodesControlJmpEq;
        private VMUT? _opCodesControlSysCall;
        private VMUT? _opCodesControlCallA;
        private VMUT? _opCodesControlAssert;
        private VMUT? _opCodesControlTryCatchFinally2;
        private VMUT? _opCodesControlTryCatchFinally3;
        private VMUT? _opCodesControlNop;
        private VMUT? _opCodesControlJmpGe;
        private VMUT? _opCodesControlTryCatchFinally10;
        private VMUT? _opCodesControlJmpLt;
        private VMUT? _opCodesControlJmpIf;
        private VMUT? _opCodesControlAbort;
        private VMUT? _opCodesControlJmpIfNotL;
        private VMUT? _opCodesControlTryCatch;
        private VMUT? _opCodesControlTryCatchFinally4;
        private VMUT? _opCodesControlJmp;
        private VMUT? _opCodesControlTryCatchFinally8;
        private VMUT? _opCodesControlTryCatchFinally9;
        private VMUT? _opCodesControlJmpLtL;
        private VMUT? _opCodesControlTryCatchFinally;
        private VMUT? _opCodesControlTryCatchFinally5;
        private VMUT? _opCodesControlThrow;
        private VMUT? _opCodesPushPushA;
        private VMUT? _opCodesPushPushM1ToPush16;
        private VMUT? _opCodesPushPushInt8ToPushInt256;
        private VMUT? _opCodesPushPushData1;
        private VMUT? _opCodesPushPushData2;
        private VMUT? _opCodesPushPushNull;
        private VMUT? _opCodesPushPushData4;
        private VMUT? _opCodesArithmeticGe;
        private VMUT? _opCodesArithmeticLt;
        private VMUT? _opCodesArithmeticModMul;
        private VMUT? _opCodesArithmeticNumNotEqual;
        private VMUT? _opCodesArithmeticNot;
        private VMUT? _opCodesArithmeticModPow;
        private VMUT? _opCodesArithmeticLe;
        private VMUT? _opCodesArithmeticShl;
        private VMUT? _opCodesArithmeticGt;
        private VMUT? _opCodesArithmeticPow;
        private VMUT? _opCodesArithmeticNumEqual;
        private VMUT? _opCodesArithmeticSign;
        private VMUT? _opCodesArithmeticSqrt;
        private VMUT? _opCodesArithmeticShr;
        private VMUT? _opCodesBitwiseLogicOr;
        private VMUT? _opCodesBitwiseLogicEqual;
        private VMUT? _opCodesBitwiseLogicInvert;
        private VMUT? _opCodesBitwiseLogicXor;
        private VMUT? _opCodesBitwiseLogicNotEqual;
        private VMUT? _opCodesBitwiseLogicAnd;
        private VMUT? _opCodesTypesConvert;
        private VMUT? _opCodesTypesIsType;
        private VMUT? _opCodesTypesIsNull;
        #endregion

        [GlobalSetup]
        public void Setup()
        {
            _othersDebugger = LoadJson("./Tests/Others/Debugger.json");
            _othersStackItemLimits = LoadJson("./Tests/Others/StackItemLimits.json");
            _othersScriptLogic = LoadJson("./Tests/Others/ScriptLogic.json");
            _othersOtherCases = LoadJson("./Tests/Others/OtherCases.json");
            _othersInvocationLimits = LoadJson("./Tests/Others/InvocationLimits.json");
            _othersInit = LoadJson("./Tests/Others/Init.json");
            _othersStackLimits = LoadJson("./Tests/Others/StackLimits.json");
            _opCodesArraysClearItems = LoadJson("./Tests/OpCodes/Arrays/CLEARITEMS.json");
            _opCodesArraysPack = LoadJson("./Tests/OpCodes/Arrays/PACK.json");
            _opCodesArraysNewArrayT = LoadJson("./Tests/OpCodes/Arrays/NEWARRAY_T.json");
            _opCodesArraysPickItem = LoadJson("./Tests/OpCodes/Arrays/PICKITEM.json");
            _opCodesArraysPackStruct = LoadJson("./Tests/OpCodes/Arrays/PACKSTRUCT.json");
            _opCodesArraysSetItem = LoadJson("./Tests/OpCodes/Arrays/SETITEM.json");
            _opCodesArraysNewStruct0 = LoadJson("./Tests/OpCodes/Arrays/NEWSTRUCT0.json");
            _opCodesArraysReverseItems = LoadJson("./Tests/OpCodes/Arrays/REVERSEITEMS.json");
            _opCodesArraysNewMap = LoadJson("./Tests/OpCodes/Arrays/NEWMAP.json");
            _opCodesArraysHasKey = LoadJson("./Tests/OpCodes/Arrays/HASKEY.json");
            _opCodesArraysNewArray = LoadJson("./Tests/OpCodes/Arrays/NEWARRAY.json");
            _opCodesArraysKeys = LoadJson("./Tests/OpCodes/Arrays/KEYS.json");
            _opCodesArraysAppend = LoadJson("./Tests/OpCodes/Arrays/APPEND.json");
            _opCodesArraysRemove = LoadJson("./Tests/OpCodes/Arrays/REMOVE.json");
            _opCodesArraysPackMap = LoadJson("./Tests/OpCodes/Arrays/PACKMAP.json");
            _opCodesArraysValues = LoadJson("./Tests/OpCodes/Arrays/VALUES.json");
            _opCodesArraysNewArray0 = LoadJson("./Tests/OpCodes/Arrays/NEWARRAY0.json");
            _opCodesArraysUnpack = LoadJson("./Tests/OpCodes/Arrays/UNPACK.json");
            _opCodesArraysSize = LoadJson("./Tests/OpCodes/Arrays/SIZE.json");
            _opCodesArraysNewStruct = LoadJson("./Tests/OpCodes/Arrays/NEWSTRUCT.json");
            _opCodesStackXDrop = LoadJson("./Tests/OpCodes/Stack/XDROP.json");
            _opCodesStackReverseN = LoadJson("./Tests/OpCodes/Stack/REVERSEN.json");
            _opCodesStackReverse4 = LoadJson("./Tests/OpCodes/Stack/REVERSE4.json");
            _opCodesStackClear = LoadJson("./Tests/OpCodes/Stack/CLEAR.json");
            _opCodesStackReverse3 = LoadJson("./Tests/OpCodes/Stack/REVERSE3.json");
            _opCodesStackRot = LoadJson("./Tests/OpCodes/Stack/ROT.json");
            _opCodesStackPick = LoadJson("./Tests/OpCodes/Stack/PICK.json");
            _opCodesStackNip = LoadJson("./Tests/OpCodes/Stack/NIP.json");
            _opCodesStackRoll = LoadJson("./Tests/OpCodes/Stack/ROLL.json");
            _opCodesStackDepth = LoadJson("./Tests/OpCodes/Stack/DEPTH.json");
            _opCodesStackSwap = LoadJson("./Tests/OpCodes/Stack/SWAP.json");
            _opCodesStackTuck = LoadJson("./Tests/OpCodes/Stack/TUCK.json");
            _opCodesStackOver = LoadJson("./Tests/OpCodes/Stack/OVER.json");
            _opCodesStackDrop = LoadJson("./Tests/OpCodes/Stack/DROP.json");
            _opCodesSlotStsFld6 = LoadJson("./Tests/OpCodes/Slot/STSFLD6.json");
            _opCodesSlotInitSlot = LoadJson("./Tests/OpCodes/Slot/INITSLOT.json");
            _opCodesSlotLdLoc4 = LoadJson("./Tests/OpCodes/Slot/LDLOC4.json");
            _opCodesSlotLdArg = LoadJson("./Tests/OpCodes/Slot/LDARG.json");
            _opCodesSlotStArg0 = LoadJson("./Tests/OpCodes/Slot/STARG0.json");
            _opCodesSlotLdSFLD0 = LoadJson("./Tests/OpCodes/Slot/LDSFLD0.json");
            _opCodesSlotLdArg4 = LoadJson("./Tests/OpCodes/Slot/LDARG4.json");
            _opCodesSlotLdArg5 = LoadJson("./Tests/OpCodes/Slot/LDARG5.json");
            _opCodesSlotLdSFLD1 = LoadJson("./Tests/OpCodes/Slot/LDSFLD1.json");
            _opCodesSlotLdLoc = LoadJson("./Tests/OpCodes/Slot/LDLOC.json");
            _opCodesSlotStArg1 = LoadJson("./Tests/OpCodes/Slot/STARG1.json");
            _opCodesSlotLdLoc5 = LoadJson("./Tests/OpCodes/Slot/LDLOC5.json");
            _opCodesSlotLdSFLD6 = LoadJson("./Tests/OpCodes/Slot/LDSFLD6.json");
            _opCodesSlotLdArg2 = LoadJson("./Tests/OpCodes/Slot/LDARG2.json");
            _opCodesSlotStsFld0 = LoadJson("./Tests/OpCodes/Slot/STSFLD0.json");
            _opCodesSlotLdLoc2 = LoadJson("./Tests/OpCodes/Slot/LDLOC2.json");
            _opCodesSlotStArg6 = LoadJson("./Tests/OpCodes/Slot/STARG6.json");
            _opCodesSlotLdLoc3 = LoadJson("./Tests/OpCodes/Slot/LDLOC3.json");
            _opCodesSlotStsFld1 = LoadJson("./Tests/OpCodes/Slot/STSFLD1.json");
            _opCodesSlotInitSSlot = LoadJson("./Tests/OpCodes/Slot/INITSSLOT.json");
            _opCodesSlotLdArg3 = LoadJson("./Tests/OpCodes/Slot/LDARG3.json");
            _opCodesSlotLdSFLD = LoadJson("./Tests/OpCodes/Slot/LDSFLD.json");
            _opCodesSlotLdArg0 = LoadJson("./Tests/OpCodes/Slot/LDARG0.json");
            _opCodesSlotLdSFLD4 = LoadJson("./Tests/OpCodes/Slot/LDSFLD4.json");
            _opCodesSlotStArg4 = LoadJson("./Tests/OpCodes/Slot/STARG4.json");
            _opCodesSlotLdLoc0 = LoadJson("./Tests/OpCodes/Slot/LDLOC0.json");
            _opCodesSlotStsFld2 = LoadJson("./Tests/OpCodes/Slot/STSFLD2.json");
            _opCodesSlotStsFld3 = LoadJson("./Tests/OpCodes/Slot/STSFLD3.json");
            _opCodesSlotLdLoc1 = LoadJson("./Tests/OpCodes/Slot/LDLOC1.json");
            _opCodesSlotStArg5 = LoadJson("./Tests/OpCodes/Slot/STARG5.json");
            _opCodesSlotLdSFLD5 = LoadJson("./Tests/OpCodes/Slot/LDSFLD5.json");
            _opCodesSlotLdArg1 = LoadJson("./Tests/OpCodes/Slot/LDARG1.json");
            _opCodesSlotStArg2 = LoadJson("./Tests/OpCodes/Slot/STARG2.json");
            _opCodesSlotLdLoc6 = LoadJson("./Tests/OpCodes/Slot/LDLOC6.json");
            _opCodesSlotStArg = LoadJson("./Tests/OpCodes/Slot/STARG.json");
            _opCodesSlotStsFld4 = LoadJson("./Tests/OpCodes/Slot/STSFLD4.json");
            _opCodesSlotLdArg6 = LoadJson("./Tests/OpCodes/Slot/LDARG6.json");
            _opCodesSlotLdSFLD2 = LoadJson("./Tests/OpCodes/Slot/LDSFLD2.json");
            _opCodesSlotLdSFLD3 = LoadJson("./Tests/OpCodes/Slot/LDSFLD3.json");
            _opCodesSlotStLoc = LoadJson("./Tests/OpCodes/Slot/STLOC.json");
            _opCodesSlotStsFld = LoadJson("./Tests/OpCodes/Slot/STSFLD.json");
            _opCodesSlotStsFld5 = LoadJson("./Tests/OpCodes/Slot/STSFLD5.json");
            _opCodesSlotStArg3 = LoadJson("./Tests/OpCodes/Slot/STARG3.json");
            _opCodesSpliceSubStr = LoadJson("./Tests/OpCodes/Splice/SUBSTR.json");
            _opCodesSpliceCat = LoadJson("./Tests/OpCodes/Splice/CAT.json");
            _opCodesSpliceMemCpy = LoadJson("./Tests/OpCodes/Splice/MEMCPY.json");
            _opCodesSpliceLeft = LoadJson("./Tests/OpCodes/Splice/LEFT.json");
            _opCodesSpliceRight = LoadJson("./Tests/OpCodes/Splice/RIGHT.json");
            _opCodesSpliceNewBuffer = LoadJson("./Tests/OpCodes/Splice/NEWBUFFER.json");
            _opCodesControlTryCatchFinally6 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY6.json");
            _opCodesControlJmpLeL = LoadJson("./Tests/OpCodes/Control/JMPLE_L.json");
            _opCodesControlJmpEqL = LoadJson("./Tests/OpCodes/Control/JMPEQ_L.json");
            _opCodesControlJmpLe = LoadJson("./Tests/OpCodes/Control/JMPLE.json");
            _opCodesControlJmpNe = LoadJson("./Tests/OpCodes/Control/JMPNE.json");
            _opCodesControlTryCatchFinally7 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY7.json");
            _opCodesControlAssertMsg = LoadJson("./Tests/OpCodes/Control/ASSERTMSG.json");
            _opCodesControlJmpGt = LoadJson("./Tests/OpCodes/Control/JMPGT.json");
            _opCodesControlJmpL = LoadJson("./Tests/OpCodes/Control/JMP_L.json");
            _opCodesControlJmpIfL = LoadJson("./Tests/OpCodes/Control/JMPIF_L.json");
            _opCodesControlTryFinally = LoadJson("./Tests/OpCodes/Control/TRY_FINALLY.json");
            _opCodesControlJmpNeL = LoadJson("./Tests/OpCodes/Control/JMPNE_L.json");
            _opCodesControlJmpIfNot = LoadJson("./Tests/OpCodes/Control/JMPIFNOT.json");
            _opCodesControlAbortMsg = LoadJson("./Tests/OpCodes/Control/ABORTMSG.json");
            _opCodesControlCall = LoadJson("./Tests/OpCodes/Control/CALL.json");
            _opCodesControlCallL = LoadJson("./Tests/OpCodes/Control/CALL_L.json");
            _opCodesControlJmpGeL = LoadJson("./Tests/OpCodes/Control/JMPGE_L.json");
            _opCodesControlRet = LoadJson("./Tests/OpCodes/Control/RET.json");
            _opCodesControlJmpGtL = LoadJson("./Tests/OpCodes/Control/JMPGT_L.json");
            _opCodesControlJmpEq = LoadJson("./Tests/OpCodes/Control/JMPEQ.json");
            _opCodesControlSysCall = LoadJson("./Tests/OpCodes/Control/SYSCALL.json");
            _opCodesControlCallA = LoadJson("./Tests/OpCodes/Control/CALLA.json");
            _opCodesControlAssert = LoadJson("./Tests/OpCodes/Control/ASSERT.json");
            _opCodesControlTryCatchFinally2 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY2.json");
            _opCodesControlTryCatchFinally3 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY3.json");
            _opCodesControlNop = LoadJson("./Tests/OpCodes/Control/NOP.json");
            _opCodesControlJmpGe = LoadJson("./Tests/OpCodes/Control/JMPGE.json");
            _opCodesControlTryCatchFinally10 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY10.json");
            _opCodesControlJmpLt = LoadJson("./Tests/OpCodes/Control/JMPLT.json");
            _opCodesControlJmpIf = LoadJson("./Tests/OpCodes/Control/JMPIF.json");
            _opCodesControlAbort = LoadJson("./Tests/OpCodes/Control/ABORT.json");
            _opCodesControlJmpIfNotL = LoadJson("./Tests/OpCodes/Control/JMPIFNOT_L.json");
            _opCodesControlTryCatch = LoadJson("./Tests/OpCodes/Control/TRY_CATCH.json");
            _opCodesControlTryCatchFinally4 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY4.json");
            _opCodesControlJmp = LoadJson("./Tests/OpCodes/Control/JMP.json");
            _opCodesControlTryCatchFinally8 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY8.json");
            _opCodesControlTryCatchFinally9 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY9.json");
            _opCodesControlJmpLtL = LoadJson("./Tests/OpCodes/Control/JMPLT_L.json");
            _opCodesControlTryCatchFinally = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY.json");
            _opCodesControlTryCatchFinally5 = LoadJson("./Tests/OpCodes/Control/TRY_CATCH_FINALLY5.json");
            _opCodesControlThrow = LoadJson("./Tests/OpCodes/Control/THROW.json");
            _opCodesPushPushA = LoadJson("./Tests/OpCodes/Push/PUSHA.json");
            _opCodesPushPushM1ToPush16 = LoadJson("./Tests/OpCodes/Push/PUSHM1_to_PUSH16.json");
            _opCodesPushPushInt8ToPushInt256 = LoadJson("./Tests/OpCodes/Push/PUSHINT8_to_PUSHINT256.json");
            _opCodesPushPushData1 = LoadJson("./Tests/OpCodes/Push/PUSHDATA1.json");
            _opCodesPushPushData2 = LoadJson("./Tests/OpCodes/Push/PUSHDATA2.json");
            _opCodesPushPushNull = LoadJson("./Tests/OpCodes/Push/PUSHNULL.json");
            _opCodesPushPushData4 = LoadJson("./Tests/OpCodes/Push/PUSHDATA4.json");
            _opCodesArithmeticGe = LoadJson("./Tests/OpCodes/Arithmetic/GE.json");
            _opCodesArithmeticLt = LoadJson("./Tests/OpCodes/Arithmetic/LT.json");
            _opCodesArithmeticModMul = LoadJson("./Tests/OpCodes/Arithmetic/MODMUL.json");
            _opCodesArithmeticNumNotEqual = LoadJson("./Tests/OpCodes/Arithmetic/NUMNOTEQUAL.json");
            _opCodesArithmeticNot = LoadJson("./Tests/OpCodes/Arithmetic/NOT.json");
            _opCodesArithmeticModPow = LoadJson("./Tests/OpCodes/Arithmetic/MODPOW.json");
            _opCodesArithmeticLe = LoadJson("./Tests/OpCodes/Arithmetic/LE.json");
            _opCodesArithmeticShl = LoadJson("./Tests/OpCodes/Arithmetic/SHL.json");
            _opCodesArithmeticGt = LoadJson("./Tests/OpCodes/Arithmetic/GT.json");
            _opCodesArithmeticPow = LoadJson("./Tests/OpCodes/Arithmetic/POW.json");
            _opCodesArithmeticNumEqual = LoadJson("./Tests/OpCodes/Arithmetic/NUMEQUAL.json");
            _opCodesArithmeticSign = LoadJson("./Tests/OpCodes/Arithmetic/SIGN.json");
            _opCodesArithmeticSqrt = LoadJson("./Tests/OpCodes/Arithmetic/SQRT.json");
            _opCodesArithmeticShr = LoadJson("./Tests/OpCodes/Arithmetic/SHR.json");
            _opCodesBitwiseLogicOr = LoadJson("./Tests/OpCodes/BitwiseLogic/OR.json");
            _opCodesBitwiseLogicEqual = LoadJson("./Tests/OpCodes/BitwiseLogic/EQUAL.json");
            _opCodesBitwiseLogicInvert = LoadJson("./Tests/OpCodes/BitwiseLogic/INVERT.json");
            _opCodesBitwiseLogicXor = LoadJson("./Tests/OpCodes/BitwiseLogic/XOR.json");
            _opCodesBitwiseLogicNotEqual = LoadJson("./Tests/OpCodes/BitwiseLogic/NOTEQUAL.json");
            _opCodesBitwiseLogicAnd = LoadJson("./Tests/OpCodes/BitwiseLogic/AND.json");
            _opCodesTypesConvert = LoadJson("./Tests/OpCodes/Types/CONVERT.json");
            _opCodesTypesIsType = LoadJson("./Tests/OpCodes/Types/ISTYPE.json");
            _opCodesTypesIsNull = LoadJson("./Tests/OpCodes/Types/ISNULL.json");
        }

        [Benchmark]
        public void TestOthersDebugger() => RunTest(_othersDebugger);

        [Benchmark]
        public void TestOthersStackItemLimits() => RunTest(_othersStackItemLimits);

        [Benchmark]
        public void TestOthersScriptLogic() => RunTest(_othersScriptLogic);

        [Benchmark]
        public void TestOthersOtherCases() => RunTest(_othersOtherCases);

        [Benchmark]
        public void TestOthersInvocationLimits() => RunTest(_othersInvocationLimits);

        [Benchmark]
        public void TestOthersInit() => RunTest(_othersInit);

        [Benchmark]
        public void TestOthersStackLimits() => RunTest(_othersStackLimits);

        [Benchmark]
        public void TestOpCodesArraysClearItems() => RunTest(_opCodesArraysClearItems);

        [Benchmark]
        public void TestOpCodesArraysPack() => RunTest(_opCodesArraysPack);

        [Benchmark]
        public void TestOpCodesArraysNewArrayT() => RunTest(_opCodesArraysNewArrayT);

        [Benchmark]
        public void TestOpCodesArraysPickItem() => RunTest(_opCodesArraysPickItem);

        [Benchmark]
        public void TestOpCodesArraysPackStruct() => RunTest(_opCodesArraysPackStruct);

        [Benchmark]
        public void TestOpCodesArraysSetItem() => RunTest(_opCodesArraysSetItem);

        [Benchmark]
        public void TestOpCodesArraysNewStruct0() => RunTest(_opCodesArraysNewStruct0);

        [Benchmark]
        public void TestOpCodesArraysReverseItems() => RunTest(_opCodesArraysReverseItems);

        [Benchmark]
        public void TestOpCodesArraysNewMap() => RunTest(_opCodesArraysNewMap);

        [Benchmark]
        public void TestOpCodesArraysHasKey() => RunTest(_opCodesArraysHasKey);

        [Benchmark]
        public void TestOpCodesArraysNewArray() => RunTest(_opCodesArraysNewArray);

        [Benchmark]
        public void TestOpCodesArraysKeys() => RunTest(_opCodesArraysKeys);

        [Benchmark]
        public void TestOpCodesArraysAppend() => RunTest(_opCodesArraysAppend);

        [Benchmark]
        public void TestOpCodesArraysRemove() => RunTest(_opCodesArraysRemove);

        [Benchmark]
        public void TestOpCodesArraysPackMap() => RunTest(_opCodesArraysPackMap);

        [Benchmark]
        public void TestOpCodesArraysValues() => RunTest(_opCodesArraysValues);

        [Benchmark]
        public void TestOpCodesArraysNewArray0() => RunTest(_opCodesArraysNewArray0);

        [Benchmark]
        public void TestOpCodesArraysUnpack() => RunTest(_opCodesArraysUnpack);

        [Benchmark]
        public void TestOpCodesArraysSize() => RunTest(_opCodesArraysSize);

        [Benchmark]
        public void TestOpCodesArraysNewStruct() => RunTest(_opCodesArraysNewStruct);

        [Benchmark]
        public void TestOpCodesStackXDrop() => RunTest(_opCodesStackXDrop);

        [Benchmark]
        public void TestOpCodesStackReverseN() => RunTest(_opCodesStackReverseN);

        [Benchmark]
        public void TestOpCodesStackReverse4() => RunTest(_opCodesStackReverse4);

        [Benchmark]
        public void TestOpCodesStackClear() => RunTest(_opCodesStackClear);

        [Benchmark]
        public void TestOpCodesStackReverse3() => RunTest(_opCodesStackReverse3);

        [Benchmark]
        public void TestOpCodesStackRot() => RunTest(_opCodesStackRot);

        [Benchmark]
        public void TestOpCodesStackPick() => RunTest(_opCodesStackPick);

        [Benchmark]
        public void TestOpCodesStackNip() => RunTest(_opCodesStackNip);

        [Benchmark]
        public void TestOpCodesStackRoll() => RunTest(_opCodesStackRoll);

        [Benchmark]
        public void TestOpCodesStackDepth() => RunTest(_opCodesStackDepth);

        [Benchmark]
        public void TestOpCodesStackSwap() => RunTest(_opCodesStackSwap);

        [Benchmark]
        public void TestOpCodesStackTuck() => RunTest(_opCodesStackTuck);

        [Benchmark]

        public void TestOpCodesStackOver() => RunTest(_opCodesStackOver);

        [Benchmark]
        public void TestOpCodesStackDrop() => RunTest(_opCodesStackDrop);

        [Benchmark]
        public void TestOpCodesSlotStsFld6() => RunTest(_opCodesSlotStsFld6);

        [Benchmark]
        public void TestOpCodesSlotInitSlot() => RunTest(_opCodesSlotInitSlot);

        [Benchmark]
        public void TestOpCodesSlotLdLoc4() => RunTest(_opCodesSlotLdLoc4);

        [Benchmark]
        public void TestOpCodesSlotLdArg() => RunTest(_opCodesSlotLdArg);

        [Benchmark]
        public void TestOpCodesSlotStArg0() => RunTest(_opCodesSlotStArg0);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD0() => RunTest(_opCodesSlotLdSFLD0);

        [Benchmark]
        public void TestOpCodesSlotLdArg4() => RunTest(_opCodesSlotLdArg4);

        [Benchmark]
        public void TestOpCodesSlotLdArg5() => RunTest(_opCodesSlotLdArg5);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD1() => RunTest(_opCodesSlotLdSFLD1);

        [Benchmark]
        public void TestOpCodesSlotLdLoc() => RunTest(_opCodesSlotLdLoc);

        [Benchmark]
        public void TestOpCodesSlotStArg1() => RunTest(_opCodesSlotStArg1);

        [Benchmark]
        public void TestOpCodesSlotLdLoc5() => RunTest(_opCodesSlotLdLoc5);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD6() => RunTest(_opCodesSlotLdSFLD6);

        [Benchmark]
        public void TestOpCodesSlotLdArg2() => RunTest(_opCodesSlotLdArg2);

        [Benchmark]
        public void TestOpCodesSlotStsFld0() => RunTest(_opCodesSlotStsFld0);

        [Benchmark]
        public void TestOpCodesSlotLdLoc2() => RunTest(_opCodesSlotLdLoc2);

        [Benchmark]
        public void TestOpCodesSlotStArg6() => RunTest(_opCodesSlotStArg6);

        [Benchmark]
        public void TestOpCodesSlotLdLoc3() => RunTest(_opCodesSlotLdLoc3);

        [Benchmark]
        public void TestOpCodesSlotStsFld1() => RunTest(_opCodesSlotStsFld1);

        [Benchmark]
        public void TestOpCodesSlotInitSSlot() => RunTest(_opCodesSlotInitSSlot);

        [Benchmark]
        public void TestOpCodesSlotLdArg3() => RunTest(_opCodesSlotLdArg3);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD() => RunTest(_opCodesSlotLdSFLD);

        [Benchmark]
        public void TestOpCodesSlotLdArg0() => RunTest(_opCodesSlotLdArg0);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD4() => RunTest(_opCodesSlotLdSFLD4);

        [Benchmark]
        public void TestOpCodesSlotStArg4() => RunTest(_opCodesSlotStArg4);

        [Benchmark]
        public void TestOpCodesSlotLdLoc0() => RunTest(_opCodesSlotLdLoc0);

        [Benchmark]
        public void TestOpCodesSlotStsFld2() => RunTest(_opCodesSlotStsFld2);

        [Benchmark]
        public void TestOpCodesSlotStsFld3() => RunTest(_opCodesSlotStsFld3);

        [Benchmark]
        public void TestOpCodesSlotLdLoc1() => RunTest(_opCodesSlotLdLoc1);

        [Benchmark]
        public void TestOpCodesSlotStArg5() => RunTest(_opCodesSlotStArg5);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD5() => RunTest(_opCodesSlotLdSFLD5);

        [Benchmark]
        public void TestOpCodesSlotLdArg1() => RunTest(_opCodesSlotLdArg1);

        [Benchmark]
        public void TestOpCodesSlotStArg2() => RunTest(_opCodesSlotStArg2);

        [Benchmark]
        public void TestOpCodesSlotLdLoc6() => RunTest(_opCodesSlotLdLoc6);

        [Benchmark]
        public void TestOpCodesSlotStArg() => RunTest(_opCodesSlotStArg);

        [Benchmark]
        public void TestOpCodesSlotStsFld4() => RunTest(_opCodesSlotStsFld4);

        [Benchmark]
        public void TestOpCodesSlotLdArg6() => RunTest(_opCodesSlotLdArg6);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD2() => RunTest(_opCodesSlotLdSFLD2);

        [Benchmark]
        public void TestOpCodesSlotLdSFLD3() => RunTest(_opCodesSlotLdSFLD3);

        [Benchmark]
        public void TestOpCodesSlotStLoc() => RunTest(_opCodesSlotStLoc);

        [Benchmark]
        public void TestOpCodesSlotStsFld() => RunTest(_opCodesSlotStsFld);

        [Benchmark]
        public void TestOpCodesSlotStsFld5() => RunTest(_opCodesSlotStsFld5);

        [Benchmark]
        public void TestOpCodesSlotStArg3() => RunTest(_opCodesSlotStArg3);

        [Benchmark]
        public void TestOpCodesSpliceSubStr() => RunTest(_opCodesSpliceSubStr);

        [Benchmark]
        public void TestOpCodesSpliceCat() => RunTest(_opCodesSpliceCat);

        [Benchmark]
        public void TestOpCodesSpliceMemCpy() => RunTest(_opCodesSpliceMemCpy);

        [Benchmark]
        public void TestOpCodesSpliceLeft() => RunTest(_opCodesSpliceLeft);

        [Benchmark]
        public void TestOpCodesSpliceRight() => RunTest(_opCodesSpliceRight);

        [Benchmark]

        public void TestOpCodesSpliceNewBuffer() => RunTest(_opCodesSpliceNewBuffer);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally6() => RunTest(_opCodesControlTryCatchFinally6);

        [Benchmark]
        public void TestOpCodesControlJmpLeL() => RunTest(_opCodesControlJmpLeL);

        [Benchmark]
        public void TestOpCodesControlJmpEqL() => RunTest(_opCodesControlJmpEqL);

        [Benchmark]
        public void TestOpCodesControlJmpLe() => RunTest(_opCodesControlJmpLe);

        [Benchmark]
        public void TestOpCodesControlJmpNe() => RunTest(_opCodesControlJmpNe);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally7() => RunTest(_opCodesControlTryCatchFinally7);

        [Benchmark]
        public void TestOpCodesControlAssertMsg() => RunTest(_opCodesControlAssertMsg);

        [Benchmark]
        public void TestOpCodesControlJmpGt() => RunTest(_opCodesControlJmpGt);

        [Benchmark]

        public void TestOpCodesControlJmpL() => RunTest(_opCodesControlJmpL);

        [Benchmark]
        public void TestOpCodesControlJmpIfL() => RunTest(_opCodesControlJmpIfL);

        [Benchmark]
        public void TestOpCodesControlTryFinally() => RunTest(_opCodesControlTryFinally);

        [Benchmark]
        public void TestOpCodesControlJmpNeL() => RunTest(_opCodesControlJmpNeL);

        [Benchmark]
        public void TestOpCodesControlJmpIfNot() => RunTest(_opCodesControlJmpIfNot);

        [Benchmark]
        public void TestOpCodesControlAbortMsg() => RunTest(_opCodesControlAbortMsg);

        [Benchmark]
        public void TestOpCodesControlCall() => RunTest(_opCodesControlCall);

        [Benchmark]
        public void TestOpCodesControlCallL() => RunTest(_opCodesControlCallL);

        [Benchmark]
        public void TestOpCodesControlJmpGeL() => RunTest(_opCodesControlJmpGeL);

        [Benchmark]
        public void TestOpCodesControlRet() => RunTest(_opCodesControlRet);

        [Benchmark]
        public void TestOpCodesControlJmpGtL() => RunTest(_opCodesControlJmpGtL);

        [Benchmark]
        public void TestOpCodesControlJmpEq() => RunTest(_opCodesControlJmpEq);

        [Benchmark]

        public void TestOpCodesControlSysCall() => RunTest(_opCodesControlSysCall);

        [Benchmark]
        public void TestOpCodesControlCallA() => RunTest(_opCodesControlCallA);

        [Benchmark]
        public void TestOpCodesControlAssert() => RunTest(_opCodesControlAssert);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally2() => RunTest(_opCodesControlTryCatchFinally2);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally3() => RunTest(_opCodesControlTryCatchFinally3);

        [Benchmark]
        public void TestOpCodesControlNop() => RunTest(_opCodesControlNop);

        [Benchmark]
        public void TestOpCodesControlJmpGe() => RunTest(_opCodesControlJmpGe);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally10() => RunTest(_opCodesControlTryCatchFinally10);

        [Benchmark]
        public void TestOpCodesControlJmpLt() => RunTest(_opCodesControlJmpLt);

        [Benchmark]
        public void TestOpCodesControlJmpIf() => RunTest(_opCodesControlJmpIf);

        [Benchmark]
        public void TestOpCodesControlAbort() => RunTest(_opCodesControlAbort);

        [Benchmark]
        public void TestOpCodesControlJmpIfNotL() => RunTest(_opCodesControlJmpIfNotL);

        [Benchmark]
        public void TestOpCodesControlTryCatch() => RunTest(_opCodesControlTryCatch);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally4() => RunTest(_opCodesControlTryCatchFinally4);

        [Benchmark]
        public void TestOpCodesControlJmp() => RunTest(_opCodesControlJmp);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally8() => RunTest(_opCodesControlTryCatchFinally8);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally9() => RunTest(_opCodesControlTryCatchFinally9);

        [Benchmark]
        public void TestOpCodesControlJmpLtL() => RunTest(_opCodesControlJmpLtL);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally() => RunTest(_opCodesControlTryCatchFinally);

        [Benchmark]
        public void TestOpCodesControlTryCatchFinally5() => RunTest(_opCodesControlTryCatchFinally5);

        [Benchmark]
        public void TestOpCodesControlThrow() => RunTest(_opCodesControlThrow);

        [Benchmark]
        public void TestOpCodesPushPushA() => RunTest(_opCodesPushPushA);

        [Benchmark]
        public void TestOpCodesPushPushM1ToPush16() => RunTest(_opCodesPushPushM1ToPush16);

        [Benchmark]
        public void TestOpCodesPushPushInt8ToPushInt256() => RunTest(_opCodesPushPushInt8ToPushInt256);

        [Benchmark]
        public void TestOpCodesPushPushData1() => RunTest(_opCodesPushPushData1);

        [Benchmark]
        public void TestOpCodesPushPushData2() => RunTest(_opCodesPushPushData2);

        [Benchmark]
        public void TestOpCodesPushPushNull() => RunTest(_opCodesPushPushNull);

        [Benchmark]
        public void TestOpCodesPushPushData4() => RunTest(_opCodesPushPushData4);

        [Benchmark]
        public void TestOpCodesArithmeticGe() => RunTest(_opCodesArithmeticGe);

        [Benchmark]
        public void TestOpCodesArithmeticLt() => RunTest(_opCodesArithmeticLt);

        [Benchmark]
        public void TestOpCodesArithmeticModMul() => RunTest(_opCodesArithmeticModMul);

        [Benchmark]
        public void TestOpCodesArithmeticNumNotEqual() => RunTest(_opCodesArithmeticNumNotEqual);

        [Benchmark]
        public void TestOpCodesArithmeticNot() => RunTest(_opCodesArithmeticNot);

        [Benchmark]
        public void TestOpCodesArithmeticModPow() => RunTest(_opCodesArithmeticModPow);

        [Benchmark]
        public void TestOpCodesArithmeticLe() => RunTest(_opCodesArithmeticLe);

        [Benchmark]
        public void TestOpCodesArithmeticShl() => RunTest(_opCodesArithmeticShl);

        [Benchmark]
        public void TestOpCodesArithmeticGt() => RunTest(_opCodesArithmeticGt);

        [Benchmark]
        public void TestOpCodesArithmeticPow() => RunTest(_opCodesArithmeticPow);

        [Benchmark]
        public void TestOpCodesArithmeticNumEqual() => RunTest(_opCodesArithmeticNumEqual);

        [Benchmark]
        public void TestOpCodesArithmeticSign() => RunTest(_opCodesArithmeticSign);

        [Benchmark]
        public void TestOpCodesArithmeticSqrt() => RunTest(_opCodesArithmeticSqrt);

        [Benchmark]

        public void TestOpCodesArithmeticShr() => RunTest(_opCodesArithmeticShr);

        [Benchmark]
        public void TestOpCodesBitwiseLogicOr() => RunTest(_opCodesBitwiseLogicOr);

        [Benchmark]
        public void TestOpCodesBitwiseLogicEqual() => RunTest(_opCodesBitwiseLogicEqual);

        [Benchmark]
        public void TestOpCodesBitwiseLogicInvert() => RunTest(_opCodesBitwiseLogicInvert);

        [Benchmark]
        public void TestOpCodesBitwiseLogicXor() => RunTest(_opCodesBitwiseLogicXor);

        [Benchmark]
        public void TestOpCodesBitwiseLogicNotEqual() => RunTest(_opCodesBitwiseLogicNotEqual);

        [Benchmark]
        public void TestOpCodesBitwiseLogicAnd() => RunTest(_opCodesBitwiseLogicAnd);

        [Benchmark]
        public void TestOpCodesTypesConvert() => RunTest(_opCodesTypesConvert);

        [Benchmark]
        public void TestOpCodesTypesIsType() => RunTest(_opCodesTypesIsType);

        [Benchmark]
        public void TestOpCodesTypesIsNull() => RunTest(_opCodesTypesIsNull);

        private VMUT LoadJson(string path)
        {
            var realFile = Path.GetFullPath(Root + path);
            var json = File.ReadAllText(realFile, Encoding.UTF8);
            var ut = json.DeserializeJson<VMUT>();
            Assert.IsFalse(string.IsNullOrEmpty(ut.Name), "Name is required");
            if (json != ut.ToJson().Replace("\r\n", "\n"))
            {
                // Format json
                // Console.WriteLine($"The file '{realFile}' was optimized");
                //File.WriteAllText(realFile, ut.ToJson().Replace("\r\n", "\n"), Encoding.UTF8);
            }
            return ut;
        }

        private void RunTest(VMUT? ut)
        {
            try
            {
                ExecuteTest(ut);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
