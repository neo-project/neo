// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;

namespace Neo.VM
{
    /// <summary>
    /// Represents the opcode of an <see cref="Instruction"/>.
    /// </summary>
    public enum OpCode : byte
    {
        #region Constants

        /// <summary>
        /// Pushes a 1-byte signed integer onto the stack.
        /// </summary>
        [OperandSize(Size = 1)]
        PUSHINT8 = 0x00,
        /// <summary>
        /// Pushes a 2-bytes signed integer onto the stack.
        /// </summary>
        [OperandSize(Size = 2)]
        PUSHINT16 = 0x01,
        /// <summary>
        /// Pushes a 4-bytes signed integer onto the stack.
        /// </summary>
        [OperandSize(Size = 4)]
        PUSHINT32 = 0x02,
        /// <summary>
        /// Pushes a 8-bytes signed integer onto the stack.
        /// </summary>
        [OperandSize(Size = 8)]
        PUSHINT64 = 0x03,
        /// <summary>
        /// Pushes a 16-bytes signed integer onto the stack.
        /// </summary>
        [OperandSize(Size = 16)]
        PUSHINT128 = 0x04,
        /// <summary>
        /// Pushes a 32-bytes signed integer onto the stack.
        /// </summary>
        [OperandSize(Size = 32)]
        PUSHINT256 = 0x05,
        /// <summary>
        /// Pushes the boolean value <see langword="true"/> onto the stack.
        /// </summary>
        PUSHT = 0x08,
        /// <summary>
        /// Pushes the boolean value <see langword="false"/> onto the stack.
        /// </summary>
        PUSHF = 0x09,
        /// <summary>
        /// Converts the 4-bytes offset to an <see cref="Pointer"/>, and pushes it onto the stack.
        /// </summary>
        [OperandSize(Size = 4)]
        PUSHA = 0x0A,
        /// <summary>
        /// The item <see langword="null"/> is pushed onto the stack.
        /// </summary>
        PUSHNULL = 0x0B,
        /// <summary>
        /// The next byte contains the number of bytes to be pushed onto the stack.
        /// </summary>
        [OperandSize(SizePrefix = 1)]
        PUSHDATA1 = 0x0C,
        /// <summary>
        /// The next two bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        [OperandSize(SizePrefix = 2)]
        PUSHDATA2 = 0x0D,
        /// <summary>
        /// The next four bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        [OperandSize(SizePrefix = 4)]
        PUSHDATA4 = 0x0E,
        /// <summary>
        /// The number -1 is pushed onto the stack.
        /// </summary>
        PUSHM1 = 0x0F,
        /// <summary>
        /// The number 0 is pushed onto the stack.
        /// </summary>
        PUSH0 = 0x10,
        /// <summary>
        /// The number 1 is pushed onto the stack.
        /// </summary>
        PUSH1 = 0x11,
        /// <summary>
        /// The number 2 is pushed onto the stack.
        /// </summary>
        PUSH2 = 0x12,
        /// <summary>
        /// The number 3 is pushed onto the stack.
        /// </summary>
        PUSH3 = 0x13,
        /// <summary>
        /// The number 4 is pushed onto the stack.
        /// </summary>
        PUSH4 = 0x14,
        /// <summary>
        /// The number 5 is pushed onto the stack.
        /// </summary>
        PUSH5 = 0x15,
        /// <summary>
        /// The number 6 is pushed onto the stack.
        /// </summary>
        PUSH6 = 0x16,
        /// <summary>
        /// The number 7 is pushed onto the stack.
        /// </summary>
        PUSH7 = 0x17,
        /// <summary>
        /// The number 8 is pushed onto the stack.
        /// </summary>
        PUSH8 = 0x18,
        /// <summary>
        /// The number 9 is pushed onto the stack.
        /// </summary>
        PUSH9 = 0x19,
        /// <summary>
        /// The number 10 is pushed onto the stack.
        /// </summary>
        PUSH10 = 0x1A,
        /// <summary>
        /// The number 11 is pushed onto the stack.
        /// </summary>
        PUSH11 = 0x1B,
        /// <summary>
        /// The number 12 is pushed onto the stack.
        /// </summary>
        PUSH12 = 0x1C,
        /// <summary>
        /// The number 13 is pushed onto the stack.
        /// </summary>
        PUSH13 = 0x1D,
        /// <summary>
        /// The number 14 is pushed onto the stack.
        /// </summary>
        PUSH14 = 0x1E,
        /// <summary>
        /// The number 15 is pushed onto the stack.
        /// </summary>
        PUSH15 = 0x1F,
        /// <summary>
        /// The number 16 is pushed onto the stack.
        /// </summary>
        PUSH16 = 0x20,

