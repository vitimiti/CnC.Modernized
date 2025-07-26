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

internal static partial class SdlLogging
{
    [LoggerMessage(LogLevel.Trace, Message = "[{Category}] {Message}")]
    public static partial void Trace(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.Trace, Message = "[{Category}] {Message}")]
    public static partial void Verbose(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.Debug, Message = "[{Category}] {Message}")]
    public static partial void Debug(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.Information, Message = "[{Category}] {Message}")]
    public static partial void Information(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.Warning, Message = "[{Category}] {Message}")]
    public static partial void Warning(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.Error, Message = "[{Category}] {Message}")]
    public static partial void Error(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.Critical, Message = "[{Category}] {Message}")]
    public static partial void Critical(ILogger logger, string category, string message);

    [LoggerMessage(LogLevel.None, Message = "[{Category}] {Message}")]
    public static partial void Invalid(ILogger logger, string category, string message);
}
