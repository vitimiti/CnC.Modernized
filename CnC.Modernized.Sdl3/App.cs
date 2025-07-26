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

using CnC.Modernized.Sdl3.Logging;
using CnC.Modernized.Sdl3.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static CnC.Modernized.Sdl3.Imports.SDL3;

namespace CnC.Modernized.Sdl3;

[PublicAPI]
public class App : IDisposable
{
    private static ILogger? Logger { get; set; }

    private readonly AppOptions _options = new();
    private readonly ILogger _logger;

    public App(ILogger logger, Action<AppOptions>? options = null)
    {
        _logger = logger;
        options?.Invoke(_options);

        SetLogger(logger);
        AppLogging.StaticLoggerObjectSet(_logger);

        Initialize();

        AppLogging.ApplicationInitialized(_logger);
    }

    private void ReleaseUnmanagedResources()
    {
        SDL_Quit();
        AppLogging.Sdl3Terminated(_logger);

        if (!SDL_LogOutputFunctionHandle.IsAllocated)
        {
            return;
        }

        SDL_LogOutputFunctionHandle.Free();
        AppLogging.InternalSdl3LogOutputHandleFreed(_logger);
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            // Nothing to release here for now.
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~App() => Dispose(disposing: false);

    private static void SetLogger(ILogger logger) => Logger = logger;

    private static string SdlCategoryToString(SDL_LogCategory category)
    {
        string categoryStr = "<Invalid>";
        if (category == SDL_LOG_CATEGORY_APPLICATION)
        {
            categoryStr = "Application";
        }
        else if (category == SDL_LOG_CATEGORY_ERROR)
        {
            categoryStr = "Error";
        }
        else if (category == SDL_LOG_CATEGORY_ASSERT)
        {
            categoryStr = "Assert";
        }
        else if (category == SDL_LOG_CATEGORY_SYSTEM)
        {
            categoryStr = "System";
        }
        else if (category == SDL_LOG_CATEGORY_AUDIO)
        {
            categoryStr = "Audio";
        }
        else if (category == SDL_LOG_CATEGORY_VIDEO)
        {
            categoryStr = "Video";
        }
        else if (category == SDL_LOG_CATEGORY_RENDER)
        {
            categoryStr = "Render";
        }
        else if (category == SDL_LOG_CATEGORY_INPUT)
        {
            categoryStr = "Input";
        }
        else if (category == SDL_LOG_CATEGORY_TEST)
        {
            categoryStr = "Test";
        }
        else if (category == SDL_LOG_CATEGORY_GPU)
        {
            categoryStr = "GPU";
        }

        return categoryStr;
    }

    private static void DoSdlLog(string categoryStr, SDL_LogPriority priority, string message)
    {
        if (priority == SDL_LOG_PRIORITY_TRACE)
        {
            SdlLogging.Trace(Logger!, categoryStr, message);
        }
        else if (priority == SDL_LOG_PRIORITY_VERBOSE)
        {
            SdlLogging.Verbose(Logger!, categoryStr, message);
        }
        else if (priority == SDL_LOG_PRIORITY_DEBUG)
        {
            SdlLogging.Debug(Logger!, categoryStr, message);
        }
        else if (priority == SDL_LOG_PRIORITY_INFO)
        {
            SdlLogging.Information(Logger!, categoryStr, message);
        }
        else if (priority == SDL_LOG_PRIORITY_WARN)
        {
            SdlLogging.Warning(Logger!, categoryStr, message);
        }
        else if (priority == SDL_LOG_PRIORITY_ERROR)
        {
            SdlLogging.Error(Logger!, categoryStr, message);
        }
        else if (priority == SDL_LOG_PRIORITY_CRITICAL)
        {
            SdlLogging.Critical(Logger!, categoryStr, message);
        }
        else
        {
            SdlLogging.Invalid(Logger!, categoryStr, message);
        }
    }

    private static void LogOutputFunction(
        SDL_LogCategory category,
        SDL_LogPriority priority,
        string message
    ) => DoSdlLog(SdlCategoryToString(category), priority, message);

    private static void InitializeLogging()
    {
#if DEBUG
        SDL_SetLogPriorities(SDL_LOG_PRIORITY_DEBUG);
#endif
        SDL_SetLogOutputFunction(LogOutputFunction);
    }

    private void InitializeSdl()
    {
        InitializeLogging();
        AppLogging.Sdl3LoggingInitialized(_logger);

        if (
            !SDL_SetAppMetadata(
                _options.AppName,
                _options.AppVersion?.ToString(),
                _options.AppIdentifier
            )
        )
        {
            AppLogging.UnableToSetAppMetadata(_logger, _options.ToString(), SDL_GetError());
        }

        if (!SDL_InitSubSystem(new SDL_InitFlags(0)))
        {
            AppLogging.UnableToInitializeSdl3(_logger, SDL_GetError());
        }
    }

    private void Initialize()
    {
        InitializeSdl();
        AppLogging.Sdl3Initialized(_logger);
    }
}
