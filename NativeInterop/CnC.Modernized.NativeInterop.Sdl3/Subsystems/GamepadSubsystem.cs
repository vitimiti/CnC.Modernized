using System.Diagnostics.CodeAnalysis;
using CnC.Modernized.NativeInterop.Sdl3.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static CnC.Modernized.NativeInterop.Sdl3.Imports.SDL3;

namespace CnC.Modernized.NativeInterop.Sdl3.Subsystems;

[PublicAPI]
public class GamepadSubsystem : IDisposable
{
    private readonly ILogger _logger;

    [SuppressMessage(
        "csharpsquid",
        "S4487:Unread \"private\" fields should be removed",
        Justification = "This is here to prevent improper initialization of the Audio subsystem."
    )]
    private readonly App _app;

    internal GamepadSubsystem(ILogger logger, App app)
    {
        _logger = logger;
        _app = app;

        if (!SDL_InitSubSystem(SDL_INIT_GAMEPAD))
        {
            SubsystemsLogging.UnableToInitializeSubsystem(
                _logger,
                nameof(GamepadSubsystem),
                SDL_GetError()
            );
        }

        SubsystemsLogging.SubsystemInitialized(_logger, nameof(GamepadSubsystem));
    }

    private void ReleaseUnmanagedResources()
    {
        SDL_QuitSubSystem(SDL_INIT_GAMEPAD);
        SubsystemsLogging.SubsystemTerminated(_logger, nameof(GamepadSubsystem));
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

    ~GamepadSubsystem() => Dispose(disposing: false);
}
