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
public class ReferenceCodec : ICodec
{
    private const int DefaultWindowSize = 4096;
    private const int DefaultMinMatch = 3;
    private const int DefaultMaxMatch = 1028;
    private const int DefaultHashChainLength = 4;

    private int[] _hashTable = [];
    private int[] _chainTable = [];

    private EncodingOptions? _options;

    private int WindowSize => _options?.WindowSize ?? DefaultWindowSize;
    private int MinMatch => _options?.MinMatch ?? DefaultMinMatch;
    private int MaxMatch => _options?.MaxMatch ?? DefaultMaxMatch;
    private bool EnableHashChaining => _options?.EnableHashChaining ?? true;
    private int HashChainLength => _options?.HashChainLength ?? DefaultHashChainLength;
    private bool OptimizeShortMatches => _options?.OptimizeShortMatches ?? true;

    public CodecInfo About =>
        new()
        {
            Name = "Reference",
            Description = "Sliding window reference compression codec",
            Version = "1.01",
        };

    private readonly record struct Match(int Length, int Offset);

    private ref struct EncodingState(Span<byte> compressedData)
    {
        public readonly Span<byte> CompressedData = compressedData;
        public int InputPos = 0;
        public int OutputPos = CompressionConstants.HeaderSize;
    }

    private static void WriteHeader(Span<byte> compressedData, ReadOnlySpan<byte> source)
    {
        CompressionConstants.Signatures.Reference.CopyTo(compressedData);
        if (!BitConverter.TryWriteBytes(compressedData[4..], source.Length))
        {
            throw new InvalidOperationException("Failed to write source size.");
        }
    }

    private int ComputeHash(ReadOnlySpan<byte> source, int pos)
    {
        return ((source[pos] << 8) | source[pos + 1]) & (WindowSize - 1);
    }

    private bool CanMatchAtPosition(int inputPos) => inputPos >= MinMatch;

    private (int Length, int Offset) FindBestMatchInternal(
        ReadOnlySpan<byte> source,
        EncodingState state
    )
    {
        return EnableHashChaining
            ? FindBestMatchWithHashing(source, state)
            : FindBestMatchLinear(source, state);
    }

    private (int Length, int Offset) FindBestMatchWithHashing(
        ReadOnlySpan<byte> source,
        EncodingState state
    )
    {
        var hash = ComputeHash(source, state.InputPos);
        var chainPos = _hashTable[hash];
        var chainLength = 0;

        var bestLength = 0;
        var bestOffset = 0;

        while (IsValidChainPosition(chainPos, state.InputPos, chainLength))
        {
            var length = CountMatchingBytes(source, chainPos, state.InputPos);
            if (IsNewBestMatch(length, bestLength))
            {
                (bestLength, bestOffset) = (length, state.InputPos - chainPos);
                if (ShouldBreakSearch(length))
                {
                    break;
                }
            }

            chainPos = _chainTable[chainPos & (WindowSize - 1)];
            chainLength++;
        }

        UpdateHashChain(state.InputPos, hash);
        return (bestLength, bestOffset);
    }

    private (int Length, int Offset) FindBestMatchLinear(
        ReadOnlySpan<byte> source,
        EncodingState state
    )
    {
        var windowStart = int.Max(0, state.InputPos - WindowSize);
        var bestLength = 0;
        var bestOffset = 0;

        for (var searchPos = windowStart; searchPos < state.InputPos; searchPos++)
        {
            var length = CountMatchingBytes(source, searchPos, state.InputPos);
            if (IsNewBestMatch(length, bestLength))
            {
                (bestLength, bestOffset) = (length, state.InputPos - searchPos);
                if (ShouldBreakSearch(length))
                {
                    break;
                }
            }
        }

        return (bestLength, bestOffset);
    }

    private bool IsValidChainPosition(int chainPos, int inputPos, int chainLength) =>
        chainPos >= int.Max(0, inputPos - WindowSize) && chainLength < HashChainLength;

    private bool IsNewBestMatch(int length, int currentBest) =>
        length >= MinMatch && length > currentBest;

    private bool ShouldBreakSearch(int length) => !OptimizeShortMatches || length >= 2 * MinMatch;

    private void UpdateHashChain(int inputPos, int hash)
    {
        _chainTable[inputPos & (WindowSize - 1)] = _hashTable[hash];
        _hashTable[hash] = inputPos;
    }

    private bool ShouldSkipShortMatch(
        ReadOnlySpan<byte> source,
        EncodingState state,
        int matchLength
    )
    {
        if (!OptimizeShortMatches || matchLength > MinMatch + 1)
        {
            return false;
        }

        const int matchEncodingOverhead = 3;
        return matchLength + 1 <= matchEncodingOverhead
            || HasBetterFutureMatch(source, state, matchLength);
    }

    private bool HasBetterFutureMatch(
        ReadOnlySpan<byte> source,
        EncodingState state,
        int currentLength
    )
    {
        if (state.InputPos + currentLength >= source.Length - MinMatch)
        {
            return false;
        }

        var lookAheadState = state with { InputPos = state.InputPos + currentLength };
        return TryFindMatch(source, lookAheadState, out var futureMatch)
            && futureMatch.Length > currentLength + 1;
    }