        #endregion

        #region Flow control

        /// <summary>
        /// The <see cref="NOP"/> operation does nothing. It is intended to fill in space if opcodes are patched.
        /// </summary>
        NOP = 0x21,
        /// <summary>
        /// Unconditionally transfers control to a target instruction. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMP = 0x22,
        /// <summary>
        /// Unconditionally transfers control to a target instruction. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMP_L = 0x23,
        /// <summary>
        /// Transfers control to a target instruction if the value is <see langword="true"/>, not <see langword="null"/>, or non-zero. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPIF = 0x24,
        /// <summary>
        /// Transfers control to a target instruction if the value is <see langword="true"/>, not <see langword="null"/>, or non-zero. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPIF_L = 0x25,
        /// <summary>
        /// Transfers control to a target instruction if the value is <see langword="false"/>, a <see langword="null"/> reference, or zero. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPIFNOT = 0x26,
        /// <summary>
        /// Transfers control to a target instruction if the value is <see langword="false"/>, a <see langword="null"/> reference, or zero. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPIFNOT_L = 0x27,
        /// <summary>
        /// Transfers control to a target instruction if two values are equal. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPEQ = 0x28,
        /// <summary>
        /// Transfers control to a target instruction if two values are equal. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPEQ_L = 0x29,
        /// <summary>
        /// Transfers control to a target instruction when two values are not equal. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPNE = 0x2A,
        /// <summary>
        /// Transfers control to a target instruction when two values are not equal. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPNE_L = 0x2B,
        /// <summary>
        /// Transfers control to a target instruction if the first value is greater than the second value. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPGT = 0x2C,
        /// <summary>
        /// Transfers control to a target instruction if the first value is greater than the second value. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPGT_L = 0x2D,
        /// <summary>
        /// Transfers control to a target instruction if the first value is greater than or equal to the second value. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPGE = 0x2E,
        /// <summary>
        /// Transfers control to a target instruction if the first value is greater than or equal to the second value. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPGE_L = 0x2F,
        /// <summary>
        /// Transfers control to a target instruction if the first value is less than the second value. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPLT = 0x30,
        /// <summary>
        /// Transfers control to a target instruction if the first value is less than the second value. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPLT_L = 0x31,
        /// <summary>
        /// Transfers control to a target instruction if the first value is less than or equal to the second value. The target instruction is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        JMPLE = 0x32,
        /// <summary>
        /// Transfers control to a target instruction if the first value is less than or equal to the second value. The target instruction is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        JMPLE_L = 0x33,
        /// <summary>
        /// Calls the function at the target address which is represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        CALL = 0x34,
        /// <summary>
        /// Calls the function at the target address which is represented as a 4-bytes signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        CALL_L = 0x35,
        /// <summary>
        /// Pop the address of a function from the stack, and call the function.
        /// </summary>
        CALLA = 0x36,
        /// <summary>
        /// Calls the function which is described by the token.
        /// </summary>
        [OperandSize(Size = 2)]
        CALLT = 0x37,
        /// <summary>
        /// It turns the vm state to FAULT immediately, and cannot be caught.
        /// </summary>
        ABORT = 0x38,
        /// <summary>
        /// Pop the top value of the stack. If it's false, exit vm execution and set vm state to FAULT.
        /// </summary>
        ASSERT = 0x39,
        /// <summary>
        /// Pop the top value of the stack, and throw it.
        /// </summary>
        THROW = 0x3A,
        /// <summary>
        /// TRY CatchOffset(sbyte) FinallyOffset(sbyte). If there's no catch body, set CatchOffset 0. If there's no finally body, set FinallyOffset 0.
        /// </summary>
        [OperandSize(Size = 2)]
        TRY = 0x3B,
        /// <summary>
        /// TRY_L CatchOffset(int) FinallyOffset(int). If there's no catch body, set CatchOffset 0. If there's no finally body, set FinallyOffset 0.
        /// </summary>
        [OperandSize(Size = 8)]
        TRY_L = 0x3C,
        /// <summary>
        /// Ensures that the appropriate surrounding finally blocks are executed. And then unconditionally transfers control to the specific target instruction, represented as a 1-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 1)]
        ENDTRY = 0x3D,
        /// <summary>
        /// Ensures that the appropriate surrounding finally blocks are executed. And then unconditionally transfers control to the specific target instruction, represented as a 4-byte signed offset from the beginning of the current instruction.
        /// </summary>
        [OperandSize(Size = 4)]
        ENDTRY_L = 0x3E,
        /// <summary>
        /// End finally, If no exception happen or be catched, vm will jump to the target instruction of ENDTRY/ENDTRY_L. Otherwise vm will rethrow the exception to upper layer.
        /// </summary>
        ENDFINALLY = 0x3F,
        /// <summary>
        /// Returns from the current method.
        /// </summary>
        RET = 0x40,
        /// <summary>
        /// Calls to an interop service.
        /// </summary>
        [OperandSize(Size = 4)]
        SYSCALL = 0x41,

