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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CnC.Modernized.NativeInterop.Sdl3.Imports;

[SuppressMessage(
    "csharpsquid",
    "S101:Types should be named in PascalCase",
    Justification = "Respect the native SDL3 naming conventions."
)]
[SuppressMessage(
    "csharpsquid",
    "S2342:Enumeration types should comply with a naming convention",
    Justification = "Respect the native SDL3 naming conventions."
)]
[SuppressMessage(
    "ReSharper",
    "InconsistentNaming",
    Justification = "Respect the native SDL3 naming conventions."
)]
internal static partial class SDL3
{
    private static KeyValuePair<string, bool>[] LibraryNames =>
        [
            new("SDL3.dll", OperatingSystem.IsWindows()),
            new("libSDL3.so", OperatingSystem.IsLinux()),
        ];

    static SDL3() =>
        NativeLibrary.SetDllImportResolver(
            typeof(SDL3).Assembly,
            (name, assembly, path) =>
                NativeLibrary.Load(
                    name switch
                    {
                        nameof(SDL3) => LibraryNames.FirstOrDefault(pair => pair.Value).Key ?? name,
                        _ => name,
                    },
                    assembly,
                    path
                )
        );
}
