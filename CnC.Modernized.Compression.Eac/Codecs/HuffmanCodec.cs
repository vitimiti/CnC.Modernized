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
public sealed class HuffmanCodec : ICodec
{
    private const int DefaultSymbolCount = 256;
    private const int DefaultMaxBitLength = 16;
    private const int DefaultMinCodeLength = 4;

    private EncodingOptions? _options;

    private int SymbolCount => _options?.SymbolCount ?? DefaultSymbolCount;
    private int MaxBitLength => _options?.MaxBitLength ?? DefaultMaxBitLength;
    private int MinCodeLength => _options?.MinCodeLength ?? DefaultMinCodeLength;
    private bool OptimizeTree => _options?.OptimizeTree ?? true;

    private sealed class Node
    {
        public int Symbol;
        public int Weight;
        public Node? Left;
        public Node? Right;
    }

    private sealed class HuffmanState(int symbolCount)
    {
        public readonly int[] Frequencies = new int[symbolCount];
        public readonly Dictionary<int, string> Codes = new();

        public Node? Root;
    }

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

    private void OptimizeCodes(Dictionary<int, string> codes)
    {
        foreach (var symbol in codes.Keys.ToList())
        {
            var code = codes[symbol];
            if (code.Length < MinCodeLength)
            {
                codes[symbol] = code.PadRight(MinCodeLength, '0');
            }
        }
    }

    private static Node BuildTree(int[] frequencies)
    {
        var pq = new PriorityQueue<Node, int>();
        for (int i = 0; i < frequencies.Length; i++)
        {
            if (frequencies[i] > 0)
            {
                pq.Enqueue(new Node { Symbol = i, Weight = frequencies[i] }, frequencies[i]);
            }
        }

        if (pq.Count == 1)
        {
            var node = pq.Dequeue();
            return new Node
            {
                Symbol = -1,
                Weight = node.Weight,
                Left = node,
                Right = new Node { Symbol = -1, Weight = 0 },
            };
        }

        while (pq.Count > 1)
        {
            var left = pq.Dequeue();
            var right = pq.Dequeue();
            var parent = new Node
            {
                Symbol = -1, // Internal nodes don't represent symbols
                Weight = left.Weight + right.Weight,
                Left = left,
                Right = right,
            };

            pq.Enqueue(parent, parent.Weight);
        }

        return pq.Dequeue();
    }

    private void GenerateCodes(Node? root, Dictionary<int, string> codes)
    {
        if (root is null)
        {
            return;
        }

        var stack = new Stack<(Node Node, string Code)>();
        stack.Push((root, ""));

        while (stack.Count > 0)
        {
            (Node node, string code) = stack.Pop();
            if (node.Symbol != -1)
            {
                if (code.Length > MaxBitLength)
                {
                    throw new InvalidOperationException(
                        $"Huffman code exceeds maximum length of {MaxBitLength} bits"
                    );
                }

                codes[node.Symbol] = code;
                continue;
            }

            if (node.Right != null)
            {
                stack.Push((node.Right, code + "1"));
            }

            if (node.Left != null)
            {
                stack.Push((node.Left, code + "0"));
            }
        }
    }

    private static int WriteHeader(Span<byte> compressedData, int sourceLength, HuffmanState state)
    {
        CompressionConstants.Signatures.Huffman.CopyTo(compressedData);
        if (!BitConverter.TryWriteBytes(compressedData[4..], sourceLength))
        {
            throw new InvalidOperationException(
                "Failed to write source length to compressed data."
            );
        }

        var pos = CompressionConstants.HeaderSize;
        var symbolCount = state.Codes.Count;
        if (!BitConverter.TryWriteBytes(compressedData[pos..], symbolCount))
        {
            throw new InvalidOperationException("Failed to write symbol count to compressed data.");
        }

        pos += sizeof(int);
        foreach (var symbol in state.Codes.Keys.OrderBy(k => k))
        {
            compressedData[pos++] = (byte)symbol;
            BitConverter.TryWriteBytes(compressedData[pos..], state.Frequencies[symbol]);
            pos += sizeof(int);
        }

        return pos;
    }

    private static int EncodeData(
        Span<byte> compressedData,
        ReadOnlySpan<byte> source,
        HuffmanState state,
        int outputPos
    )
    {
        var bitBuffer = 0;
        var bitsInBuffer = 0;
        foreach (var symbol in source)
        {
            var code = state.Codes[symbol];
            foreach (var bit in code)
            {
                bitBuffer = (bitBuffer << 1) | (bit == '1' ? 1 : 0);
                bitsInBuffer++;

                if (bitsInBuffer != 8)
                {
                    continue;
                }

                compressedData[outputPos++] = (byte)bitBuffer;
                bitBuffer = 0;
                bitsInBuffer = 0;
            }
        }

        if (bitsInBuffer > 0)
        {
            bitBuffer <<= (8 - bitsInBuffer);
            compressedData[outputPos++] = (byte)bitBuffer;
        }

        BitConverter.TryWriteBytes(compressedData[8..], outputPos);
        return outputPos;
    }

    private int EncodeInternal(Span<byte> compressedData, ReadOnlySpan<byte> source)
    {
        var state = new HuffmanState(SymbolCount);
        foreach (var b in source)
        {
            state.Frequencies[b]++;
        }

        state.Root = BuildTree(state.Frequencies);
        GenerateCodes(state.Root, state.Codes);
        if (OptimizeTree)
        {
            OptimizeCodes(state.Codes);
        }

        var outputPos = WriteHeader(compressedData, source.Length, state);
        outputPos = EncodeData(compressedData, source, state, outputPos);
        return outputPos;
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
