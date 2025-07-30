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
    private const int WindowSize = 4096;
    private const int MinMatch = 3;
    private const int MaxMatch = 1028;

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

    private static bool TryFindMatch(
        ReadOnlySpan<byte> source,
        EncodingState state,
        out Match match
    )
    {
        match = default;
        if (state.InputPos < MinMatch)
        {
            return false;
        }

        var bestLength = 0;
        var bestOffset = 0;
        var windowStart = Math.Max(0, state.InputPos - WindowSize);

        for (var searchPos = windowStart; searchPos < state.InputPos; searchPos++)
        {
            var length = CountMatchingBytes(source, searchPos, state.InputPos);
            if (length < MinMatch || length <= bestLength)
            {
                continue;
            }

            bestLength = length;
            bestOffset = state.InputPos - searchPos;
        }

        if (bestLength < MinMatch)
        {
            return false;
        }

        match = new Match(bestLength, bestOffset);
        return true;
    }

    private static int CountMatchingBytes(ReadOnlySpan<byte> source, int searchPos, int currentPos)
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

    private static void WriteMatch(EncodingState state, Match match)
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

    private static void WriteRunLength(ReadOnlySpan<byte> source, EncodingState state)
    {
        var runStart = state.InputPos;
        var runLength = GetRunLength(source, state);

        state.CompressedData[state.OutputPos++] = (byte)((runLength - 1) & 0x7F);
        source.Slice(runStart, runLength).CopyTo(state.CompressedData.Slice(state.OutputPos));
        state.OutputPos += runLength;
        state.InputPos += runLength;
    }

    private static int GetRunLength(ReadOnlySpan<byte> source, EncodingState state)
    {
        var runLength = 1;
        var pos = state.InputPos + 1;
        while (pos < source.Length && runLength < 128 && !HasMatchAhead(source, pos))
        {
            runLength++;
            pos++;
        }

        return runLength;
    }

    private static bool HasMatchAhead(ReadOnlySpan<byte> source, int pos)
    {
        if (pos < MinMatch)
        {
            return false;
        }

        var windowStart = Math.Max(0, pos - WindowSize);
        for (var searchPos = windowStart; searchPos < pos; searchPos++)
        {
            if (CountMatchingBytes(source, searchPos, pos) >= MinMatch)
                return true;
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
        WriteHeader(compressedData, source);
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
}
