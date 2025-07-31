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

using System.ComponentModel;
using JetBrains.Annotations;

namespace CnC.Modernized.GeneralsAndZeroHour.Compression.Eac;

[PublicAPI]
public record EncodingOptions
{
    [DefaultValue(4096)]
    public int WindowSize { get; set; } = 4096;

    [DefaultValue(3)]
    public int MinMatch { get; set; } = 3;

    [DefaultValue(1028)]
    public int MaxMatch { get; set; } = 1028;

    [DefaultValue(12)]
    public int HashBits { get; set; } = 12;

    [DefaultValue(6)]
    public int CompressionLevel { get; set; } = 6;

    [DefaultValue(true)]
    public bool LazyMatching { get; set; } = true;

    [DefaultValue(128)]
    public int MaxCandidates { get; set; } = 128;

    [DefaultValue(false)]
    public bool AggressiveMatching { get; set; }

    [DefaultValue(256)]
    public int SymbolCount { get; set; } = 256;

    [DefaultValue(16)]
    public int MaxBitLength { get; set; } = 16;

    [DefaultValue(true)]
    public bool OptimizeTree { get; set; } = true;

    [DefaultValue(4)]
    public int MinCodeLength { get; set; } = 4;

    [DefaultValue(true)]
    public bool EnableHashChaining { get; set; } = true;

    [DefaultValue(4)]
    public int HashChainLength { get; set; } = 4;

    [DefaultValue(true)]
    public bool OptimizeShortMatches { get; set; } = true;

    public static EncodingOptions BestCompression =>
        new()
        {
            WindowSize = 32768,
            MinMatch = 3,
            MaxMatch = 4096,
            HashBits = 15,
            CompressionLevel = 9,
            LazyMatching = true,
            MaxCandidates = 256,
            AggressiveMatching = true,
            SymbolCount = 1024,
            MaxBitLength = 16,
            OptimizeTree = true,
            MinCodeLength = 4,
            EnableHashChaining = true,
            HashChainLength = 4,
            OptimizeShortMatches = true,
        };

    public static EncodingOptions FastestCompression =>
        new()
        {
            WindowSize = 2048,
            MinMatch = 4,
            MaxMatch = 64,
            HashBits = 10,
            CompressionLevel = 1,
            LazyMatching = false,
            MaxCandidates = 16,
            AggressiveMatching = false,
            SymbolCount = 256,
            MaxBitLength = 16,
            OptimizeTree = false,
            MinCodeLength = 4,
            EnableHashChaining = false,
            HashChainLength = 4,
            OptimizeShortMatches = false,
        };

    public static EncodingOptions BalancedCompression => new();
}