        #endregion

        #region Stack

        /// <summary>
        /// Puts the number of stack items onto the stack.
        /// </summary>
        DEPTH = 0x43,
        /// <summary>
        /// Removes the top stack item.
        /// </summary>
        DROP = 0x45,
        /// <summary>
        /// Removes the second-to-top stack item.
        /// </summary>
        NIP = 0x46,
        /// <summary>
        /// The item n back in the main stack is removed.
        /// </summary>
        XDROP = 0x48,
        /// <summary>
        /// Clear the stack
        /// </summary>
        CLEAR = 0x49,
        /// <summary>
        /// Duplicates the top stack item.
        /// </summary>
        DUP = 0x4A,
        /// <summary>
        /// Copies the second-to-top stack item to the top.
        /// </summary>
        OVER = 0x4B,
        /// <summary>
        /// The item n back in the stack is copied to the top.
        /// </summary>
        PICK = 0x4D,
        /// <summary>
        /// The item at the top of the stack is copied and inserted before the second-to-top item.
        /// </summary>
        TUCK = 0x4E,
        /// <summary>
        /// The top two items on the stack are swapped.
        /// </summary>
        SWAP = 0x50,
        /// <summary>
        /// The top three items on the stack are rotated to the left.
        /// </summary>
        ROT = 0x51,
        /// <summary>
        /// The item n back in the stack is moved to the top.
        /// </summary>
        ROLL = 0x52,
        /// <summary>
        /// Reverse the order of the top 3 items on the stack.
        /// </summary>
        REVERSE3 = 0x53,
        /// <summary>
        /// Reverse the order of the top 4 items on the stack.
        /// </summary>
        REVERSE4 = 0x54,
        /// <summary>
        /// Pop the number N on the stack, and reverse the order of the top N items on the stack.
        /// </summary>
        REVERSEN = 0x55,

        #endregion

        #region Slot

