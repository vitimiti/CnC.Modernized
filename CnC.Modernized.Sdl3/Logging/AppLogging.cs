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

namespace CnC.Modernized.Sdl3.Logging;

internal static partial class AppLogging
{
    [LoggerMessage(LogLevel.Critical, Message = "Unable to initialize SDL3 ({ErrorMessage}).")]
    public static partial void UnableToInitializeSdl3(ILogger logger, string errorMessage);

    [LoggerMessage(
        LogLevel.Error,
        Message = "Unable to set the app metadata {AppMetadata} ({ErrorMessage})."
    )]
    public static partial void UnableToSetAppMetadata(
        ILogger logger,
        string appMetadata,
        string errorMessage
    );

    [LoggerMessage(LogLevel.Debug, Message = "SDL3 initialized.")]
    public static partial void Sdl3Initialized(ILogger logger);

    [LoggerMessage(LogLevel.Debug, Message = "Application initialized.")]
    public static partial void ApplicationInitialized(ILogger logger);

    [LoggerMessage(LogLevel.Debug, Message = "SDL3 terminated.")]
    public static partial void Sdl3Terminated(ILogger logger);

    [LoggerMessage(LogLevel.Debug, Message = "Internal SDL3 log output handle freed.")]
    public static partial void InternalSdl3LogOutputHandleFreed(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "SDL3 logging initialized.")]
    public static partial void Sdl3LoggingInitialized(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Static logger object set for SDL3 logging use.")]
    public static partial void StaticLoggerObjectSet(ILogger logger);
}
