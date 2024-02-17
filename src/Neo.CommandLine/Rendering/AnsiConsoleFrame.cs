// Copyright (C) 2015-2024 The Neo Project.
//
// AnsiConsoleFrame.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.CommandLine.Rendering
{
    internal partial class AnsiConsoleFrame
    {
        public int Height => Console.BufferHeight;
        public int Width => Console.BufferWidth;

        public AnsiStringStyle Style { get; init; } = new();

        private readonly AnsiString[] _frameBuffer = new AnsiString[Console.BufferHeight];

        public AnsiConsoleFrame()
        {
            Clear();
        }

        public AnsiString this[int index]
        {
            get => _frameBuffer[index];
            set => _frameBuffer[index] = value;
        }

        public AnsiString[] this[Range range]
        {
            get => _frameBuffer[range.Start.Value..range.End.Value];
            set => Array.Copy(value, 0, _frameBuffer, range.Start.Value, value.Length);
        }

        public void Add(int x, int y, char value) =>
            _frameBuffer[x][y] = value;

        public void Add(int x, int y, AnsiString value) =>
            _frameBuffer[x][y..] = $"{value}".ToCharArray();

        public void Add(int x, int y, string value) =>
            _frameBuffer[x][y..] = value.ToCharArray();

        public void Add(int x, int y, char[] value) =>
            _frameBuffer[x][y..] = value;

        public void Clear()
        {
            for (var x = 0; x < _frameBuffer.Length; x++)
                _frameBuffer[x] = new(string.Empty, Style);
        }

        public void Clear(int x) =>
            _frameBuffer[x] = new(string.Empty, Style);

        public void Clear(int x, int y) =>
            _frameBuffer[x][y] = ' ';

    }
}
