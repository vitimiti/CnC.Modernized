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

using Microsoft.Extensions.Logging;

namespace CnC.Modernized.NativeInterop.Sdl3.Logging;

internal static partial class SubsystemsLogging
{
    [LoggerMessage(LogLevel.Error, Message = "Unable to initialize the {Subsystem} ({Error}).")]
    public static partial void UnableToInitializeSubsystem(
        ILogger logger,
        string subsystem,
        string error
    );

    [LoggerMessage(LogLevel.Debug, Message = "{Subsystem} initialized.")]
    public static partial void SubsystemInitialized(ILogger logger, string subsystem);

    [LoggerMessage(LogLevel.Debug, Message = "{Subsystem} terminated.")]
    public static partial void SubsystemTerminated(ILogger logger, string subsystem);

    [LoggerMessage(
        LogLevel.Warning,
        Message = "Events pump already initialized, only one instance can be active at a time."
    )]
    public static partial void EventsPumpAlreadyInitialized(ILogger logger);
}
