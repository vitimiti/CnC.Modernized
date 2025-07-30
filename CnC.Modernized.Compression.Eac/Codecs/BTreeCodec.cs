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
    private const int DefaultWindowSize = 4096;
    private const int DefaultMinMatch = 3;
    private const int DefaultMaxMatch = 1028;
    private const int DefaultHashBits = 12;

    private int WindowSize => _options?.WindowSize ?? DefaultWindowSize;
    private int MinMatch => _options?.MinMatch ?? DefaultMinMatch;
    private int MaxMatch => _options?.MaxMatch ?? DefaultMaxMatch;
    private int HashBits => _options?.HashBits ?? DefaultHashBits;
    private int HashSize => 1 << HashBits;
    private int HashMask => HashSize - 1;

    private EncodingOptions? _options;

    public CodecInfo About =>
        new()
        {
            Name = "BTree",
            Description = "Binary Tree based LZ compression codec",
            Version = "1.02",
        };

    private readonly struct Match(int length, int offset)
    {
        public readonly int Length = length;
        public readonly int Offset = offset;
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

    private bool CanFindMatch(ReadOnlySpan<byte> source, int inputPos)
    {
        return inputPos + MinMatch <= source.Length;
    }

    private int ComputeHash(ReadOnlySpan<byte> source, int pos)
    {
        return ((source[pos] << 8) | source[pos + 1]) & HashMask;
    }

    private (int Length, int Offset) FindBestMatch(
        ReadOnlySpan<byte> source,
        EncodingContext context,
        int matchPos
    )
    {
        var bestLength = 0;
        var bestOffset = 0;
        var candidatesChecked = 0;
        var minPos = context.InputPos - WindowSize;

        while (matchPos >= 0 && matchPos > minPos)
        {
            if (ShouldStopSearching(candidatesChecked))
                break;

            var matchLength = CountMatchingBytes(source, matchPos, context.InputPos, MaxMatch);
            if (IsNewBestMatch(matchLength, bestLength))
            {
                bestLength = matchLength;
                bestOffset = context.InputPos - matchPos;

                if (!_options?.AggressiveMatching ?? false)
                    break;
            }

            matchPos = context.ChainTable[matchPos & (WindowSize - 1)];
            candidatesChecked++;
        }

        return (bestLength, bestOffset);
    }

    private bool ShouldStopSearching(int candidatesChecked)
    {
        return _options?.MaxCandidates > 0 && candidatesChecked >= _options.MaxCandidates;
    }

    private bool IsNewBestMatch(int matchLength, int bestLength)
    {
        return matchLength >= MinMatch && matchLength > bestLength;
    }

    private bool ShouldTryLazyMatching(int bestLength, int inputPos, int sourceLength)
    {
        return (_options?.LazyMatching ?? false)
            && bestLength >= MinMatch
            && inputPos + 1 < sourceLength;
    }

    private Match FindLongestMatch(ReadOnlySpan<byte> source, EncodingContext context)
    {
        if (!CanFindMatch(source, context.InputPos))
        {
            return new Match(0, 0);
        }

        var hash = ComputeHash(source, context.InputPos);
        var matchPos = context.HashTable[hash];
        (int bestLength, int bestOffset) = FindBestMatch(source, context, matchPos);

        if (ShouldTryLazyMatching(bestLength, context.InputPos, source.Length))
        {
            var nextMatch = FindLongestMatchAt(source, context, context.InputPos + 1);
            if (nextMatch.Length > bestLength + 1)
            {
                context.InputPos++; // Skip current byte
                return nextMatch;
            }
        }

        UpdateTables(context, hash, WindowSize);
        return new Match(bestLength, bestOffset);
    }

    private Match FindLongestMatchAt(ReadOnlySpan<byte> source, EncodingContext context, int pos)
    {
        var savedPos = context.InputPos;
        context.InputPos = pos;
        var match = FindLongestMatch(source, context);
        context.InputPos = savedPos;
        return match;
    }

    private static int CountMatchingBytes(
        ReadOnlySpan<byte> source,
        int matchPos,
        int currentPos,
        int maxMatch
    )
    {
        var matchLength = 0;
        while (
            currentPos + matchLength < source.Length
            && matchLength < maxMatch
            && source[matchPos + matchLength] == source[currentPos + matchLength]
        )
        {
            matchLength++;
        }

        return matchLength;
    }

    private static void UpdateTables(EncodingContext context, int hash, int windowSize)
    {
        context.ChainTable[context.InputPos & (windowSize - 1)] = context.HashTable[hash];
        context.HashTable[hash] = context.InputPos;
    }

    private static void WriteMatch(
        Span<byte> compressedData,
        ref EncodingContext context,
        ref ControlByteState controlState,
        Match match,
        int minMatch
    )
    {
        controlState.Value |= (byte)(1 << controlState.BitsUsed);
        var matchInfo = (ushort)((match.Offset << 4) | (match.Length - minMatch));
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

    private int EncodeInternal(Span<byte> compressedData, ReadOnlySpan<byte> source)
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
                WriteMatch(compressedData, ref context, ref controlState, match, MinMatch);
            }
            else
            {
                WriteLiteral(compressedData, source, ref context, ref controlState);
            }
        }

        FinalizeEncoding(compressedData, controlState, context.OutputPos);
        return context.OutputPos;
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
        _options = options;
        try
        {
            return EncodeInternal(compressedData, source);
        }
        finally
        {
            _options = null;
        }
    }
}
