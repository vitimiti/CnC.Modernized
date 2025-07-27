using System.Diagnostics.CodeAnalysis;
using CnC.Modernized.Sdl3.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static CnC.Modernized.Sdl3.Imports.SDL3;

namespace CnC.Modernized.Sdl3.Subsystems;

[PublicAPI]
public class CameraSubsystem : IDisposable
{
    private readonly ILogger _logger;

    [SuppressMessage(
        "csharpsquid",
        "S4487:Unread \"private\" fields should be removed",
        Justification = "This is here to prevent improper initialization of the Audio subsystem."
    )]
    private readonly App _app;

    internal CameraSubsystem(ILogger logger, App app)
    {
        _logger = logger;
        _app = app;

        if (!SDL_InitSubSystem(SDL_INIT_CAMERA))
        {
            SubsystemsLogging.UnableToInitializeSubsystem(
                _logger,
                nameof(CameraSubsystem),
                SDL_GetError()
            );
        }

        SubsystemsLogging.SubsystemInitialized(_logger, nameof(CameraSubsystem));
    }

    private void ReleaseUnmanagedResources()
    {
        SDL_QuitSubSystem(SDL_INIT_CAMERA);
        SubsystemsLogging.SubsystemTerminated(_logger, nameof(CameraSubsystem));
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

    ~CameraSubsystem() => Dispose(disposing: false);
}
