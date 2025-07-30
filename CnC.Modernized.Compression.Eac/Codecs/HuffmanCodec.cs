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
public class HuffmanCodec : ICodec
{
    private sealed class HuffmanNode(int symbol, int frequency)
    {
        public int Symbol { get; } = symbol;
        public int Frequency { get; } = frequency;
        public HuffmanNode? Left { get; }
        public HuffmanNode? Right { get; }
        public bool IsLeaf => Left is null && Right is null;

        public HuffmanNode(HuffmanNode left, HuffmanNode right)
            : this(-1, left.Frequency + right.Frequency)
        {
            Left = left;
            Right = right;
        }
    }

    private readonly struct HuffmanCode(ushort code, byte length)
    {
        public ushort Code { get; } = code;
        public byte Length { get; } = length;
    }

    private ref struct BitReader(ReadOnlySpan<byte> data)
    {
        private readonly ReadOnlySpan<byte> _data = data;

        private int _bitPosition = 0;
        private int _bytePosition = 0;

        public bool ReadBit()
        {
            var result = (_data[_bytePosition] & (1 << (7 - _bitPosition))) != 0;
            _bitPosition++;
            if (_bitPosition != 8)
            {
                return result;
            }

            _bitPosition = 0;
            _bytePosition++;

            return result;
        }
    }

    private ref struct BitWriter(Span<byte> data)
    {
        private readonly Span<byte> _data = data;

        private byte _currentByte = 0;
        private int _bitPosition = 0;
        private int _bytePosition = 0;

        public int BytesWritten => _bytePosition + (_bitPosition > 0 ? 1 : 0);

        public void WriteBits(ushort bits, byte length)
        {
            for (var i = length - 1; i >= 0; i--)
            {
                var bit = (bits & (1 << i)) != 0;
                _currentByte = (byte)(_currentByte | (bit ? 1 : 0) << (7 - _bitPosition));
                _bitPosition++;

                if (_bitPosition != 8)
                {
                    continue;
                }

                _data[_bytePosition++] = _currentByte;
                _currentByte = 0;
                _bitPosition = 0;
            }
        }

        public void Flush()
        {
            if (_bitPosition > 0)
            {
                _data[_bytePosition] = _currentByte;
            }
        }
    }

    private static HuffmanNode BuildHuffmanTree(int[] frequencies)
    {
        PriorityQueue<HuffmanNode, int> priorityQueue = new();
        for (var i = 0; i < frequencies.Length; i++)
        {
            if (frequencies[i] > 0)
            {
                priorityQueue.Enqueue(new HuffmanNode(i, frequencies[i]), frequencies[i]);
            }
        }

        if (priorityQueue.Count == 1)
        {
            var leaf = priorityQueue.Dequeue();
            return new HuffmanNode(leaf, new HuffmanNode(-1, 0));
        }

        while (priorityQueue.Count > 1)
        {
            var left = priorityQueue.Dequeue();
            var right = priorityQueue.Dequeue();
            var parent = new HuffmanNode(left, right);
            priorityQueue.Enqueue(parent, parent.Frequency);
        }

        return priorityQueue.Dequeue();
    }

    private static void GenerateHuffmanCodes(HuffmanNode root, HuffmanCode[] codes)
    {
        var stack = new Stack<(HuffmanNode Node, ushort Code, byte Length)>();
        stack.Push((root, 0, 0));

        while (stack.Count > 0)
        {
            (HuffmanNode node, ushort code, byte length) = stack.Pop();

            if (node.IsLeaf)
            {
                codes[node.Symbol] = new HuffmanCode(code, length);
                continue;
            }

            if (node.Right is not null)
            {
                stack.Push((node.Right, (ushort)(code << 1 | 1), (byte)(length + 1)));
            }

            if (node.Left is not null)
            {
                stack.Push((node.Left, (ushort)(code << 1), (byte)(length + 1)));
            }
        }
    }

    public CodecInfo About =>
        new()
        {
            Name = "Huffman",
            Description = "Huffman compression algorithm",
            Version = "1.04",
        };

    public bool IsCompressedData(ReadOnlySpan<byte> compressedData) =>
        compressedData.Length >= CompressionConstants.HeaderSize
        && compressedData[..4].SequenceEqual(CompressionConstants.Signatures.Huffman);

    public int GetDecompressedSize(ReadOnlySpan<byte> compressedData)
    {
        if (!IsCompressedData(compressedData))
        {
            throw new InvalidDataException($"Invalid {nameof(HuffmanCodec)} compressed data.");
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
            throw new InvalidDataException($"Invalid {nameof(HuffmanCodec)} compressed data.");
        }

        var uncompressedSize = GetDecompressedSize(compressedData);
        compressedSize = BitConverter.ToInt32(compressedData[8..12]);

        if (destination.Length < uncompressedSize)
        {
            throw new ArgumentException("Destination buffer is too small.", nameof(destination));
        }

        var frequencyTableSize = compressedData[12];
        var frequencies = new int[CompressionConstants.MaxSymbols];
        var offset = 13;

        for (var i = 0; i < frequencyTableSize; i++)
        {
            var symbol = compressedData[offset++];
            frequencies[symbol] = BitConverter.ToInt32(compressedData.Slice(offset, 4));
        }

        var treeRoot = BuildHuffmanTree(frequencies);
        var bitReader = new BitReader(compressedData[offset..]);
        var decodedCount = 0;

        while (decodedCount < uncompressedSize)
        {
            var node = treeRoot;
            while (!node.IsLeaf)
            {
                var bit = bitReader.ReadBit();
                node = bit ? node.Right! : node.Left!;
            }

            destination[decodedCount++] = (byte)node.Symbol;
        }

        return uncompressedSize;
    }

    public int Encode(
        Span<byte> compressedData,
        ReadOnlySpan<byte> source,
        EncodingOptions? options = null
    )
    {
        var frequencies = new int[CompressionConstants.MaxSymbols];
        foreach (var @byte in source)
        {
            frequencies[@byte]++;
        }

        var treeRoot = BuildHuffmanTree(frequencies);
        var codes = new HuffmanCode[CompressionConstants.MaxSymbols];
        GenerateHuffmanCodes(treeRoot, codes);

        var offset = 0;
        CompressionConstants.Signatures.Huffman.CopyTo(compressedData);
        offset += 4;

        if (!BitConverter.TryWriteBytes(compressedData[offset..], source.Length))
        {
            throw new InvalidOperationException("Unable to write the source bytes.");
        }

        offset += 8;
        var usedSymbols = frequencies.Count(@byte => @byte > 0);
        compressedData[offset++] = (byte)usedSymbols;

        for (var i = 0; i < frequencies.Length; i++)
        {
            if (frequencies[i] <= 0)
            {
                continue;
            }

            compressedData[offset++] = (byte)i;
            if (!BitConverter.TryWriteBytes(compressedData[offset..], frequencies[i]))
            {
                throw new InvalidOperationException("Unable to write the source bytes.");
            }

            offset += 4;
        }

        var bitWriter = new BitWriter(compressedData[offset..]);
        foreach (var @byte in source)
        {
            var code = codes[@byte];
            bitWriter.WriteBits(code.Code, code.Length);
        }

        var totalSize = offset + bitWriter.BytesWritten;
        if (!BitConverter.TryWriteBytes(compressedData[8..], totalSize))
        {
            throw new InvalidOperationException("Unable to write the source bytes.");
        }

        bitWriter.Flush();
        return totalSize;
    }
}
