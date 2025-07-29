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

namespace CnC.Modernized.Compression.Eac;

[PublicAPI]
public readonly struct CodecSignature : IEquatable<CodecSignature>
{
    public CodecSignature(string signature)
    {
        Span<char> chars = [' ', ' ', ' ', ' '];
        signature.AsSpan()[..Math.Min(signature.Length, 4)].CopyTo(chars);
        Value = (uint)(chars[0] << 24 | chars[1] << 16 | chars[2] << 8 | chars[3]);
    }

    public uint Value { get; }

    public override string ToString() =>
        new(new[] { (char)(Value >> 24), (char)(Value >> 16), (char)(Value >> 8), (char)Value });

    public bool Equals(CodecSignature other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is CodecSignature other && Equals(other);

    public override int GetHashCode() => (int)Value;

    public static bool operator ==(CodecSignature left, CodecSignature right) => left.Equals(right);

    public static bool operator !=(CodecSignature left, CodecSignature right) => !(left == right);
}