    private bool TryFindMatch(ReadOnlySpan<byte> source, EncodingState state, out Match match)
    {
        match = default;
        if (!CanMatchAtPosition(state.InputPos))
        {
            return false;
        }

        var (bestLength, bestOffset) = FindBestMatchInternal(source, state);

        if (bestLength < MinMatch)
        {
            return false;
        }

        if (ShouldSkipShortMatch(source, state, bestLength))
        {
            return false;
        }

        match = new Match(bestLength, bestOffset);
        return true;
    }

    private int CountMatchingBytes(ReadOnlySpan<byte> source, int searchPos, int currentPos)
    {
        var length = 0;
        while (
            currentPos + length < source.Length
            && length < MaxMatch
            && source[searchPos + length] == source[currentPos + length]
        )
        {
            length++;
        }

        return length;
    }

    private void WriteMatch(EncodingState state, Match match)
    {
        state.CompressedData[state.OutputPos++] = (byte)(0x80 | (match.Length - MinMatch));
        if (
            !BitConverter.TryWriteBytes(
                state.CompressedData.Slice(state.OutputPos, 2),
                (ushort)match.Offset
            )
        )
        {
            throw new InvalidOperationException("Failed to write match offset.");
        }

        state.OutputPos += 2;
        state.InputPos += match.Length;
    }

    private void WriteRunLength(ReadOnlySpan<byte> source, EncodingState state)
    {
        var runStart = state.InputPos;
        var runLength = GetRunLength(source, state);

        state.CompressedData[state.OutputPos++] = (byte)((runLength - 1) & 0x7F);
        source.Slice(runStart, runLength).CopyTo(state.CompressedData.Slice(state.OutputPos));
        state.OutputPos += runLength;
        state.InputPos += runLength;
    }

    private int GetRunLength(ReadOnlySpan<byte> source, EncodingState state)
    {
        var runLength = 1;
        var pos = state.InputPos + 1;
        while (
            pos < source.Length
            && runLength < 128
            && !HasMatchAhead(source, pos, MinMatch, WindowSize)
        )
        {
            runLength++;
            pos++;
        }

        return runLength;
    }

    private bool HasMatchAhead(ReadOnlySpan<byte> source, int pos, int minMatch, int windowSize)
    {
        if (pos < minMatch)
        {
            return false;
        }

        var windowStart = int.Max(0, pos - windowSize);
        for (var searchPos = windowStart; searchPos < pos; searchPos++)
        {
            if (CountMatchingBytes(source, searchPos, pos) >= minMatch)
            {
                return true;
            }
        }

        return false;
    }

    private static int FinalizeLzEncoding(Span<byte> compressedData, EncodingState state)
    {
        if (!BitConverter.TryWriteBytes(compressedData[8..], state.OutputPos))
        {
            throw new InvalidOperationException("Failed to write compressed size.");
        }

        return state.OutputPos;
    }

    private int EncodeInternal(Span<byte> compressedData, ReadOnlySpan<byte> source)
    {
        WriteHeader(compressedData, source);
        if (EnableHashChaining)
        {
            _hashTable = new int[WindowSize];
            _chainTable = new int[WindowSize];
            Array.Fill(_hashTable, -1);
        }

        var state = new EncodingState(compressedData);
        while (state.InputPos < source.Length)
        {
            if (TryFindMatch(source, state, out var match))
            {
                WriteMatch(state, match);
            }
            else
            {
                WriteRunLength(source, state);
            }
        }

        return FinalizeLzEncoding(compressedData, state);
    }

    public bool IsCompressedData(ReadOnlySpan<byte> compressedData) =>
        compressedData.Length >= CompressionConstants.HeaderSize
        && compressedData[..4].SequenceEqual(CompressionConstants.Signatures.Reference);

    public int GetDecompressedSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsCompressedData(compressedData))
        {
            throw new InvalidDataException($"Invalid {nameof(ReferenceCodec)} compressed data");
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
            throw new InvalidDataException($"Invalid {nameof(ReferenceCodec)} compressed data");
        }

        var uncompressedSize = GetDecompressedSize(compressedData);
        compressedSize = BitConverter.ToInt32(compressedData[8..12]);

        var inputPos = CompressionConstants.HeaderSize;
        var outputPos = 0;
        while (outputPos < uncompressedSize)
        {
            var token = compressedData[inputPos++];
            if ((token & 0x80) == 0)
            {
                var runLength = (token & 0x7F) + 1;
                compressedData.Slice(inputPos, runLength).CopyTo(destination[outputPos..]);
                inputPos += runLength;
                outputPos += runLength;
            }
            else
            {
                var matchLength = (token & 0x7F) + MinMatch;
                var matchOffset = BitConverter.ToUInt16(compressedData.Slice(inputPos, 2));
                inputPos += 2;

                var matchPos = outputPos - matchOffset;
                for (int i = 0; i < matchLength; i++)
                {
                    destination[outputPos + i] = destination[matchPos + i];
                }

                outputPos += matchLength;
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
