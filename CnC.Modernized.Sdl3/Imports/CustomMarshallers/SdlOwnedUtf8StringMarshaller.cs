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

using System.Runtime.InteropServices.Marshalling;
using static CnC.Modernized.Sdl3.Imports.SDL3;

namespace CnC.Modernized.Sdl3.Imports.CustomMarshallers;

[CustomMarshaller(typeof(string), MarshalMode.ManagedToUnmanagedOut, typeof(ManagedToUnmanagedOut))]
internal static class SdlOwnedUtf8StringMarshaller
{
    public unsafe ref struct ManagedToUnmanagedOut
    {
        private byte* _unmanaged;
        private string? _managed;

        public void FromUnmanaged(byte* unmanaged)
        {
            if (unmanaged is null)
            {
                _unmanaged = null;
                _managed = null;
                return;
            }

            _unmanaged = SDL_strdup(unmanaged);
            _managed = Utf8StringMarshaller.ConvertToManaged(_unmanaged);
        }

        public string? ToManaged() => _managed;

        public void Free()
        {
            if (_unmanaged is not null)
            {
                SDL_free(_unmanaged);
            }
        }
    }
}