        /// <summary>
        /// Initialize the static field list for the current execution context.
        /// </summary>
        [OperandSize(Size = 1)]
        INITSSLOT = 0x56,
        /// <summary>
        /// Initialize the argument slot and the local variable list for the current execution context.
        /// </summary>
        [OperandSize(Size = 2)]
        INITSLOT = 0x57,
        /// <summary>
        /// Loads the static field at index 0 onto the evaluation stack.
        /// </summary>
        LDSFLD0 = 0x58,
        /// <summary>
        /// Loads the static field at index 1 onto the evaluation stack.
        /// </summary>
        LDSFLD1 = 0x59,
        /// <summary>
        /// Loads the static field at index 2 onto the evaluation stack.
        /// </summary>
        LDSFLD2 = 0x5A,
        /// <summary>
        /// Loads the static field at index 3 onto the evaluation stack.
        /// </summary>
        LDSFLD3 = 0x5B,
        /// <summary>
        /// Loads the static field at index 4 onto the evaluation stack.
        /// </summary>
        LDSFLD4 = 0x5C,
        /// <summary>
        /// Loads the static field at index 5 onto the evaluation stack.
        /// </summary>
        LDSFLD5 = 0x5D,
        /// <summary>
        /// Loads the static field at index 6 onto the evaluation stack.
        /// </summary>
        LDSFLD6 = 0x5E,
        /// <summary>
        /// Loads the static field at a specified index onto the evaluation stack. The index is represented as a 1-byte unsigned integer.
        /// </summary>
        [OperandSize(Size = 1)]
        LDSFLD = 0x5F,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 0.
        /// </summary>
        STSFLD0 = 0x60,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 1.
        /// </summary>
        STSFLD1 = 0x61,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 2.
        /// </summary>
        STSFLD2 = 0x62,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 3.
        /// </summary>
        STSFLD3 = 0x63,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 4.
        /// </summary>
        STSFLD4 = 0x64,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 5.
        /// </summary>
        STSFLD5 = 0x65,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at index 6.
        /// </summary>
        STSFLD6 = 0x66,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the static field list at a specified index. The index is represented as a 1-byte unsigned integer.
        /// </summary>
        [OperandSize(Size = 1)]
        STSFLD = 0x67,
        /// <summary>
        /// Loads the local variable at index 0 onto the evaluation stack.
        /// </summary>
        LDLOC0 = 0x68,
        /// <summary>
        /// Loads the local variable at index 1 onto the evaluation stack.
        /// </summary>
        LDLOC1 = 0x69,
        /// <summary>
        /// Loads the local variable at index 2 onto the evaluation stack.
        /// </summary>
        LDLOC2 = 0x6A,
        /// <summary>
        /// Loads the local variable at index 3 onto the evaluation stack.
        /// </summary>
        LDLOC3 = 0x6B,
        /// <summary>
        /// Loads the local variable at index 4 onto the evaluation stack.
        /// </summary>
        LDLOC4 = 0x6C,
        /// <summary>
        /// Loads the local variable at index 5 onto the evaluation stack.
        /// </summary>
        LDLOC5 = 0x6D,
        /// <summary>
        /// Loads the local variable at index 6 onto the evaluation stack.
        /// </summary>
        LDLOC6 = 0x6E,
        /// <summary>
        /// Loads the local variable at a specified index onto the evaluation stack. The index is represented as a 1-byte unsigned integer.
        /// </summary>
        [OperandSize(Size = 1)]
        LDLOC = 0x6F,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 0.
        /// </summary>
        STLOC0 = 0x70,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 1.
        /// </summary>
        STLOC1 = 0x71,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 2.
        /// </summary>
        STLOC2 = 0x72,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 3.
        /// </summary>
        STLOC3 = 0x73,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 4.
        /// </summary>
        STLOC4 = 0x74,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 5.
        /// </summary>
        STLOC5 = 0x75,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at index 6.
        /// </summary>
        STLOC6 = 0x76,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the local variable list at a specified index. The index is represented as a 1-byte unsigned integer.
        /// </summary>
        [OperandSize(Size = 1)]
        STLOC = 0x77,
        /// <summary>
        /// Loads the argument at index 0 onto the evaluation stack.
        /// </summary>
        LDARG0 = 0x78,
        /// <summary>
        /// Loads the argument at index 1 onto the evaluation stack.
        /// </summary>
        LDARG1 = 0x79,
        /// <summary>
        /// Loads the argument at index 2 onto the evaluation stack.
        /// </summary>
        LDARG2 = 0x7A,
        /// <summary>
        /// Loads the argument at index 3 onto the evaluation stack.
        /// </summary>
        LDARG3 = 0x7B,
        /// <summary>
        /// Loads the argument at index 4 onto the evaluation stack.
        /// </summary>
        LDARG4 = 0x7C,
        /// <summary>
        /// Loads the argument at index 5 onto the evaluation stack.
        /// </summary>
        LDARG5 = 0x7D,
        /// <summary>
        /// Loads the argument at index 6 onto the evaluation stack.
        /// </summary>
        LDARG6 = 0x7E,
        /// <summary>
        /// Loads the argument at a specified index onto the evaluation stack. The index is represented as a 1-byte unsigned integer.
        /// </summary>
        [OperandSize(Size = 1)]
        LDARG = 0x7F,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 0.
        /// </summary>
        STARG0 = 0x80,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 1.
        /// </summary>
        STARG1 = 0x81,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 2.
        /// </summary>
        STARG2 = 0x82,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 3.
        /// </summary>
        STARG3 = 0x83,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 4.
        /// </summary>
        STARG4 = 0x84,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 5.
        /// </summary>
        STARG5 = 0x85,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at index 6.
        /// </summary>
        STARG6 = 0x86,
        /// <summary>
        /// Stores the value on top of the evaluation stack in the argument slot at a specified index. The index is represented as a 1-byte unsigned integer.
        /// </summary>
        [OperandSize(Size = 1)]
        STARG = 0x87,

