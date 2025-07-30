// The MIT License (MIT)
//
// Copyright (c) 2025 Victor Matia <vmatir@outlook.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the “Software”), to deal in the Software without
// restriction, including without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using JetBrains.Annotations;

namespace CnC.Modernized.Compression.Eac.Codecs;

[PublicAPI]
public sealed class BTreeCodec : ICodec
{
    private const int MinMatch = 3;
    private const int MaxMatch = 1028;
    private const int WindowSize = 4096;
    private const int HashBits = 12;
    private const int HashSize = 1 << HashBits;
    private const int HashMask = HashSize - 1;

    public CodecInfo About =>
        new()
        {
            Name = "BTree",
            Description = "Binary Tree based LZ compression codec",
            Version = "1.02",
        };

    private readonly struct Match
    {
        public readonly int Length;
        public readonly int Offset;

        public Match(int length, int offset)
        {
            Length = length;
            Offset = offset;
        }
    }

    private struct EncodingContext
    {
        public int[] HashTable;
        public int[] ChainTable;
        public int OutputPos;
        public int InputPos;
    }

    private struct ControlByteState
    {
        public int Position;
        public byte Value;
        public byte BitsUsed;
    }

    private static void WriteHeader(Span<byte> compressedData, int sourceLength)
    {
        CompressionConstants.Signatures.BTree.CopyTo(compressedData);
        BitConverter.TryWriteBytes(compressedData[4..], sourceLength);
    }

    private static void UpdateControlByte(Span<byte> compressedData, ref ControlByteState state)
    {
        if (state.BitsUsed != 8)
        {
            return;
        }

        compressedData[state.Position] = state.Value;
        state.Position++;
        state.Value = 0;
        state.BitsUsed = 0;
    }

    private static Match FindLongestMatch(ReadOnlySpan<byte> source, EncodingContext context)
    {
        if (context.InputPos + MinMatch > source.Length)
        {
            return new Match(0, 0);
        }

        var hash = ((source[context.InputPos] << 8) | source[context.InputPos + 1]) & HashMask;
        var matchPos = context.HashTable[hash];

        var bestLength = 0;
        var bestOffset = 0;

        while (matchPos >= 0 && matchPos > context.InputPos - WindowSize)
        {
            var matchLength = CountMatchingBytes(source, matchPos, context.InputPos);

            if (matchLength >= MinMatch && matchLength > bestLength)
            {
                bestLength = matchLength;
                bestOffset = context.InputPos - matchPos;
            }

            matchPos = context.ChainTable[matchPos & (WindowSize - 1)];
        }

        UpdateTables(context, hash);
        return new Match(bestLength, bestOffset);
    }

    private static int CountMatchingBytes(ReadOnlySpan<byte> source, int matchPos, int currentPos)
    {
        var matchLength = 0;
        while (
            currentPos + matchLength < source.Length
            && matchLength < MaxMatch
            && source[matchPos + matchLength] == source[currentPos + matchLength]
        )
        {
            matchLength++;
        }

        return matchLength;
    }

    private static void UpdateTables(EncodingContext context, int hash)
    {
        context.ChainTable[context.InputPos & (WindowSize - 1)] = context.HashTable[hash];
        context.HashTable[hash] = context.InputPos;
    }

    private static void WriteMatch(
        Span<byte> compressedData,
        ref EncodingContext context,
        ref ControlByteState controlState,
        Match match
    )
    {
        controlState.Value |= (byte)(1 << controlState.BitsUsed);
        var matchInfo = (ushort)((match.Offset << 4) | (match.Length - MinMatch));
        if (!BitConverter.TryWriteBytes(compressedData.Slice(context.OutputPos, 2), matchInfo))
        {
            throw new InvalidOperationException("Failed to write match info.");
        }

        context.OutputPos += 2;
        context.InputPos += match.Length;
        controlState.BitsUsed++;
    }

    private static void WriteLiteral(
        Span<byte> compressedData,
        ReadOnlySpan<byte> source,
        ref EncodingContext context,
        ref ControlByteState controlState
    )
    {
        compressedData[context.OutputPos++] = source[context.InputPos++];
        controlState.BitsUsed++;
    }

    private static void FinalizeEncoding(
        Span<byte> compressedData,
        ControlByteState controlState,
        int outputPos
    )
    {
        if (controlState.BitsUsed > 0)
        {
            compressedData[controlState.Position] = controlState.Value;
        }

        if (!BitConverter.TryWriteBytes(compressedData[8..], outputPos))
        {
            throw new InvalidOperationException("Failed to write compressed size.");
        }
    }

    public bool IsCompressedData(ReadOnlySpan<byte> compressedData) =>
        compressedData.Length >= CompressionConstants.HeaderSize
        && compressedData[..4].SequenceEqual(CompressionConstants.Signatures.BTree);

    public int GetDecompressedSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsCompressedData(compressedData))
        {
            throw new InvalidDataException($"Invalid {nameof(BTreeCodec)} compressed data.");
        }

        return BitConverter.ToInt32(compressedData[4..8]);
    }

    public int Decode(
        Span<byte> destination,
        ReadOnlySpan<byte> compressedData,
        out int compressedSize
    )
    {
        if (!IsCompressedData(compressedData))
        {
            throw new InvalidDataException($"Invalid {nameof(BTreeCodec)} compressed data.");
        }

        var uncompressedSize = GetDecompressedSize(compressedData);
        compressedSize = BitConverter.ToInt32(compressedData[8..12]);

        if (destination.Length < uncompressedSize)
        {
            throw new ArgumentException("Destination buffer too small.", nameof(destination));
        }

        var inputPos = CompressionConstants.HeaderSize;
        var outputPos = 0;
        while (outputPos < uncompressedSize)
        {
            var control = compressedData[inputPos++];
            for (int i = 0; i < 8 && outputPos < uncompressedSize; i++)
            {
                if ((control & (1 << i)) == 0)
                {
                    destination[outputPos++] = compressedData[inputPos++];
                }
                else
                {
                    var matchInfo = BitConverter.ToUInt16(compressedData.Slice(inputPos, 2));
                    inputPos += 2;

                    var matchLength = (matchInfo & 0x000F) + MinMatch;
                    var matchOffset = matchInfo >> 4;
                    var matchPos = outputPos - matchOffset;
                    for (int j = 0; j < matchLength; j++)
                    {
                        destination[outputPos + j] = destination[matchPos + j];
                    }

                    outputPos += matchLength;
                }
            }
        }

        return uncompressedSize;
    }

    public int Encode(
        Span<byte> compressedData,
        ReadOnlySpan<byte> source,
        EncodingOptions? options = null
    )
    {
        WriteHeader(compressedData, source.Length);

        var context = new EncodingContext
        {
            HashTable = new int[HashSize],
            ChainTable = new int[WindowSize],
            OutputPos = CompressionConstants.HeaderSize,
            InputPos = 0,
        };

        Array.Fill(context.HashTable, -1);

        var controlState = new ControlByteState { Position = context.OutputPos++ };

        while (context.InputPos < source.Length)
        {
            UpdateControlByte(compressedData, ref controlState);

            var match = FindLongestMatch(source, context);

            if (match.Length >= MinMatch)
            {
                WriteMatch(compressedData, ref context, ref controlState, match);
            }
            else
            {
                WriteLiteral(compressedData, source, ref context, ref controlState);
            }
        }

        FinalizeEncoding(compressedData, controlState, context.OutputPos);
        return context.OutputPos;
    }
}
