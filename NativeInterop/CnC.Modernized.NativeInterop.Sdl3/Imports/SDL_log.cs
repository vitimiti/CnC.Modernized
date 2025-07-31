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
using System.Runtime.InteropServices.Marshalling;

namespace CnC.Modernized.NativeInterop.Sdl3.Imports;

[SuppressMessage(
    "ReSharper",
    "InconsistentNaming",
    Justification = "Respect the native SDL3 naming conventions."
)]
internal static partial class SDL3
{
    public enum SDL_LogCategory;

    public static SDL_LogCategory SDL_LOG_CATEGORY_APPLICATION => 0;
    public static SDL_LogCategory SDL_LOG_CATEGORY_ERROR => (SDL_LogCategory)1;
    public static SDL_LogCategory SDL_LOG_CATEGORY_ASSERT => (SDL_LogCategory)2;
    public static SDL_LogCategory SDL_LOG_CATEGORY_SYSTEM => (SDL_LogCategory)3;
    public static SDL_LogCategory SDL_LOG_CATEGORY_AUDIO => (SDL_LogCategory)4;
    public static SDL_LogCategory SDL_LOG_CATEGORY_VIDEO => (SDL_LogCategory)5;
    public static SDL_LogCategory SDL_LOG_CATEGORY_RENDER => (SDL_LogCategory)6;
    public static SDL_LogCategory SDL_LOG_CATEGORY_INPUT => (SDL_LogCategory)7;
    public static SDL_LogCategory SDL_LOG_CATEGORY_TEST => (SDL_LogCategory)8;
    public static SDL_LogCategory SDL_LOG_CATEGORY_GPU => (SDL_LogCategory)9;

    public enum SDL_LogPriority;

    public static SDL_LogPriority SDL_LOG_PRIORITY_INVALID => 0;
    public static SDL_LogPriority SDL_LOG_PRIORITY_TRACE => (SDL_LogPriority)1;
    public static SDL_LogPriority SDL_LOG_PRIORITY_VERBOSE => (SDL_LogPriority)2;
    public static SDL_LogPriority SDL_LOG_PRIORITY_DEBUG => (SDL_LogPriority)3;
    public static SDL_LogPriority SDL_LOG_PRIORITY_INFO => (SDL_LogPriority)4;
    public static SDL_LogPriority SDL_LOG_PRIORITY_WARN => (SDL_LogPriority)5;
    public static SDL_LogPriority SDL_LOG_PRIORITY_ERROR => (SDL_LogPriority)6;
    public static SDL_LogPriority SDL_LOG_PRIORITY_CRITICAL => (SDL_LogPriority)7;

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SetLogPriorities))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void SDL_SetLogPriorities(SDL_LogPriority priority);

    public delegate void SDL_LogOutputFunction(
        SDL_LogCategory category,
        SDL_LogPriority priority,
        string message
    );

    private static readonly unsafe delegate* unmanaged[Cdecl]<
        nint,
        int,
        SDL_LogPriority,
        byte*,
        void> SDL_LogOutputFunctionPtr = &SDL_LogOutputFunctionImpl;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void SDL_LogOutputFunctionImpl(
        nint userdata,
        int category,
        SDL_LogPriority priority,
        byte* message
    )
    {
        if (userdata == IntPtr.Zero)
        {
            return;
        }

        var callback = (SDL_LogOutputFunction)GCHandle.FromIntPtr(userdata).Target!;
        callback(
            (SDL_LogCategory)category,
            priority,
            Utf8StringMarshaller.ConvertToManaged(message) ?? string.Empty
        );
    }

    [LibraryImport(nameof(SDL3), EntryPoint = nameof(SDL_SetLogOutputFunction))]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe partial void SDL_SetLogOutputFunctionActual(
        delegate* unmanaged[Cdecl]<nint, int, SDL_LogPriority, byte*, void> callback,
        nint userdata
    );

    public static GCHandle SDL_LogOutputFunctionHandle { get; private set; }

    public static unsafe void SDL_SetLogOutputFunction(SDL_LogOutputFunction callback)
    {
        var userdata = GCHandle.Alloc(callback);
        SDL_LogOutputFunctionHandle = userdata;
        SDL_SetLogOutputFunctionActual(SDL_LogOutputFunctionPtr, GCHandle.ToIntPtr(userdata));
    }
}