        #endregion

        #region Splice

        /// <summary>
        /// Creates a new <see cref="Buffer"/> and pushes it onto the stack.
        /// </summary>
        NEWBUFFER = 0x88,
        /// <summary>
        /// Copies a range of bytes from one <see cref="Buffer"/> to another.
        /// </summary>
        MEMCPY = 0x89,
        /// <summary>
        /// Concatenates two strings.
        /// </summary>
        CAT = 0x8B,
        /// <summary>
        /// Returns a section of a string.
        /// </summary>
        SUBSTR = 0x8C,
        /// <summary>
        /// Keeps only characters left of the specified point in a string.
        /// </summary>
        LEFT = 0x8D,
        /// <summary>
        /// Keeps only characters right of the specified point in a string.
        /// </summary>
        RIGHT = 0x8E,

        #endregion

        #region Bitwise logic

        /// <summary>
        /// Flips all of the bits in the input.
        /// </summary>
        INVERT = 0x90,
        /// <summary>
        /// Boolean and between each bit in the inputs.
        /// </summary>
        AND = 0x91,
        /// <summary>
        /// Boolean or between each bit in the inputs.
        /// </summary>
        OR = 0x92,
        /// <summary>
        /// Boolean exclusive or between each bit in the inputs.
        /// </summary>
        XOR = 0x93,
        /// <summary>
        /// Returns 1 if the inputs are exactly equal, 0 otherwise.
        /// </summary>
        EQUAL = 0x97,
        /// <summary>
        /// Returns 1 if the inputs are not equal, 0 otherwise.
        /// </summary>
        NOTEQUAL = 0x98,

        #endregion

        #region Arithmetic

