using System.Diagnostics.CodeAnalysis;
using CnC.Modernized.Sdl3.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static CnC.Modernized.Sdl3.Imports.SDL3;

namespace CnC.Modernized.Sdl3.Subsystems;

[PublicAPI]
public class VideoSubsystem : IDisposable
{
    private readonly ILogger _logger;

    [SuppressMessage(
        "csharpsquid",
        "S4487:Unread \"private\" fields should be removed",
        Justification = "This is here to prevent improper initialization of the Audio subsystem."
    )]
    private readonly App _app;

    internal VideoSubsystem(ILogger logger, App app)
    {
        _logger = logger;
        _app = app;

        if (!SDL_InitSubSystem(SDL_INIT_VIDEO))
        {
            SubsystemsLogging.UnableToInitializeSubsystem(
                _logger,
                nameof(VideoSubsystem),
                SDL_GetError()
            );
        }

        SubsystemsLogging.SubsystemInitialized(_logger, nameof(VideoSubsystem));
    }

    private void ReleaseUnmanagedResources()
    {
        SDL_QuitSubSystem(SDL_INIT_VIDEO);
        SubsystemsLogging.SubsystemTerminated(_logger, nameof(VideoSubsystem));
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~VideoSubsystem() => Dispose(disposing: false);
}
