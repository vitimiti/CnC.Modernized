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

using System.Drawing;
using CnC.Modernized.Compression.Eac.Extensions;
using JetBrains.Annotations;

namespace CnC.Modernized.Compression.Eac;

[PublicAPI]
public class ImageInfo(string signature)
{
    private float _quality;

    public int Signature { get; init; } = signature.GetGimexSignature();
    public Size Size { get; init; }
    public uint BitsPerPixel { get; init; }
    public uint OriginalBpp { get; init; }
    public required IReadOnlyList<Argb> ColorPalette { get; init; }
    public required string FrameName { get; init; }
    public required string Comment { get; init; }
    public uint FrameNumber { get; init; }

    public float Quality
    {
        get => _quality;
        init => _quality = float.Clamp(value, 0F, 1F);
    }

    public ChannelBits ChannelBits { get; init; }
    public Point Center { get; init; }
    public required IReadOnlyList<Point> Hotspots { get; init; }
    public float Dpi { get; init; }
    public float Fps { get; init; }

    public override string ToString() =>
        $"{nameof(ImageInfo)} {{ {nameof(Signature)} = {Signature}, {nameof(Size)} = {Size}, {nameof(BitsPerPixel)} = {BitsPerPixel}, {nameof(OriginalBpp)} = {OriginalBpp}, {nameof(ColorPalette)} = {ColorPalette}, {nameof(FrameName)} = {FrameName}, {nameof(Comment)} = {Comment}, {nameof(FrameNumber)} = {FrameNumber}, {nameof(Quality)} = {Quality}, {nameof(ChannelBits)} = {ChannelBits}, {nameof(Center)} = {Center}, {nameof(Hotspots)} = {Hotspots}, {nameof(Dpi)} = {Dpi}, {nameof(Fps)} = {Fps} }}";
}