        /// <summary>
        /// Puts the sign of top stack item on top of the main stack. If value is negative, put -1; if positive, put 1; if value is zero, put 0.
        /// </summary>
        SIGN = 0x99,
        /// <summary>
        /// The input is made positive.
        /// </summary>
        ABS = 0x9A,
        /// <summary>
        /// The sign of the input is flipped.
        /// </summary>
        NEGATE = 0x9B,
        /// <summary>
        /// 1 is added to the input.
        /// </summary>
        INC = 0x9C,
        /// <summary>
        /// 1 is subtracted from the input.
        /// </summary>
        DEC = 0x9D,
        /// <summary>
        /// a is added to b.
        /// </summary>
        ADD = 0x9E,
        /// <summary>
        /// b is subtracted from a.
        /// </summary>
        SUB = 0x9F,
        /// <summary>
        /// a is multiplied by b.
        /// </summary>
        MUL = 0xA0,
        /// <summary>
        /// a is divided by b.
        /// </summary>
        DIV = 0xA1,
        /// <summary>
        /// Returns the remainder after dividing a by b.
        /// </summary>
        MOD = 0xA2,
        /// <summary>
        /// The result of raising value to the exponent power.
        /// </summary>
        POW = 0xA3,
        /// <summary>
        /// Returns the square root of a specified number.
        /// </summary>
        SQRT = 0xA4,
        /// <summary>
        /// Performs modulus division on a number multiplied by another number.
        /// </summary>
        MODMUL = 0xA5,
        /// <summary>
        /// Performs modulus division on a number raised to the power of another number. If the exponent is -1, it will have the calculation of the modular inverse.
        /// </summary>
        MODPOW = 0xA6,
        /// <summary>
        /// Shifts a left b bits, preserving sign.
        /// </summary>
        SHL = 0xA8,
        /// <summary>
        /// Shifts a right b bits, preserving sign.
        /// </summary>
        SHR = 0xA9,
        /// <summary>
        /// If the input is 0 or 1, it is flipped. Otherwise the output will be 0.
        /// </summary>
        NOT = 0xAA,
        /// <summary>
        /// If both a and b are not 0, the output is 1. Otherwise 0.
        /// </summary>
        BOOLAND = 0xAB,
        /// <summary>
        /// If a or b is not 0, the output is 1. Otherwise 0.
        /// </summary>
        BOOLOR = 0xAC,
        /// <summary>
        /// Returns 0 if the input is 0. 1 otherwise.
        /// </summary>
        NZ = 0xB1,
        /// <summary>
        /// Returns 1 if the numbers are equal, 0 otherwise.
        /// </summary>
        NUMEQUAL = 0xB3,
        /// <summary>
        /// Returns 1 if the numbers are not equal, 0 otherwise.
        /// </summary>
        NUMNOTEQUAL = 0xB4,
        /// <summary>
        /// Returns 1 if a is less than b, 0 otherwise.
        /// </summary>
        LT = 0xB5,
        /// <summary>
        /// Returns 1 if a is less than or equal to b, 0 otherwise.
        /// </summary>
        LE = 0xB6,
        /// <summary>
        /// Returns 1 if a is greater than b, 0 otherwise.
        /// </summary>
        GT = 0xB7,
        /// <summary>
        /// Returns 1 if a is greater than or equal to b, 0 otherwise.
        /// </summary>
        GE = 0xB8,
        /// <summary>
        /// Returns the smaller of a and b.
        /// </summary>
        MIN = 0xB9,
        /// <summary>
        /// Returns the larger of a and b.
        /// </summary>
        MAX = 0xBA,
        /// <summary>
        /// Returns 1 if x is within the specified range (left-inclusive), 0 otherwise.
        /// </summary>
        WITHIN = 0xBB,

        #endregion

        #region Compound-type

