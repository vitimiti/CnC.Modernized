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
using JetBrains.Annotations;

namespace CnC.Modernized.Compression.Eac;

[PublicAPI]
public class Gimex
{
    private readonly List<IImageFormat> _formats = [];

    public void RegisterFormat(IImageFormat format) => _formats.Add(format);

    public async ValueTask<ImageInstance> ReadImageAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        await using var stream = new ImageStream(path);
        foreach (var format in _formats)
        {
            var isValid = await format.CheckFormatAsync(stream, cancellationToken);
            if (!isValid)
            {
                continue;
            }

            return await format.ReadImageAsync(stream, cancellationToken);
        }

        throw new InvalidFormatException($"Format found at \"{path}\" is invalid.");
    }

    public async ValueTask WriteImageAsync(
        ImageInstance instance,
        string path,
        CancellationToken cancellationToken = default
    )
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        var format = _formats.FirstOrDefault(file =>
            file.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
        );

        if (format is null)
        {
            throw new UnsupportedOperationException(
                $"Unable to write image to \"{path}\". No format found."
            );
        }

        await using var stream = new ImageStream(path, writeMode: true);
        await format.WriteImageAsync(instance, stream, cancellationToken);
    }
}
