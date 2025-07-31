using System.Diagnostics.CodeAnalysis;
using CnC.Modernized.NativeInterop.Sdl3.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static CnC.Modernized.NativeInterop.Sdl3.Imports.SDL3;

namespace CnC.Modernized.NativeInterop.Sdl3.Subsystems;

[PublicAPI]
public class EventsSubsystem : IDisposable
{
    private static bool IsInitialized { get; set; }

    private readonly ILogger _logger;

    [SuppressMessage(
        "csharpsquid",
        "S4487:Unread \"private\" fields should be removed",
        Justification = "This is here to prevent improper initialization of the Audio subsystem."
    )]
    private readonly App _app;

    internal EventsSubsystem(ILogger logger, App app)
    {
        _logger = logger;
        _app = app;

        if (IsInitialized)
        {
            SubsystemsLogging.EventsPumpAlreadyInitialized(_logger);
            return;
        }

        if (!SDL_InitSubSystem(SDL_INIT_EVENTS))
        {
            SubsystemsLogging.UnableToInitializeSubsystem(
                _logger,
                nameof(EventsSubsystem),
                SDL_GetError()
            );
        }

        SetIsInitialized(true);
        SubsystemsLogging.SubsystemInitialized(_logger, nameof(EventsSubsystem));
    }

    private void ReleaseUnmanagedResources()
    {
        SDL_QuitSubSystem(SDL_INIT_EVENTS);
        SubsystemsLogging.SubsystemTerminated(_logger, nameof(EventsSubsystem));
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

    ~EventsSubsystem() => Dispose(disposing: false);

    private static void SetIsInitialized(bool isInitialized) => IsInitialized = isInitialized;
}