        /// <summary>
        /// A value n is taken from top of main stack. The next n*2 items on main stack are removed, put inside n-sized map and this map is put on top of the main stack.
        /// </summary>
        PACKMAP = 0xBE,
        /// <summary>
        /// A value n is taken from top of main stack. The next n items on main stack are removed, put inside n-sized struct and this struct is put on top of the main stack.
        /// </summary>
        PACKSTRUCT = 0xBF,
        /// <summary>
        /// A value n is taken from top of main stack. The next n items on main stack are removed, put inside n-sized array and this array is put on top of the main stack.
        /// </summary>
        PACK = 0xC0,
        /// <summary>
        /// A collection is removed from top of the main stack. Its elements are put on top of the main stack (in reverse order) and the collection size is also put on main stack.
        /// </summary>
        UNPACK = 0xC1,
        /// <summary>
        /// An empty array (with size 0) is put on top of the main stack.
        /// </summary>
        NEWARRAY0 = 0xC2,
        /// <summary>
        /// A value n is taken from top of main stack. A null-filled array with size n is put on top of the main stack.
        /// </summary>
        NEWARRAY = 0xC3,
        /// <summary>
        /// A value n is taken from top of main stack. An array of type T with size n is put on top of the main stack.
        /// </summary>
        [OperandSize(Size = 1)]
        NEWARRAY_T = 0xC4,
        /// <summary>
        /// An empty struct (with size 0) is put on top of the main stack.
        /// </summary>
        NEWSTRUCT0 = 0xC5,
        /// <summary>
        /// A value n is taken from top of main stack. A zero-filled struct with size n is put on top of the main stack.
        /// </summary>
        NEWSTRUCT = 0xC6,
        /// <summary>
        /// A Map is created and put on top of the main stack.
        /// </summary>
        NEWMAP = 0xC8,
        /// <summary>
        /// An array is removed from top of the main stack. Its size is put on top of the main stack.
        /// </summary>
        SIZE = 0xCA,
        /// <summary>
        /// An input index n (or key) and an array (or map) are removed from the top of the main stack. Puts True on top of main stack if array[n] (or map[n]) exist, and False otherwise.
        /// </summary>
        HASKEY = 0xCB,
        /// <summary>
        /// A map is taken from top of the main stack. The keys of this map are put on top of the main stack.
        /// </summary>
        KEYS = 0xCC,
        /// <summary>
        /// A map is taken from top of the main stack. The values of this map are put on top of the main stack.
        /// </summary>
        VALUES = 0xCD,
        /// <summary>
        /// An input index n (or key) and an array (or map) are taken from main stack. Element array[n] (or map[n]) is put on top of the main stack.
        /// </summary>
        PICKITEM = 0xCE,
        /// <summary>
        /// The item on top of main stack is removed and appended to the second item on top of the main stack.
        /// </summary>
        APPEND = 0xCF,
        /// <summary>
        /// A value v, index n (or key) and an array (or map) are taken from main stack. Attribution array[n]=v (or map[n]=v) is performed.
        /// </summary>
        SETITEM = 0xD0,
        /// <summary>
        /// An array is removed from the top of the main stack and its elements are reversed.
        /// </summary>
        REVERSEITEMS = 0xD1,
        /// <summary>
        /// An input index n (or key) and an array (or map) are removed from the top of the main stack. Element array[n] (or map[n]) is removed.
        /// </summary>
        REMOVE = 0xD2,
        /// <summary>
        /// Remove all the items from the compound-type.
        /// </summary>
        CLEARITEMS = 0xD3,
        /// <summary>
        /// Remove the last element from an array, and push it onto the stack.
        /// </summary>
        POPITEM = 0xD4,

        #endregion

        #region Types

        /// <summary>
        /// Returns <see langword="true"/> if the input is <see langword="null"/>;
        /// <see langword="false"/> otherwise.
        /// </summary>
        ISNULL = 0xD8,
        /// <summary>
        /// Returns <see langword="true"/> if the top item of the stack is of the specified type;
        /// <see langword="false"/> otherwise.
        /// </summary>
        [OperandSize(Size = 1)]
        ISTYPE = 0xD9,
        /// <summary>
        /// Converts the top item of the stack to the specified type.
        /// </summary>
        [OperandSize(Size = 1)]
        CONVERT = 0xDB,

        #endregion

        #region Extensions

        /// <summary>
        /// Pops the top stack item. Then, turns the vm state to FAULT immediately, and cannot be caught. The top stack
        /// value is used as reason.
        /// </summary>
        ABORTMSG = 0xE0,
        /// <summary>
        /// Pops the top two stack items. If the second-to-top stack value is false, exits the vm execution and sets the
        /// vm state to FAULT. In this case, the top stack value is used as reason for the exit. Otherwise, it is ignored.
        /// </summary>
        ASSERTMSG = 0xE1

        #endregion
    }
}
