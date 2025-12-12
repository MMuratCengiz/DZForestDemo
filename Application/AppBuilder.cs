using DenOfIz;

namespace Application;

/// <summary>
/// Fluent builder for creating and configuring applications.
/// </summary>
public sealed class AppBuilder
{
    private readonly ApplicationOptions _options = new();
    private readonly List<(ISubsystem Subsystem, int Priority)> _pendingSubsystems = new();
    private readonly List<(Func<App, ISubsystem> Factory, int Priority)> _pendingFactories = new();

    /// <summary>
    /// Creates a new application builder.
    /// </summary>
    public static AppBuilder Create() => new();

    /// <summary>
    /// Sets the window title.
    /// </summary>
    public AppBuilder WithTitle(string title)
    {
        _options.Title = title;
        return this;
    }

    /// <summary>
    /// Sets the window size.
    /// </summary>
    public AppBuilder WithSize(uint width, uint height)
    {
        _options.Width = width;
        _options.Height = height;
        return this;
    }

    /// <summary>
    /// Sets the number of back buffers.
    /// </summary>
    public AppBuilder WithNumFrames(uint numFrames)
    {
        _options.NumFrames = numFrames;
        return this;
    }

    /// <summary>
    /// Sets the fixed update rate in Hz. Set to 0 to disable fixed updates.
    /// </summary>
    public AppBuilder WithFixedUpdateRate(double hz)
    {
        _options.FixedUpdateRate = hz;
        return this;
    }

    /// <summary>
    /// Sets the back buffer format.
    /// </summary>
    public AppBuilder WithBackBufferFormat(Format format)
    {
        _options.BackBufferFormat = format;
        return this;
    }

    /// <summary>
    /// Sets the depth buffer format.
    /// </summary>
    public AppBuilder WithDepthBufferFormat(Format format)
    {
        _options.DepthBufferFormat = format;
        return this;
    }

    /// <summary>
    /// Enables or disables tearing for variable refresh rate.
    /// </summary>
    public AppBuilder WithTearing(bool allow)
    {
        _options.AllowTearing = allow;
        return this;
    }

    /// <summary>
    /// Sets the preferred graphics API.
    /// </summary>
    public AppBuilder WithApiPreference(APIPreference preference)
    {
        _options.ApiPreference = preference;
        return this;
    }

    /// <summary>
    /// Adds a subsystem to be registered when the application is built.
    /// </summary>
    /// <param name="subsystem">The subsystem instance.</param>
    /// <param name="eventPriority">Event priority if subsystem implements IEventReceiver.</param>
    public AppBuilder AddSubsystem(ISubsystem subsystem, int eventPriority = 0)
    {
        _pendingSubsystems.Add((subsystem, eventPriority));
        return this;
    }

    /// <summary>
    /// Adds a subsystem factory that creates the subsystem with access to the App instance.
    /// Use this when your subsystem needs a reference to the App.
    /// </summary>
    /// <param name="factory">Factory function that receives the App and returns a subsystem.</param>
    /// <param name="eventPriority">Event priority if subsystem implements IEventReceiver.</param>
    public AppBuilder AddSubsystem(Func<App, ISubsystem> factory, int eventPriority = 0)
    {
        _pendingFactories.Add((factory, eventPriority));
        return this;
    }

    /// <summary>
    /// Builds the application with the configured settings.
    /// </summary>
    public App Build()
    {
        var app = new App(_options);

        foreach (var (subsystem, priority) in _pendingSubsystems)
        {
            app.AddSubsystem(subsystem, priority);
        }

        foreach (var (factory, priority) in _pendingFactories)
        {
            app.AddSubsystem(factory(app), priority);
        }

        return app;
    }

    /// <summary>
    /// Builds and runs the application. Blocks until the application exits.
    /// </summary>
    public void Run()
    {
        using var app = Build();
        app.Run();
    }
}
