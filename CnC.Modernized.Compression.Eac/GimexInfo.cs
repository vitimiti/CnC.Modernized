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
public class GimexInfo(string signature)
{
    private const int ArgbSize = 256;
    private const int FrameNameSize = 512;
    private const int CommentSize = 1024;
    private const int HotspotTableSize = 1024;

    private float _lossyPackingQuality;
    private string _frameName = string.Empty;
    private string _comment = string.Empty;

    public static Version InfoVersion => new(3, 0, 0);

    public int Signature { get; } = signature.GetGimexSignature();
    public int CurrentFrame { get; set; }
    public Size BitmapSize { get; set; }
    public int BitsPerPixel { get; set; }
    public int OriginalBitsPerPixel { get; set; }
    public int StartColor { get; set; }
    public IReadOnlyCollection<Argb> ColorTable { get; set; } = new Argb[ArgbSize];
    public int Subtype { get; set; }
    public int PackedLevel { get; set; }

    public float LossyPackingQuality
    {
        get => _lossyPackingQuality;
        set => _lossyPackingQuality = float.Clamp(value, 0F, 1F);
    }

    public int FrameSizeInBytes { get; set; }
    public ChannelBits ColorBits { get; set; } = new(0, 0, 0, 0);
    public Point Center { get; set; }
    public Point Default { get; set; }

    public string FrameName
    {
        get => _frameName;
        set => _frameName = value.Length > FrameNameSize ? value[..FrameNameSize] : value;
    }

    public string Comment
    {
        get => _comment;
        set => _comment = value.Length > CommentSize ? value[..CommentSize] : value;
    }

    public IReadOnlyCollection<Point> HotspotTable { get; set; } = new Point[HotspotTableSize];
    public float Dpi { get; set; }
    public float Fps { get; set; }
}
