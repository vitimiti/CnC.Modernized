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

using CnC.Modernized.Compression.Eac.Exceptions;

namespace CnC.Modernized.Compression.Eac.Extensions;

public static class CodecExtensions
{
    public static byte[] DecodeBuffer(this Codec codec, ReadOnlySpan<byte> source)
    {
        int size;
        try
        {
            size = codec.GetDecodedSize(source);
        }
        catch (Exception exception)
        {
            throw new InvalidInputException("Failed to get the decoded size.", exception);
        }

        var result = new byte[size];
        int actualSize;
        try
        {
            actualSize = codec.Decode(result, source);
        }
        catch (Exception exception)
        {
            throw new InvalidInputException("Failed to decode the buffer.", exception);
        }

        if (actualSize != size)
        {
            Array.Resize(ref result, actualSize);
        }

        return result;
    }

    public static byte[] EncodeBuffer(
        this Codec codec,
        ReadOnlySpan<byte> source,
        ReadOnlySpan<uint> options = default
    )
    {
        var result = new byte[source.Length + 1024];
        int encodedSize;
        try
        {
            encodedSize = codec.Encode(result, source, options);
        }
        catch (Exception exception)
        {
            throw new InvalidInputException("Failed to encode the buffer.", exception);
        }

        Array.Resize(ref result, encodedSize);
        return result;
    }
}
