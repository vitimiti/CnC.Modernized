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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CnC.Modernized.NativeInterop.Sdl3.Imports;

[SuppressMessage(
    "ReSharper",
    "InconsistentNaming",
    Justification = "Respect the native SDL3 naming conventions."
)]
internal static partial class SDL3
{
    public record struct SDL_InitFlags(uint Value)
    {
        public static SDL_InitFlags operator |(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value | right.Value);

        public static SDL_InitFlags operator &(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value & right.Value);

        public static SDL_InitFlags operator ^(SDL_InitFlags left, SDL_InitFlags right) =>
            new(left.Value ^ right.Value);

        public static SDL_InitFlags operator ~(SDL_InitFlags value) => new(~value.Value);
    }

    public static SDL_InitFlags SDL_INIT_AUDIO => new(0x00000010U);
    public static SDL_InitFlags SDL_INIT_VIDEO => new(0x00000020U);
    public static SDL_InitFlags SDL_INIT_JOYSTICK => new(0x00000200U);
    public static SDL_InitFlags SDL_INIT_HAPTIC => new(0x00001000U);
    public static SDL_InitFlags SDL_INIT_GAMEPAD => new(0x00002000U);
    public static SDL_InitFlags SDL_INIT_EVENTS => new(0x00004000U);
    public static SDL_InitFlags SDL_INIT_SENSOR => new(0x00008000U);
    public static SDL_InitFlags SDL_INIT_CAMERA => new(0x00010000U);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_InitSubSystem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_InitSubSystem(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_QuitSubSystem))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_QuitSubSystem(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_WasInit))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial SDL_InitFlags SDL_WasInit(SDL_InitFlags flags);

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_Quit))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_Quit();

    [LibraryImport(
        nameof(SDL3),
        EntryPoint = nameof(SDL_SetAppMetadata),
        StringMarshalling = StringMarshalling.Utf8
    )]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetAppMetadata(
        string? appname,
        string? appversion,
        string? appidentifier
    );
}
